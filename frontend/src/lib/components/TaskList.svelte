<script lang="ts">
  interface Props {
    onAdd: (title: string, dueDate?: string, dueTime?: string) => void;
  }

  let { onAdd }: Props = $props();

  let newTitle = $state("");
  let newDueDate = $state("");
  let newDueTime = $state("");
  let allDay = $state(true);

  function handleDateChange(e: Event) {
    newDueDate = (e.target as HTMLInputElement).value;
    if (!newDueDate) {
      allDay = true;
      newDueTime = "";
    }
  }

  function handleAllDayChange(e: Event) {
    allDay = (e.target as HTMLInputElement).checked;
    if (allDay) newDueTime = "";
  }

  function handleAdd() {
    const title = newTitle.trim();
    if (!title) return;
    if (newDueDate && !allDay && !newDueTime) return;
    onAdd(title, newDueDate || undefined, (!allDay && newDueTime) ? newDueTime : undefined);
    newTitle = "";
    newDueDate = "";
    newDueTime = "";
    allDay = true;
  }
</script>

<div class="space-y-2">
  <div class="flex gap-2">
    <input
      bind:value={newTitle}
      onkeydown={(e) => e.key === "Enter" && handleAdd()}
      placeholder="Enter description..."
      class="flex-1 bg-[var(--surface)] text-[var(--text-1)] placeholder-[var(--text-4)]
             rounded-lg px-4 py-2.5 text-sm outline-none
             focus:ring-1 focus:ring-[var(--border)] transition"
    />
    <input
      type="date"
      value={newDueDate}
      oninput={handleDateChange}
      class="bg-[var(--surface)] text-[var(--text-3)] rounded-lg px-3 py-2.5 text-sm
             outline-none focus:ring-1 focus:ring-[var(--border)] transition"
    />
    <button
      onclick={handleAdd}
      disabled={!!newDueDate && !allDay && !newDueTime}
      class="px-4 py-2.5 bg-[var(--accent)] hover:bg-[var(--accent-hi)] text-[var(--accent-fg)]
             rounded-lg text-sm font-medium transition-colors
             disabled:opacity-40 disabled:cursor-not-allowed disabled:hover:bg-[var(--accent)]"
    >
      Add
    </button>
  </div>

  {#if newDueDate}
    <div class="flex items-center gap-3">
      <div class="flex-1"></div>
      <label class="flex items-center gap-2 text-sm text-[var(--text-2)] cursor-pointer select-none">
        <input
          type="checkbox"
          checked={allDay}
          onchange={handleAllDayChange}
          class="rounded accent-[var(--accent)] cursor-pointer"
        />
        All day
      </label>
      {#if !allDay}
        <input
          type="time"
          bind:value={newDueTime}
          class="bg-[var(--surface)] text-[var(--text-3)] rounded-lg px-3 py-2 text-sm
                 outline-none focus:ring-1 focus:ring-[var(--border)] transition
                 {!newDueTime ? 'ring-1 ring-[var(--border)]' : ''}"
        />
      {/if}
    </div>
  {/if}
</div>
