<script lang="ts">
    interface Props {
        checked: boolean;
        onchange: () => void;
        disabled?: boolean;
    }

    let { checked, onchange, disabled = false }: Props = $props();
</script>

<button
    role="switch"
    aria-checked={checked}
    onclick={onchange}
    {disabled}
    class="inline-flex h-5 w-9 shrink-0 items-center rounded-full border px-[1px] transition-colors duration-200
        {disabled ? 'cursor-not-allowed' : 'cursor-pointer'}
        {checked ? 'border-[var(--accent)] bg-[var(--accent)]' : 'border-[var(--border)] bg-[var(--surface-hi)]'}"
    aria-label="Toggle"
>
    <span
        class="h-4 w-4 rounded-full bg-[var(--bg)] shadow-sm transition-transform duration-200
            {checked ? 'translate-x-4' : 'translate-x-0'}"
    ></span>
</button>
