<script lang="ts">
    import type { Snippet } from 'svelte';

    interface Props {
        /** error: something failed (accent, announced). muted: an empty state or a note. */
        tone?: 'error' | 'muted';
        class?: string;
        children: Snippet;
    }

    let { tone = 'muted', class: extra = '', children }: Props = $props();
</script>

<p
    class="type-label {tone === 'error' ? 'text-(--accent)' : 'text-(--text-2)'} {extra}"
    role={tone === 'error' ? 'alert' : undefined}
>
    {@render children()}
</p>
