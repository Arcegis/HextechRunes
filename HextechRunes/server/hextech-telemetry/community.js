// 社区配置分享：开放上传 + 点赞 + 举报 + 热度/最新物化列表 + 管理 API。
// 存储：configs.json(快照原子重写,低频) + community-likes.jsonl(追加式,高频) + bans.json。
// 读路径全静态：public/community-hot.json / community-new.json 由本模块物化,nginx 直出。
const fs = require("node:fs");
const path = require("node:path");
const zlib = require("node:zlib");
const crypto = require("node:crypto");

const LIMITS = {
  maxConfigsPerSteamId: 3,
  uploadIntervalMs: 10 * 60 * 1000,
  uploadsPerDay: 5,
  likeIntervalMs: 2 * 1000,
  titleMaxChars: 30,
  authorMaxChars: 24,
  codeMaxBytes: 16 * 1024,
  decodedMaxBytes: 512 * 1024,
  reportAutoHideThreshold: 5,
  materializedTop: 100,
  maxTotalConfigs: 5000
};

const state = {
  dataDir: null,
  publicDir: null,
  configsFile: null,
  likesFile: null,
  bansFile: null,
  adminTokenFile: null,
  wordsFile: null,
  configs: new Map(),        // id -> entry
  likes: new Map(),          // id -> Set(steamId)
  bans: new Set(),           // steamId
  sensitiveWords: [],
  adminToken: null,
  uploadTimes: new Map(),    // steamId -> [timestamps]  (内存限速,重启重置)
  likeTimes: new Map(),      // steamId -> timestamp
  materializeTimer: null
};

function init({ dataDir, publicDir }) {
  state.dataDir = dataDir;
  state.publicDir = publicDir;
  state.configsFile = path.join(dataDir, "community-configs.json");
  state.likesFile = path.join(dataDir, "community-likes.jsonl");
  state.bansFile = path.join(dataDir, "community-bans.json");
  state.adminTokenFile = path.join(dataDir, "community-admin-token.txt");
  state.wordsFile = path.join(__dirname, "sensitive-words.txt");
  loadConfigs();
  loadLikes();
  loadBans();
  loadSensitiveWords();
  loadOrCreateAdminToken();
  scheduleMaterialize(0);
}

function loadConfigs() {
  try {
    if (fs.existsSync(state.configsFile)) {
      const entries = JSON.parse(fs.readFileSync(state.configsFile, "utf8"));
      for (const entry of entries) {
        if (entry && typeof entry.id === "string") {
          state.configs.set(entry.id, entry);
        }
      }
    }
  } catch (error) {
    console.error(`community: failed to load configs: ${error.message}`);
  }
}

function loadLikes() {
  try {
    if (!fs.existsSync(state.likesFile)) {
      return;
    }
    const lines = fs.readFileSync(state.likesFile, "utf8").split("\n");
    for (const line of lines) {
      if (!line.trim()) continue;
      try {
        const record = JSON.parse(line);
        if (typeof record.id !== "string" || typeof record.steamId !== "string") continue;
        let set = state.likes.get(record.id);
        if (!set) {
          set = new Set();
          state.likes.set(record.id, set);
        }
        if (record.on) {
          set.add(record.steamId);
        } else {
          set.delete(record.steamId);
        }
      } catch {
        // 坏行忽略
      }
    }
  } catch (error) {
    console.error(`community: failed to load likes: ${error.message}`);
  }
}

function loadBans() {
  try {
    if (fs.existsSync(state.bansFile)) {
      for (const id of JSON.parse(fs.readFileSync(state.bansFile, "utf8"))) {
        state.bans.add(String(id));
      }
    }
  } catch (error) {
    console.error(`community: failed to load bans: ${error.message}`);
  }
}

function loadSensitiveWords() {
  try {
    if (fs.existsSync(state.wordsFile)) {
      state.sensitiveWords = fs.readFileSync(state.wordsFile, "utf8")
        .split("\n")
        .map((word) => word.trim().toLowerCase())
        .filter((word) => word.length > 0 && !word.startsWith("#"));
    }
  } catch (error) {
    console.error(`community: failed to load sensitive words: ${error.message}`);
  }
}

