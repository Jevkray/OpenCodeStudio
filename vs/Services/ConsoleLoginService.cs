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
        /// Возврат в приложение и автооткрытие поповера выполняет вызывающая сторона.
        /// </summary>
        public static async Task<bool> WaitForLoginAsync(CoreWebView2 core, CancellationToken ct)
        {
            Log.Info("Console login: navigating to " + LoginUrl);
            core.Navigate(LoginUrl);

            var deadline = DateTime.UtcNow.AddMinutes(5);
            var iteration = 0;
            try
            {
                while (DateTime.UtcNow < deadline)
                {
                    ct.ThrowIfCancellationRequested();
                    iteration++;

                    var url = core.Source;
                    if (iteration % 4 == 1)
                        Log.Info($"Console login: url={url}");

                    var cookie = await ReadCookieAsync(core);
                    if (!string.IsNullOrEmpty(cookie))
                    {
                        // Проверяем cookie на живой сессии, иначе не сохраняем мусор.
                        var c = new SpendClient(cookie);
                        if (await c.ResolveOrgAsync(ct))
                        {
                            ConsoleSessionStore.Save(cookie);
                            Log.Info("Console login: session cookie captured");
                            return true;
                        }
                        if (iteration % 4 == 1)
                            Log.Warn("Console login: cookie present but org check failed");
                    }

                    // Дополнительно определяем возврат в приложение по URL.
                    if (Uri.TryCreate(url, UriKind.Absolute, out var u) && u.Host == "opencode.ai"
                        && url.IndexOf("/console/login", StringComparison.OrdinalIgnoreCase) < 0
                        && url.IndexOf("/authorize", StringComparison.OrdinalIgnoreCase) < 0
                        && url.IndexOf("/auth/callback", StringComparison.OrdinalIgnoreCase) < 0
                        && url.IndexOf("/console/auth/", StringComparison.OrdinalIgnoreCase) < 0)
                    {
                        await Task.Delay(1000, ct);
                        var late = await ReadCookieAsync(core);
                        if (!string.IsNullOrEmpty(late))
                            ConsoleSessionStore.Save(late);
                        Log.Info("Console login: returned to opencode.ai");
                        return true;
                    }

                    await Task.Delay(1500, ct);
                }
            }
            catch (OperationCanceledException)
            {
                Log.Warn("Console login: timed out");
                return false;
            }

            Log.Warn("Console login: timed out");
            return false;
        }
    }
}
