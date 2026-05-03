<script>
    const todaysDate = $state(new Date());
</script>

<div class="text-left md:text-right">
    <p class="type-title font-bold text-[var(--text-1)] leading-tight">
        {todaysDate.toLocaleDateString('en-US', { day: 'numeric', month: 'long' })}
    </p>
    <p class="type-subtitle tracking-widest uppercase text-[var(--text-2)] mt-2">
        {todaysDate.toLocaleDateString('en-US', { weekday: 'long' })}
    </p>
</div>