function loadOrCreateAdminToken() {
  try {
    if (fs.existsSync(state.adminTokenFile)) {
      state.adminToken = fs.readFileSync(state.adminTokenFile, "utf8").trim();
    }
    if (!state.adminToken) {
      state.adminToken = crypto.randomBytes(24).toString("hex");
      fs.writeFileSync(state.adminTokenFile, state.adminToken + "\n", { mode: 0o600 });
    }
  } catch (error) {
    console.error(`community: failed to init admin token: ${error.message}`);
  }
}

function atomicWrite(file, text) {
  const tmp = `${file}.tmp.${process.pid}`;
  fs.writeFileSync(tmp, text);
  fs.renameSync(tmp, file);
}

function persistConfigs() {
  atomicWrite(state.configsFile, JSON.stringify([...state.configs.values()], null, 1) + "\n");
}

function persistBans() {
  atomicWrite(state.bansFile, JSON.stringify([...state.bans]) + "\n");
}

function appendLike(record) {
  fs.appendFileSync(state.likesFile, JSON.stringify(record) + "\n");
}

function containsSensitiveWord(text) {
  const normalized = String(text).toLowerCase().replace(/[\s​‌‍]+/g, "");
  return state.sensitiveWords.some((word) => normalized.includes(word));
}

function charLength(text) {
  return [...String(text)].length;
}

function isValidSteamId(value) {
  return typeof value === "string" && /^\d{10,20}$/.test(value);
}

function validateCode(code) {
  if (typeof code !== "string" || !code.startsWith("HEXCFG1:") || Buffer.byteLength(code) > LIMITS.codeMaxBytes) {
    return false;
  }
  try {
    const compressed = Buffer.from(code.slice("HEXCFG1:".length), "base64");
    const decoded = zlib.gunzipSync(compressed, { maxOutputLength: LIMITS.decodedMaxBytes });
    const payload = JSON.parse(decoded.toString("utf8"));
    return payload && payload.v === 1;
  } catch {
    return false;
  }
}

function likeCount(id) {
  return state.likes.get(id)?.size ?? 0;
}

function hotScore(entry) {
  const ageHours = Math.max(0, (Date.now() - Date.parse(entry.createdAt || 0)) / 3600000);
  return Math.log10(likeCount(entry.id) + 1) / Math.pow(ageHours + 2, 1.2);
}

function publicView(entry) {
  return {
    id: entry.id,
    title: entry.title,
    author: entry.author,
    code: entry.code,
    likes: likeCount(entry.id),
    createdAt: entry.createdAt
  };
}

function visibleConfigs() {
  return [...state.configs.values()].filter((entry) => !entry.hidden);
}

function scheduleMaterialize(delayMs = 15000) {
  if (state.materializeTimer) {
    return;
  }
  state.materializeTimer = setTimeout(() => {
    state.materializeTimer = null;
    try {
      materialize();
    } catch (error) {
      console.error(`community: materialize failed: ${error.message}`);
    }
  }, delayMs);
}

function materialize() {
  const visible = visibleConfigs();
  const updatedAtUtc = new Date().toISOString();
  const hot = [...visible].sort((a, b) => hotScore(b) - hotScore(a)).slice(0, LIMITS.materializedTop);
  const fresh = [...visible].sort((a, b) => Date.parse(b.createdAt) - Date.parse(a.createdAt)).slice(0, LIMITS.materializedTop);
  const wrap = (configs) => JSON.stringify({ schemaVersion: 1, updatedAtUtc, configs: configs.map(publicView) }) + "\n";
  atomicWrite(path.join(state.publicDir, "community-hot.json"), wrap(hot));
  atomicWrite(path.join(state.publicDir, "community-new.json"), wrap(fresh));
}

function readBody(req, maxBytes = 64 * 1024) {
  return new Promise((resolve, reject) => {
    const chunks = [];
    let total = 0;
    req.on("data", (chunk) => {
      total += chunk.length;
      if (total > maxBytes) {
        reject(new Error("body too large"));
        req.destroy();
        return;
      }
      chunks.push(chunk);
    });
    req.on("end", () => resolve(Buffer.concat(chunks).toString("utf8")));
    req.on("error", reject);
  });
}

function sendJson(res, status, value) {
  const body = JSON.stringify(value);
  res.writeHead(status, {
    "content-type": "application/json; charset=utf-8",
    "cache-control": "no-store",
    "access-control-allow-origin": "*",
    "content-length": Buffer.byteLength(body)
  });
  res.end(body);
}

