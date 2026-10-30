<script lang="ts">
    import { CalendarDays, Image, Keyboard, LayoutList, Settings } from '@lucide/svelte';
    import { page } from '$app/state';
    import { showShortcuts } from '#lib/components/KeyboardShortcuts.svelte';
    import IconButton from '#lib/components/ui/IconButton.svelte';
    import { kioskStore } from '#lib/stores/KioskStore.svelte.ts';

    const links = [
        { href: '/', label: 'Dashboard', icon: LayoutList },
        { href: '/calendar', label: 'Calendar', icon: CalendarDays },
        { href: '/ambient', label: 'Ambient', icon: Image },
        { href: '/settings', label: 'Settings', icon: Settings },
    ];
</script>

<!-- Desktop masthead — single compact row: logo left, links right -->
<header
    class="border-b-2 border-(--border) bg-(--bg)
    {kioskStore.isKiosk ? 'hidden' : 'hidden lg:block'}"
>
    <div class="mx-auto flex items-center justify-between gap-8 px-4 py-2 sm:px-6 lg:px-8">
        <a href="/" class="inline-flex items-center">
            <span class="font-bold tracking-[0.25em] text-(--text-1) uppercase transition-colors hover:text-(--text-2)">
                Hearth
            </span>
        </a>

        <nav class="flex items-center divide-x divide-(--border)">
            {#each links as link (link.href)}
                <a
                    href={link.href}
                    aria-current={page.url.pathname === link.href ? 'page' : undefined}
                    class="type-label px-3 tracking-widest text-(--text-2) uppercase
                     transition-colors hover:text-(--text-1)
                     aria-[current=page]:font-semibold aria-[current=page]:text-(--text-1)"
                >
                    {link.label}
                </a>
            {/each}
            <!-- The one visible way in to the shortcuts sheet ("?" opens it too) -->
            <div class="pl-2">
                <IconButton
                    label="Keyboard shortcuts (?)"
                    title="Keyboard shortcuts (?)"
                    size="sm"
                    onclick={showShortcuts}
                >
                    <Keyboard class="icon-sm" aria-hidden="true" />
                </IconButton>
            </div>
        </nav>
    </div>
</header>

<!-- Mobile bottom nav -->
<nav
    class="fixed right-0 bottom-0 left-0 flex min-h-max justify-around border-t-2 border-(--border) bg-(--bg) pt-2 pb-[calc(0.5rem+env(safe-area-inset-bottom))] lg:hidden"
>
    {#each links as link (link.href)}
        <a
            href={link.href}
            aria-current={page.url.pathname === link.href ? 'page' : undefined}
            class="flex flex-col items-center gap-0.5 px-4 py-1 text-(--text-2)
               transition-colors hover:text-(--text-1) aria-[current=page]:font-semibold aria-[current=page]:text-(--text-1)"
        >
            <link.icon class="icon-lg" />
            <span class="type-caption tracking-wide uppercase">{link.label}</span>
        </a>
    {/each}
</nav>
