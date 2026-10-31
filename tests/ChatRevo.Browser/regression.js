window.createInputProbe = () => {
  const box=document.createElement('div');
  box.style.cssText='position:fixed;inset:0 auto auto 0;z-index:999999;width:300px;height:250px;overflow:auto;display:flex;flex-direction:column-reverse;background:#111;';
  document.body.append(box);
  const follow=window.ChatRevoLayout.create();follow.attach(box);
  const add=()=>{const row=document.createElement('div');row.style.cssText='min-height:50px;flex-shrink:0';row.textContent='과거 채팅';box.prepend(row);follow.refresh(box.children);};
  for(let i=0;i<30;i++)add();
  window.inputProbe={box,follow,add,dispose(){follow.dispose();box.remove();}};
};
(async () => {
  const results = {};
  const wait = (ms=100) => new Promise(resolve=>setTimeout(resolve,ms));
  const assert = (name, value) => { if (!value) throw new Error(name); results[name]=true; };
  const host = document.createElement('div');
  host.style.cssText='position:fixed;inset:0;z-index:999999;background:#161616;color:white;';
  document.body.append(host);
  const style = document.createElement('style');
  style.textContent=window.ChatRevoReadability.fontCss()+window.ChatRevoReadability.css(window.ChatRevoReadability.defaults);
  host.append(style);
  let follow, effects;
  try {
    for (const reversed of [true,false]) {
      const prefix = reversed ? 'reverse' : 'forward';
      const box = document.createElement('div');
      box.tabIndex=0;
      box.style.cssText=`height:240px;width:300px;overflow:auto;display:flex;flex-direction:${reversed?'column-reverse':'column'};`;
      host.append(box);
      follow=window.ChatRevoLayout.create();follow.attach(box);
      const add = (text) => {
        const row=document.createElement('div');row.dataset.chatRevoMessage='';row.textContent=text;
        if(reversed)box.prepend(row);else box.append(row);
        follow.refresh(box.children);return row;
      };
      for(let i=0;i<40;i++)add('메시지 '+i+' 길게 이어지는 한국어 채팅 테스트입니다.');
      await wait();
      assert(prefix+'InitialBottom',follow.distance<2);
      // Reproduce site scroll movement that occurs while the custom style changes height.
      box.scrollTop=reversed?-90:Math.max(0,box.scrollTop-90);
      add('새 채팅 여러 줄\n'+'긴 메시지 '.repeat(20));
      await wait();
      assert(prefix+'NewMessageFullyVisible',follow.distance<2);
      const late=add('늦게 로딩되는 이미지');
      await wait();
      const image=new Image();image.style.cssText='display:block;width:120px';late.append(image);
      image.src='data:image/svg+xml,'+encodeURIComponent('<svg xmlns="http://www.w3.org/2000/svg" width="120" height="190"><rect width="120" height="190" fill="orange"/></svg>');
      await image.decode();await wait();
      assert(prefix+'ImageResize',follow.distance<2);
      style.textContent=window.ChatRevoReadability.fontCss()+window.ChatRevoReadability.css({...window.ChatRevoReadability.defaults,fontId:'pretendard',fontSize:34,lineHeight:1.8,messageBackgroundOpacity:50});
      await document.fonts.ready;await wait();
      assert(prefix+'FontResize',follow.distance<2);
      box.style.width='230px';box.style.height='180px';await wait();
      assert(prefix+'ViewportResize',follow.distance<2);
      follow.pause();box.scrollTop=reversed?-400:Math.max(0,box.scrollTop-400);await wait();
      const historyTop=box.scrollTop;add('읽는 동안 도착한 채팅');await wait();
      assert(prefix+'HistoryNotDragged',!follow.following && Math.abs(box.scrollTop-historyTop)<2);
      follow.setLocked(true);await wait();assert(prefix+'LockResumes',follow.distance<2 && follow.following);
      for(let i=0;i<100;i++)add('연속 채팅 '+i);await wait();
      assert(prefix+'Burst',follow.distance<2);
      effects=window.ChatRevoLayout.createEffects();
      for(const effect of ['fade','rise','slide','pop']) {
        effects.configure({...window.ChatRevoReadability.defaults,messageAnimation:effect,animationDuration:100});
        const row=add('효과 '+effect);effects.enter([row]);await wait(170);
        assert(prefix+effect+'Visible',getComputedStyle(row).opacity==='1' && row.getAnimations().length===0 && follow.distance<2);
      }
      effects.dispose();follow.dispose();box.remove();
    }
    // FontFaceSet.load can succeed on fallback; require a matching loaded face.
    for(const font of window.ChatRevoFonts) {
      const family=window.ChatRevoReadability.family(font.id);
      const normal=await document.fonts.load(`400 20px ${family}`,'반가워요 가나다 123');
      assert('font-'+font.id,normal.some(f=>f.family===family && f.status==='loaded'));
      if(font.boldFile){const bold=await document.fonts.load(`700 20px ${family}`,'한글');assert('bold-'+font.id,bold.some(f=>f.weight==='700' && f.status==='loaded'));}
    }
    window.__chatRevoRegression={ok:true,results};
  } catch(error) { window.__chatRevoRegression={ok:false,error:String(error),results}; }
  finally {follow?.dispose();effects?.dispose();host.remove();}
})();
