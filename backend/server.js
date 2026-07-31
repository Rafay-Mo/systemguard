const http = require("http");
const crypto = require("crypto");
const fs = require("fs");
const path = require("path");
const { URL } = require("url");

const PORT = Number(process.env.SYSTEM_GUARDIAN_PORT || 4739);
const HOST = process.env.SYSTEM_GUARDIAN_HOST || "127.0.0.1";
const SECRET = process.env.SYSTEM_GUARDIAN_SECRET || crypto.randomBytes(32).toString("hex");
const DATA_DIR = path.join(__dirname, "data");
const DB_PATH = path.join(DATA_DIR, "db.json");

const SAFE_ACTIONS = {
  open_windows_update: {
    title: "Open Windows Update",
    tool: "Windows Update",
    risk: "low",
    requiresAdmin: false
  },
  check_windows_support: {
    title: "Check Windows support options",
    tool: "Windows Update",
    risk: "planning",
    requiresAdmin: false
  },
  restart_prompt: {
    title: "Restart Windows",
    tool: "Windows restart prompt",
    risk: "user-approved restart",
    requiresAdmin: false
  },
  open_storage_settings: {
    title: "Open Storage cleanup",
    tool: "Storage settings",
    risk: "low",
    requiresAdmin: false
  },
  open_startup_apps: {
    title: "Review startup apps",
    tool: "Startup Apps settings",
    risk: "low",
    requiresAdmin: false
  },
  open_device_manager: {
    title: "Open driver tools",
    tool: "Device Manager and optional updates",
    risk: "manual review",
    requiresAdmin: false
  },
  update_defender_signatures: {
    title: "Update Microsoft Defender",
    tool: "Windows Security",
    risk: "needs admin",
    requiresAdmin: true
  },
  start_core_services: {
    title: "Start important Windows services",
    tool: "Windows services",
    risk: "needs admin",
    requiresAdmin: true
  },
  flush_dns: {
    title: "Flush DNS and open Network settings",
    tool: "Network settings",
    risk: "low",
    requiresAdmin: false
  },
  run_dism_sfc: {
    title: "Run DISM and SFC repair",
    tool: "DISM and SFC",
    risk: "needs admin",
    requiresAdmin: true
  }
};

const CHECK_TO_ACTION = {
  "Windows Update": "open_windows_update",
  "Windows Support": "check_windows_support",
  "Pending Reboot": "restart_prompt",
  "Disk Space": "open_storage_settings",
  "Startup Apps": "open_startup_apps",
  "Device Drivers": "open_device_manager",
  "Microsoft Defender": "update_defender_signatures",
  "Core Services": "start_core_services",
  "Network": "flush_dns",
  "System Integrity": "run_dism_sfc"
};

function ensureDb() {
  fs.mkdirSync(DATA_DIR, { recursive: true });
  if (!fs.existsSync(DB_PATH)) {
    writeDb({
      users: [],
      devices: [],
      reports: [],
      crashReports: [],
      createdAt: new Date().toISOString()
    });
  }
}

function readDb() {
  ensureDb();
  return JSON.parse(fs.readFileSync(DB_PATH, "utf8"));
}

function writeDb(db) {
  fs.mkdirSync(DATA_DIR, { recursive: true });
  fs.writeFileSync(DB_PATH, JSON.stringify(db, null, 2), "utf8");
}

function id(prefix) {
  return `${prefix}_${crypto.randomBytes(12).toString("hex")}`;
}

function json(res, status, body) {
  const payload = JSON.stringify(body);
  res.writeHead(status, {
    "Content-Type": "application/json; charset=utf-8",
    "Content-Length": Buffer.byteLength(payload),
    "Access-Control-Allow-Origin": "*",
    "Access-Control-Allow-Methods": "GET,POST,OPTIONS",
    "Access-Control-Allow-Headers": "Content-Type,Authorization"
  });
  res.end(payload);
}

function readJson(req) {
  return new Promise((resolve, reject) => {
    let body = "";
    req.on("data", chunk => {
      body += chunk;
      if (body.length > 1024 * 1024 * 3) {
        reject(new Error("Request too large"));
        req.destroy();
      }
    });
    req.on("end", () => {
      if (!body.trim()) return resolve({});
      try {
        resolve(JSON.parse(body));
      } catch {
        reject(new Error("Invalid JSON"));
      }
    });
    req.on("error", reject);
  });
}

function hashPassword(password) {
  const salt = crypto.randomBytes(16).toString("hex");
  const hash = crypto.pbkdf2Sync(password, salt, 120000, 32, "sha256").toString("hex");
  return `${salt}:${hash}`;
}

function verifyPassword(password, stored) {
  const [salt, hash] = String(stored || "").split(":");
  if (!salt || !hash) return false;
  const test = crypto.pbkdf2Sync(password, salt, 120000, 32, "sha256").toString("hex");
  return crypto.timingSafeEqual(Buffer.from(hash, "hex"), Buffer.from(test, "hex"));
}

