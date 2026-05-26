(function () {
  const root = document.documentElement;
  const panel = document.querySelector(".panel");
  const dockBtn = document.querySelector(".dock-btn--daylog");
  const dateEl = document.querySelector(".panel-date");
  const noteEl = document.querySelector(".panel-note");
  const clockEl = document.querySelector(".scene-clock");

  const baseDate = new Date(2026, 4, 25);
  let offset = 0;

  const notesByOffset = {
    0:
      "- hang out with interns\n" +
      "- buy/get some chairs for the apartment\n" +
      "- reply email to professor\n" +
      "- buy: mesh wash bag\n" +
      "\n" +
      "BACKLOG\n" +
      "- sign lease of new apt\n" +
      "- update my resume for summer apps\n" +
      "- whiten my necklace",
    "-1": "- yesterday's note\n- what got done?",
    1: "- tomorrow\n- plan the week",
  };

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

  function noteForOffset(dayOffset) {
    if (notesByOffset[dayOffset]) return notesByOffset[dayOffset];
    if (dayOffset < 0) return "- past day\n- empty or older tasks";
    return "- future day\n- plan ahead";
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
    if (open && noteEl) noteEl.focus();
  }

  function moveDay(delta) {
    offset += delta;
    flashDate();
  }

  renderDate();
  if (clockEl) clockEl.textContent = formatClock();

  dockBtn?.addEventListener("click", () => {
    setPanelOpen(!root.classList.contains("is-panel-open"));
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
