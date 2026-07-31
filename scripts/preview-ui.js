const http = require("http");
const fs = require("fs");
const path = require("path");

const root = path.resolve(__dirname, "..", "src", "ui");
const rootPrefix = root.endsWith(path.sep) ? root : root + path.sep;
const port = Number(process.argv[2] || 8765);

http.createServer((request, response) => {
  const relative = decodeURIComponent(request.url === "/" ? "/SystemGuardianUI.html" : request.url);
  const target = path.resolve(root, "." + relative);
  if (target !== root && !target.startsWith(rootPrefix)) {
    response.writeHead(403);
    response.end("Forbidden");
    return;
  }

  fs.readFile(target, (error, data) => {
    if (error) {
      response.writeHead(404);
      response.end("Not found");
      return;
    }
    response.setHeader("Content-Type", target.endsWith(".html") ? "text/html; charset=utf-8" : "application/octet-stream");
    response.end(data);
  });
}).listen(port, "127.0.0.1", () => {
  console.log(`System Guardian preview: http://127.0.0.1:${port}`);
});
