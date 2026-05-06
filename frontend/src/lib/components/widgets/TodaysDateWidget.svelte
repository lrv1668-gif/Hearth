<script lang="ts">
    const todaysDate = $state(new Date());
</script>

<div>
    <p class="type-title font-bold leading-tight text-[var(--text-1)]">
        {todaysDate.toLocaleDateString('en-US', { day: 'numeric', month: 'long' })}
    </p>
    <p class="type-subtitle mt-2 uppercase tracking-widest text-[var(--text-2)]">
        {todaysDate.toLocaleDateString('en-US', { weekday: 'long' })}
    </p>
</div>
