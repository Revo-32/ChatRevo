(() => {
  const defaults = {
    fontSize:20, fontId:'jamsil', bold:false, textShadow:true, nativeAppearance:false,
    messageAnimation:'none', animationDuration:240, animationDistance:12,
    textColor:'#FFFFFF', uniformNicknameColor:false, nicknameColor:'#FFD09A',
    outlineWidth:1, outlineColor:'#000000', shadowOpacity:90, shadowBlur:3,
    shadowDistance:1, shadowColor:'#000000', messageBackgroundOpacity:0,
    messageBackgroundColor:'#000000', lineHeight:1.45, messageSpacing:4
  };
  const fonts = window.ChatRevoFonts;
  const family = id => id === 'jamsil' ? 'ChatRevoJamsil' : 'ChatRevo_' + id;
  const fontCss = () => fonts.map(f => `
    @font-face {font-family:${family(f.id)};src:url('https://chatrevo.local/Fonts/${f.regularFile}');font-weight:400;font-display:swap;}
    ${f.boldFile ? `@font-face {font-family:${family(f.id)};src:url('https://chatrevo.local/Fonts/${f.boldFile}');font-weight:700;font-display:swap;}` : ''}
  `).join('');
  const rgba = (hex, opacity) => {
    const parts = [1,3,5].map(i => parseInt(hex.slice(i,i+2),16));
    return `rgba(${parts.join(',')},${opacity/100})`;
  };
  // Shared by the official chat adapter and the preview: preview must not drift.
  const css = (value, preview = false) => {
    const s = {...defaults,...value};
    const selected = fonts.find(f => f.id === s.fontId) ?? fonts[0];
    const row = preview ? '.row, .card' : '[data-chat-revo-message]';
    const content = preview ? '.body-text' : '[class*="_chatting_message_"] > [class*="_text_"], [data-chat-revo-message] p';
    const nickname = preview ? '.name' : '[data-chat-revo-message] [class*="_nickname_"], [data-chat-revo-message] [class*="_nickname_"] *';
    const descendants = row.split(',').flatMap(selector => ['span','p','strong'].map(tag => `${selector.trim()} ${tag}`)).join(',');
    const shadow = s.textShadow && s.shadowOpacity > 0
      ? `0 ${s.shadowDistance}px ${s.shadowBlur}px ${rgba(s.shadowColor,s.shadowOpacity)}` : 'none';
    return `
      ${row} {
        font-family:${family(selected.id)},ChatRevoJamsil,'Segoe UI Emoji','Malgun Gothic',sans-serif !important;
        flex-shrink:0 !important; min-height:min-content !important;
        font-size:${s.fontSize}px !important; font-weight:${s.bold ? 700 : 400} !important;
        line-height:${s.lineHeight} !important; color:${s.textColor} !important;
        -webkit-text-stroke:${s.outlineWidth * 2}px ${s.outlineColor} !important;
        paint-order:stroke fill !important; text-shadow:${shadow} !important;
        margin-bottom:${s.messageSpacing}px !important;
        ${s.messageBackgroundOpacity > 0 ? `background-color:${rgba(s.messageBackgroundColor,s.messageBackgroundOpacity)} !important;
        border-radius:6px !important; padding:6px 9px !important;` : ''}
      }
      ${descendants},
      [data-chat-revo-message] [class*="_container_"],
      [data-chat-revo-message] [class*="_chatting_message_"],
      [data-chat-revo-message] button[class*="_nickname_"] {
        font-family:inherit !important; font-size:inherit !important;
        font-weight:inherit !important; line-height:inherit !important;
        -webkit-text-stroke:inherit !important; paint-order:stroke fill !important; text-shadow:inherit !important;
      }
      ${content} {color:${s.textColor} !important;}
      ${s.uniformNicknameColor ? `${nickname} {color:${s.nicknameColor} !important;}` : ''}
      [data-chat-revo-message] [class*="_chatting_message_"] {overflow-wrap:anywhere !important;}
      [data-chat-revo-viewport] {scroll-behavior:auto !important;overflow-anchor:none !important;}
      [data-chat-revo-message] img[class*="_emoji_"] {width:1.5em !important;height:1.5em !important;object-fit:contain;}
    `;
  };
  window.ChatRevoReadability = {defaults,css,fontCss,family};
})();
