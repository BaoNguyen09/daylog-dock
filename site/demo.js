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
    "  - 1 session on LSP\n" +
    "  - reply email\n" +
    "  - clean up notes\n" +
    "WAITING: unlimited, but review daily\n" +
    "DONE: unlimited\n" +
    "  - clean both my phone photos\n" +
    "  - prepare some goals/checklist";

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
      btnToday.setAttribute("aria-hidden", offset === 0 ? "true" : "false");
    }
  }

  function setEditorOpen(open) {
    stage.classList.toggle("is-editor-open", open);
    if (editor) {
      editor.hidden = !open;
      editor.setAttribute("aria-hidden", open ? "false" : "true");
      if ("inert" in editor) editor.inert = !open;
    }
    if (dockBtn) dockBtn.setAttribute("aria-expanded", open ? "true" : "false");
    if (hint) hint.classList.toggle("is-hidden", open);
    if (open && noteEl) {
      noteEl.focus();
    }
  }

  function openEditor() {
    setEditorOpen(true);
  }

  function closeEditor() {
    setEditorOpen(false);
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
    openEditor();
  });

  btnMin?.addEventListener("click", (e) => {
    e.stopPropagation();
    closeEditor();
  });

  btnClose?.addEventListener("click", (e) => {
    e.stopPropagation();
    closeEditor();
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
