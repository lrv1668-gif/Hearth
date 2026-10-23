<script lang="ts" generics="T extends string">
    interface Props {
        options: { value: T; label: string }[];
        value: T;
        onchange: (value: T) => void;
        /** Names the group for screen readers. */
        label?: string;
    }

    let { options, value, onchange, label }: Props = $props();
</script>

<!-- Wraps rather than running off a phone screen -->
<div class="inline-flex max-w-full flex-wrap rounded-lg border border-(--border) p-0.5" role="group" aria-label={label}>
    {#each options as o (o.value)}
        <button
            type="button"
            onclick={() => onchange(o.value)}
            aria-pressed={value === o.value}
            class="type-label rounded-md px-3 py-2 text-(--text-2) transition-colors hover:text-(--text-1) aria-pressed:bg-(--surface-hi)
                aria-pressed:font-semibold aria-pressed:text-(--text-1) sm:py-1"
        >
            {o.label}
        </button>
    {/each}
</div>
