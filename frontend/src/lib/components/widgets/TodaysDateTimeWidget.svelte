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
    <p class="type-display leading-none font-bold text-(--text-1)">
        {now.toLocaleTimeString('en-US', { hour: 'numeric', minute: '2-digit' })}
    </p>
    <p class="type-subtitle mt-4 tracking-widest text-(--text-2) uppercase">
        {weekdayShort} · {dateShort}
    </p>
</div>
