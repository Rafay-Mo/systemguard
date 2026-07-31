const fs = require("fs");
const path = require("path");

const root = path.resolve(__dirname, "..");
const uiPath = path.join(root, "src", "ui", "SystemGuardianUI.html");
const html = fs.readFileSync(uiPath, "utf8");

const scriptMatch = html.match(/<script>([\s\S]*?)<\/script>\s*<\/body>/i);
if (!scriptMatch) throw new Error("Could not find the inline application script.");
new Function(scriptMatch[1]);

const requiredIds = [
  "overviewPage",
  "reportPage",
  "scanButton",
  "reportContent",
  "repairModal",
  "confirmRepair"
];

for (const id of requiredIds) {
  const matches = html.match(new RegExp('id="' + id + '"', "g")) || [];
  if (matches.length !== 1) {
    throw new Error(`Expected one #${id} element, found ${matches.length}.`);
  }
}

if (/<(script|link)[^>]+https?:\/\//i.test(html)) {
  throw new Error("The standalone UI must not depend on remote scripts or stylesheets.");
}

console.log(`UI source verified: ${requiredIds.length} required elements and valid JavaScript syntax.`);
