export interface TrainStop {
    label: string;
    stopKey: string;
    // Keys `${routeShortName}|${headsign ?? ''}|${mode}` (see TrainsWidget.svelte); undefined/empty = show all lines.
    lineFilter?: string[];
}

export const DEFAULT_TRAIN_STOPS: TrainStop[] = [];
