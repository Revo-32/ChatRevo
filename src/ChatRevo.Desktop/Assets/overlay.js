(() => {
  if (location.hostname !== 'chzzk.naver.com' || !/^\/live\/[a-f0-9]{32}\/chat$/.test(location.pathname)) return;
  if (window.__chatRevo) return;
  let settings = {...window.ChatRevoReadability.defaults};
  let style;
  let observer;
  let scheduled = false;
  const follow = window.ChatRevoLayout.create();
  const effects = window.ChatRevoLayout.createEffects();
  const seen = new WeakSet();
  let activeViewport;
  let activeChat;
  const addedRoots = new Set();
  const markNeutral = node => {
    if (node.hasAttribute('data-chat-revo-neutral')) return;
    const color = getComputedStyle(node).backgroundColor.match(/[\d.]+/g)?.map(Number);
    if (color && color.length >= 3 && (color.length === 3 || color[3] > 0) &&
        Math.max(...color.slice(0,3)) < 65 && Math.max(...color.slice(0,3))-Math.min(...color.slice(0,3)) < 24)
      node.dataset.chatRevoNeutral='';
  };
  const tagChat = () => {
    // Discover containers by their semantic CSS-module names, not generated hashes.
    const chat = document.querySelector('[class*="_is_popup_chat_"]');
    if (!chat) { activeChat = null; activeViewport = null; follow.attach(null); addedRoots.clear(); return; }
    for (let node = chat; node && node !== document.documentElement; node = node.parentElement) node.dataset.chatRevoSurface='';
    const panels = 'div,section,article,ul';
    if (activeChat !== chat) {
      activeChat = chat;
      chat.querySelectorAll(panels).forEach(markNeutral);
    } else {
      for (const root of addedRoots) {
        if (!chat.contains(root)) continue;
        if (root.matches(panels)) markNeutral(root);
        root.querySelectorAll(panels).forEach(markNeutral);
      }
    }
    addedRoots.clear();
    const list = chat.querySelector('[class*="_list_bottom_"]');
    if (list) {
      for (let node=list; node && node!==chat; node=node.parentElement) node.dataset.chatRevoSurface='';
      let viewport = list.parentElement;
      while (viewport && viewport !== chat && !/auto|scroll/.test(getComputedStyle(viewport).overflowY)) viewport = viewport.parentElement;
      if (viewport && viewport !== chat) {
        const initial = activeViewport !== viewport;
        activeViewport = viewport; follow.attach(viewport);
        const rows = [...list.parentElement.children].filter(item=>item.matches('[class*="_item_"]'));
        const fresh = [];
        for (const item of rows) {
          if (!seen.has(item)) {
            item.dataset.chatRevoMessage='';
            seen.add(item);
            if (!initial) fresh.push(item);
          }
        }
        follow.refresh(rows);
        if (follow.following) effects.enter(fresh);
      }
    } else if (activeViewport) {
      activeViewport = null; follow.attach(null);
    }
    const header = [...chat.children].find(node => node.querySelector('[class*="_scale_button_"]'));
    if (header) header.dataset.chatRevoHeader='';
    const ranking = [...chat.children].find(node => node.querySelector('[class*="_ranking_button_"]'));
    if (ranking) ranking.dataset.chatRevoRanking='';
    const input = chat.querySelector('textarea, [contenteditable="true"], [class*="_not_login_"]');
    if (input) {
      const composer = input.closest('[class*="_area_"]') ?? input.closest('[class*="_container_"]');
      if (composer) composer.dataset.chatRevoComposer='';
    }
  };
  const post = data => window.chrome?.webview?.postMessage(data);
  const ensureStyle = () => {
    if (!document.head) return;
    style = document.getElementById('chat-revo-style');
    if (!style) { style = document.createElement('style'); style.id = 'chat-revo-style'; document.head.append(style); }
    if (settings.nativeAppearance) { style.textContent = '[data-chat-revo-composer],[data-chat-revo-ranking]{display:none!important;}[data-chat-revo-viewport]{scroll-behavior:auto!important;overflow-anchor:none!important;}'; return; }
    style.textContent = `
      ${window.ChatRevoReadability.fontCss()}
      html, body, #root { background:transparent !important; }
      [data-chat-revo-surface] { background:transparent !important; min-height:0 !important; }
      [data-chat-revo-neutral] { background-color:transparent !important; }
      [data-chat-revo-composer], [data-chat-revo-header], [data-chat-revo-ranking] { display:none !important; }
      ${window.ChatRevoReadability.css(settings)}
      ::-webkit-scrollbar { width:5px; }
      ::-webkit-scrollbar-thumb { background:#77839966; border-radius:5px; }
    `;
  };
  window.__chatRevo = {
    apply: value => { settings = {...window.ChatRevoReadability.defaults,...value}; effects.configure(settings); ensureStyle(); scheduleTag(); follow.schedule(); },
    setLocked: value => follow.setLocked(value),
    follow
  };
  const scheduleTag = () => {
    if (scheduled) return;
    scheduled = true;
    requestAnimationFrame(() => {scheduled=false;tagChat();});
  };
  const channelId = location.pathname.split('/')[2];
  let failures = 0;
  const updateLive = async () => {
    const timeout = new AbortController();
    const timer = setTimeout(() => timeout.abort(), 10000);
    try {
      // Website adapter. This is deliberately separate from the public Open API.
      const response = await fetch(`https://api.chzzk.naver.com/service/v3/channels/${channelId}/live-detail`, {credentials:'include', signal:timeout.signal});
      if (!response.ok) throw new Error('Live unavailable');
      const payload = await response.json();
      const info = payload.content;
      if (!info || !['OPEN','CLOSE','CLOSED'].includes(info.status)) throw new Error('Unknown schema');
      failures = 0;
      post({kind:'live',channelId,channelName:info.channel?.channelName ?? '',status:info.status,
        count:info.cvExposure === false ? null : (Number.isInteger(info.concurrentUserCount) && info.concurrentUserCount >= 0 ? info.concurrentUserCount : null)});
    } catch { failures++; post({kind:'liveUnavailable',channelId}); }
    finally { clearTimeout(timer); setTimeout(updateLive, Math.min(30000 * 2 ** Math.min(failures,3), 240000)); }
  };
  const start = () => {
    ensureStyle(); tagChat();
    observer = new MutationObserver(records => {
      if (activeChat) {
        outer: for (const record of records) {
          for (const node of record.addedNodes) {
            if (node.nodeType === Node.ELEMENT_NODE) addedRoots.add(node);
            // A hidden WebView can defer animation frames for a long time.
            if (addedRoots.size >= 256) { addedRoots.clear(); activeChat = null; break outer; }
          }
        }
      }
      scheduleTag();
    });
    observer.observe(document.documentElement,{childList:true,subtree:true});
  };
  window.addEventListener('pagehide', event => {
    if (!event.persisted) { observer?.disconnect(); follow.dispose(); effects.dispose(); }
  });
  window.addEventListener('pageshow', event => { if (event.persisted) scheduleTag(); });
  if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', start, {once:true}); else start();
  setTimeout(updateLive, 1500);
})();
