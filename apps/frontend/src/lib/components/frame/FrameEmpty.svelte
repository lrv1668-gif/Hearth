<script lang="ts">
    // `small`: a quarter slot or the free columns, where the note stays body-sized.
    let { text, small = false }: { text: string; small?: boolean } = $props();
</script>

<!--
@component
A widget's empty or unavailable state. In S slots and the free columns it's a quiet
line of body text; in M and L slots it sits centred in the slot in the display face,
so an empty slot reads as designed rather than broken.
-->
{#if small}
    <p class="type-body text-(--text-2)">{text}.</p>
{:else}
    <div class="flex h-full items-center justify-center px-8 text-center">
        <p class="type-title font-(family-name:--display-family) text-(--text-2) italic">{text}</p>
    </div>
{/if}
