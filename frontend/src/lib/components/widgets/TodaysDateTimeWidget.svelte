<script lang="ts">
    let now = $state(new Date());

    $effect(() => {
        const id = setInterval(() => (now = new Date()), 1000);
        return () => clearInterval(id);
    });

    const weekdayShort = $derived(now.toLocaleDateString('en-US', { weekday: 'long' }).toUpperCase());
    const dateShort = $derived(now.toLocaleDateString('en-US', { month: 'short', day: 'numeric' }).toUpperCase());
</script>

<div>
    <p class="type-display font-bold leading-none text-[var(--text-1)]">
        {now.toLocaleTimeString('en-US', { hour: 'numeric', minute: '2-digit' })}
    </p>
    <p class="type-subtitle mt-4 uppercase tracking-widest text-[var(--text-2)]">
        {weekdayShort} · {dateShort}
    </p>
</div>
