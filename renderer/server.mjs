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
// data-ready="true". One render at a time: the host may be a Raspberry Pi.
import { createServer } from 'node:http';
import { createCanvas, loadImage } from 'canvas';
import { chromium } from 'playwright';
import { dither, paintPaper } from './dither.mjs';

const PORT = Number(process.env.PORT ?? 3000);
const FRAME_BASE_URL = process.env.FRAME_BASE_URL ?? 'http://caddy';
// The frame draws the date and time from the browser's clock, so the browser needs the
// household's zone — a container otherwise runs on UTC.
const TIMEZONE = process.env.TZ || undefined;
// /frame flips data-ready on its own deadline; this only catches a page that never loads.
const READY_TIMEOUT_MS = 30_000;
const MAX_SIDE = 4000;

let browser;
async function getBrowser() {
    if (!browser?.isConnected()) {
        // /dev/shm is 64 MB in a container by default — too small for a full-panel page.
        browser = await chromium.launch({ args: ['--disable-dev-shm-usage'] });
    }
    return browser;
}

async function screenshot(path, width, height) {
    const context = await (
        await getBrowser()
    ).newContext({
        viewport: { width, height },
        deviceScaleFactor: 1,
        timezoneId: TIMEZONE,
    });
    try {
        const page = await context.newPage();
        await page.goto(new URL(path, FRAME_BASE_URL).href);
        await page.waitForSelector('[data-ready="true"]', { timeout: READY_TIMEOUT_MS });
        return await page.screenshot({ type: 'png' });
    } finally {
        await context.close();
    }
}

let queue = Promise.resolve();
function serialized(task) {
    const result = queue.then(task);
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
    const kernel = url.searchParams.get('kernel') ?? undefined;
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
    try {
        const png = await serialized(async () =>
            render(await screenshot(path, width, height), { method, kernel, look })
        );
        res.writeHead(200, { 'Content-Type': 'image/png', 'Cache-Control': 'no-store' }).end(png);
    } catch (error) {
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
