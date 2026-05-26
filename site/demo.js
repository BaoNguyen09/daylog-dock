(function () {
  const root = document.documentElement;
  const dockButton = document.querySelector(".cmdpal-item-daylog");
  const appWindow = document.querySelector(".daylog-window");
  const dateEl = document.querySelector(".editor-date");
  const noteEl = document.querySelector(".note-paper");
  const clockEls = document.querySelectorAll(".site-time, .taskbar-clock");

  const baseDate = new Date(2026, 4, 25);
  let offset = 0;

  const notes = [
    "- capture the shape of the day\n- keep the file local\n- come back tomorrow",
    "- this is a demo note\n- your real journal stays on your machine",
  ];

  function formatDate(date) {
    return date.toLocaleDateString("en-US", {
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

  function render() {
    const shownDate = new Date(baseDate);
    shownDate.setDate(shownDate.getDate() + offset);
    if (dateEl) dateEl.textContent = formatDate(shownDate);
    if (noteEl) noteEl.textContent = offset === 0 ? notes[0] : notes[1];

    const todayButton = document.querySelector('[data-action="today"]');
    if (todayButton) todayButton.disabled = offset === 0;
  }

  function setOpen(open) {
    root.classList.toggle("is-panel-open", open);
    dockButton?.classList.toggle("is-active", open);
    dockButton?.setAttribute("aria-expanded", open ? "true" : "false");
    appWindow?.classList.toggle("is-minimized", !open);
  }

  function setOffset(nextOffset) {
    offset = nextOffset;
    render();
  }

  render();
  const now = formatClock();
  clockEls.forEach((el) => {
    el.textContent = now;
  });

  dockButton?.addEventListener("click", () => {
    setOpen(!root.classList.contains("is-panel-open"));
  });

  document.addEventListener("click", (event) => {
    const action = event.target.closest("[data-action]");
    if (!action) return;

    const name = action.getAttribute("data-action");
    if (name === "prev-day") setOffset(offset - 1);
    if (name === "next-day") setOffset(offset + 1);
    if (name === "today") setOffset(0);
    if (name === "minimize" || name === "close") setOpen(false);
  });
})();