function checkUploadRate(steamId) {
  const now = Date.now();
  const times = (state.uploadTimes.get(steamId) || []).filter((t) => now - t < 24 * 3600 * 1000);
  state.uploadTimes.set(steamId, times);
  if (times.length >= LIMITS.uploadsPerDay) {
    return "daily_limit";
  }
  if (times.length > 0 && now - times[times.length - 1] < LIMITS.uploadIntervalMs) {
    return "too_frequent";
  }
  return null;
}

async function handleUpload(req, res) {
  let payload;
  try {
    payload = JSON.parse(await readBody(req));
  } catch {
    return sendJson(res, 400, { ok: false, error: "bad_request" });
  }
  const steamId = payload?.steamId;
  if (!isValidSteamId(steamId)) {
    return sendJson(res, 400, { ok: false, error: "bad_steam_id" });
  }
  if (state.bans.has(steamId)) {
    return sendJson(res, 403, { ok: false, error: "banned" });
  }
  const title = typeof payload.title === "string" ? payload.title.trim() : "";
  if (!title || charLength(title) > LIMITS.titleMaxChars) {
    return sendJson(res, 400, { ok: false, error: "bad_title" });
  }
  if (containsSensitiveWord(title)) {
    return sendJson(res, 400, { ok: false, error: "title_rejected" });
  }
  let author = typeof payload.authorName === "string" ? payload.authorName.trim() : "";
  if (!author || charLength(author) > LIMITS.authorMaxChars || containsSensitiveWord(author)) {
    author = "Player";
  }
  if (!validateCode(payload.code)) {
    return sendJson(res, 400, { ok: false, error: "bad_code" });
  }
  const owned = [...state.configs.values()].filter((entry) => entry.steamId === steamId && !entry.deleted);
  if (owned.length >= LIMITS.maxConfigsPerSteamId) {
    return sendJson(res, 409, { ok: false, error: "quota_exceeded" });
  }
  if (state.configs.size >= LIMITS.maxTotalConfigs) {
    return sendJson(res, 503, { ok: false, error: "full" });
  }
  const rateError = checkUploadRate(steamId);
  if (rateError) {
    return sendJson(res, 429, { ok: false, error: rateError });
  }

  const entry = {
    id: crypto.randomBytes(8).toString("hex"),
    steamId,
    author,
    title,
    code: payload.code,
    createdAt: new Date().toISOString(),
    hidden: false,
    reporters: []
  };
  state.configs.set(entry.id, entry);
  state.uploadTimes.get(steamId).push(Date.now());
  persistConfigs();
  scheduleMaterialize(0);
  return sendJson(res, 200, { ok: true, id: entry.id });
}

async function handleDelete(req, res) {
  let payload;
  try {
    payload = JSON.parse(await readBody(req));
  } catch {
    return sendJson(res, 400, { ok: false, error: "bad_request" });
  }
  const entry = state.configs.get(payload?.id);
  if (!entry || !isValidSteamId(payload?.steamId) || entry.steamId !== payload.steamId) {
    return sendJson(res, 404, { ok: false, error: "not_found" });
  }
  state.configs.delete(entry.id);
  state.likes.delete(entry.id);
  persistConfigs();
  scheduleMaterialize(0);
  return sendJson(res, 200, { ok: true });
}

async function handleLike(req, res) {
  let payload;
  try {
    payload = JSON.parse(await readBody(req));
  } catch {
    return sendJson(res, 400, { ok: false, error: "bad_request" });
  }
  const steamId = payload?.steamId;
  if (!isValidSteamId(steamId) || state.bans.has(steamId)) {
    return sendJson(res, 403, { ok: false, error: "forbidden" });
  }
  const entry = state.configs.get(payload?.id);
  if (!entry || entry.hidden) {
    return sendJson(res, 404, { ok: false, error: "not_found" });
  }
  const now = Date.now();
  const last = state.likeTimes.get(steamId) || 0;
  if (now - last < LIMITS.likeIntervalMs) {
    return sendJson(res, 429, { ok: false, error: "too_frequent" });
  }
  state.likeTimes.set(steamId, now);
  const on = payload.on !== false;
  let set = state.likes.get(entry.id);
  if (!set) {
    set = new Set();
    state.likes.set(entry.id, set);
  }
  const changed = on ? !set.has(steamId) : set.has(steamId);
  if (changed) {
    if (on) {
      set.add(steamId);
    } else {
      set.delete(steamId);
    }
    appendLike({ id: entry.id, steamId, on, ts: now });
    scheduleMaterialize();
  }
  return sendJson(res, 200, { ok: true, likes: set.size });
}

