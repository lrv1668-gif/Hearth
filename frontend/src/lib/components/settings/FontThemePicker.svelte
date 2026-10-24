<script lang="ts">
    import { fontThemes } from '$lib/constants/fontThemes';
    import { fontThemeStore, setFontTheme } from '$lib/stores/FontThemeStore.svelte.ts';
    import PickerCard from '../ui/PickerCard.svelte';
</script>

<!-- Columns follow the card's width, not the screen's. Phones get a denser three-up grid: "Aa" and name, each in the preset's own face -->
<div class="@container">
    <div class="grid grid-cols-3 gap-2 sm:gap-3 @xl:grid-cols-4 @4xl:grid-cols-5">
        {#each fontThemes as f}
            {@const selected = fontThemeStore.fontTheme === f.id}
            <!-- data-font on the button scopes the preset's [data-font] block to this preview -->
            <PickerCard data-font={f.id} pressed={selected} onclick={() => setFontTheme(f.id)}>
                <!-- Preview body -->
                <div class="flex min-w-0 flex-1 flex-col gap-1 p-2.5 sm:p-4">
                    <p class="type-title font-semibold text-(--text-1)">Aa</p>
                    <p class="type-body hidden truncate font-medium text-(--text-1) sm:block">Water the plants</p>
                    <p class="type-label hidden truncate text-(--text-2) sm:block">71° · Sunny, gentle breeze</p>
                </div>
                <!-- Footer -->
                <div
                    class="flex items-center justify-between gap-2 border-t border-(--border) px-2.5 py-2 sm:px-4 sm:py-2.5"
                >
                    <span class="type-label min-w-0 truncate font-medium text-(--text-1)">{f.label}</span>
                    <span class="type-label hidden shrink-0 tracking-widest text-(--text-2) uppercase sm:inline"
                        >{f.tag}</span
                    >
                </div>
            </PickerCard>
        {/each}
    </div>
</div>
