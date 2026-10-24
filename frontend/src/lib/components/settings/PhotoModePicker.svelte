<script lang="ts">
    import { onMount, untrack } from 'svelte';
    import { Check, Plus } from '@lucide/svelte';
    import { api, type PhotoGroup, type UploadedPhoto } from '$lib/api';
    import { isGroupPicked, pickPhotoIn, toggleGroup, type PickMode } from '$lib/photos';
    import { frameSettings } from '$lib/stores/FrameSettingsStore.svelte.ts';
    import Button from '../ui/Button.svelte';
    import Chip from '../ui/Chip.svelte';
    import InlineMessage from '../ui/InlineMessage.svelte';
    import ListRow from '../ui/ListRow.svelte';
    import Modal from '../ui/Modal.svelte';
    import SegmentedControl from '../ui/SegmentedControl.svelte';
    import TextInput from '../ui/TextInput.svelte';

    interface Props {
        open?: boolean;
        uploads: UploadedPhoto[];
    }

    let { open = $bindable(false), uploads }: Props = $props();

    // Ids that were chosen but have since been deleted don't count
    const chosen = $derived(uploads.filter((u) => frameSettings.photoIds.includes(u.id)).length);
    const order = $derived(uploads.map((u) => u.id));

    let mode = $state<PickMode>('several');
    // The last photo clicked, where a shift-click's run starts
    let anchor = $state<string | null>(null);

    let groups = $state<PhotoGroup[]>([]);
    let naming = $state(false);
    let newName = $state('');
    let editing = $state(false);
    let error = $state<string | null>(null);

    onMount(async () => {
        groups = await api.photos.groups();
    });

    $effect(() => {
        if (!open) return;
        // Open in the mode that matches what's already picked — once, not on every pick
        mode = untrack(() => frameSettings.photoIds.length) > 1 ? 'several' : 'one';
        anchor = null;
        naming = editing = false;
        error = null;
    });

    function setPicked(ids: string[]) {
        frameSettings.photoIds = ids;
    }

    function clickPhoto(id: string, e: MouseEvent) {
        setPicked(pickPhotoIn(frameSettings.photoIds, id, mode, order, anchor, e.shiftKey));
        anchor = id;
    }

    function clickGroup(group: PhotoGroup) {
        // A group is several photos, so picking one switches modes
        mode = 'several';
        setPicked(toggleGroup(frameSettings.photoIds, group));
    }

    const pickedIds = () => order.filter((id) => frameSettings.photoIds.includes(id));

    async function saveGroup(e: SubmitEvent) {
        e.preventDefault();
        const name = newName.trim();
        if (!name) return;
        try {
            groups = [...groups, await api.photos.createGroup(name, pickedIds())];
            naming = false;
            newName = '';
            error = null;
        } catch {
            error = 'Couldn’t save the group — is the Photos service running?';
        }
    }

    async function replaceGroup(group: PhotoGroup) {
        try {
            const updated = await api.photos.updateGroup(group.id, group.name, pickedIds());
            groups = groups.map((g) => (g.id === group.id ? updated : g));
        } catch {
            error = `Couldn’t update ${group.name}.`;
        }
    }

    async function deleteGroup(group: PhotoGroup) {
        if (await api.photos.deleteGroup(group.id)) groups = groups.filter((g) => g.id !== group.id);
        else error = `Couldn’t delete ${group.name}.`;
    }

    const count = (n: number) => `${n} photo${n === 1 ? '' : 's'}`;
</script>

