mergeInto(LibraryManager.library, {
  PongState: function (json) {
    window.dispatchEvent(new CustomEvent('pasture-pong-state', {
      detail: JSON.parse(UTF8ToString(json))
    }));
  }
});
