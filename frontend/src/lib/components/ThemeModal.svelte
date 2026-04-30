<script lang="ts">
  import { themes, themeGroups, type ThemeId } from '$lib/themes';

  interface Props {
    theme: ThemeId;
    onChange: (id: ThemeId) => void;
    onClose: () => void;
  }

  let { theme, onChange, onClose }: Props = $props();

  function handleKeydown(e: KeyboardEvent) {
    if (e.key === 'Escape') onClose();
  }

  function handleChange(id: string) {
    onChange(id);
    onClose();
  }
</script>

<svelte:window onkeydown={handleKeydown} />

<!-- Backdrop -->
<div
  class="fixed inset-0 z-50 flex items-center justify-center"
  role="dialog"
  aria-modal="true"
  aria-label="Choose theme"
>
  <button
    class="absolute inset-0 bg-black/40"
    onclick={onClose}
    aria-label="Close"
  ></button>

  <!-- Modal card -->
  <div class="relative z-10 bg-[var(--surface)] border border-[var(--border)] rounded-lg px-8 py-6 shadow-xl flex flex-col gap-6 min-w-[340px]">
    <div class="flex items-center justify-between">
      <h2 class="font-serif text-lg font-semibold text-[var(--text-1)]">Choose Theme</h2>
      <button
        onclick={onClose}
        class="text-[var(--text-3)] hover:text-[var(--text-1)] transition-colors text-xl leading-none"
        aria-label="Close"
      >✕</button>
    </div>

    <div class="flex flex-col gap-5">
      {#each themeGroups as group}
        <div class="flex flex-col gap-3">
          <p class="text-xs tracking-widest uppercase text-[var(--text-3)]">{group}</p>
          <div class="flex gap-4">
            {#each themes.filter(t => t.group === group) as t}
              <button
                onclick={() => handleChange(t.id)}
                aria-pressed={theme === t.id}
                class="flex flex-col items-center gap-2 group"
              >
                <span
                  style="background: {t.fill}; border: 2px solid {t.stroke};"
                  class="block w-10 h-10 rounded-full transition-all
                         {theme === t.id
                           ? 'ring-2 ring-offset-2 ring-[var(--text-2)] ring-offset-[var(--surface)]'
                           : 'opacity-60 group-hover:opacity-100'}"
                ></span>
                <span class="text-xs tracking-wide {theme === t.id ? 'text-[var(--text-1)] font-medium' : 'text-[var(--text-2)]'}">
                  {t.label}
                </span>
              </button>
            {/each}
          </div>
        </div>
      {/each}
    </div>
  </div>
</div>
