<script lang="ts">
    import { CalendarDays, Image, LayoutList, Settings } from '@lucide/svelte';
</script>

<!-- Desktop masthead -->
<header class="border-b border-[var(--border)] bg-[var(--bg)] hidden md:block">
    <div class="max-w-5xl mx-auto flex items-center px-8 pt-4 pb-2">
        <div class="flex-1"></div>
        <a href="/">
            <span
                class="text-2xl font-bold tracking-[0.25em] uppercase text-[var(--text-1)] hover:text-[var(--text-2)] transition-colors"
            >
                Hearth
            </span>
        </a>
        <div class="flex-1"></div>
    </div>

    <div class="border-t-2 border-[var(--text-1)]"></div>

    <nav class="flex justify-center items-center py-3">
        <a
            href="/"
            class="px-6 text-xs tracking-widest uppercase transition-colors
             text-[var(--text-3)] hover:text-[var(--text-1)]
             aria-[current=page]:text-[var(--text-1)] aria-[current=page]:font-semibold"
        >
            Schedule
        </a>
        <span class="w-px h-3 bg-[var(--border)]" aria-hidden="true"></span>
        <a
            href="/calendar"
            class="px-6 text-xs tracking-widest uppercase transition-colors
             text-[var(--text-3)] hover:text-[var(--text-1)]
             aria-[current=page]:text-[var(--text-1)] aria-[current=page]:font-semibold"
        >
            Calendar
        </a>
        <span class="w-px h-3 bg-[var(--border)]" aria-hidden="true"></span>
        <a
            href="/ambient"
            class="px-6 text-xs tracking-widest uppercase transition-colors
             text-[var(--text-3)] hover:text-[var(--text-1)]
             aria-[current=page]:text-[var(--text-1)] aria-[current=page]:font-semibold"
        >
            Ambient
        </a>
        <span class="w-px h-3 bg-[var(--border)]" aria-hidden="true"></span>
        <a
            href="/settings"
            class="px-6 text-xs tracking-widest uppercase transition-colors
             text-[var(--text-3)] hover:text-[var(--text-1)]
             aria-[current=page]:text-[var(--text-1)] aria-[current=page]:font-semibold"
        >
            Settings
        </a>
    </nav>
</header>

<!-- Mobile: compact top bar -->
<header class="flex items-center justify-center px-4 py-3 border-b border-[var(--border)] bg-[var(--bg)] md:hidden">
    <a href="/" class="text-lg font-bold tracking-widest uppercase text-[var(--text-1)]">Hearth</a>
</header>

<!-- Mobile bottom nav -->
<nav
    class="fixed bottom-0 left-0 right-0 flex justify-around border-t border-[var(--border)] bg-[var(--bg)] py-2 z-50 md:hidden"
>
    <a
        href="/"
        class="flex flex-col items-center gap-0.5 px-4 py-1 text-[var(--text-3)]
           hover:text-[var(--text-1)] aria-[current=page]:text-[var(--text-1)] transition-colors"
    >
        <LayoutList size={20} />
        <span class="text-[10px] tracking-wide uppercase">Schedule</span>
    </a>
    <a
        href="/calendar"
        class="flex flex-col items-center gap-0.5 px-4 py-1 text-[var(--text-3)]
           hover:text-[var(--text-1)] aria-[current=page]:text-[var(--text-1)] transition-colors"
    >
        <CalendarDays size={20} />
        <span class="text-[10px] tracking-wide uppercase">Calendar</span>
    </a>
    <a
        href="/ambient"
        class="flex flex-col items-center gap-0.5 px-4 py-1 text-[var(--text-3)]
           hover:text-[var(--text-1)] aria-[current=page]:text-[var(--text-1)] transition-colors"
    >
        <Image size={20} />
        <span class="text-[10px] tracking-wide uppercase">Ambient</span>
    </a>
    <a
        href="/settings"
        class="flex flex-col items-center gap-0.5 px-4 py-1 text-[var(--text-3)]
           hover:text-[var(--text-1)] aria-[current=page]:text-[var(--text-1)] transition-colors"
    >
        <Settings size={20} />
        <span class="text-[10px] tracking-wide uppercase">Settings</span>
    </a>
</nav>
