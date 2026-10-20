<script lang="ts">
    import { taskStore } from '$lib/stores/TaskStore.svelte.ts';
    import CountdownWidget from '$lib/components/widgets/CountdownWidget.svelte';
    import { itemsThatFit } from '$lib/frame';

    let { lines }: { lines: number } = $props();

    // A countdown row (number + title + date) is ~1.85 lines with its gap.
    const limit = $derived(Math.min(5, itemsThatFit(lines + 0.25, 1.85)));
</script>

<CountdownWidget tasks={taskStore.tasks} onEdit={() => {}} {limit} />
