/** Shared look for text inputs, text areas and selects. No width — callers set it. */

export type FieldSize = 'sm' | 'md';

const sizes: Record<FieldSize, string> = {
    sm: 'type-label px-2.5 py-1.5',
    md: 'type-body px-3 py-2',
};

export function fieldClass(size: FieldSize = 'md'): string {
    return `min-w-0 rounded-lg border border-(--border) bg-(--surface) text-(--text-1) transition-colors outline-none placeholder:text-(--text-2) focus:border-(--text-2) disabled:opacity-50 ${sizes[size]}`;
}
