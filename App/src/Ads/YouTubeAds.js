(() => {
  if (window.__mwYouTubeAds) {
    return;
  }

  window.__mwYouTubeAds = true;

  const keys = ['adPlacements', 'playerAds', 'adSlots', 'adBreakHeartbeatParams'];

  const strip = value => {
    if (value && typeof value === 'object') {
      for (const key of keys) {
        if (key in value) {
          delete value[key];
        }
      }

      if (value.playerResponse && typeof value.playerResponse === 'object') {
        strip(value.playerResponse);
      }
    }

    return value;
  };

  const parse = JSON.parse;
  JSON.parse = function (text, reviver) {
    const value = parse.call(this, text, reviver);

    try {
      strip(value);
    } catch { }

    return value;
  };

  const json = Response.prototype.json;
  Response.prototype.json = function () {
    return json.call(this).then(value => {
      try {
        strip(value);
      } catch { }

      return value;
    });
  };

  try {
    let initial;
    Object.defineProperty(window, 'ytInitialPlayerResponse', {
      configurable: true,
      get: () => initial,
      set: value => { initial = strip(value); }
    });
  } catch { }

  const hide = 'ytd-ad-slot-renderer,ytd-in-feed-ad-layout-renderer,ytd-banner-promo-renderer,ytd-promoted-sparkles-web-renderer,ytd-display-ad-renderer,ytd-promoted-video-renderer,ytd-player-legacy-desktop-watch-ads-renderer,ytd-companion-slot-renderer,ytd-statement-banner-renderer,#masthead-ad,#player-ads,.ytp-ad-overlay-container,.ytp-ad-module,.ytp-paid-content-overlay{display:none!important}';

  const add = () => {
    const style = document.createElement('style');
    style.textContent = hide;
    (document.head || document.documentElement).appendChild(style);
  };

  if (document.documentElement) {
    add();
  } else {
    new MutationObserver((_, observer) => {
      if (document.documentElement) {
        observer.disconnect();
        add();
      }
    }).observe(document, { childList: true });
  }

  let restore = null;

  const skip = () => {
    const player = document.getElementById('movie_player');

    if (!player) {
      return;
    }

    const video = player.querySelector('video.html5-main-video');
    const showing = player.classList.contains('ad-showing') || player.classList.contains('ad-interrupting');

    if (showing && video) {
      for (const button of document.querySelectorAll('.ytp-skip-ad-button, .ytp-ad-skip-button, .ytp-ad-skip-button-modern, .ytp-ad-skip-button-container button')) {
        button.click();
      }

      document.querySelector('.ytp-ad-overlay-close-button')?.click();

      if (!restore) {
        restore = { muted: video.muted, rate: video.playbackRate };
      }

      video.muted = true;
      video.playbackRate = 16;

      if (isFinite(video.duration) && video.duration > 0) {
        video.currentTime = video.duration;
      }
    } else if (restore) {
      if (video) {
        video.muted = restore.muted;
        video.playbackRate = restore.rate;
      }

      restore = null;
    }
  };

  setInterval(skip, 100);
})();
