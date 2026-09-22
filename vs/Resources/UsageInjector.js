(function(){
  if (window.__ocstudioToggle) return; window.__ocstudioToggle = true;

  function findContextButton(){
    var all=document.querySelectorAll('button,[role="button"]');
    for (var i=0;i<all.length;i++){
      var al=(all[i].getAttribute('aria-label')||'').toLowerCase();
      if (al.indexOf('context')>=0 || al.indexOf('контекст')>=0) return all[i];
    }
    var circles=document.querySelectorAll('svg circle');
    for (var j=0;j<circles.length;j++){ var b=circles[j].closest('button,[role="button"]'); if(b) return b; }
    return null;
  }

  function findAnchor(){
    var btn=findContextButton();
    if (!btn) return null;
    var anchor=btn;
    while (anchor.parentElement && anchor.parentElement!==document.body
           && anchor.parentElement.children.length===1){
      anchor=anchor.parentElement;
    }
    return anchor;
  }

  function ensureButton(){
    var el=document.getElementById('ocstudio-usage-btn');
    var anchor=findAnchor();
    if (el){
      if (anchor && anchor.parentElement && el.nextElementSibling!==anchor){
        anchor.parentElement.insertBefore(el, anchor);
        el.style.position=''; el.style.left=''; el.style.bottom=''; el.style.zIndex='';
      }
      return el;
    }
    el=document.createElement('button');
    el.id='ocstudio-usage-btn'; el.type='button'; el.tabIndex=-1;
    el.setAttribute('aria-label','Статистика OpenCode'); el.title='Статистика OpenCode';
    el.innerHTML='<svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round"><path d="M3 3v18h18"/><rect x="7" y="12" width="3" height="6"/><rect x="12" y="8" width="3" height="10"/><rect x="17" y="5" width="3" height="13"/></svg>';
    el.style.cssText='display:inline-flex;align-items:center;justify-content:center;height:28px;width:28px;padding:0;margin-right:4px;border:0;border-radius:8px;background:transparent;color:var(--v2-icon-icon-base,#cfcfcf);cursor:pointer;';
    el.addEventListener('mouseover',function(e){ e.stopPropagation(); });
    el.addEventListener('mouseenter',function(){ el.style.background='var(--v2-overlay-simple-overlay-hover,rgba(128,128,128,.15))'; });
    el.addEventListener('mouseleave',function(){ el.style.background='transparent'; });
    el.addEventListener('click',function(e){
      e.preventDefault(); e.stopPropagation();
      try{ window.chrome.webview.postMessage('toggle-console'); }catch(err){}
    });
    if (anchor && anchor.parentElement) anchor.parentElement.insertBefore(el, anchor);
    else { el.style.position='fixed'; el.style.left='8px'; el.style.bottom='8px'; el.style.zIndex='99999'; document.body.appendChild(el); }
    return el;
  }

  setInterval(ensureButton, 1500);
  ensureButton();
})();
