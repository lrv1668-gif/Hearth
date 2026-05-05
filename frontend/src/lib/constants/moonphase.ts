export const SYNODIC_PERIOD = 29.53059; // days

export const KNOWN_NEW_MOON = new Date('2000-01-06T18:14:00Z');

export const phaseNames: [number, string][] = [
    [0.0625, 'New Moon'],
    [0.1875, 'Waxing Crescent'],
    [0.3125, 'First Quarter'],
    [0.4375, 'Waxing Gibbous'],
    [0.5625, 'Full Moon'],
    [0.6875, 'Waning Gibbous'],
    [0.8125, 'Last Quarter'],
    [0.9375, 'Waning Crescent'],
    [1.0001, 'New Moon'],
];

// Next major phase (0 = new, 0.25 = first quarter, 0.5 = full, 0.75 = last quarter)
export const majors: [number, string][] = [
    [0, 'New Moon'],
    [0.25, 'First Quarter'],
    [0.5, 'Full Moon'],
    [0.75, 'Last Quarter'],
];
