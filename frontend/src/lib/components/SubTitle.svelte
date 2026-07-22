<script lang="ts">
    interface Props {
        subTitleText: string;
        subTitleDescription?: string;
    }

    let { subTitleText, subTitleDescription }: Props = $props();
</script>

<div class="mb-4">
    <p class="type-body tracking-widest text-(--text-1) uppercase">{subTitleText}</p>

    {#if subTitleDescription}
        <p class="type-label text-(--text-2)">{subTitleDescription}</p>
    {/if}
</div>
