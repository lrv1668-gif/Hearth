import type { PhotoCategory } from './photos';

export const cadenceOptions = [
    { label: '2m', value: 120 },
    { label: '5m', value: 300 },
    { label: '10m', value: 600 },
    { label: '30m', value: 1800 },
    { label: '1hr', value: 3600 },
    { label: '2hr', value: 7200 },
];

// null = same cadence as daytime
export const nightCadenceOptions: { label: string; value: number | null }[] = [
    { label: 'Same as day', value: null },
    ...cadenceOptions,
];

export const categoryOptions: { id: PhotoCategory; label: string }[] = [
    { id: 'nature', label: 'Nature' },
    { id: 'architecture', label: 'Architecture' },
    { id: 'interiors', label: 'Interiors' },
    { id: 'abstract', label: 'Abstract art' },
    // Expanded by the Photos service into a season-appropriate query
    { id: 'seasonal', label: 'Seasonal' },
];
