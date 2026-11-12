(() => {
  if (window.__mwState) {
    return;
  }

  window.__mwState = true;
  const isTop = window === window.top;
  const id = Math.random().toString(36).slice(2);
  const typedIn = new Set();
  const noText = ['checkbox', 'radio', 'button', 'submit', 'reset', 'range', 'color', 'file', 'image', 'hidden'];
  let uploads = 0;
  let last = '';
  let timer = 0;
  let due = 0;
  let sentBusy = false;
  let videoAt = 0;
  let cachedVideo = null;
  let wantVideo = false;

  const editable = (element) => {
    if (!(element instanceof Element)) {
      return false;
    }

    if (element.isContentEditable || element instanceof HTMLTextAreaElement) {
      return true;
    }

    return element instanceof HTMLInputElement && !noText.includes(element.type);
  };

  const textOf = (element) => 'value' in element ? element.value : element.textContent;

  const audible = () => {
    for (const media of document.querySelectorAll('video,audio')) {
      const decoded = media.webkitAudioDecodedByteCount;

      if (!media.paused && !media.ended && !media.muted && media.volume > 0 && media.readyState > 2 && (decoded === undefined || decoded > 0)) {
        return true;
      }
    }

    return false;
  };

  const pickMedia = () => {
    let best = null;
    let bestArea = -1;

    for (const media of document.querySelectorAll('video,audio')) {
      if (!Number.isFinite(media.duration) || media.duration < 30 || media.currentTime < 2) {
        continue;
      }

      const area = (media.videoWidth || 0) * (media.videoHeight || 0) + (media.paused ? 0 : 1);

      if (area > bestArea) {
        best = media;
        bestArea = area;
      }
    }

    return best;
  };

  const round = (value) => Math.round(value * 10) / 10;

  const snapshot = () => {
    for (const element of [...typedIn]) {
      if (!element.isConnected || !textOf(element)) {
        typedIn.delete(element);
      }
    }

    const state = { id, top: isTop, typed: typedIn.size > 0, uploading: uploads > 0, audible: audible() };

    if (isTop) {
      state.scroll = [Math.round(window.scrollX), Math.round(window.scrollY)];
      const now = Date.now();

      if (wantVideo || now - videoAt >= 5000) {
        const media = pickMedia();
        cachedVideo = media ? { t: round(media.currentTime), d: round(media.duration), p: media.paused } : null;
        videoAt = now;
        wantVideo = false;
      }

      if (cachedVideo) {
        state.video = cachedVideo;
      }
    }

    return state;
  };

  const send = () => {
    timer = 0;
    due = 0;
    const state = snapshot();
    const busy = state.typed || state.uploading || state.audible;

    if (!isTop && !busy && !sentBusy) {
      return;
    }

    sentBusy = busy;
    const text = JSON.stringify(state);

    if (text !== last) {
      last = text;
      console.log('MWSTATE ' + text);
    }
  };

  const soon = (delay, video) => {
    if (video) {
      wantVideo = true;
    }

    const at = Date.now() + delay;

    if (timer && due <= at) {
      return;
    }

    clearTimeout(timer);
    due = at;
    timer = setTimeout(send, delay);
  };

  document.addEventListener('input', (event) => {
    if (!event.isTrusted) {
      return;
    }

    const target = event.composedPath()[0];

    if (editable(target)) {
      if (textOf(target)) {
        typedIn.add(target);
      } else {
        typedIn.delete(target);
      }

      soon(500, false);
    }
  }, true);

  if (isTop) {
    window.addEventListener('scroll', () => soon(1000, false), { capture: true, passive: true });
  }

  for (const name of ['play', 'pause', 'ended', 'emptied', 'volumechange', 'seeked']) {
    document.addEventListener(name, () => soon(300, true), true);
  }

  document.addEventListener('timeupdate', () => soon(5000, false), true);
  setInterval(() => soon(0, false), 5000);

  const hasUpload = (body) => {
    if (!body) {
      return false;
    }

    if (body instanceof Blob) {
      return body.size > 0;
    }

    if (body instanceof FormData) {
      for (const value of body.values()) {
        if (value instanceof Blob && value.size > 0) {
          return true;
        }
      }

      return false;
    }

    if (body instanceof ArrayBuffer || ArrayBuffer.isView(body)) {
      return body.byteLength >= 1048576;
    }

    if (typeof body === 'string') {
      return body.length >= 1048576;
    }

    return typeof ReadableStream !== 'undefined' && body instanceof ReadableStream;
  };

  const begin = () => {
    uploads++;
    soon(200, false);
  };

  const end = () => {
    uploads = Math.max(0, uploads - 1);
    soon(200, false);
  };

  const originalSend = XMLHttpRequest.prototype.send;

  XMLHttpRequest.prototype.send = function (body) {
    if (!hasUpload(body)) {
      return originalSend.apply(this, arguments);
    }

    begin();
    this.addEventListener('loadend', end, { once: true });

    try {
      return originalSend.apply(this, arguments);
    } catch (error) {
      end();
      throw error;
    }
  };

  if (typeof window.fetch === 'function') {
    const originalFetch = window.fetch;

    window.fetch = function (input, init) {
      if (!hasUpload(init && init.body)) {
        return originalFetch.apply(this, arguments);
      }

      begin();
      return originalFetch.apply(this, arguments).finally(end);
    };
  }

  document.addEventListener('submit', (event) => {
    const form = event.target;

    if (!event.defaultPrevented && form instanceof HTMLFormElement && [...form.querySelectorAll('input[type=file]')].some((input) => input.files && input.files.length > 0)) {
      begin();
    }
  });
})();
