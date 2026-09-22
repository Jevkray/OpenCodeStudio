(function(){
  if (window.__ocstudioV3) return; window.__ocstudioV3 = true;
  var STATE = { state:'login', todayCost:0, showLimits:true, limits:null, rows:[] };
  var isOpen = false, MONTH = null;
  var PALETTE = ['#7C3AED','#22d3ee','#f59e0b','#ef4444','#22c55e','#ec4899','#8b5cf6','#14b8a6','#eab308','#64748b'];

  function tk(n, fb){ try{ var x=getComputedStyle(document.documentElement).getPropertyValue(n); return (x&&x.trim())||fb; }catch(e){ return fb; } }
  function esc(s){ return String(s==null?'':s).replace(/[&<>"]/g,function(c){return {'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;'}[c];}); }
  function usd(v){ return (Number(v)||0).toFixed(4)+'$'; }
  function monthOf(iso){ if(!iso) return null; var d=new Date(iso); if(isNaN(d.getTime())) return null; return d.getFullYear()+'-'+String(d.getMonth()+1).padStart(2,'0'); }
  function dayOf(iso){ if(!iso) return null; var d=new Date(iso); if(isNaN(d.getTime())) return null; return d.getDate(); }
  function months(){ var s={}; (STATE.rows||[]).forEach(function(r){ var m=monthOf(r.createdAt); if(m) s[m]=1; }); return Object.keys(s).sort().reverse(); }

  function ensureStyle(){
    if (document.getElementById('ocstudio-style')) return;
    var st=document.createElement('style'); st.id='ocstudio-style';
    st.textContent='@keyframes ocspin{to{transform:rotate(360deg)}} .ocspin{border:2px solid rgba(128,128,128,.35);border-top-color:currentColor;border-radius:50%;width:14px;height:14px;display:inline-block;animation:ocspin .8s linear infinite}';
    document.head.appendChild(st);
  }

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

  function ensureButton(){
    ensureStyle();
    var el=document.getElementById('ocstudio-usage-btn');
    var anchor=findContextButton();
    if (el){
      if (anchor && anchor.parentElement && el.nextElementSibling!==anchor){
        anchor.parentElement.insertBefore(el, anchor);
        el.style.position=''; el.style.left=''; el.style.bottom=''; el.style.zIndex='';
      }
      return el;
    }
    el=document.createElement('button');
    el.id='ocstudio-usage-btn'; el.type='button';
    el.setAttribute('aria-label','Использование'); el.title='Использование';
    el.innerHTML='<svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round"><path d="M3 3v18h18"/><rect x="7" y="12" width="3" height="6"/><rect x="12" y="8" width="3" height="10"/><rect x="17" y="5" width="3" height="13"/></svg>';
    el.style.cssText='display:inline-flex;align-items:center;justify-content:center;height:28px;width:28px;padding:0;margin-right:4px;border:0;border-radius:8px;background:transparent;color:var(--v2-icon-icon-base,#cfcfcf);cursor:pointer;';
    el.addEventListener('mouseenter',function(){ el.style.background='var(--v2-overlay-simple-overlay-hover,rgba(128,128,128,.15))'; });
    el.addEventListener('mouseleave',function(){ el.style.background='transparent'; });
    el.addEventListener('click',function(e){ e.preventDefault(); e.stopPropagation(); toggle(); });
    if (anchor && anchor.parentElement) anchor.parentElement.insertBefore(el, anchor);
    else { el.style.position='fixed'; el.style.left='8px'; el.style.bottom='8px'; el.style.zIndex='99999'; document.body.appendChild(el); }
    return el;
  }

  function ensurePopover(){
    ensureStyle();
    var p=document.getElementById('ocstudio-usage-pop');
    if (p) return p;
    p=document.createElement('div'); p.id='ocstudio-usage-pop';
    p.style.cssText='position:fixed;z-index:100000;display:none;width:420px;max-height:72vh;overflow:auto;padding:14px;border-radius:12px;border:1px solid var(--v2-border-border-base,rgba(128,128,128,.25));background:var(--v2-background-bg-layer-01,#252526);color:var(--v2-text-text-base,#f1f1f1);box-shadow:var(--v2-elevation-raised,0 8px 24px rgba(0,0,0,.4));font:13px/1.45 "Segoe UI",system-ui,sans-serif;';
    document.body.appendChild(p);
    return p;
  }

  function positionPopover(){
    var p=ensurePopover(), b=document.getElementById('ocstudio-usage-btn');
    if (!b) return;
    var r=b.getBoundingClientRect();
    var w=Math.min(420, Math.max(260, window.innerWidth-16));
    p.style.width=w+'px';
    var left=Math.max(8, Math.min(r.left, window.innerWidth-w-8));
    var maxH=Math.max(160, r.top-16);
    p.style.maxHeight=maxH+'px';
    p.style.left=left+'px';
    p.style.top=Math.max(8, r.top-8-maxH)+'px';
    p.style.bottom='auto';
  }

  function openPopover(){ isOpen=true; positionPopover(); render(); positionPopover(); }

  function toggle(){
    var p=ensurePopover();
    if (!isOpen){ openPopover(); } else { isOpen=false; p.style.display='none'; }
  }

  function loginHtml(muted){
    return '<div style="font-size:13px;font-weight:600;margin-bottom:6px">Использование OpenCode</div>'+
      '<div style="color:'+muted+';font-size:12px;margin-bottom:12px">Войдите в OpenCode Console, чтобы видеть расход и остаток лимитов подписки Go.</div>'+
      '<button id="ocstudio-login" style="cursor:pointer;border:1px solid var(--v2-border-border-strong,rgba(128,128,128,.35));background:var(--v2-overlay-simple-overlay-hover,rgba(128,128,128,.15));color:inherit;border-radius:8px;padding:8px 14px;font-size:12px">Войти в OpenCode Console</button>';
  }

  function limitsHtml(L, muted){
    var items=[['День',L.fivePct,L.fiveUsed,L.fiveLimit,L.fiveResetSec],['Неделя',L.weekPct,L.weekUsed,L.weekLimit,L.weekResetSec],['Месяц',L.monthPct,L.monthUsed,L.monthLimit,L.monthResetSec]];
    return items.map(function(it){
      var p=Number(it[1])||0, rem=Math.max(0,100-p), r=Number(it[4])||0;
      var col=rem<15?'#ef4444':rem<40?'#f59e0b':'#7C3AED';
      var rs=r>0?(' · сброс через '+Math.floor(r/3600)+'ч '+Math.floor((r%3600)/60)+'м'):'';
      return '<div style="margin-bottom:10px"><div style="display:flex;justify-content:space-between;font-size:12px;margin-bottom:4px"><span>'+it[0]+'</span><span style="font-weight:600">осталось '+Math.round(rem)+'%</span></div>'+
        '<div style="height:6px;background:var(--v2-overlay-simple-overlay-hover,rgba(128,128,128,.18));border-radius:4px;overflow:hidden"><i style="display:block;height:100%;width:'+Math.min(100,p)+'%;background:'+col+'"></i></div>'+
        '<div style="color:'+muted+';font-size:11px;margin-top:3px">'+usd(it[2])+' из '+usd(it[3])+rs+'</div></div>';
    }).join('');
  }

  function modelsHtml(muted){
    var map={};
    (STATE.rows||[]).filter(function(r){return monthOf(r.createdAt)===MONTH;}).forEach(function(r){ map[r.model]=(map[r.model]||0)+(Number(r.cost)||0); });
    var arr=Object.keys(map).map(function(k){return [k,map[k]];}).sort(function(a,b){return b[1]-a[1];});
    if (!arr.length) return '<div style="color:'+muted+';font-size:12px">Нет данных за '+MONTH+'</div>';
    var max=arr[0][1]||1;
    return arr.map(function(a){ return '<div style="margin:7px 0"><div style="display:flex;justify-content:space-between;font-size:12px"><span style="overflow:hidden;text-overflow:ellipsis;white-space:nowrap;max-width:300px">'+esc(a[0])+'</span><span style="color:'+muted+'">'+usd(a[1])+'</span></div>'+
      '<div style="height:6px;background:var(--v2-overlay-simple-overlay-hover,rgba(128,128,128,.18));border-radius:4px;overflow:hidden;margin-top:4px"><i style="display:block;height:100%;width:'+(a[1]/max*100)+'%;background:#7C3AED"></i></div></div>'; }).join('');
  }

  function histHtml(muted){
    var rs=(STATE.rows||[]).filter(function(r){return monthOf(r.createdAt)===MONTH;});
    if (!rs.length) return '<div style="color:'+muted+';font-size:12px">Нет данных за '+MONTH+'</div>';
    var parts=MONTH.split('-'), y=Number(parts[0]), mo=Number(parts[1]), days=new Date(y,mo,0).getDate();
    var perDay={}, colorMap={}, total={};
    rs.forEach(function(r){ var d=dayOf(r.createdAt); if(!d) return; perDay[d]=perDay[d]||{}; perDay[d][r.model]=(perDay[d][r.model]||0)+(Number(r.cost)||0); total[r.model]=(total[r.model]||0)+(Number(r.cost)||0); });
    var max=0; Object.keys(perDay).forEach(function(k){ var s=0; Object.keys(perDay[k]).forEach(function(m){s+=perDay[k][m];}); if(s>max)max=s; });
    var models=Object.keys(total).sort(function(a,b){return total[b]-total[a];});
    models.forEach(function(m,i){ colorMap[m]=PALETTE[i%PALETTE.length]; });
    var cols='';
    for (var d=1;d<=days;d++){
      var o=perDay[d]||{}, sum=0; Object.keys(o).forEach(function(m){sum+=o[m];});
      var h=max>0?sum/max*100:0, segs='';
      models.forEach(function(m){ var v=o[m]||0; if(v<=0)return; segs+='<div style="width:100%;height:'+(sum>0?h*v/sum:0)+'%;background:'+colorMap[m]+'" title="'+esc(m)+': '+usd(v)+'"></div>'; });
      cols+='<div style="flex:1 0 12px;display:flex;flex-direction:column-reverse;height:100%" title="'+MONTH+'-'+String(d).padStart(2,'0')+': '+usd(sum)+'">'+segs+'</div>';
    }
    var legend=models.map(function(m){ return '<span style="display:inline-flex;align-items:center;gap:5px"><i style="width:9px;height:9px;border-radius:2px;background:'+colorMap[m]+';display:inline-block"></i>'+esc(m)+' — '+usd(total[m])+'</span>'; }).join('');
    return '<div style="display:flex;align-items:flex-end;gap:2px;height:150px;overflow-x:auto">'+cols+'</div><div style="display:flex;flex-wrap:wrap;gap:9px;margin-top:10px;font-size:11px;color:'+muted+'">'+legend+'</div>';
  }

  function readyHtml(muted){
    var L=STATE.limits, hasGo=L&&L.hasGo&&STATE.showLimits;
    var ms=months(); if(!MONTH||ms.indexOf(MONTH)<0) MONTH=ms[0]||null;
    var h='<div style="display:flex;justify-content:space-between;align-items:center;margin-bottom:12px;gap:8px">';
    h+='<div style="font-size:13px;font-weight:600">Использование</div>';
    h+='<div style="display:flex;gap:8px;align-items:center"><span style="font-size:12px;color:'+muted+'">Сегодня: '+usd(STATE.todayCost)+'</span>';
    h+='<select id="ocstudio-month" style="background:transparent;color:inherit;border:1px solid var(--v2-border-border-base,rgba(128,128,128,.25));border-radius:6px;padding:3px 6px;font-size:11px">'+(ms.length?ms.map(function(m){return '<option'+(m===MONTH?' selected':'')+'>'+m+'</option>';}).join(''):'<option>—</option>')+'</select></div></div>';
    if (hasGo){ h+='<div style="font-size:11px;text-transform:uppercase;letter-spacing:.04em;color:'+muted+';margin:4px 0 8px">Лимиты подписки</div>'+limitsHtml(L,muted); }
    h+='<div style="font-size:11px;text-transform:uppercase;letter-spacing:.04em;color:'+muted+';margin:14px 0 8px">Расход по моделям</div>'+modelsHtml(muted);
    h+='<div style="font-size:11px;text-transform:uppercase;letter-spacing:.04em;color:'+muted+';margin:14px 0 8px">По дням месяца</div>'+histHtml(muted);
    return h;
  }

  function bind(p){
    var login=p.querySelector('#ocstudio-login');
    if (login) login.addEventListener('click', function(){ try{ window.chrome.webview.postMessage('login'); }catch(e){} p.innerHTML='<div style="display:flex;align-items:center;gap:10px;color:var(--v2-text-text-muted,#9a9a9a)"><span class="ocspin"></span> Открываем страницу входа…</div>'; });
    var sel=p.querySelector('#ocstudio-month');
    if (sel) sel.addEventListener('change', function(){ MONTH=sel.value; render(); });
  }

  function render(){
    var p=ensurePopover();
    if (!isOpen){ p.style.display='none'; return; }
    p.style.display='block';
    var muted=tk('--v2-text-text-muted','#9a9a9a');
    if (STATE.state==='login') p.innerHTML=loginHtml(muted);
    else if (STATE.state==='loading') p.innerHTML='<div style="display:flex;align-items:center;gap:10px;color:'+muted+'"><span class="ocspin"></span> Загрузка данных…</div>';
    else p.innerHTML=readyHtml(muted);
    bind(p);
  }

  try{ if (window.chrome&&window.chrome.webview) window.chrome.webview.addEventListener('message', function(e){ STATE=e.data||STATE; render(); if (STATE.autoOpen && !isOpen) openPopover(); }); }catch(e){}
  setInterval(function(){ ensureButton(); if (isOpen) positionPopover(); }, 1500);
  document.addEventListener('click', function(e){ if(!isOpen) return; var p=document.getElementById('ocstudio-usage-pop'); var b=document.getElementById('ocstudio-usage-btn'); if((p&&p.contains(e.target))||(b&&b.contains(e.target))) return; toggle(); }, true);
  ensureButton();
})();
