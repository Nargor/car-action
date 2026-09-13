// ===== HIGH-SPEED CAR RACE — WEB UI CONTROLLER =====

const UI = {
  currentMode: "PlayerRace",
  selectedLaps: 3,
  selectedAICount: 5,
  apiBase: "http://localhost:8888",
  activeScreen: "screen-title",

  init() {
    this.initDraggableLeaderboard();
    this.startStatePolling();
    console.log("[WebUI] Initialized. Connected to Unity Bridge at " + this.apiBase);
  },

  showScreen(screenId) {
    document.querySelectorAll(".screen-panel").forEach(p => p.classList.remove("active"));
    const target = document.getElementById(screenId);
    if (target) target.classList.add("active");
    this.activeScreen = screenId;
  },

  openLobby(mode) {
    this.currentMode = mode;
    this.showScreen("screen-lobby");

    const titleEl = document.getElementById("lobby-title");
    const aiSlider = document.getElementById("slider-ai");

    if (mode === "PlayerRace") {
      titleEl.innerText = "RACE LOBBY (PLAYER VS AI)";
      aiSlider.min = 0;
      aiSlider.max = 49;
      if (this.selectedAICount > 49) this.selectedAICount = 5;
    } else {
      titleEl.innerText = "SPECTATOR LOBBY (AI CHAMPIONSHIP)";
      aiSlider.min = 2;
      aiSlider.max = 50;
      if (this.selectedAICount < 2) this.selectedAICount = 10;
    }

    aiSlider.value = this.selectedAICount;
    this.updateLobbyLabels();
  },

  openTikTokLobby() {
    this.currentMode = "TikTokLive";
    this.showScreen("screen-tiktok");
    this.sendAction("openTikTokLobby");
  },

  onLapsSlider(val) {
    this.selectedLaps = parseInt(val);
    this.updateLobbyLabels();
    this.sendAction("setLaps", { laps: this.selectedLaps });
  },

  onAISlider(val) {
    this.selectedAICount = parseInt(val);
    this.updateLobbyLabels();
    this.sendAction("setAICount", { aiCount: this.selectedAICount });
  },

  updateLobbyLabels() {
    document.getElementById("lobby-laps-val").innerText = `${this.selectedLaps} LAPS`;
    document.getElementById("lobby-ai-val").innerText = `${this.selectedAICount} AI CARS`;

    const total = (this.currentMode === "PlayerRace") ? (1 + this.selectedAICount) : this.selectedAICount;
    document.getElementById("lobby-total-text").innerText = `TOTAL RACERS: ${total} / 50`;
  },

  startRace() {
    this.showScreen("screen-hud");
    this.sendAction("startRace", {
      laps: this.selectedLaps,
      aiCount: this.selectedAICount,
      mode: this.currentMode
    });
  },

  connectTikTok() {
    const input = document.getElementById("tiktok-username-input");
    const username = input.value.trim() || "my_stream";
    this.sendAction("connectTikTok", { username: username });
    document.getElementById("btn-start-tiktok-race").disabled = false;
  },

  disconnectTikTok() {
    this.sendAction("disconnectTikTok");
    this.showScreen("screen-title");
  },

  startTikTokRace() {
    this.showScreen("screen-hud");
    this.sendAction("startTikTokRace");
  },

  setCameraMode(mode) {
    this.sendAction("setCameraMode", { mode: mode });
  },

  focusRacer(racerName) {
    this.sendAction("focusRacer", { name: racerName });
  },

  sendAction(actionName, data = {}) {
    fetch(`${this.apiBase}/api/action`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ action: actionName, ...data })
    }).catch(err => {
      // Offline fallback
    });
  },

  // ===== REAL-TIME STATE SYNC FROM UNITY =====
  startStatePolling() {
    setInterval(() => {
      fetch(`${this.apiBase}/api/state`)
        .then(res => res.json())
        .then(data => this.applyState(data))
        .catch(() => {});
    }, 60); // 16-20 updates per second for smooth UI
  },

  applyState(state) {
    if (!state) return;

    // 1. Synchronize Active Screen if changed from Unity
    if (state.activeScreen && state.activeScreen !== this.activeScreen) {
      if (document.getElementById(state.activeScreen)) {
        this.showScreen(state.activeScreen);
      }
    }

    // 2. Speedometer
    const speedVal = document.getElementById("hud-speed-val");
    if (speedVal) speedVal.innerText = Math.round(state.speed || 0);

    const driverName = document.getElementById("hud-driver-name");
    if (driverName) driverName.innerText = (state.driverName || "PLAYER").toUpperCase();

    const driverDot = document.getElementById("hud-driver-dot");
    if (driverDot && state.driverColor) driverDot.style.color = state.driverColor;

    const speedFill = document.getElementById("hud-speed-fill");
    if (speedFill) {
      const pct = Math.min(100, Math.max(0, ((state.speed || 0) / 240) * 100));
      speedFill.style.width = `${pct}%`;
    }

    // 3. Race Stats
    const lapText = document.getElementById("hud-lap-text");
    if (lapText) lapText.innerText = `LAP ${state.currentLap || 1} / ${state.totalLaps || 3}`;

    const posBadge = document.getElementById("hud-pos-badge");
    if (posBadge) {
      const pos = state.playerPosition || 1;
      const total = state.totalRacers || 1;
      const suffix = (pos === 1) ? "ST" : (pos === 2) ? "ND" : (pos === 3) ? "RD" : "TH";
      posBadge.innerText = `POS ${pos}${suffix} / ${total}`;
    }

    const curTime = document.getElementById("hud-current-time");
    if (curTime && state.currentLapTime) curTime.innerText = state.currentLapTime;

    const bestTime = document.getElementById("hud-best-time");
    if (bestTime && state.bestLapTime) bestTime.innerText = state.bestLapTime;

    const totTime = document.getElementById("hud-total-time");
    if (totTime && state.totalRaceTime) totTime.innerText = state.totalRaceTime;

    // 4. Countdown Banner
    const cdBanner = document.getElementById("hud-countdown-banner");
    const cdText = document.getElementById("hud-countdown-text");
    if (cdBanner && cdText) {
      if (state.isCountdownActive) {
        cdBanner.classList.remove("hidden");
        cdText.innerText = state.countdownText || "";
        if (state.countdownText === "GO!") cdBanner.classList.add("go");
        else cdBanner.classList.remove("go");
      } else {
        cdBanner.classList.add("hidden");
      }
    }

    // 5. Camera Toolbar active state
    if (state.activeCameraMode) {
      document.querySelectorAll(".cam-btn").forEach(btn => btn.classList.remove("active"));
      const activeBtn = document.getElementById(`cam-btn-${state.activeCameraMode.toLowerCase()}`);
      if (activeBtn) activeBtn.classList.add("active");
    }

    // 6. Dynamic Leaderboard
    if (state.racers && Array.isArray(state.racers)) {
      this.updateLeaderboard(state.racers);
    }

    // 7. Finish Modal
    const finishPanel = document.getElementById("hud-finish-panel");
    if (finishPanel) {
      if (state.isRaceFinished) {
        finishPanel.classList.remove("hidden");
        const pos = state.finishRank || 1;
        const total = state.totalRacers || 1;
        const rankBadge = document.getElementById("finish-badge");
        if (rankBadge) {
          rankBadge.innerText = (pos === 1) ? "1st PLACE! 🏆 WINNER"
                              : (pos === 2) ? "2nd PLACE! 🥈 PODIUM"
                              : (pos === 3) ? "3rd PLACE! 🥉 PODIUM"
                              : `${pos}th PLACE`;
        }
        const posText = document.getElementById("finish-pos-text");
        if (posText) posText.innerText = `Position: ${pos} / ${total}`;

        const finTot = document.getElementById("finish-total-time");
        if (finTot && state.totalRaceTime) finTot.innerText = state.totalRaceTime;

        const finBest = document.getElementById("finish-best-time");
        if (finBest && state.bestLapTime) finBest.innerText = state.bestLapTime;
      } else {
        finishPanel.classList.add("hidden");
      }
    }

    // 8. TikTok Live Lobby Status
    const ttStatus = document.getElementById("tiktok-status-badge");
    if (ttStatus && state.tiktokUsername) {
      if (state.tiktokConnected) {
        ttStatus.innerHTML = `<span style="color:#00ff88">● LIVE CONNECTED: @${state.tiktokUsername}</span>`;
      }
    }
    const ttCount = document.getElementById("tiktok-racers-count");
    if (ttCount && state.tiktokJoinedCount !== undefined) {
      ttCount.innerHTML = `RACERS JOINED: <span>${state.tiktokJoinedCount}</span> / 50`;
    }
  },

  updateLeaderboard(racers) {
    const list = document.getElementById("leaderboard-list");
    if (!list) return;

    list.innerHTML = "";
    racers.forEach(r => {
      const row = document.createElement("div");
      row.className = "racer-row" + (r.isFocused ? " focused" : "");
      row.onclick = () => this.focusRacer(r.name);

      const posClass = (r.position === 1) ? "pos-1" : (r.position === 2) ? "pos-2" : (r.position === 3) ? "pos-3" : "";

      row.innerHTML = `
        <span class="row-pos ${posClass}">${r.position}</span>
        <span class="row-swatch" style="background:${r.carColor || '#f00'}"></span>
        <span class="row-name">${r.name}</span>
        <span class="row-lap">L${r.currentLap || 1}</span>
      `;
      list.appendChild(row);
    });
  },

  // ===== DRAGGABLE WINDOW LOGIC =====
  initDraggableLeaderboard() {
    const win = document.getElementById("hud-leaderboard");
    const header = document.getElementById("leaderboard-header");
    if (!win || !header) return;

    let isDragging = false;
    let offsetX = 0, offsetY = 0;

    header.onmousedown = (e) => {
      isDragging = true;
      const rect = win.getBoundingClientRect();
      offsetX = e.clientX - rect.left;
      offsetY = e.clientY - rect.top;
      win.style.right = "auto";
      win.style.top = `${rect.top}px`;
      win.style.left = `${rect.left}px`;
      win.style.transform = "none";
    };

    document.onmousemove = (e) => {
      if (!isDragging) return;
      let x = e.clientX - offsetX;
      let y = e.clientY - offsetY;

      // Clamp to screen bounds
      x = Math.max(10, Math.min(window.innerWidth - win.offsetWidth - 10, x));
      y = Math.max(10, Math.min(window.innerHeight - win.offsetHeight - 10, y));

      win.style.left = `${x}px`;
      win.style.top = `${y}px`;
    };

    document.onmouseup = () => {
      isDragging = false;
    };
  }
};

window.onload = () => UI.init();
