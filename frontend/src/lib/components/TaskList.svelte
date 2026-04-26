<script lang="ts">
  import { ChevronDown, ChevronUp } from '@lucide/svelte';

  interface Props {
    onAdd: (
      title: string,
      dueDate?: string,
      dueTime?: string,
      description?: string,
      assignee?: string,
      recurrenceUnit?: string,
      recurrenceInterval?: number,
      recurrenceDays?: string,
    ) => void;
  }

  let { onAdd }: Props = $props();

  let newTitle = $state('');
  let newDueDate = $state('');
  let newDueTime = $state('');
  let allDay = $state(true);
  let showMore = $state(false);

  let description = $state('');
  let assignee = $state('');
  let recurrenceUnit = $state('');    // '' | 'day' | 'week' | 'month'
  let recurrenceInterval = $state(1);
  let recurrenceDays = $state<string[]>([]);

  const weekdays = ['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun'];

  function handleDateChange(e: Event) {
    newDueDate = (e.target as HTMLInputElement).value;
    if (!newDueDate) { allDay = true; newDueTime = ''; }
  }

  function toggleDay(day: string) {
    recurrenceDays = recurrenceDays.includes(day)
      ? recurrenceDays.filter(d => d !== day)
      : [...recurrenceDays, day];
  }

  function handleAdd() {
    const title = newTitle.trim();
    if (!title) return;
    if (newDueDate && !allDay && !newDueTime) return;

    onAdd(
      title,
      newDueDate || undefined,
      !allDay && newDueTime ? newDueTime : undefined,
      description.trim() || undefined,
      assignee.trim() || undefined,
      recurrenceUnit || undefined,
      recurrenceUnit ? recurrenceInterval : undefined,
      recurrenceUnit === 'week' && recurrenceDays.length > 0
        ? recurrenceDays.join(',')
        : undefined,
    );

    newTitle = '';
    newDueDate = '';
    newDueTime = '';
    allDay = true;
    description = '';
    assignee = '';
    recurrenceUnit = '';
    recurrenceInterval = 1;
    recurrenceDays = [];
    showMore = false;
  }
</script>

<div class="space-y-2">
  <!-- Main row -->
  <div class="flex gap-2">
    <input
      bind:value={newTitle}
      onkeydown={(e) => e.key === 'Enter' && handleAdd()}
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
             disabled:opacity-40 disabled:cursor-not-allowed"
    >
      Add
    </button>
  </div>

  <!-- Time row -->
  {#if newDueDate}
    <div class="flex items-center gap-3">
      <div class="flex-1"></div>
      <label class="flex items-center gap-2 text-sm text-[var(--text-2)] cursor-pointer select-none">
        <input
          type="checkbox"
          checked={allDay}
          onchange={(e) => { allDay = (e.target as HTMLInputElement).checked; if (allDay) newDueTime = ''; }}
          class="rounded accent-[var(--accent)] cursor-pointer"
        />
        All day
      </label>
      {#if !allDay}
        <input
          type="time"
          bind:value={newDueTime}
          step="900"
          class="bg-[var(--surface)] text-[var(--text-3)] rounded-lg px-3 py-2 text-sm
                 outline-none focus:ring-1 focus:ring-[var(--border)] transition"
        />
      {/if}
    </div>
  {/if}

  <!-- More options toggle -->
  <button
    onclick={() => (showMore = !showMore)}
    class="flex items-center gap-1 text-xs text-[var(--text-4)] hover:text-[var(--text-2)] transition-colors"
  >
    {#if showMore}<ChevronUp size={12} />{:else}<ChevronDown size={12} />{/if}
    More options
  </button>

  {#if showMore}
    <div class="space-y-3 pt-1 border-t border-[var(--border)]">
      <!-- Description -->
      <textarea
        bind:value={description}
        placeholder="Description (optional)"
        rows={2}
        class="w-full bg-[var(--surface)] text-[var(--text-1)] placeholder-[var(--text-4)]
               rounded-lg px-4 py-2.5 text-sm outline-none resize-none
               focus:ring-1 focus:ring-[var(--border)] transition mt-1"
      ></textarea>

      <!-- Assignee -->
      <input
        bind:value={assignee}
        placeholder="Assign to"
        class="w-full bg-[var(--surface)] text-[var(--text-1)] placeholder-[var(--text-4)]
               rounded-lg px-4 py-2.5 text-sm outline-none
               focus:ring-1 focus:ring-[var(--border)] transition"
      />

      <!-- Recurrence -->
      <div class="space-y-2">
        <label class="text-xs text-[var(--text-3)] uppercase tracking-wide">Repeat</label>
        <div class="flex gap-2 flex-wrap">
          {#each [['', 'None'], ['day', 'Daily'], ['week', 'Weekly'], ['month', 'Monthly']] as [val, label]}
            <button
              onclick={() => { recurrenceUnit = val; recurrenceInterval = 1; recurrenceDays = []; }}
              class="px-3 py-1 rounded-full text-xs transition-colors
                     {recurrenceUnit === val
                       ? 'bg-[var(--accent)] text-[var(--accent-fg)]'
                       : 'bg-[var(--surface)] text-[var(--text-2)] hover:text-[var(--text-1)]'}"
            >
              {label}
            </button>
          {/each}
        </div>

        {#if recurrenceUnit === 'week'}
          <div class="flex gap-1 flex-wrap">
            {#each weekdays as day}
              <button
                onclick={() => toggleDay(day)}
                class="px-2 py-1 rounded text-xs transition-colors
                       {recurrenceDays.includes(day)
                         ? 'bg-[var(--accent)] text-[var(--accent-fg)]'
                         : 'bg-[var(--surface)] text-[var(--text-3)] hover:text-[var(--text-1)]'}"
              >
                {day}
              </button>
            {/each}
          </div>
        {/if}

        {#if recurrenceUnit && recurrenceUnit !== 'week'}
          <div class="flex items-center gap-2 text-sm text-[var(--text-2)]">
            <span>Every</span>
            <input
              type="number"
              bind:value={recurrenceInterval}
              min={1}
              max={365}
              class="w-16 bg-[var(--surface)] text-[var(--text-1)] rounded-lg px-3 py-1.5 text-sm
                     outline-none focus:ring-1 focus:ring-[var(--border)] text-center"
            />
            <span>{recurrenceUnit === 'day' ? 'day(s)' : recurrenceUnit === 'month' ? 'month(s)' : ''}</span>
          </div>
        {/if}
      </div>
    </div>
  {/if}
</div>
