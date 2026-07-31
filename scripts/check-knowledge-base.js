"use strict";

const fs = require("fs");
const path = require("path");

const root = path.resolve(__dirname, "..");
const file = path.join(root, "data", "windows-troubleshooting-knowledge-base.json");
const required = [
  "id", "category", "title", "symptoms", "error_codes", "diagnostics",
  "safe_actions", "automation_level", "stop_conditions", "avoid", "confidence", "sources"
];
const validConfidence = new Set(["confirmed", "supported_general", "watchlist"]);

if (!fs.existsSync(file)) throw new Error(`Missing knowledge base: ${file}`);
const data = JSON.parse(fs.readFileSync(file, "utf8"));
if (!Array.isArray(data.issue_families) || data.issue_families.length === 0) {
  throw new Error("Knowledge base has no issue families.");
}

const ids = new Set();
for (const issue of data.issue_families) {
  for (const field of required) {
    if (!(field in issue)) throw new Error(`${issue.id || "Unknown issue"} is missing ${field}.`);
  }
  if (ids.has(issue.id)) throw new Error(`Duplicate issue id: ${issue.id}`);
  ids.add(issue.id);
  if (!Number.isInteger(issue.automation_level) || issue.automation_level < 0 || issue.automation_level > 4) {
    throw new Error(`${issue.id} has an invalid automation level.`);
  }
  if (!validConfidence.has(issue.confidence)) throw new Error(`${issue.id} has invalid confidence.`);
  if (!Array.isArray(issue.sources) || issue.sources.some(source => !/^https:\/\//i.test(source))) {
    throw new Error(`${issue.id} must contain HTTPS sources only.`);
  }
  if (issue.confidence === "watchlist" && issue.automation_level < 4) {
    throw new Error(`${issue.id} is watchlist-only and must remain manual-only.`);
  }
}

console.log(`Knowledge base verified: ${ids.size} unique issue families.`);
