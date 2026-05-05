<script lang="ts">
    interface Props {
        subTitleText: string;
        subTitleDescription?: string;
    }

    let { subTitleText, subTitleDescription }: Props = $props();
</script>

<div class="mb-4">
    <p class="type-body tracking-widest uppercase text-[var(--text-1)]">{subTitleText}</p>

    {#if subTitleDescription}
        <p class="type-label text-[var(--text-2)]">{subTitleDescription}</p>
    {/if}
</div>
