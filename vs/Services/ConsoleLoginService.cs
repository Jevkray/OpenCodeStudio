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
        /// Открывает страницу входа и ждёт валидную сессию до 5 минут.
        /// Определение по валидной cookie (а не по URL) — работает и для
        /// редиректного, и для popup-сценария. По таймауту/отмене — null.
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