{#snippet errorNotice()}
    <InlineMessage tone="error">{error}</InlineMessage>
{/snippet}

<!--
@component
Photo mode's picker: one photo at a time or several (shift-click picks a run), select
all/none, and named groups — Winter, Family, … — that pick a set of photos in one tap.
Groups live in the Photos service; the picks are saved with the frame settings.
-->
<Modal
    bind:open
    title="Photo mode"
    subtitle={chosen === 0 ? 'None picked — shows Ambient’s photos' : `${chosen} picked`}
    size="xl"
    divided
    notice={error ? errorNotice : undefined}
>
    <div class="mb-4 flex flex-wrap items-center justify-between gap-3">
        <SegmentedControl
            options={[
                { value: 'one', label: 'One photo' },
                { value: 'several', label: 'Several' },
            ]}
            value={mode}
            onchange={(v) => (mode = v)}
            label="Pick"
        />
        <div class="flex items-center gap-4">
            <Button variant="link" onclick={() => setPicked([...order])} disabled={mode === 'one'}>Select all</Button>
            <Button variant="link" onclick={() => setPicked([])} disabled={chosen === 0}>Select none</Button>
        </div>
    </div>
    <p class="type-label mb-4 text-(--text-2)">
        {#if mode === 'one'}
            Tap a photo to show just that one.
        {:else}
            Tap photos to make an album, shown one per refresh.
            <span class="pointer-coarse:hidden">Shift-click picks a run.</span>
        {/if}
    </p>

    <div class="mb-5">
        <div class="mb-2 flex items-center justify-between gap-3">
            <p class="eyebrow">Groups</p>
            {#if groups.length > 0}
                <Button variant="ghost" size="sm" onclick={() => (editing = !editing)}>
                    {editing ? 'Done' : 'Edit groups'}
                </Button>
            {/if}
        </div>
        {#if editing}
            <div class="flex flex-col">
                {#each groups as g (g.id)}
                    <ListRow
                        title={g.name}
                        subtitle={count(g.photo_ids.length)}
                        onremove={() => deleteGroup(g)}
                        removeLabel="Delete {g.name}"
                    >
                        {#snippet actions()}
                            <Button size="sm" onclick={() => replaceGroup(g)} disabled={chosen === 0}>
                                Use current picks
                            </Button>
                        {/snippet}
                    </ListRow>
                {/each}
            </div>
        {:else}
            {#if groups.length === 0 && !naming}
                <p class="type-label mb-2 text-(--text-2)">
                    Save what you’ve picked as a group — a season, a theme, a trip — to pick it again in one tap.
                </p>
            {/if}
            <div class="flex flex-wrap items-center gap-2" role="group" aria-label="Photo groups">
                {#each groups as g (g.id)}
                    <Chip
                        pressed={isGroupPicked(frameSettings.photoIds, g)}
                        onclick={() => clickGroup(g)}
                        disabled={g.photo_ids.length === 0}
                    >
                        {g.name}
                        <span class="tabular-nums opacity-70">{g.photo_ids.length}</span>
                    </Chip>
                {/each}
                {#if naming}
                    <form class="flex flex-wrap items-center gap-2" onsubmit={saveGroup}>
                        <TextInput
                            size="sm"
                            bind:value={newName}
                            maxlength={40}
                            placeholder="Group name, e.g. Winter"
                            aria-label="Group name"
                            autofocus
                        />
                        <Button type="submit" variant="primary" size="sm" disabled={!newName.trim()}>Save</Button>
                        <Button variant="ghost" size="sm" onclick={() => (naming = false)}>Cancel</Button>
                    </form>
                {:else}
                    <Button size="sm" onclick={() => (naming = true)} disabled={chosen === 0}>
                        <Plus class="icon-sm" />
                        Save picks as a group
                    </Button>
                {/if}
            </div>
        {/if}
    </div>

    <div class="@container">
        <div class="grid grid-cols-4 gap-2 @sm:grid-cols-6">
            {#each uploads as u (u.id)}
                {@const on = frameSettings.photoIds.includes(u.id)}
                <button
                    onclick={(e) => clickPhoto(u.id, e)}
                    aria-pressed={on}
                    aria-label={u.caption ?? 'Photo'}
                    class="relative aspect-square overflow-hidden rounded-lg border-2 transition-all
                        {on ? 'border-(--accent)' : 'border-transparent opacity-70 hover:opacity-100'}"
                >
                    <img src={u.thumb_url} alt="" class="absolute inset-0 h-full w-full object-cover" />
                    {#if on}
                        <span class="absolute top-1 right-1 rounded-full bg-(--accent) p-0.5 text-(--accent-fg)">
                            <Check class="icon-xs" />
                        </span>
                    {/if}
                </button>
            {/each}
        </div>
    </div>
</Modal>
