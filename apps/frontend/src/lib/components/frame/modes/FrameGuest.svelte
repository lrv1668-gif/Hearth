<script lang="ts">
    import { Wifi } from '@lucide/svelte';
    import { getFrameScale } from '#lib/frame/layout.ts';
    import { qrModules, wifiQrText } from '#lib/frame/guest.ts';
    import type { GuestSecurity } from '#lib/constants/frameModes.ts';

    let {
        title,
        network,
        password,
        security,
        notes,
        portrait = false,
    }: {
        title: string;
        network: string;
        password: string;
        security: GuestSecurity;
        notes: string;
        portrait?: boolean;
    } = $props();

    const scale = getFrameScale();
    const heading = $derived(title.trim() || 'Welcome');
    const lines = $derived(notes.split('\n').filter((l) => l.trim()));
    const showPassword = $derived(security !== 'nopass' && password !== '');

    // The QR code: its modules plus a 4-module quiet zone, which scanners need. Each module
    // is a whole number of pixels so its edges land exactly on the panel's pixels.
    const QUIET = 4;
    const qrText = $derived(wifiQrText(network, password, security));
    const modules = $derived(qrText ? qrModules(qrText) : null);
    const side = $derived(modules ? modules.length + 2 * QUIET : 0);
    const modulePx = $derived(side ? Math.max(1, Math.floor(((portrait ? 720 : 640) * scale()) / side)) : 0);
    // One path of unit squares for the dark modules, offset by the quiet zone.
    const path = $derived(
        modules
            ?.flatMap((row, y) => row.map((dark, x) => (dark ? `M${x + QUIET} ${y + QUIET}h1v1h-1z` : '')))
            .join('') ?? ''
    );
</script>

<!--
@component
Guest mode for the e-paper frame: a welcome for visitors. A heading in the display face
over a --block rule, a few notes, and the guest Wi-Fi — its name and password in type
and as a QR code a phone's camera joins from (WIFI: format, see lib/frame/guest.ts).
The code is black modules on a white plate in every theme: scanners need dark on
light, and both are pure Spectra inks, so the ordered dither leaves every module exact.
The network comes from the shared frame settings, never the /frame URL.
-->
<div
    class="absolute inset-x-24 inset-y-20 flex items-center gap-20 {portrait
        ? 'flex-col justify-center text-center'
        : ''}"
>
    <div class="min-w-0 {portrait ? '' : 'flex-1'}">
        <p class="type-hero font-(family-name:--display-family) leading-[1.05] font-semibold">{heading}</p>
        <div class="mt-6 mb-8 h-3 w-24 bg-(--block) {portrait ? 'mx-auto' : ''}"></div>
        {#each lines as line, i (i)}
            <p class="type-subtitle mb-3 leading-tight">{line}</p>
        {/each}
        {#if network}
            <div class="mt-10 flex flex-col gap-3 {portrait ? 'items-center' : ''}">
                <p class="type-label flex items-center gap-3 font-semibold tracking-[0.16em] text-(--text-2) uppercase">
                    <Wifi class="icon-sm" />Wi-Fi
                </p>
                <p class="type-title font-semibold break-all">{network}</p>
                {#if showPassword}
                    <p class="type-title break-all">
                        <span class="type-label mr-3 text-(--text-2)">Password</span>{password}
                    </p>
                {/if}
            </div>
        {:else}
            <p class="type-body mt-10 text-(--text-2)">Add your guest Wi-Fi in Settings → Frame.</p>
        {/if}
    </div>
    {#if modules}
        <svg
            class="qr shrink-0"
            width={modulePx * side}
            height={modulePx * side}
            viewBox="0 0 {side} {side}"
            shape-rendering="crispEdges"
            role="img"
            aria-label="QR code to join {network}"
        >
            <rect width={side} height={side} fill="#ffffff" />
            <path d={path} fill="#000000" />
        </svg>
    {/if}
</div>

<style>
    /* Prose, not figures: the frame's tabular numerals also widen hyphens ("Wi-Fi") */
    div {
        font-variant-numeric: normal;
    }
</style>
