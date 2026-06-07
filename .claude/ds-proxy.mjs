/**
 * DeepSeek Claude Code Proxy
 * 修复 Claude Code 2.1.166+ 子 Agent 不可用问题
 * 将子 Agent 请求中的 thinking.type: "disabled" → "enabled"
 *
 * 用法: node .claude/ds-proxy.mjs
 * 默认监听 localhost:11434，转发到 api.deepseek.com
 */

import http from "node:http";
import https from "node:https";

const LISTEN_PORT = parseInt(process.env.DS_PROXY_PORT || "11434");
const TARGET_HOST = "api.deepseek.com";
const TARGET_PORT = 443;

const server = http.createServer((clientReq, clientRes) => {
  const chunks = [];
  clientReq.on("data", (chunk) => chunks.push(chunk));
  clientReq.on("end", () => {
    let body = Buffer.concat(chunks);
    let bodyStr = body.toString("utf-8");

    // 修复: thinking.type "disabled" → "enabled"
    if (bodyStr.includes('"thinking"') && bodyStr.includes('"disabled"')) {
      const before = bodyStr;
      bodyStr = bodyStr.replace(/"type"\s*:\s*"disabled"/g, '"type":"enabled"');
      body = Buffer.from(bodyStr, "utf-8");
      console.log("[proxy] FIXED thinking.type: disabled → enabled");
    }

    const options = {
      hostname: TARGET_HOST,
      port: TARGET_PORT,
      path: clientReq.url,
      method: clientReq.method,
      headers: {
        ...clientReq.headers,
        host: TARGET_HOST,
        "content-length": Buffer.byteLength(bodyStr),
      },
    };

    const proxyReq = https.request(options, (proxyRes) => {
      clientRes.writeHead(proxyRes.statusCode, proxyRes.headers);
      proxyRes.pipe(clientRes);
    });

    proxyReq.on("error", (err) => {
      console.error("[proxy] Error:", err.message);
      clientRes.writeHead(502);
      clientRes.end("Proxy error");
    });

    proxyReq.write(body);
    proxyReq.end();
  });
});

server.listen(LISTEN_PORT, "127.0.0.1", () => {
  console.log(`[proxy] DeepSeek CC Proxy listening on http://127.0.0.1:${LISTEN_PORT}`);
  console.log(`[proxy] Forwarding to https://${TARGET_HOST}`);
  console.log(`[proxy] Fix: thinking.type "disabled" → "enabled"`);
});