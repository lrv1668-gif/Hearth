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
A form control with a small-caps label above it. The <label> wraps the control, so
it names it without an id.
-->
<label class="flex flex-col gap-1 {extra}">
    <span class="type-caption tracking-widest text-(--text-2) uppercase">{label}</span>
    {@render children()}
</label>
