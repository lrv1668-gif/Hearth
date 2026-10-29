<script lang="ts">
    let { class: className = '' }: { class?: string } = $props();
</script>

<div class="animate-pulse rounded bg-(--surface-hi) {className}"></div>
