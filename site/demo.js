(function () {
  const stage = document.querySelector(".win-stage");
  if (!stage) return;

  const editor = stage.querySelector(".win-editor");
  const dockBtn = stage.querySelector(".dock-item--daylog");
  const hint = stage.querySelector(".demo-hint");
  const dateEl = stage.querySelector(".editor-date");
  const noteEl = stage.querySelector(".editor-note");
  const btnPrev = stage.querySelector('[data-action="prev-day"]');
  const btnNext = stage.querySelector('[data-action="next-day"]');
  const btnToday = stage.querySelector('[data-action="today"]');
  const btnMin = stage.querySelector('[data-action="minimize"]');
  const btnClose = stage.querySelector('[data-action="close"]');

  const baseDate = new Date(2026, 4, 25);
  let offset = 0;

  const sampleNote =
    "WIN: 1 only\n" +
    "NOW: 1 only at a time\n" +
    "NEXT: 3 max\n" +
    "LATER: 5 max\n" +
    "WAITING: review daily\n" +
    "DONE: clean phone photos";

  function formatDate(d) {
    return d.toLocaleDateString("en-US", {
      weekday: "long",
      month: "long",
      day: "numeric",
    });
  }

  function renderDate() {
    const d = new Date(baseDate);
    d.setDate(d.getDate() + offset);
    if (dateEl) dateEl.textContent = formatDate(d);
    if (btnToday) {
      btnToday.hidden = offset === 0;
    }
  }

  function setEditorOpen(open) {
    stage.classList.toggle("is-editor-open", open);
    if (editor) {
      editor.setAttribute("aria-hidden", open ? "false" : "true");
      if ("inert" in editor) editor.inert = !open;
    }
    if (dockBtn) {
      dockBtn.setAttribute("aria-expanded", open ? "true" : "false");
      dockBtn.classList.toggle("is-active", open);
    }
    if (hint) hint.classList.toggle("is-hidden", open);
    if (open && noteEl) noteEl.focus();
  }

  if (noteEl && !noteEl.textContent.trim()) {
    noteEl.textContent = sampleNote;
  }

  renderDate();

  dockBtn?.addEventListener("click", () => {
    if (stage.classList.contains("is-editor-open")) {
      noteEl?.focus();
      return;
    }
    setEditorOpen(true);
  });

  btnMin?.addEventListener("click", (e) => {
    e.stopPropagation();
    setEditorOpen(false);
  });

  btnClose?.addEventListener("click", (e) => {
    e.stopPropagation();
    setEditorOpen(false);
  });

  btnPrev?.addEventListener("click", (e) => {
    e.stopPropagation();
    offset -= 1;
    renderDate();
  });

  btnNext?.addEventListener("click", (e) => {
    e.stopPropagation();
    offset += 1;
    renderDate();
  });

  btnToday?.addEventListener("click", (e) => {
    e.stopPropagation();
    offset = 0;
    renderDate();
  });

  stage.querySelectorAll(".dock-item:not(.dock-item--daylog)").forEach((el) => {
    el.addEventListener("click", () => {
      el.classList.add("is-nudge");
      window.setTimeout(() => el.classList.remove("is-nudge"), 400);
    });
  });
})();
