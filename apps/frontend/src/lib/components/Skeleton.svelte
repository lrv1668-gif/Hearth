<script lang="ts">
    let { class: className = '' }: { class?: string } = $props();
</script>

<div class="rounded bg-(--surface-hi) motion-safe:animate-pulse {className}" aria-hidden="true"></div>
