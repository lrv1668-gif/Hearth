/** @type {import('tailwindcss').Config} */
export default {
    content: ['./src/**/*.{html,js,svelte,ts}'],
    theme: {
        // Weights are themed — font-medium is var(--weight-medium) (set per font
        // theme in src/fonts.css), not literal 500. Replacing the scale (not
        // extending) keeps un-themed weights like font-light out of the codebase.
        fontWeight: {
            normal: 'var(--weight-regular)',
            medium: 'var(--weight-medium)',
            semibold: 'var(--weight-semibold)',
            bold: 'var(--weight-bold)',
        },
        extend: {},
    },
    plugins: [],
};
