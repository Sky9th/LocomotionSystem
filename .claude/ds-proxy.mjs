/**
 * DeepSeek Claude Code Proxy v3
 *
 * 功能:
 *   1. 修复子 Agent thinking.type "disabled" → "enabled"（DeepSeek 兼容）
 *   2. 彩色打印每个请求的关键参数，方便验证模型/参数是否正确
 *
 * 用法: node .claude/ds-proxy.mjs
 * 环境变量:
 *   DS_PROXY_PORT  — 监听端口 (默认 11434)
 *   DS_PROXY_QUIET — 设为 1 只打印错误和修复动作，不打印正常请求
 *   DS_PROXY_NOFIX — 设为 1 跳过 thinking.type 修复
 */

import http from "node:http";
import https from "node:https";

// ── 配置 ──────────────────────────────────────────────────────────
const LISTEN_PORT = parseInt(process.env.DS_PROXY_PORT || "11434");
const TARGET_HOST = "api.deepseek.com";
const TARGET_PORT = 443;
const QUIET = process.env.DS_PROXY_QUIET === "1";
const NOFIX = process.env.DS_PROXY_NOFIX === "1";

// ── ANSI 颜色 ─────────────────────────────────────────────────────
const C = {
  reset:   "\x1b[0m",
  dim:     "\x1b[2m",
  bold:    "\x1b[1m",
  cyan:    "\x1b[36m",
  green:   "\x1b[32m",
  yellow:  "\x1b[33m",
  red:     "\x1b[31m",
  magenta: "\x1b[35m",
  blue:    "\x1b[34m",
  white:   "\x1b[37m",
};

// ── 工具函数 ──────────────────────────────────────────────────────
const ts = () => new Date().toISOString().replace("T"," ").slice(0,23);

function fmt(n) {
  if (n == null) return "–";
  if (n >= 1e6) return (n/1e6).toFixed(1)+"M";
  if (n >= 1e3) return (n/1e3).toFixed(1)+"K";
  return String(n);
}

function ms(ms) {
  if (ms < 1000) return Math.round(ms)+"ms";
  return (ms/1000).toFixed(2)+"s";
}

function parseBody(str) {
  try { return JSON.parse(str); }
  catch { return null; }
}

