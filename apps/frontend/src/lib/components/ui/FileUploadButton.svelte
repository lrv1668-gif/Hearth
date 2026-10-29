<script lang="ts">
    import type { Component } from 'svelte';
    import { buttonClass } from './button';

    interface Props {
        label: string;
        icon?: Component<{ class?: string }>;
        accept?: string;
        multiple?: boolean;
        /** Disables picking while an upload runs. */
        disabled?: boolean;
        onfiles: (files: File[]) => void;
    }

    let { label, icon: Icon, accept, multiple = false, disabled = false, onfiles }: Props = $props();

    function handleChange(e: Event & { currentTarget: HTMLInputElement }) {
        const files = Array.from(e.currentTarget.files ?? []);
        // Cleared so picking the same file again still fires a change
        e.currentTarget.value = '';
        if (files.length > 0) onfiles(files);
    }
</script>

<!--
@component
A file picker that looks like a secondary Button: a visually hidden <input type=file>
inside a <label>, so it keeps the native keyboard and screen-reader behaviour.
-->
<label
    class="{buttonClass(
        'secondary'
    )} cursor-pointer has-focus-visible:outline-2 has-focus-visible:outline-offset-2 has-focus-visible:outline-(--accent)"
    aria-disabled={disabled || undefined}
>
    {#if Icon}<Icon class="icon-sm" />{/if}
    {label}
    <input type="file" {accept} {multiple} {disabled} class="sr-only" onchange={handleChange} />
</label>
