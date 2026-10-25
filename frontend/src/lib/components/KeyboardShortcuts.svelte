<script lang="ts">
    // App-wide shortcuts: "G then D/C/A/S" to go to a page, and "?" for the sheet that
    // lists every shortcut that works here. Page-level ones (Settings) live on their page.
    import { goto } from '$app/navigation';
    import { page } from '$app/state';
    import { tinykeys } from 'tinykeys';
    import Modal from '$lib/components/ui/Modal.svelte';
    import { formatKeys, goToRoutes, ignoreWhileTyping, isMacPlatform, shortcuts } from '$lib/shortcuts';

    const mac = isMacPlatform(navigator.platform);

    let open = $state(false);

    // Ambient exits on any key, so it gets none of these.
    const enabled = $derived(!page.url.pathname.startsWith('/ambient'));
    const listed = $derived(
        shortcuts.filter((s) => s.scope === 'global' || (s.scope === 'settings' && page.url.pathname === '/settings'))
    );

    $effect(() => {
        if (!enabled) return;
        return tinykeys(
            window,
            {
                ...Object.fromEntries(
                    goToRoutes.map((r) => [
                        `g ${r.key}`,
                        (e: KeyboardEvent) => {
                            e.preventDefault();
                            goto(r.href);
                        },
                    ])
                ),
                '[Shift]+?': (e) => {
                    e.preventDefault();
                    open = true;
                },
            },
            // A little longer than tinykeys' 1 s, for the second key of "G then …".
            { ignore: ignoreWhileTyping, timeout: 1500 }
        );
    });
</script>

{#snippet kbd(keys: readonly string[])}
    <kbd
        class="type-label inline-flex min-w-7 items-center justify-center rounded-md border-2 border-(--border) bg-(--surface-hi) px-1.5 py-0.5 font-mono text-(--text-1)"
        >{formatKeys(keys, mac)}</kbd
    >
{/snippet}

<Modal bind:open title="Keyboard shortcuts" size="sm">
    <ul class="flex flex-col gap-3">
        {#each listed as s (s.description)}
            <li class="flex items-center justify-between gap-4">
                <span class="type-body text-(--text-1)">{s.description}</span>
                <span class="type-label flex shrink-0 items-center gap-1.5 text-(--text-2)">
                    {#each s.keys as keys, i}
                        {#if i > 0}<span>{s.join ?? 'then'}</span>{/if}
                        {@render kbd(keys)}
                    {/each}
                </span>
            </li>
        {/each}
    </ul>
    <p class="type-label mt-4 text-(--text-2)">Single keys pause while you type in a field.</p>
</Modal>