// ── 核心 ──────────────────────────────────────────────────────────
const server = http.createServer((clientReq, clientRes) => {
  const t0 = performance.now();
  const chunks = [];

  clientReq.on("data", c => chunks.push(c));
  clientReq.on("end", () => {
    let body     = Buffer.concat(chunks);
    let bodyStr  = body.toString("utf-8");
    let fixed    = false;
    const obj    = parseBody(bodyStr);

    // ── 修复 thinking.type ──────────────────────────────────────
    if (!NOFIX && bodyStr.includes('"thinking"') && bodyStr.includes('"disabled"')) {
      bodyStr = bodyStr.replace(/"type"\s*:\s*"disabled"/g, '"type":"enabled"');
      body    = Buffer.from(bodyStr, "utf-8");
      fixed   = true;
    }

    // ── 重新解析（可能已修改）──────────────────────────────────
    const obj2 = parseBody(bodyStr);

    // ── 请求日志 ────────────────────────────────────────────────
    if (!QUIET && obj2) {
      const method = clientReq.method;
      const path   = clientReq.url;
      const model  = obj2.model || "?";

      console.log(`\n${C.bold}${C.white}${ts()}${C.reset}`);
      console.log(`${C.bold}${C.cyan}┌─ ${method} ${path}${C.reset}`);

      // Model
      const hasSuffix  = /\[1m\]/.test(model);
      const modelBase  = model.replace(/\[1m\]/, "");
      const modelColor = model.includes("flash") ? C.green :
                         model.includes("pro")   ? C.blue  : C.yellow;
      console.log(`${C.dim}│${C.reset} Model:  ${C.bold}${modelColor}${modelBase}${C.reset}` +
                  (hasSuffix ? ` ${C.magenta}[1m]${C.reset}` : ""));

      // Thinking 配置
      if (obj2.thinking) {
        const t      = obj2.thinking;
        const tColor = t.type === "enabled" ? C.green : C.yellow;
        let line     = `Thinking: ${tColor}${t.type}${C.reset}`;
        if (t.budget_tokens) line += ` ${C.dim}budget=${fmt(t.budget_tokens)}${C.reset}`;
        if (fixed) line += ` ${C.magenta}◀ FIXED (disabled→enabled)${C.reset}`;
        console.log(`${C.dim}│${C.reset} ${line}`);
      }

      // 生成参数
      const params = [];
      if (obj2.temperature !== undefined) params.push(`temp=${obj2.temperature}`);
      if (obj2.max_tokens)               params.push(`max_tok=${fmt(obj2.max_tokens)}`);
      if (obj2.top_p !== undefined)      params.push(`top_p=${obj2.top_p}`);
      if (obj2.top_k !== undefined)      params.push(`top_k=${obj2.top_k}`);
      if (params.length) console.log(`${C.dim}│${C.reset} Params:  ${params.join("  ")}`);

      // Stream
      console.log(`${C.dim}│${C.reset} Stream:  ${obj2.stream ? C.green+"true"+C.reset : C.dim+"false"+C.reset}`);

      // Messages
      if (obj2.messages?.length) {
        const msgs     = obj2.messages;
        const count    = msgs.length;
        const lastMsgs = msgs.slice(-4);
        const preview  = lastMsgs.map(m => {
          const role = m.role.slice(0,4);
          const content = typeof m.content === "string"
            ? m.content.slice(0, 60).replace(/\n/g, "↵")
            : (Array.isArray(m.content) ? `[${m.content.length} blocks]` : "[?]");
          return `${C.dim}${role}${C.reset}:${content}`;
        }).join(`\n${C.dim}│        ${C.reset}`);
        const totalChars = msgs.reduce((s,m) => s + (
          typeof m.content === "string" ? m.content.length : JSON.stringify(m.content).length
        ), 0);
        console.log(`${C.dim}│${C.reset} Msgs:    ${count}  ${C.dim}(${fmt(totalChars)} chars)${C.reset}`);
        console.log(`${C.dim}│        ${C.reset}${preview}`);
      }

      // System prompt
      if (obj2.system) {
        const s = typeof obj2.system === "string" ? obj2.system : JSON.stringify(obj2.system);
        console.log(`${C.dim}│${C.reset} System:  ${fmt(s.length)} chars`);
      }

      // Tools
      if (obj2.tools?.length) {
        const names = obj2.tools.map(t => t.name || t.function?.name || "?").slice(0, 12);
        console.log(`${C.dim}│${C.reset} Tools:   ${obj2.tools.length}  [${names.join(", ")}${obj2.tools.length > 12 ? " …" : ""}]`);
      }

      // Body size
      console.log(`${C.dim}│${C.reset} Body:    ${fmt(bodyStr.length)} bytes`);
    } else if (!QUIET) {
      // 非 JSON body（count_tokens 等小请求）
      console.log(`${C.dim}${ts()} ${clientReq.method} ${clientReq.url}  (${bodyStr.length}B)${C.reset}`);
    }

    // ── 转发 ────────────────────────────────────────────────────
    const options = {
      hostname: TARGET_HOST,
      port:     TARGET_PORT,
      path:     clientReq.url,
      method:   clientReq.method,
      headers:  {
        ...clientReq.headers,
        host:           TARGET_HOST,
        "content-length": Buffer.byteLength(bodyStr),
      },
    };

    const proxyReq = https.request(options, (proxyRes) => {
      const elapsed = performance.now() - t0;
      const sc      = proxyRes.statusCode;
      const scColor = sc < 300 ? C.green : sc < 500 ? C.yellow : C.red;

      if (!QUIET) {
        const tags = [];
        if (fixed) tags.push(`${C.magenta}fix${C.reset}`);
        console.log(`${C.bold}${C.cyan}└─ ${scColor}${sc} ${proxyRes.statusMessage || ""}${C.reset}  ${C.dim}${ms(elapsed)}${C.reset}` +
                    (tags.length ? `  [${tags.join(" ")}]` : ""));
      }

      // 如果有错误（4xx/5xx），打印响应体前 500 字符方便排查
      if (sc >= 400) {
        const errChunks = [];
        proxyRes.on("data", c => errChunks.push(c));
        proxyRes.on("end", () => {
          const errBody = Buffer.concat(errChunks).toString("utf-8").slice(0, 500);
          if (errBody) console.log(`${C.red}┊ ${errBody}${C.reset}`);
        });
      }

      clientRes.writeHead(sc, proxyRes.headers);
      proxyRes.pipe(clientRes);
    });

    proxyReq.on("error", (err) => {
      const elapsed = performance.now() - t0;
      console.log(`${C.bold}${C.red}└─ ERR ${err.message}${C.reset}  ${C.dim}${ms(elapsed)}${C.reset}`);
      if (!clientRes.headersSent) {
        clientRes.writeHead(502);
        clientRes.end("Proxy error");
      }
    });

    proxyReq.write(body);
    proxyReq.end();
  });
});

// ── 启动 ──────────────────────────────────────────────────────────
server.listen(LISTEN_PORT, "127.0.0.1", () => {
  console.log("");
  console.log(`${C.bold}${C.green}  DeepSeek CC Proxy v3${C.reset}`);
  console.log(`${C.dim}  ──────────────────────────────────────────${C.reset}`);
  console.log(`${C.dim}  Listen:  http://127.0.0.1:${LISTEN_PORT}${C.reset}`);
  console.log(`${C.dim}  Target:  https://${TARGET_HOST}${C.reset}`);
  console.log(`${C.dim}  Fix:     thinking.type "disabled" → "enabled"${C.reset}`);
  console.log(`${C.dim}  ──────────────────────────────────────────${C.reset}`);
  console.log("");
});
