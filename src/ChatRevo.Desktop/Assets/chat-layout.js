(() => {
  // The official chat is a reverse flex list: its bottom is scrollTop=0,
  // not scrollHeight-clientHeight. Layout changes must not mean "user scrolled".
  const create = () => {
    let viewport, rows = new Set(), frame = 0, following = true, locked = false;
    let gestureUntil = 0, disposed = false, dragging = false;
    const reverse = () => getComputedStyle(viewport).flexDirection === 'column-reverse';
    const distance = () => !viewport ? 0 : reverse() ? Math.abs(viewport.scrollTop)
      : Math.max(0, viewport.scrollHeight - viewport.clientHeight - viewport.scrollTop);
    const pin = () => {
      frame = 0;
      if (disposed || !viewport?.isConnected || !following) return;
      const target = reverse() ? 0 : viewport.scrollHeight - viewport.clientHeight;
      if (Math.abs(viewport.scrollTop - target) > 0.5) viewport.scrollTo({top:target, behavior:'instant'});
    };
    const schedule = () => { if (!disposed && !frame) frame = requestAnimationFrame(pin); };
    const resize = new ResizeObserver(schedule);
    const onScroll = () => {
      if (!locked && (dragging || performance.now() < gestureUntil)) following = distance() <= 24;
      else if (locked || distance() <= 1) following = true;
      if (following && distance() > 1) schedule();
    };
    const onWheel = event => {
      if (locked || !event.isTrusted) return;
      gestureUntil = performance.now() + 300;
      if (event.deltaY < 0 && viewport.scrollHeight > viewport.clientHeight + 2) following = false;
      if (event.deltaY > 0) requestAnimationFrame(() => { if (distance() <= 24) { following=true; schedule(); } });
    };
    const onPointer = event => { if (!locked && event.isTrusted) { dragging=true; gestureUntil=performance.now()+300; } };
    const onPointerEnd = () => { if (dragging) gestureUntil=performance.now()+300; dragging=false; };
    const onKey = event => {
      if (locked || !event.isTrusted) return;
      if (['ArrowUp','ArrowDown','PageUp','PageDown','Home','End',' '].includes(event.key)) {
        gestureUntil = performance.now() + 300;
        if (['ArrowUp','PageUp','Home'].includes(event.key) && viewport.scrollHeight > viewport.clientHeight + 2) following = false;
        if (event.key === 'End') { following = true; schedule(); }
      }
    };
    const listen = (node, method) => {
      node[method]('scroll', onScroll);
      node[method]('wheel', onWheel);
      node[method]('pointerdown', onPointer);
      node[method]('keydown', onKey);
      node[method]('load', schedule, true);
    };
    const attach = node => {
      if (node === viewport) return;
      if (viewport) { listen(viewport, 'removeEventListener'); delete viewport.dataset.chatRevoViewport; }
      resize.disconnect(); rows.clear(); viewport = node;
      if (!viewport) return;
      viewport.dataset.chatRevoViewport = '';
      following = true;
      listen(viewport, 'addEventListener'); resize.observe(viewport); schedule();
    };
    const refresh = items => {
      const next = new Set(items);
      for (const row of rows) if (!next.has(row)) resize.unobserve(row);
      for (const row of next) if (!rows.has(row)) resize.observe(row);
      rows = next; schedule();
    };
    document.fonts.addEventListener('loadingdone', schedule);
    window.addEventListener('pointerup', onPointerEnd);
    window.addEventListener('pointercancel', onPointerEnd);
    return {
      attach, refresh, schedule,
      setLocked(value) { locked = value; if (locked) { following = true; schedule(); } },
      resume() { following = true; schedule(); },
      // Used by preview/testing as well as deliberate history browsing.
      pause() { if (!locked) following = false; },
      get following() { return following; },
      get distance() { return distance(); },
      dispose() {
        disposed = true; if (frame) cancelAnimationFrame(frame);
        if (viewport) listen(viewport, 'removeEventListener');
        resize.disconnect(); rows.clear(); document.fonts.removeEventListener('loadingdone', schedule);
        window.removeEventListener('pointerup', onPointerEnd); window.removeEventListener('pointercancel', onPointerEnd);
      }
    };
  };
  const createEffects = () => {
    let settings = {}, animations = new Set();
    const stop = () => { for (const animation of animations) animation.cancel(); animations.clear(); };
    return {
      configure(value) { settings = value; stop(); },
      enter(nodes) {
        if (settings.nativeAppearance || settings.messageAnimation === 'none' || !settings.messageAnimation ||
            matchMedia('(prefers-reduced-motion: reduce)').matches || nodes.length > 30) return;
        const distance = settings.animationDistance ?? 12;
        const start = {
          fade:{opacity:0}, rise:{opacity:0,transform:`translateY(${distance}px)`},
          slide:{opacity:0,transform:`translateX(-${distance}px)`},
          pop:{opacity:0,transform:`scale(${1-distance/160})`}
        }[settings.messageAnimation];
        if (!start) return;
        for (const node of nodes) {
          const animation = node.animate([start,{opacity:1,transform:'none'}],
            {duration:settings.animationDuration ?? 240,easing:'cubic-bezier(.2,.7,.2,1)',fill:'none'});
          animations.add(animation);
          animation.finished.catch(()=>{}).finally(()=>animations.delete(animation));
        }
      },
      dispose:stop
    };
  };
  window.ChatRevoLayout = {create, createEffects};
})();
