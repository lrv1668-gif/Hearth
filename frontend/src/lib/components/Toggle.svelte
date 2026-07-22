<script lang="ts">
    interface Props {
        checked: boolean;
        onchange: () => void;
    }

    let { checked, onchange }: Props = $props();
</script>

<button
    role="switch"
    aria-checked={checked}
    onclick={onchange}
    class="inline-flex h-5 w-9 shrink-0 cursor-pointer items-center rounded-full border px-[1px] transition-colors duration-200
        {checked ? 'border-(--accent) bg-(--accent)' : 'border-(--border) bg-(--surface-hi)'}"
    aria-label="Toggle"
>
    <span
        class="h-4 w-4 rounded-full bg-(--bg) shadow-sm transition-transform duration-200
            {checked ? 'translate-x-4' : 'translate-x-0'}"
    ></span>
</button>
