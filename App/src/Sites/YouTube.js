(() => {
  if (window.__markwardtYouTube) {
    return;
  }

  window.__markwardtYouTube = true;

  const qualities = ['hd1080', 'hd720', 'large', 'medium', 'small', 'tiny'];
  const state = { id: null, quality: 0, qualityDone: false, theater: 0, theaterClick: 0, video: false };

  window.__markwardtVideoMode?.useContainer(() => document.getElementById('movie_player'));

  try {
    const now = Date.now();
    localStorage.setItem('yt-player-quality', JSON.stringify({
      data: JSON.stringify({ quality: 1080, previousQuality: 1080 }),
      expiration: now + 2592000000,
      creation: now
    }));
  } catch { }

  const addressVideo = () => {
    const url = new URL(location.href);

    if (url.pathname === '/watch') {
      return url.searchParams.get('v');
    }

    const match = url.pathname.match(/^\/(?:embed|shorts|live)\/([^/?#]+)/);
    return match ? match[1] : null;
  };

  const applyQuality = player => {
    if (state.qualityDone || state.quality >= 12) {
      return;
    }

    const available = player.getAvailableQualityLevels?.() ?? [];
    const target = qualities.find(quality => available.includes(quality));

    if (!target) {
      return;
    }

    if (state.quality > 0 && player.getPlaybackQuality?.() === target) {
      state.qualityDone = true;
      return;
    }

    state.quality++;
    player.setPlaybackQualityRange?.(target, target);
    player.setPlaybackQuality?.(target);
  };

  const applyTheater = () => {
    if (state.theater >= 6 || Date.now() - state.theaterClick < 2000) {
      return;
    }

    const flexy = document.querySelector('ytd-watch-flexy');

    if (!flexy) {
      return;
    }

    if (flexy.hasAttribute('theater') || flexy.hasAttribute('fullscreen')) {
      state.theater = 6;
      return;
    }

    const button = document.querySelector('.ytp-size-button');

    if (button) {
      state.theater++;
      state.theaterClick = Date.now();
      button.click();
    }
  };

  const tick = () => {
    if (location.pathname !== '/watch' && !location.pathname.startsWith('/embed/') && window.__markwardtVideoMode?.isActive() && state.video) {
      window.__markwardtVideoMode.exit();
      state.video = false;
    }

    const wanted = addressVideo();
    const player = document.getElementById('movie_player');

    if (!wanted || !player || !player.getVideoData || player.classList.contains('ad-showing')) {
      return;
    }

    const id = player.getVideoData()?.video_id;

    if (!id || (location.pathname === '/watch' && id !== wanted)) {
      return;
    }

    if (state.id !== id) {
      state.id = id;
      state.quality = 0;
      state.qualityDone = false;
      state.theater = 0;
      state.video = false;
    }

    applyQuality(player);

    if (location.pathname === '/watch') {
      applyTheater();

      if (!state.video && window.__markwardtVideoMode?.enter()) {
        state.video = true;
      }
    }
  };

  setInterval(tick, 500);
})();
