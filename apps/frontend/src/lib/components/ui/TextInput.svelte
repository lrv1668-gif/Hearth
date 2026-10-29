<script lang="ts">
    import type { HTMLInputAttributes } from 'svelte/elements';
    import { fieldClass, type FieldSize } from './field';

    interface Props extends Omit<HTMLInputAttributes, 'size'> {
        value?: HTMLInputAttributes['value'];
        size?: FieldSize;
        /** The <input> element, for callers that read it directly. */
        ref?: HTMLInputElement;
    }

    let { value = $bindable(), size = 'md', ref = $bindable(), class: extra = '', ...rest }: Props = $props();
</script>

<input bind:this={ref} bind:value class="{fieldClass(size)} {extra}" {...rest} />
