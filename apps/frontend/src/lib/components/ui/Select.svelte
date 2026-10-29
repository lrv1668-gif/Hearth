<script lang="ts">
    import type { Snippet } from 'svelte';
    import type { HTMLSelectAttributes } from 'svelte/elements';
    import { fieldClass, type FieldSize } from './field';

    interface Props extends Omit<HTMLSelectAttributes, 'size'> {
        value?: HTMLSelectAttributes['value'];
        size?: FieldSize;
        /** The <option>s. */
        children: Snippet;
    }

    let { value = $bindable(), size = 'md', class: extra = '', children, ...rest }: Props = $props();
</script>

<select bind:value class="{fieldClass(size)} {extra}" {...rest}>
    {@render children()}
</select>
