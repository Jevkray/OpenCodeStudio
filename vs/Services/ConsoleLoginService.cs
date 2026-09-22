using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Web.WebView2.Core;

namespace OpenCodeStudio.Services
{
    /// <summary>
    /// Захватывает сессионную cookie opencode.ai через WebView2 после входа
    /// пользователя в консоль. Пока не подключён к UI.
    /// </summary>
    internal static class ConsoleLoginService
    {
        public const string Home = "https://opencode.ai";
        public const string LoginUrl = "https://opencode.ai/console/login";

        private static readonly string[] CallbackMarkers =
        {
            "/console/login", "/authorize", "/auth/callback", "/console/auth/"
        };

        /// <summary>Собирает cookie для домашнего домена в строку "name=value; ...".</summary>
        public static async Task<string> ReadCookieAsync(CoreWebView2 core)
        {
            var cookies = await core.CookieManager.GetCookiesAsync(Home);
            var parts = new List<string>();
            foreach (var c in cookies)
                parts.Add(c.Name + "=" + c.Value);
            return parts.Count == 0 ? null : string.Join("; ", parts);
        }

        /// <summary>
        /// Открывает страницу входа и ждёт возврат на opencode.ai до 5 минут.
        /// Сохраняет cookie при успехе. По таймауту/отмене возвращает null.
        /// </summary>
        public static async Task<string> WaitForLoginAsync(CoreWebView2 core, CancellationToken ct)
        {
            core.Navigate(LoginUrl);

            var deadline = DateTime.UtcNow.AddMinutes(5);
            try
            {
                while (DateTime.UtcNow < deadline)
                {
                    ct.ThrowIfCancellationRequested();

                    var url = core.Source ?? "";
                    bool onHome = Uri.TryCreate(url, UriKind.Absolute, out var u)
                        && string.Equals(u.Host, "opencode.ai", StringComparison.OrdinalIgnoreCase);
                    bool isCallback = false;
                    foreach (var marker in CallbackMarkers)
                    {
                        if (url.IndexOf(marker, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            isCallback = true;
                            break;
                        }
                    }

                    if (onHome && !isCallback)
                    {
                        await Task.Delay(1000, ct);
                        var cookie = await ReadCookieAsync(core);
                        if (!string.IsNullOrEmpty(cookie))
                        {
                            // Проверяем cookie на живой сессии, иначе не сохраняем мусор.
                            var c = new SpendClient(cookie);
                            if (await c.ResolveOrgAsync(ct))
                            {
                                ConsoleSessionStore.Save(cookie);
                                return cookie;
                            }
                        }
                    }

                    await Task.Delay(1500, ct);
                }
            }
            catch (OperationCanceledException)
            {
                return null;
            }

            return null;
        }
    }
}
