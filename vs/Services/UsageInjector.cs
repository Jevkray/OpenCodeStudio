using System;
using System.Threading.Tasks;
using Microsoft.Web.WebView2.Core;
using Newtonsoft.Json;

namespace OpenCodeStudio.Services
{
    /// <summary>
    /// Встраивает в веб-UI OpenCode полоску расхода/лимитов и пушит в неё данные.
    /// </summary>
    internal static class UsageInjector
    {
        public static async Task InstallAsync(CoreWebView2 core)
        {
            await core.AddScriptToExecuteOnDocumentCreatedAsync(Script);
        }

        public static void Push(CoreWebView2 core, string json)
        {
            try
            {
                core.PostWebMessageAsJson(json);
            }
            catch (Exception ex)
            {
                Log.Error("Usage push failed", ex);
            }
        }

        /// <summary>JSON для инъекции: todayCost, showLimits и лимиты Go.</summary>
        public static string BuildPayload(SpendSnapshot s, bool showLimits)
        {
            var limits = s?.Limits;
            return JsonConvert.SerializeObject(new
            {
                todayCost = s?.TodayCost ?? 0,
                showLimits,
                limits = new
                {
                    hasGo = limits?.HasGo ?? false,
                    fivePct = limits?.FivePct ?? 0,
                    weekPct = limits?.WeekPct ?? 0,
                    monthPct = limits?.MonthPct ?? 0
                }
            });
        }

        public const string Script = @"(function(){
  if (window.__ocstudioInstalled) return; window.__ocstudioInstalled = true;
  function ensure(){
    var el = document.getElementById('ocstudio-usage');
    if (el) return el;
    el = document.createElement('div');
    el.id = 'ocstudio-usage';
    el.style.cssText = 'display:inline-flex;align-items:center;gap:8px;font-size:12px;padding:2px 8px;border-radius:6px;background:rgba(124,58,237,.12);border:1px solid rgba(124,58,237,.45);color:inherit;white-space:nowrap;margin-right:6px;';
    var t = document.createElement('span'); t.id='ocstudio-usage-text';
    var b = document.createElement('button'); b.id='ocstudio-usage-btn'; b.textContent='Использование';
    b.style.cssText='cursor:pointer;border:0;background:transparent;color:#a78bfa;font-size:12px;padding:0;';
    b.addEventListener('click', function(){ try{ window.chrome.webview.postMessage('open-usage'); }catch(e){} });
    el.appendChild(t); el.appendChild(b);
    var anchor = document.querySelector('[data-slot=""context-tool-group-item""]')
              || document.querySelector('[data-slot=""context-tool-group-summary""]');
    if (anchor && anchor.parentElement) anchor.parentElement.insertBefore(el, anchor);
    else { el.style.position='fixed'; el.style.left='8px'; el.style.bottom='8px'; el.style.zIndex='99999'; document.body.appendChild(el); }
    return el;
  }
  function render(d){
    try{
      var el = ensure(); var t = document.getElementById('ocstudio-usage-text'); if(!t) return;
      var parts=[];
      if(d && d.showLimits && d.limits && d.limits.hasGo){
        parts.push('д. ' + Math.round(100-(d.limits.fivePct||0)) + '%');
        parts.push('н. ' + Math.round(100-(d.limits.weekPct||0)) + '%');
        parts.push('м. ' + Math.round(100-(d.limits.monthPct||0)) + '%');
      }
      parts.push('Сегодня потрачено: ' + (Number(d&&d.todayCost)||0).toFixed(4) + '$');
      t.textContent = parts.join(' | ');
    }catch(e){}
  }
  if (window.chrome && window.chrome.webview) window.chrome.webview.addEventListener('message', function(e){ render(e.data); });
  setInterval(ensure, 2000);
})();";
    }
}