function base64url(input) {
  return Buffer.from(input).toString("base64url");
}

function signToken(payload) {
  const header = base64url(JSON.stringify({ alg: "HS256", typ: "JWT-lite" }));
  const body = base64url(JSON.stringify(payload));
  const sig = crypto.createHmac("sha256", SECRET).update(`${header}.${body}`).digest("base64url");
  return `${header}.${body}.${sig}`;
}

function verifyToken(token) {
  const parts = String(token || "").split(".");
  if (parts.length !== 3) return null;
  const expected = crypto.createHmac("sha256", SECRET).update(`${parts[0]}.${parts[1]}`).digest("base64url");
  if (!crypto.timingSafeEqual(Buffer.from(expected), Buffer.from(parts[2]))) return null;
  const payload = JSON.parse(Buffer.from(parts[1], "base64url").toString("utf8"));
  if (payload.exp && Date.now() > payload.exp) return null;
  return payload;
}

function getAuth(req, db) {
  const header = req.headers.authorization || "";
  const token = header.startsWith("Bearer ") ? header.slice(7) : "";
  const payload = verifyToken(token);
  if (!payload) return null;
  const user = db.users.find(u => u.id === payload.sub);
  return user ? { user, payload } : null;
}

function requireAuth(req, res, db) {
  const auth = getAuth(req, db);
  if (!auth) {
    json(res, 401, { error: "auth_required" });
    return null;
  }
  return auth;
}

function sanitizeReport(report) {
  const copy = JSON.parse(JSON.stringify(report || {}));
  delete copy.userName;
  delete copy.UserName;
  if (copy.deviceName) copy.deviceName = "redacted-device";
  if (copy.DeviceName) copy.DeviceName = "redacted-device";
  if (copy.computerName) copy.computerName = "redacted-device";
  if (copy.ComputerName) copy.ComputerName = "redacted-device";
  if (Array.isArray(copy.checks)) {
    for (const check of copy.checks) {
      if (Array.isArray(check.details)) {
        check.details = check.details.map(d => String(d).replace(/[A-Z]:\\Users\\[^\\\s]+/gi, "C:\\Users\\[redacted]"));
      }
    }
  }
  return copy;
}

function getChecks(report) {
  if (!report) return [];
  if (Array.isArray(report.checks)) return report.checks;
  if (Array.isArray(report.Checks)) return report.Checks;
  return [];
}

function isActionable(status, name) {
  return status === "Warn" || status === "Fail" || (status === "Skipped" && name === "System Integrity");
}

function createFixPlan(report) {
  const checks = getChecks(report);
  const steps = [];
  for (const check of checks) {
    const name = check.name || check.Name;
    const status = check.status || check.Status;
    if (!isActionable(status, name)) continue;
    const actionId = CHECK_TO_ACTION[name];
    if (!actionId || !SAFE_ACTIONS[actionId]) continue;
    const action = SAFE_ACTIONS[actionId];
    steps.push({
      action: actionId,
      title: action.title,
      checkName: name,
      reason: check.message || check.Message || "This item needs attention.",
      risk: action.risk,
      requiresAdmin: action.requiresAdmin,
      tool: action.tool
    });
  }

  return {
    mode: "safe_repair_plan",
    availableModes: [
      "guide_only",
      "approved_local_changes"
    ],
    summary: steps.length
      ? `System Guardian found ${steps.length} approved repair step(s).`
      : "No approved automatic repair steps were found.",
    riskLevel: steps.some(s => s.requiresAdmin) ? "needs_admin" : "low",
    allowedActions: Object.keys(SAFE_ACTIONS),
    blockedActions: [
      "edit_registry_freely",
      "delete_system_files",
      "disable_defender",
      "disable_windows_update",
      "run_unknown_scripts",
      "install_random_drivers",
      "reset_pc",
      "clean_reinstall"
    ],
    steps,
    guideSteps: steps.map((step, index) => ({
      order: index + 1,
      title: step.title,
      why: step.reason,
      tool: step.tool,
      risk: step.risk,
      changesSystem: false
    }))
  };
}

function publicUser(user) {
  return { id: user.id, email: user.email, createdAt: user.createdAt };
}

