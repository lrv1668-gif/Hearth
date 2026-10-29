/** Shared button look, for <Button> and for other elements that should read as one
 *  (FileUploadButton's <label>). */

export type ButtonVariant = 'primary' | 'secondary' | 'link' | 'ghost';
export type ButtonSize = 'sm' | 'md' | 'lg';

const variants: Record<ButtonVariant, string> = {
    // A filled accent action — one per group, for the thing the user came to do
    primary: 'rounded-lg bg-(--accent) font-semibold text-(--accent-fg) hover:bg-(--accent-hi)',
    // An outlined pill — everyday actions
    secondary: 'rounded-full border border-(--border) text-(--text-1) hover:border-(--text-2) hover:bg-(--surface-hi)',
    // Inline accent text
    link: 'text-(--accent) underline-offset-2 hover:text-(--accent-hi) hover:underline',
    // Quiet text that brightens on hover
    ghost: 'text-(--text-2) hover:text-(--text-1)',
};

// link and ghost sit inline with text, so they take the type size but no box
const boxed: Record<ButtonSize, string> = {
    sm: 'type-label min-h-9 px-3',
    md: 'type-label min-h-10 px-4',
    lg: 'type-body min-h-11 px-4',
};
const inline: Record<ButtonSize, string> = { sm: 'type-label', md: 'type-label', lg: 'type-body' };

export function buttonClass(variant: ButtonVariant = 'secondary', size: ButtonSize = 'md'): string {
    const isBoxed = variant === 'primary' || variant === 'secondary';
    // Boxed labels stay on one line; inline ones wrap with the text around them
    const sizing = isBoxed ? `${boxed[size]} whitespace-nowrap` : `${inline[size]} text-left`;
    return `inline-flex items-center justify-center gap-1.5 transition-colors disabled:pointer-events-none disabled:opacity-50 aria-disabled:pointer-events-none aria-disabled:opacity-50 ${sizing} ${variants[variant]}`;
}
