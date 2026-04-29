<script lang="ts">
  interface Theme {
    id: string;
    label: string;
    fill: string;
    stroke: string;
  }

  const themes: Theme[] = [
    { id: 'ash',        label: 'Ash',        fill: '#1d1d1d', stroke: '#6a6a6a' },
    { id: 'stone',      label: 'Stone',      fill: '#292524', stroke: '#78716c' },
    { id: 'linen',      label: 'Linen',      fill: '#f0e8da', stroke: '#9e8c75' },
    { id: 'forest',     label: 'Forest',     fill: '#192118', stroke: '#4a6845' },
    { id: 'dusk',       label: 'Dusk',       fill: '#151b2a', stroke: '#4e5478' },
    { id: 'terracotta', label: 'Terracotta', fill: '#ece0d0', stroke: '#ac7850' },
  ];

  interface Props {
    theme: string;
    onChange: (id: string) => void;
  }

  let { theme, onChange }: Props = $props();
</script>

<div class="flex items-center gap-2" role="group" aria-label="Color theme">
  {#each themes as t}
    <button
      onclick={() => onChange(t.id)}
      title={t.label}
      aria-pressed={theme === t.id}
      style="background: {t.fill}; border: 1.5px solid {t.stroke};"
      class="w-4 h-4 rounded-full transition-all
             {theme === t.id
               ? 'ring-2 ring-offset-2 ring-[var(--text-3)] ring-offset-[var(--bg)] opacity-100'
               : 'opacity-40 hover:opacity-80'}"
    ></button>
  {/each}
</div>
