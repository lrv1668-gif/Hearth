<script lang="ts">
  import TaskList from "./TaskList.svelte";
  import { X } from "@lucide/svelte";

  interface Props {
    open?: boolean;
    onAdd: (title: string, dueDate?: string, dueTime?: string) => void;
  }

  let { open = $bindable(false), onAdd }: Props = $props();

  let dialog = $state<HTMLDialogElement | null>(null);

  $effect(() => {
    if (!dialog) return;
    if (open) dialog.showModal();
    else if (dialog.open) dialog.close();
  });

  function close() {
    open = false;
  }

  function handleBackdropClick(e: MouseEvent) {
    if (e.target === dialog) close();
  }

  function handleAdd(title: string, dueDate?: string, dueTime?: string) {
    onAdd(title, dueDate, dueTime);
    close();
  }
</script>

<!-- svelte-ignore a11y_no_noninteractive_element_interactions -->
<dialog
  bind:this={dialog}
  onclose={close}
  onclick={handleBackdropClick}
  class="bg-transparent p-0 max-w-lg w-full backdrop:bg-black/40 backdrop:backdrop-blur-sm"
>
  <div class="bg-[var(--bg)] border border-[var(--border)] rounded-xl shadow-xl p-6 space-y-5 mx-4">
    <div class="flex items-center justify-between">
      <h2 class="text-[var(--text-1)]">
        Add a New Task
      </h2>
      <button
        onclick={close}
        class="text-[var(--text-3)] hover:text-[var(--text-1)] transition-colors w-6 h-6 flex items-center justify-center"
        aria-label="Close"
      >
        <X />
      </button>
    </div>

    <TaskList onAdd={handleAdd} />
  </div>
</dialog>
