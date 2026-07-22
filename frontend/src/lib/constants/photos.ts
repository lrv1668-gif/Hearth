export type PhotoCategory = 'nature' | 'architecture' | 'interiors' | 'abstract' | 'seasonal';

// Open-ended union: exhaustive for known values, forward-compatible for future sources.
export type PhotoSource = 'unsplash' | 'local' | (string & {});
