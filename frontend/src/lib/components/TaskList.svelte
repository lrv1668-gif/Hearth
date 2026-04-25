<script lang="ts">
  interface Props {
    onAdd: (title: string, dueDate?: string) => void;
  }

  let { onAdd }: Props = $props();

  let newTitle = $state('');
  let newDueDate = $state('');

  function handleAdd() {
    const title = newTitle.trim();
    if (!title) return;
    onAdd(title, newDueDate || undefined);
    newTitle = '';
    newDueDate = '';
  }
</script>

<div class="flex gap-2">
  <input
    bind:value={newTitle}
    onkeydown={(e) => e.key === 'Enter' && handleAdd()}
    placeholder="Add a task..."
    class="flex-1 bg-stone-800 text-stone-200 placeholder-stone-600 rounded-lg px-4 py-2.5 text-sm outline-none focus:ring-1 focus:ring-stone-600 transition"
  />
  <input
    type="date"
    bind:value={newDueDate}
    class="bg-stone-800 text-stone-400 rounded-lg px-3 py-2.5 text-sm outline-none focus:ring-1 focus:ring-stone-600 transition"
  />
  <button
    onclick={handleAdd}
    class="px-4 py-2.5 bg-stone-800 hover:bg-stone-700 text-stone-400 hover:text-stone-200 rounded-lg text-sm transition-colors"
  >
    Add
  </button>
</div>