// 「我的」列表：返回该 steamId 的全部上传(含隐藏状态,便于玩家知道自己的配置被隐藏了)。
async function handleMine(req, res) {
  let payload;
  try {
    payload = JSON.parse(await readBody(req));
  } catch {
    return sendJson(res, 400, { ok: false, error: "bad_request" });
  }
  const steamId = payload?.steamId;
  if (!isValidSteamId(steamId)) {
    return sendJson(res, 400, { ok: false, error: "bad_steam_id" });
  }
  const configs = [...state.configs.values()]
    .filter((entry) => entry.steamId === steamId)
    .sort((a, b) => Date.parse(b.createdAt) - Date.parse(a.createdAt))
    .map((entry) => ({ ...publicView(entry), hidden: !!entry.hidden }));
  return sendJson(res, 200, { ok: true, configs });
}

async function handleReport(req, res) {
  let payload;
  try {
    payload = JSON.parse(await readBody(req));
  } catch {
    return sendJson(res, 400, { ok: false, error: "bad_request" });
  }
  const steamId = payload?.steamId;
  const entry = state.configs.get(payload?.id);
  if (!isValidSteamId(steamId) || !entry) {
    return sendJson(res, 404, { ok: false, error: "not_found" });
  }
  entry.reporters = entry.reporters || [];
  if (!entry.reporters.includes(steamId)) {
    entry.reporters.push(steamId);
    if (!entry.hidden && entry.reporters.length >= LIMITS.reportAutoHideThreshold) {
      entry.hidden = true;
      entry.hiddenReason = "auto_reports";
      scheduleMaterialize(0);
    }
    persistConfigs();
  }
  return sendJson(res, 200, { ok: true });
}

// —— 管理 API：header x-admin-token ——
function isAdmin(req) {
  const token = req.headers["x-admin-token"];
  return typeof token === "string" && state.adminToken && token === state.adminToken;
}

function adminView(entry) {
  return { ...entry, likes: likeCount(entry.id), reportCount: entry.reporters?.length ?? 0 };
}

