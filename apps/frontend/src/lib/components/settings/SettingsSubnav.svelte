<script lang="ts">
    // The active section's cards, listed under it in the desktop sidebar. It reads them off
    // the page — every card the Settings page renders through its `card` snippet carries
    // `data-settings-card` — so a new card shows up here without touching this list, and
    // cards that come and go (Ambient's photo source) follow along.
    let { container }: { container: HTMLElement | undefined } = $props();

    type Item = { id: string; title: string };

    let items = $state<Item[]>([]);
    let current = $state<string | undefined>();
    // While a click's smooth scroll runs, keep the clicked item lit rather than every card
    // it passes on the way.
    let scrollingTo: string | undefined;

    // How far below the top of the scroll area a card counts as reached — its scroll-mt-6
    // plus a little slack.
    const REACHED_PX = 32;

    function collect(root: HTMLElement) {
        const next = [...root.querySelectorAll<HTMLElement>('[data-settings-card]')].map((el) => ({
            id: el.id,
            title: el.dataset.settingsCard ?? '',
        }));
        if (next.map((i) => i.id).join() !== items.map((i) => i.id).join()) items = next;
    }

    function spy(root: HTMLElement) {
        if (scrollingTo || items.length === 0) return;
        // A short last card can never reach the top, so the bottom of the page lights it.
        if (root.scrollTop + root.clientHeight >= root.scrollHeight - 2) {
            current = items.at(-1)!.id;
            return;
        }
        const top = root.getBoundingClientRect().top + REACHED_PX;
        let reached = items[0].id;
        for (const item of items) {
            const el = document.getElementById(item.id);
            if (el && el.getBoundingClientRect().top <= top) reached = item.id;
        }
        current = reached;
    }

    $effect(() => {
        const root = container;
        if (!root) return;
        collect(root);
        spy(root);
        const observer = new MutationObserver(() => {
            collect(root);
            spy(root);
        });
        observer.observe(root, { childList: true, subtree: true });
        const onScroll = () => spy(root);
        const onScrollEnd = () => {
            scrollingTo = undefined;
            spy(root);
        };
        root.addEventListener('scroll', onScroll, { passive: true });
        root.addEventListener('scrollend', onScrollEnd);
        return () => {
            observer.disconnect();
            root.removeEventListener('scroll', onScroll);
            root.removeEventListener('scrollend', onScrollEnd);
        };
    });

    function go(id: string) {
        const el = document.getElementById(id);
        if (!el || !container) return;
        current = id;
        const smooth = !matchMedia('(prefers-reduced-motion: reduce)').matches;
        scrollingTo = id;
        el.scrollIntoView({ behavior: smooth ? 'smooth' : 'auto', block: 'start' });
        // scrollend unlocks the spy; this covers a click that didn't scroll (no scrollend).
        setTimeout(() => {
            if (scrollingTo === id) scrollingTo = undefined;
        }, 1000);
        // Bring keyboard focus along, so Tab carries on inside the card.
        el.focus({ preventScroll: true });
    }
</script>

{#if items.length > 1}
    <ul class="mt-0.5 mb-1 ml-[1.375rem] flex flex-col border-l-2 border-(--border) pl-2">
        {#each items as item (item.id)}
            {@const isCurrent = current === item.id}
            <li>
                <button
                    type="button"
                    onclick={() => go(item.id)}
                    aria-current={isCurrent ? 'location' : undefined}
                    class="type-label -ml-[calc(0.5rem+2px)] w-[calc(100%+0.5rem+2px)] border-l-2 py-1.5 pr-2 pl-[calc(1rem-2px)] text-left transition-colors
                        {isCurrent
                        ? 'border-(--accent) font-medium text-(--text-1)'
                        : 'border-transparent text-(--text-2) hover:text-(--text-1)'}"
                >
                    {item.title}
                </button>
            </li>
        {/each}
    </ul>
{/if}
