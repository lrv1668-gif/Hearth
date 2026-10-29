<script lang="ts">
    import type { HTMLTextareaAttributes } from 'svelte/elements';
    import { fieldClass, type FieldSize } from './field';

    interface Props extends HTMLTextareaAttributes {
        value?: HTMLTextareaAttributes['value'];
        size?: FieldSize;
    }

    let { value = $bindable(), size = 'md', class: extra = '', ...rest }: Props = $props();
</script>

<textarea bind:value class="{fieldClass(size)} {extra}" {...rest}></textarea>