async function handleAdmin(req, res, action) {
  if (!isAdmin(req)) {
    return sendJson(res, 401, { ok: false, error: "unauthorized" });
  }
  if (req.method === "GET" && action === "list") {
    const entries = [...state.configs.values()]
      .sort((a, b) => Date.parse(b.createdAt) - Date.parse(a.createdAt))
      .map(adminView);
    return sendJson(res, 200, { ok: true, bans: [...state.bans], configs: entries });
  }
  let payload = {};
  if (req.method === "POST") {
    try {
      payload = JSON.parse(await readBody(req));
    } catch {
      return sendJson(res, 400, { ok: false, error: "bad_request" });
    }
  }
  const entry = payload?.id ? state.configs.get(payload.id) : null;
  switch (action) {
    case "hide":
    case "unhide":
      if (!entry) return sendJson(res, 404, { ok: false, error: "not_found" });
      entry.hidden = action === "hide";
      entry.hiddenReason = action === "hide" ? "admin" : undefined;
      if (action === "unhide") entry.reporters = [];
      persistConfigs();
      scheduleMaterialize(0);
      return sendJson(res, 200, { ok: true });
    case "remove":
      if (!entry) return sendJson(res, 404, { ok: false, error: "not_found" });
      state.configs.delete(entry.id);
      state.likes.delete(entry.id);
      persistConfigs();
      scheduleMaterialize(0);
      return sendJson(res, 200, { ok: true });
    case "ban":
    case "unban": {
      const target = String(payload?.steamId || "");
      if (!isValidSteamId(target)) return sendJson(res, 400, { ok: false, error: "bad_steam_id" });
      if (action === "ban") {
        state.bans.add(target);
        // 封号同时隐藏其全部上传
        for (const item of state.configs.values()) {
          if (item.steamId === target) {
            item.hidden = true;
            item.hiddenReason = "banned";
          }
        }
        persistConfigs();
      } else {
        state.bans.delete(target);
      }
      persistBans();
      scheduleMaterialize(0);
      return sendJson(res, 200, { ok: true });
    }
    case "feature": {
      // 追加进 featured-configs.json（精选区）
      if (!entry) return sendJson(res, 404, { ok: false, error: "not_found" });
      const featuredFile = path.join(state.publicDir, "featured-configs.json");
      let doc = { schemaVersion: 1, updatedAtUtc: "", configs: [] };
      try {
        doc = JSON.parse(fs.readFileSync(featuredFile, "utf8"));
      } catch {
        // 缺失/损坏则重建
      }
      doc.configs = (doc.configs || []).filter((item) => item.id !== entry.id);
      doc.configs.push({ id: entry.id, name: entry.title, author: entry.author, description: "", code: entry.code });
      doc.updatedAtUtc = new Date().toISOString();
      atomicWrite(featuredFile, JSON.stringify(doc, null, 2) + "\n");
      return sendJson(res, 200, { ok: true });
    }
    case "unfeature": {
      const featuredFile = path.join(state.publicDir, "featured-configs.json");
      try {
        const doc = JSON.parse(fs.readFileSync(featuredFile, "utf8"));
        doc.configs = (doc.configs || []).filter((item) => item.id !== payload?.id);
        doc.updatedAtUtc = new Date().toISOString();
        atomicWrite(featuredFile, JSON.stringify(doc, null, 2) + "\n");
      } catch {
        // featured 文件缺失时视为已移除
      }
      return sendJson(res, 200, { ok: true });
    }
    case "featured-get": {
      const featuredFile = path.join(state.publicDir, "featured-configs.json");
      try {
        return sendJson(res, 200, { ok: true, doc: JSON.parse(fs.readFileSync(featuredFile, "utf8")) });
      } catch {
        return sendJson(res, 200, { ok: true, doc: { schemaVersion: 1, updatedAtUtc: "", configs: [] } });
      }
    }
    case "featured-save": {
      // 全量保存精选列表(编辑/排序/增删统一走这里);逐条校验,坏条目直接拒绝整个保存。
      const configs = payload?.configs;
      if (!Array.isArray(configs) || configs.length > 50) {
        return sendJson(res, 400, { ok: false, error: "bad_request" });
      }
      const cleaned = [];
      const usedIds = new Set();
      for (const item of configs) {
        const name = typeof item?.name === "string" ? item.name.trim() : "";
        if (!name || charLength(name) > 40 || !validateCode(item?.code)) {
          return sendJson(res, 400, { ok: false, error: "bad_entry", entry: name || "(unnamed)" });
        }
        let id = typeof item.id === "string" && /^[a-zA-Z0-9_-]{1,32}$/.test(item.id)
          ? item.id
          : crypto.randomBytes(8).toString("hex");
        while (usedIds.has(id)) {
          id = crypto.randomBytes(8).toString("hex");
        }
        usedIds.add(id);
        cleaned.push({
          id,
          name,
          author: typeof item.author === "string" ? item.author.trim().slice(0, 40) : "",
          description: typeof item.description === "string" ? item.description.trim().slice(0, 200) : "",
          code: item.code
        });
      }
      const doc = { schemaVersion: 1, updatedAtUtc: new Date().toISOString(), configs: cleaned };
      atomicWrite(path.join(state.publicDir, "featured-configs.json"), JSON.stringify(doc, null, 2) + "\n");
      return sendJson(res, 200, { ok: true, doc });
    }
    default:
      return sendJson(res, 404, { ok: false, error: "unknown_action" });
  }
}

// 返回 true 表示本模块处理了该请求。
function handleRequest(req, res, url) {
  const prefix = "/api/hextech-runes/community/";
  if (!url.pathname.startsWith(prefix)) {
    return false;
  }
  const action = url.pathname.slice(prefix.length);
  const route = async () => {
    if (action.startsWith("admin/")) {
      return handleAdmin(req, res, action.slice("admin/".length));
    }
    if (req.method !== "POST") {
      return sendJson(res, 405, { ok: false, error: "method_not_allowed" });
    }
    switch (action) {
      case "upload": return handleUpload(req, res);
      case "delete": return handleDelete(req, res);
      case "like": return handleLike(req, res);
      case "report": return handleReport(req, res);
      case "mine": return handleMine(req, res);
      default: return sendJson(res, 404, { ok: false, error: "unknown_action" });
    }
  };
  route().catch((error) => {
    console.error(`community: ${action} failed: ${error.message}`);
    try {
      sendJson(res, 500, { ok: false, error: "internal" });
    } catch {
      // 响应已发出
    }
  });
  return true;
}

module.exports = { init, handleRequest };
