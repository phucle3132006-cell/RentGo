import { createServer, request as httpRequest } from 'node:http';
import { request as httpsRequest } from 'node:https';
import { createReadStream } from 'node:fs';
import { stat } from 'node:fs/promises';
import { extname, resolve, sep } from 'node:path';
import { fileURLToPath } from 'node:url';

const root = fileURLToPath(new URL('.', import.meta.url));
const backend = new URL(process.env.RENTGO_BACKEND || 'https://localhost:7076');
const port = Number(process.env.RENTGO_PORT || 5500);
if (!['localhost', '127.0.0.1', '[::1]'].includes(backend.hostname)
    || !['http:', 'https:'].includes(backend.protocol)) {
  throw new Error('RENTGO_BACKEND phải là địa chỉ HTTP(S) loopback.');
}
const files = { '.html': 'text/html; charset=utf-8', '.css': 'text/css; charset=utf-8', '.js': 'text/javascript; charset=utf-8' };
const hopHeaders = ['connection', 'keep-alive', 'proxy-authenticate', 'proxy-authorization', 'te', 'trailer', 'transfer-encoding', 'upgrade'];
function proxyHeaders(headers) {
  const blocked = new Set([...hopHeaders, ...String(headers.connection || '').toLowerCase().split(/\s*,\s*/)]);
  return Object.fromEntries(Object.entries(headers).filter(([name]) => !blocked.has(name)));
}

function proxy(req, res, pathname) {
  const target = new URL(backend);
  target.pathname = pathname;
  target.search = new URL(req.url, 'http://localhost').search;
  const forward = backend.protocol === 'https:' ? httpsRequest : httpRequest;
  const upstream = forward(target, {
    method: req.method, headers: { ...proxyHeaders(req.headers), host: backend.host }
  }, response => {
    const headers = proxyHeaders(response.headers);
    if (headers.location) {
      const redirect = new URL(headers.location, backend);
      if (redirect.origin === backend.origin) headers.location = redirect.pathname + redirect.search;
    }
    res.writeHead(response.statusCode, headers);
    response.pipe(res);
    response.on('error', error => res.destroy(error));
  });
  upstream.setTimeout(18000, () => upstream.destroy(new Error('Timeout')));
  upstream.on('error', error => {
    if (res.headersSent) return res.destroy();
    console.error('Kết nối API:', error.code || error.message);
    res.writeHead(502, { 'Content-Type': 'application/json; charset=utf-8' });
    res.end(JSON.stringify({ message: 'Không kết nối được backend RentGo. Kiểm tra địa chỉ, tiến trình và chứng chỉ local.' }));
  });
  res.on('close', () => upstream.destroy());
  req.pipe(upstream);
}

createServer(async (req, res) => {
  try {
    const pathname = new URL(req.url, 'http://localhost').pathname;
    if (pathname.startsWith('/api/') || pathname.startsWith('/uploads/vehicles/')) {
      return proxy(req, res, pathname);
    }
    if (!['GET', 'HEAD'].includes(req.method)) {
      res.writeHead(405, { Allow: 'GET, HEAD' });
      return res.end();
    }
    const path = resolve(root, '.' + decodeURIComponent(pathname.endsWith('/') ? pathname + 'index.html' : pathname));
    const type = path.endsWith(sep + "mock.js") ? undefined : files[extname(path)];
    const allowed = /^(?:[a-z-]+\.html|admin[\\/][a-z-]+\.html|css[\\/][a-z-]+\.css|js[\\/][a-z-]+\.js)$/;
    if (path.startsWith(root) && type && allowed.test(path.slice(root.length)) && (await stat(path)).isFile()) {
      res.writeHead(200, { 'Content-Type': type, 'Cache-Control': 'no-store', 'X-Content-Type-Options': 'nosniff' });
      if (req.method === 'HEAD') return res.end();
      const stream = createReadStream(path);
      stream.on('error', error => res.destroy(error));
      return stream.pipe(res);
    }
  } catch (error) {
    if (!['ENOENT', 'ENOTDIR'].includes(error.code) && !(error instanceof URIError)) console.error(error.message);
  }
  res.writeHead(404);
  res.end('Not found');
}).listen(port, '127.0.0.1', () => console.log('RentGo http://127.0.0.1:' + port + ' → ' + backend.origin));

