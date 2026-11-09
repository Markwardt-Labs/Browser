(() => {
  if (window.__mwVideoStats) {
    return;
  }

  window.__mwVideoStats = true;
  const seen = new WeakMap();

  const report = () => {
    let best = null;

    for (const video of document.querySelectorAll('video')) {
      if (video.paused || video.ended || video.readyState < 2 || !video.videoWidth) {
        continue;
      }

      const quality = video.getVideoPlaybackQuality();
      const now = performance.now();
      const previous = seen.get(video);
      seen.set(video, { total: quality.totalVideoFrames, dropped: quality.droppedVideoFrames, at: now });

      if (previous && now > previous.at) {
        const seconds = (now - previous.at) / 1000;
        const entry = { f: (quality.totalVideoFrames - previous.total - (quality.droppedVideoFrames - previous.dropped)) / seconds, d: quality.droppedVideoFrames - previous.dropped, w: video.videoWidth, h: video.videoHeight };

        if (!best || entry.w * entry.h > best.w * best.h) {
          best = entry;
        }
      }
    }

    if (best) {
      best.v = document.visibilityState;
      best.x = document.hasFocus();
      console.log('MWSTAT ' + JSON.stringify(best));
    }
  };

  setInterval(report, 2000);
})();
