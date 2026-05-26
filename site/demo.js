(function () {
  const root = document.documentElement;
  const panel = document.querySelector(".panel");
  const dockBtn = document.querySelector(".dock-btn--daylog");
  const dateEl = document.querySelector(".panel-date");
  const noteEl = document.querySelector(".panel-note");
  const clockEl = document.querySelector(".scene-clock");
  const taskbarClock = document.querySelector(".taskbar-clock");

  const baseDate = new Date(2026, 4, 25);
  let offset = 0;

  const PLACEHOLDER_TODAY =
    "- write the day down while it is still fresh\n" +
    "- keep the file local\n" +
    "- come back tomorrow";

  const PLACEHOLDER_OTHER =
    "- this is placeholder demo text\n" +
    "- your real notes stay on your machine only";

  function formatDate(d) {
    return d.toLocaleDateString("en-US", {
      weekday: "long",
      month: "long",
      day: "numeric",
    });
  }

  function formatClock() {
    return new Date().toLocaleString("en-US", {
      hour: "numeric",
      minute: "2-digit",
    });
  }

  function formatTrayDate() {
    return new Date().toLocaleString("en-US", {
      hour: "numeric",
      minute: "2-digit",
    });
  }

  function noteForOffset(dayOffset) {
    return dayOffset === 0 ? PLACEHOLDER_TODAY : PLACEHOLDER_OTHER;
  }

  function renderDate() {
    const d = new Date(baseDate);
    d.setDate(d.getDate() + offset);
    if (dateEl) {
      dateEl.textContent = formatDate(d);
      dateEl.classList.remove("is-updating");
    }
    const btnToday = panel?.querySelector('[data-action="today"]');
    if (btnToday) btnToday.disabled = offset === 0;
    if (noteEl) noteEl.textContent = noteForOffset(offset);
  }

  function flashDate() {
    if (!dateEl) return;
    dateEl.classList.add("is-updating");
    window.requestAnimationFrame(() => {
      window.setTimeout(renderDate, 80);
    });
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
    document.querySelector(".taskbar-app--daylog")?.classList.toggle("is-running", open);
    if (open && noteEl) noteEl.focus();
  }

  function moveDay(delta) {
    offset += delta;
    flashDate();
  }

  renderDate();
  const clockText = formatClock();
  if (clockEl) clockEl.textContent = clockText;
  if (taskbarClock) taskbarClock.textContent = clockText;

  dockBtn?.addEventListener("click", () => {
    setPanelOpen(!root.classList.contains("is-panel-open"));
  });

  document.querySelector(".taskbar-app--daylog")?.addEventListener("click", () => {
    setPanelOpen(true);
    dockBtn?.focus();
  });

  panel?.addEventListener("click", (e) => {
    const action = e.target.closest("[data-action]");
    if (!action || !panel.contains(action)) return;

    const name = action.getAttribute("data-action");
    if (name === "prev-day") {
      e.preventDefault();
      moveDay(-1);
      return;
    }
    if (name === "next-day") {
      e.preventDefault();
      moveDay(1);
      return;
    }
    if (name === "today") {
      e.preventDefault();
      if (offset !== 0) {
        offset = 0;
        flashDate();
      }
      return;
    }
    if (name === "minimize" || name === "close") {
      e.preventDefault();
      setPanelOpen(false);
      return;
    }
    if (name === "folder") {
      e.preventDefault();
      action.classList.add("panel-nav-text--flash");
      window.setTimeout(() => action.classList.remove("panel-nav-text--flash"), 200);
    }
  });

  document.querySelectorAll(".dock-btn:not(.dock-btn--daylog)").forEach((btn) => {
    btn.addEventListener("click", () => {
      btn.classList.add("dock-btn--wiggle");
      window.setTimeout(() => btn.classList.remove("dock-btn--wiggle"), 350);
    });
  });

  panel?.querySelectorAll(".panel-foot-btn").forEach((btn) => {
    btn.addEventListener("click", () => {
      const group = btn.closest(".panel-foot-group");
      group?.querySelectorAll(".panel-foot-btn").forEach((b) => b.classList.remove("is-active"));
      btn.classList.add("is-active");
    });
  });
})();
