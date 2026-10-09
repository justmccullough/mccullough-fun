mergeInto(LibraryManager.library, {
  MooState: function (json) {
    window.dispatchEvent(new CustomEvent('moo-quest-state', {
      detail: JSON.parse(UTF8ToString(json))
    }));
  }
});