async function handle(req, res) {
  if (req.method === "OPTIONS") {
    return json(res, 200, { ok: true });
  }

  const db = readDb();
  const url = new URL(req.url, `http://${req.headers.host}`);
  const route = `${req.method} ${url.pathname}`;

  try {
    if (route === "GET /health") {
      return json(res, 200, { ok: true, service: "system-guardian-backend", version: "1.0.0" });
    }

    if (route === "POST /auth/register") {
      const body = await readJson(req);
      const email = String(body.email || "").trim().toLowerCase();
      const password = String(body.password || "");
      if (!email.includes("@") || password.length < 8) {
        return json(res, 400, { error: "email_and_8_char_password_required" });
      }
      if (db.users.some(u => u.email === email)) {
        return json(res, 409, { error: "email_already_registered" });
      }
      const user = { id: id("usr"), email, passwordHash: hashPassword(password), createdAt: new Date().toISOString() };
      db.users.push(user);
      writeDb(db);
      const token = signToken({ sub: user.id, exp: Date.now() + 1000 * 60 * 60 * 24 * 30 });
      return json(res, 201, { user: publicUser(user), token });
    }

    if (route === "POST /auth/login") {
      const body = await readJson(req);
      const email = String(body.email || "").trim().toLowerCase();
      const user = db.users.find(u => u.email === email);
      if (!user || !verifyPassword(String(body.password || ""), user.passwordHash)) {
        return json(res, 401, { error: "invalid_login" });
      }
      const token = signToken({ sub: user.id, exp: Date.now() + 1000 * 60 * 60 * 24 * 30 });
      return json(res, 200, { user: publicUser(user), token });
    }

    if (route === "GET /me") {
      const auth = requireAuth(req, res, db);
      if (!auth) return;
      return json(res, 200, { user: publicUser(auth.user) });
    }

    if (route === "POST /devices/register") {
      const auth = requireAuth(req, res, db);
      if (!auth) return;
      const body = await readJson(req);
      const device = {
        id: id("dev"),
        userId: auth.user.id,
        deviceName: String(body.deviceName || "Windows PC").slice(0, 120),
        windowsVersion: String(body.windowsVersion || "").slice(0, 120),
        appVersion: String(body.appVersion || "").slice(0, 40),
        lastSeen: new Date().toISOString(),
        createdAt: new Date().toISOString()
      };
      db.devices.push(device);
      writeDb(db);
      return json(res, 201, { device });
    }

    if (route === "GET /devices") {
      const auth = requireAuth(req, res, db);
      if (!auth) return;
      return json(res, 200, { devices: db.devices.filter(d => d.userId === auth.user.id) });
    }

    if (route === "POST /reports") {
      const auth = requireAuth(req, res, db);
      if (!auth) return;
      const body = await readJson(req);
      const report = {
        id: id("rep"),
        userId: auth.user.id,
        deviceId: body.deviceId || null,
        scanSummary: body.scanSummary || null,
        fullReport: sanitizeReport(body.fullReport || body.report || body),
        createdAt: new Date().toISOString()
      };
      db.reports.push(report);
      writeDb(db);
      return json(res, 201, { reportId: report.id, report });
    }

    if (req.method === "GET" && url.pathname.startsWith("/reports/")) {
      const auth = requireAuth(req, res, db);
      if (!auth) return;
      const reportId = url.pathname.split("/")[2];
      const report = db.reports.find(r => r.id === reportId && r.userId === auth.user.id);
      if (!report) return json(res, 404, { error: "report_not_found" });
      return json(res, 200, { report });
    }

    if (route === "POST /reports/analyze") {
      const auth = requireAuth(req, res, db);
      if (!auth) return;
      const body = await readJson(req);
      const report = body.reportId
        ? (db.reports.find(r => r.id === body.reportId && r.userId === auth.user.id) || {}).fullReport
        : sanitizeReport(body.report || body.fullReport || body);
      return json(res, 200, { analysis: createFixPlan(report) });
    }

    if (route === "GET /repair-guidance") {
      return json(res, 200, { safeActions: SAFE_ACTIONS });
    }

    if (route === "GET /app/update") {
      return json(res, 200, {
        latestVersion: "1.0.0",
        updateRequired: false,
        downloadUrl: "https://github.com/Rafay-Mo/systemguard/releases",
        notes: "Initial System Guardian release."
      });
    }

    if (route === "POST /crash-report") {
      const body = await readJson(req);
      db.crashReports.push({
        id: id("crash"),
        appVersion: String(body.appVersion || "").slice(0, 40),
        message: redactLocalPaths(String(body.message || "")).slice(0, 4000),
        stack: redactLocalPaths(String(body.stack || "")).slice(0, 12000),
        createdAt: new Date().toISOString()
      });
      writeDb(db);
      return json(res, 201, { ok: true });
    }

    return json(res, 404, { error: "not_found", route });
  } catch (error) {
    return json(res, 500, { error: "server_error", message: error.message });
  }
}

function redactLocalPaths(value) {
  return value.replace(/[A-Z]:\\Users\\[^\\\s]+/gi, "C:\\Users\\[redacted]");
}

ensureDb();
http.createServer(handle).listen(PORT, HOST, () => {
  if (!process.env.SYSTEM_GUARDIAN_SECRET) {
    console.warn("SYSTEM_GUARDIAN_SECRET is not set; using an ephemeral development secret.");
  }
  console.log(`System Guardian backend running at http://${HOST}:${PORT}`);
});
