<script lang="ts">
    let { now, compact = false }: { now: Date; compact?: boolean } = $props();

    const month = $derived(now.toLocaleDateString('en-US', { month: 'short' }));
</script>

<!--
@component
The almanac edition's date block: the day of the month on a solid square of the
theme's --block ink. A pure-ink fill maps exactly to the panel, so unlike a tint it
never dithers. `compact` is the narrower version on the Ambient plate.
-->
<div
    class="flex shrink-0 flex-col justify-between bg-(--block) text-(--on-block) {compact
        ? 'w-34 px-4 pt-4 pb-3'
        : 'h-46 w-46 px-5 pt-5 pb-4'}"
>
    <p class="type-label leading-none font-semibold tracking-[0.16em] uppercase">{month}</p>
    <p
        class="{compact
            ? 'type-stamp'
            : 'type-date'} font-(family-name:--display-family) leading-[0.8] font-semibold tracking-[-0.04em]"
    >
        {now.getDate()}
    </p>
</div>
