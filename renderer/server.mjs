// Screenshots Hearth's /frame route in headless Chromium and dithers it to the panel's
// six inks, for the Display service.
//
//   GET /render?path=/frame?...&width=1600&height=1200
//       &dither=ordered|errorDiffusion|none   (default none: the raw screenshot)
//       &kernel=floydSteinberg|atkinson|stucki|…  (errorDiffusion only; epdoptimize's names)
//       &look=screen|paper                    (paper: inks repainted as they look on the panel)
//     → image/png
//
// The page is loaded from FRAME_BASE_URL (default http://caddy, whose :80 site serves
// the frontend and every API route the frame calls) and captured once its root sets
// data-ready="true". One render at a time — the host may be a Raspberry Pi — with at most
// one more waiting (503 beyond that); a render whose client has gone is dropped.
import { createServer } from 'node:http';
import { createCanvas, loadImage } from 'canvas';
import { chromium } from 'playwright';
import { dither, KERNELS, paintPaper } from './dither.mjs';

const PORT = Number(process.env.PORT ?? 3000);
const FRAME_BASE_URL = process.env.FRAME_BASE_URL ?? 'http://caddy';
// The frame draws the date and time from the browser's clock, so the browser needs the
// household's zone — a container otherwise runs on UTC.
const TIMEZONE = validTimeZone(process.env.TZ);
// Loading the page and waiting for data-ready share one deadline. /frame flips data-ready
// on its own deadline; this only catches a page that never gets there. Display's
// HttpClient timeout allows for this twice over (a render running plus this one).
const CAPTURE_DEADLINE_MS = 40_000;
const MAX_PENDING = 2;
const MAX_SIDE = 4000;

function validTimeZone(zone) {
    if (!zone) return undefined;
    try {
        new Intl.DateTimeFormat('en', { timeZone: zone });
        return zone;
    } catch {
        console.error(`TZ "${zone}" is not an IANA time zone — drawing the frame in UTC.`);
        return undefined;
    }
}

let browser;
async function getBrowser() {
    if (!browser?.isConnected()) {
        browser = await chromium.launch({
            args: [
                // /dev/shm is 64 MB in a container by default — too small for a full-panel page
                '--disable-dev-shm-usage',
                // Subpixel antialiasing tints glyph edges cyan and pink, which dither into
                // stray inks; greyscale edges are blends of the two inks either side
                '--disable-lcd-text',
            ],
        });
    }
    return browser;
}

async function screenshot(path, width, height, signal) {
    const context = await (
        await getBrowser()
    ).newContext({
        viewport: { width, height },
        deviceScaleFactor: 1,
        timezoneId: TIMEZONE,
    });
    // Closing the context aborts whatever the page is waiting on
    const abort = () => context.close().catch(() => {});
    signal.addEventListener('abort', abort);
    try {
        const deadline = Date.now() + CAPTURE_DEADLINE_MS;
        const page = await context.newPage();
        const response = await page.goto(new URL(path, FRAME_BASE_URL).href, { timeout: CAPTURE_DEADLINE_MS });
        // An error page never sets data-ready — fail now rather than at the deadline
        if (!response?.ok()) throw new Error(`frame page returned ${response?.status() ?? 'nothing'}`);
        await page.waitForSelector('[data-ready="true"]', { timeout: Math.max(1, deadline - Date.now()) });
        return await page.screenshot({ type: 'png' });
    } finally {
        signal.removeEventListener('abort', abort);
        await context.close().catch(() => {});
    }
}

let queue = Promise.resolve();
let pending = 0;
function serialized(task) {
    pending++;
    const result = queue.then(task).finally(() => pending--);
    queue = result.catch(() => {});
    return result;
}

async function render(png, { method, kernel, look }) {
    const image = await loadImage(png);
    const canvas = () => createCanvas(image.width, image.height);
    let result = canvas();
    result.getContext('2d').drawImage(image, 0, 0);
    if (method !== 'none') {
        const dithered = canvas();
        await dither(result, dithered, { method, kernel });
        result = dithered;
    }
    if (look === 'paper') {
        const paper = canvas();
        paintPaper(result, paper);
        result = paper;
    }
    return result.toBuffer('image/png');
}

function size(value) {
    const n = Number(value);
    return Number.isInteger(n) && n > 0 && n <= MAX_SIDE ? n : null;
}

createServer(async (req, res) => {
    const url = new URL(req.url, 'http://renderer');
    if (req.method !== 'GET' || url.pathname !== '/render') {
        res.writeHead(404).end();
        return;
    }
    const path = url.searchParams.get('path') ?? '';
    const width = size(url.searchParams.get('width'));
    const height = size(url.searchParams.get('height'));
    const method = url.searchParams.get('dither') ?? 'none';
    const kernel = url.searchParams.get('kernel') ?? 'floydSteinberg';
    const look = url.searchParams.get('look') ?? 'screen';
    // Only the frame route — this is not a general-purpose browser.
    if (!/^\/frame(\?|$)/.test(path) || !width || !height) {
        res.writeHead(400).end('path must be /frame?..., width and height 1–4000');
        return;
    }
    if (!['ordered', 'errorDiffusion', 'none'].includes(method) || !['screen', 'paper'].includes(look)) {
        res.writeHead(400).end('dither must be ordered, errorDiffusion or none; look screen or paper');
        return;
    }
    if (!KERNELS.includes(kernel)) {
        res.writeHead(400).end(`kernel must be one of ${KERNELS.join(', ')}`);
        return;
    }
    if (pending >= MAX_PENDING) {
        res.writeHead(503).end('renderer busy');
        return;
    }

    // The client gave up (Display's timeout, or the preview moved on): drop the render
    const gone = new AbortController();
    res.on('close', () => {
        if (!res.writableEnded) gone.abort();
    });
    try {
        const png = await serialized(async () => {
            if (gone.signal.aborted) return null;
            return render(await screenshot(path, width, height, gone.signal), { method, kernel, look });
        });
        if (gone.signal.aborted) return;
        res.writeHead(200, { 'Content-Type': 'image/png', 'Cache-Control': 'no-store' }).end(png);
    } catch (error) {
        if (gone.signal.aborted) return;
        console.error(`render ${path} failed:`, error);
        res.writeHead(error?.name === 'TimeoutError' ? 504 : 502).end(String(error?.message ?? error));
    }
}).listen(PORT, () =>
    console.log(`renderer listening on :${PORT}, frames from ${FRAME_BASE_URL} in ${TIMEZONE ?? 'UTC'}`)
);

for (const signal of ['SIGTERM', 'SIGINT']) {
    process.on(signal, async () => {
        await browser?.close();
        process.exit(0);
    });
}
