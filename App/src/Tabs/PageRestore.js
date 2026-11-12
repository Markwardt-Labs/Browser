(() => {
  const state = __STATE__;
  const started = Date.now();
  let following = true;
  let scrolled = !state.scroll || (state.scroll[0] === 0 && state.scroll[1] === 0);
  let seeked = !state.video;

  for (const name of ['wheel', 'keydown', 'mousedown', 'touchstart']) {
    window.addEventListener(name, () => { following = false; }, { once: true, capture: true, passive: true });
  }

  const step = () => {
    if (!following || (scrolled && seeked) || Date.now() - started > 15000) {
      clearInterval(handle);
      return;
    }

    if (!scrolled) {
      const root = document.scrollingElement || document.documentElement;

      if (root.scrollHeight - window.innerHeight >= state.scroll[1] - 2) {
        window.scrollTo(state.scroll[0], state.scroll[1]);
        scrolled = true;
      }
    }

    if (!seeked) {
      for (const media of document.querySelectorAll('video,audio')) {
        if (media.readyState >= 1 && Number.isFinite(media.duration) && Math.abs(media.duration - state.video.d) < 1.5) {
          media.currentTime = state.video.t;
          media.pause();
          seeked = true;
          break;
        }
      }
    }
  };

  const handle = setInterval(step, 300);
  step();
})();
