<script lang="ts">
    interface Props {
        checked: boolean;
        onchange: () => void;
        /** Accessible name when the switch stands alone. Inside ToggleRow, the row names it. */
        label?: string;
        labelledby?: string;
        describedby?: string;
        disabled?: boolean;
    }

    let { checked, onchange, label, labelledby, describedby, disabled = false }: Props = $props();
</script>

<button
    type="button"
    role="switch"
    aria-checked={checked}
    aria-label={label}
    aria-labelledby={labelledby}
    aria-describedby={describedby}
    {disabled}
    onclick={onchange}
    class="inline-flex h-5 w-9 shrink-0 cursor-pointer items-center rounded-full border px-[1px] transition-colors duration-200 disabled:cursor-not-allowed
        {checked ? 'border-(--accent) bg-(--accent)' : 'border-(--border) bg-(--surface-hi)'}"
>
    <span
        class="h-4 w-4 rounded-full bg-(--bg) shadow-sm transition-transform duration-200
            {checked ? 'translate-x-4' : 'translate-x-0'}"
    ></span>
</button>
