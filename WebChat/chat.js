(function () {
  const log = document.getElementById("log");

  function scrollBottom() {
    requestAnimationFrame(() => {
      window.scrollTo(0, document.body.scrollHeight);
    });
  }

  function rowFor(id) {
    const safe = String(id).replace(/["\\]/g, "");
    return log.querySelector(`[data-id="${safe}"]`);
  }

  function bubbleEl(row) {
    return row ? row.querySelector(".bubble") : null;
  }

  function clear() {
    log.innerHTML = "";
  }

  function appendUser(text) {
    const id = "u-" + Date.now().toString(36) + "-" + Math.random().toString(36).slice(2, 7);
    const row = document.createElement("div");
    row.className = "row user";
    row.dataset.id = id;
    const b = document.createElement("div");
    b.className = "bubble user";
    b.textContent = text;
    row.appendChild(b);
    log.appendChild(row);
    scrollBottom();
    return id;
  }

  function appendAiTyping(id) {
    const row = document.createElement("div");
    row.className = "row ai";
    row.dataset.id = id;
    const b = document.createElement("div");
    b.className = "bubble ai typing";
    for (let i = 0; i < 3; i++) {
      const d = document.createElement("span");
      d.className = "dot";
      b.appendChild(d);
    }
    row.appendChild(b);
    log.appendChild(row);
    scrollBottom();
  }

  function setAiText(id, text, isError) {
    let row = rowFor(id);
    if (!row) {
      row = document.createElement("div");
      row.className = "row ai";
      row.dataset.id = id;
      log.appendChild(row);
    }
    let b = bubbleEl(row);
    if (!b) {
      b = document.createElement("div");
      row.appendChild(b);
    }
    b.className = "bubble ai" + (isError ? " error" : "");
    b.textContent = text;
    scrollBottom();
  }

  function handleMessage(ev) {
    let data = ev.data;
    if (typeof data === "string") {
      try {
        data = JSON.parse(data);
      } catch {
        return;
      }
    }
    if (!data || typeof data !== "object") return;
    const t = data.t;
    if (t === "clear") {
      clear();
      return;
    }
    if (t === "user") {
      appendUser(data.text || "");
      return;
    }
    if (t === "aiTyping") {
      appendAiTyping(data.id || "ai");
      return;
    }
    if (t === "aiSet") {
      setAiText(data.id || "ai", data.text || "", !!data.error);
      return;
    }
  }

  if (window.chrome && window.chrome.webview) {
    window.chrome.webview.addEventListener("message", handleMessage);
  }
})();
