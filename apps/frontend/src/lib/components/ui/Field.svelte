<script lang="ts">
    import type { Snippet } from 'svelte';

    interface Props {
        label: string;
        class?: string;
        /** The control — a TextInput, TextArea or Select. */
        children: Snippet;
    }

    let { label, class: extra = '', children }: Props = $props();
</script>

<!--
@component
A form control with a small label above it (sentence case, so it never reads as a
SettingsGroup title). The <label> wraps the control, so
it names it without an id.
-->
<label class="flex flex-col gap-1 {extra}">
    <span class="type-label text-(--text-2)">{label}</span>
    {@render children()}
</label>
