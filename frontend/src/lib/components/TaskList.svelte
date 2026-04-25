<script lang="ts">
  interface Props {
    onAdd: (title: string, dueDate?: string) => void;
  }

  let { onAdd }: Props = $props();

  let newTitle = $state("");
  let newDueDate = $state("");

  function handleAdd() {
    const title = newTitle.trim();
    if (!title) return;
    onAdd(title, newDueDate || undefined);
    newTitle = "";
    newDueDate = "";
  }
</script>

<div class="flex gap-2">
  <input
    bind:value={newTitle}
    onkeydown={(e) => e.key === "Enter" && handleAdd()}
    placeholder="Add a task..."
    class="flex-1 bg-[var(--surface)] text-[var(--text-1)] placeholder-[var(--text-4)]
           rounded-lg px-4 py-2.5 text-sm outline-none
           focus:ring-1 focus:ring-[var(--border)] transition"
  />
  <input
    type="date"
    bind:value={newDueDate}
    class="bg-[var(--surface)] text-[var(--text-3)] rounded-lg px-3 py-2.5 text-sm
           outline-none focus:ring-1 focus:ring-[var(--border)] transition"
  />
  <button
    onclick={handleAdd}
    class="px-4 py-2.5 bg-[var(--surface)] hover:bg-[var(--surface-hi)]
           text-[var(--text-3)] hover:text-[var(--text-1)]
           rounded-lg text-sm transition-colors"
  >
    Add
  </button>
</div>
