<script lang="ts">
    import { CalendarDays, Image, LayoutList, Settings } from '@lucide/svelte';
    import { page } from '$app/state';
    import { kioskStore } from '$lib/stores/KioskStore.svelte.ts';
</script>

<!-- Desktop masthead — single compact row: logo left, links right -->
<header
    class="mb-4 border-b-2 border-(--border) bg-(--bg)
    {kioskStore.isKiosk ? 'hidden' : 'hidden lg:block'}"
>
    <div class="mx-auto flex items-center justify-between gap-8 px-8 py-2">
        <a href="/" class="inline-flex items-center">
            <span class="font-bold tracking-[0.25em] text-(--text-1) uppercase transition-colors hover:text-(--text-2)">
                Hearth
            </span>
        </a>

        <nav class="flex items-center divide-x divide-(--border)">
            <a
                href="/"
                aria-current={page.url.pathname === '/' ? 'page' : undefined}
                class="type-label px-3 tracking-widest text-(--text-2) uppercase
                 transition-colors hover:text-(--text-1)
                 aria-[current=page]:font-semibold aria-[current=page]:text-(--text-1)"
            >
                Schedule
            </a>
            <a
                href="/calendar"
                aria-current={page.url.pathname === '/calendar' ? 'page' : undefined}
                class="type-label px-3 tracking-widest text-(--text-2) uppercase
                 transition-colors hover:text-(--text-1)
                 aria-[current=page]:font-semibold aria-[current=page]:text-(--text-1)"
            >
                Calendar
            </a>
            <a
                href="/ambient"
                aria-current={page.url.pathname === '/ambient' ? 'page' : undefined}
                class="type-label px-3 tracking-widest text-(--text-2) uppercase
                 transition-colors hover:text-(--text-1)
                 aria-[current=page]:font-semibold aria-[current=page]:text-(--text-1)"
            >
                Ambient
            </a>
            <a
                href="/settings"
                aria-current={page.url.pathname === '/settings' ? 'page' : undefined}
                class="type-label px-3 tracking-widest text-(--text-2) uppercase
                 transition-colors hover:text-(--text-1)
                 aria-[current=page]:font-semibold aria-[current=page]:text-(--text-1)"
            >
                Settings
            </a>
        </nav>
    </div>
</header>

<!-- Mobile bottom nav -->
<nav
    class="fixed right-0 bottom-0 left-0 flex min-h-max justify-around border-t-2 border-(--border) bg-(--bg) py-2 lg:hidden"
>
    <a
        href="/"
        aria-current={page.url.pathname === '/' ? 'page' : undefined}
        class="flex flex-col items-center gap-0.5 px-4 py-1 text-(--text-3)
           transition-colors hover:text-(--text-1) aria-[current=page]:text-(--text-1)"
    >
        <LayoutList class="icon-lg" />
        <span class="type-caption tracking-wide uppercase">Schedule</span>
    </a>
    <a
        href="/calendar"
        aria-current={page.url.pathname === '/calendar' ? 'page' : undefined}
        class="flex flex-col items-center gap-0.5 px-4 py-1 text-(--text-3)
           transition-colors hover:text-(--text-1) aria-[current=page]:text-(--text-1)"
    >
        <CalendarDays class="icon-lg" />
        <span class="type-caption tracking-wide uppercase">Calendar</span>
    </a>
    <a
        href="/ambient"
        aria-current={page.url.pathname === '/ambient' ? 'page' : undefined}
        class="flex flex-col items-center gap-0.5 px-4 py-1 text-(--text-3)
           transition-colors hover:text-(--text-1) aria-[current=page]:text-(--text-1)"
    >
        <Image class="icon-lg" />
        <span class="type-caption tracking-wide uppercase">Ambient</span>
    </a>
    <a
        href="/settings"
        aria-current={page.url.pathname === '/settings' ? 'page' : undefined}
        class="flex flex-col items-center gap-0.5 px-4 py-1 text-(--text-3)
           transition-colors hover:text-(--text-1) aria-[current=page]:text-(--text-1)"
    >
        <Settings class="icon-lg" />
        <span class="type-caption tracking-wide uppercase">Settings</span>
    </a>
</nav>
