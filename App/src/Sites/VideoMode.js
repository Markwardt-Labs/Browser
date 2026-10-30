(() => {
  if (window.__markwardtVideoMode) {
    return;
  }

  const css = `
    html.mw-video-mode, html.mw-video-mode body { overflow: hidden !important; }
    .mw-video-chain { transform: none !important; filter: none !important; perspective: none !important; contain: none !important; backdrop-filter: none !important; will-change: auto !important; clip-path: none !important; opacity: 1 !important; isolation: auto !important; z-index: auto !important; }
    .mw-video-fill { position: fixed !important; inset: 0 !important; width: 100vw !important; height: 100vh !important; max-width: none !important; max-height: none !important; margin: 0 !important; padding: 0 !important; z-index: 2147483647 !important; background: #000 !important; display: block !important; visibility: visible !important; }
    .mw-video-hide { visibility: hidden !important; }
    video.mw-video-fill { object-fit: contain !important; }
  `;

  let active = null;
  let finder = null;

  const area = element => {
    const rect = element.getBoundingClientRect();
    const width = Math.min(rect.right, innerWidth) - Math.max(rect.left, 0);
    const height = Math.min(rect.bottom, innerHeight) - Math.max(rect.top, 0);
    return width > 0 && height > 0 ? width * height : 0;
  };

  const target = () => {
    const videos = [...document.querySelectorAll('video')]
      .map(video => ({ video, score: area(video) * (video.paused ? 1 : 2) }))
      .filter(entry => entry.score > 0)
      .sort((a, b) => b.score - a.score);
    return videos.length > 0 ? videos[0].video : null;
  };

  const container = video => {
    const custom = finder?.(video);

    if (custom) {
      return custom;
    }

    let node = video;

    while (node.parentElement && node.parentElement !== document.body && node.parentElement !== document.documentElement) {
      const parent = node.parentElement;

      if (area(parent) > area(node) * 1.35 || parent.querySelectorAll('video').length > 1) {
        break;
      }

      node = parent;
    }

    return node;
  };

  const relayout = () => {
    window.dispatchEvent(new Event('resize'));
    setTimeout(() => window.dispatchEvent(new Event('resize')), 300);
  };

  const apply = element => {
    if (!document.getElementById('mw-video-style')) {
      const style = document.createElement('style');
      style.id = 'mw-video-style';
      style.textContent = css;
      document.documentElement.appendChild(style);
    }

    const chain = [];

    for (let node = element.parentElement; node; node = node.parentElement) {
      node.classList.add('mw-video-chain');
      chain.push(node);
    }

    const hidden = [];

    for (const node of [element, ...chain]) {
      for (const sibling of node.parentElement?.children ?? []) {
        if (sibling !== node && !sibling.classList.contains('mw-video-chain') && sibling !== element) {
          sibling.classList.add('mw-video-hide');
          hidden.push(sibling);
        }
      }
    }

    element.classList.add('mw-video-fill');
    document.documentElement.classList.add('mw-video-mode');
    active = { element, chain, hidden };
    relayout();
  };

  const notifyParent = on => {
    if (window.parent !== window) {
      window.parent.postMessage({ markwardtVideoMode: on }, '*');
    }
  };

  const enter = () => {
    if (active) {
      return true;
    }

    const video = target();

    if (!video) {
      return false;
    }

    apply(container(video));
    notifyParent(true);
    return true;
  };

  const exit = () => {
    if (!active) {
      return;
    }

    active.element.classList.remove('mw-video-fill');
    active.chain.forEach(node => node.classList.remove('mw-video-chain'));
    active.hidden.forEach(node => node.classList.remove('mw-video-hide'));
    document.documentElement.classList.remove('mw-video-mode');
    active = null;
    relayout();
    notifyParent(false);
  };

  const toggle = () => {
    if (active) {
      exit();
    } else {
      enter();
    }
  };

  const toggleAll = () => {
    toggle();

    for (const frame of document.querySelectorAll('iframe')) {
      frame.contentWindow?.postMessage({ markwardtVideoModeToggle: true }, '*');
    }
  };

  window.addEventListener('message', event => {
    const data = event.data;

    if (!data || typeof data !== 'object') {
      return;
    }

    if (data.markwardtVideoModeToggle && event.source === window.parent) {
      toggleAll();
      return;
    }

    if (typeof data.markwardtVideoMode === 'boolean') {
      const frame = [...document.querySelectorAll('iframe')].find(candidate => candidate.contentWindow === event.source);

      if (!frame) {
        return;
      }

      if (data.markwardtVideoMode && !active) {
        apply(frame);
        notifyParent(true);
      } else if (!data.markwardtVideoMode) {
        exit();
      }
    }
  });

  setInterval(() => {
    if (active && !active.element.isConnected) {
      exit();
    }
  }, 500);

  window.__markwardtVideoMode = {
    enter,
    exit,
    toggleAll,
    isActive: () => active !== null,
    useContainer: callback => { finder = callback; }
  };
})();
