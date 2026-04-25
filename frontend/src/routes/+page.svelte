<script lang="ts">
    import TaskList from '$lib/components/TaskList.svelte';
</script>

<svelte:head>
    <title>Hearth</title>
</svelte:head>

<main class="min-h-screen bg-stone-900 text-stone-100 p-8">
    <div class="max-w-xl mx-auto space-y-10">
        <header>
            <h1 class="text-xl font-light tracking-[0.3em] text-stone-500 uppercase">Hearth</h1>
        </header>

        <section>
            <h2 class="text-xs font-medium tracking-widest text-stone-600 uppercase mb-4">Today</h2>
            <TaskList />
        </section>
    </div>
</main>
