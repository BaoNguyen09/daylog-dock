(function () {
  const root = document.documentElement;
  const panel = document.querySelector(".panel");
  const dockBtn = document.querySelector(".dock-btn--daylog");
  const dateEl = document.querySelector(".panel-date");
  const noteEl = document.querySelector(".panel-note");
  const clockEl = document.querySelector(".scene-clock");
  const btnToday = document.querySelector('[data-action="today"]');

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

  function formatClock() {
    return new Date().toLocaleString("en-US", {
      month: "short",
      day: "numeric",
      hour: "numeric",
      minute: "2-digit",
    });
  }

  function renderDate() {
    const d = new Date(baseDate);
    d.setDate(d.getDate() + offset);
    if (dateEl) dateEl.textContent = formatDate(d);
    if (btnToday) btnToday.hidden = offset === 0;
  }

  function setPanelOpen(open) {
    root.classList.toggle("is-panel-open", open);
    if (dockBtn) {
      dockBtn.setAttribute("aria-expanded", open ? "true" : "false");
      dockBtn.classList.toggle("is-active", open);
    }
    if (panel) {
      panel.setAttribute("aria-hidden", open ? "false" : "true");
      if ("inert" in panel) panel.inert = !open;
    }
    if (open && noteEl) noteEl.focus();
  }

  if (noteEl && !noteEl.textContent.trim()) {
    noteEl.textContent = sampleNote;
  }

  renderDate();
  if (clockEl) clockEl.textContent = formatClock();

  dockBtn?.addEventListener("click", () => {
    const open = !root.classList.contains("is-panel-open");
    setPanelOpen(open);
  });

  document.querySelector('[data-action="minimize"]')?.addEventListener("click", () => {
    setPanelOpen(false);
  });

  document.querySelector('[data-action="close"]')?.addEventListener("click", () => {
    setPanelOpen(false);
  });

  document.querySelector('[data-action="prev-day"]')?.addEventListener("click", () => {
    offset -= 1;
    renderDate();
  });

  document.querySelector('[data-action="next-day"]')?.addEventListener("click", () => {
    offset += 1;
    renderDate();
  });

  btnToday?.addEventListener("click", () => {
    offset = 0;
    renderDate();
  });

  document.querySelectorAll(".dock-btn:not(.dock-btn--daylog)").forEach((btn) => {
    btn.addEventListener("click", () => {
      btn.classList.add("dock-btn--wiggle");
      window.setTimeout(() => btn.classList.remove("dock-btn--wiggle"), 350);
    });
  });
})();
