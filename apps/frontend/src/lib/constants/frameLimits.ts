// Limits the Display service also enforces on the frame's shared settings
// (Display/Records/FrameSettings.cs and FrameModes.cs). Display.Tests'
// FrontendLimitsTests reads this file and fails when a value drifts from its C#
// twin, so keep each one a plain `export const NAME = <integer>;` line.

/** Percent on the frame's reading type and icons. Never below 100: the tuned sizes
 *  are the e-paper legibility floor. FrameSettings.Min/Max/DefaultTextSize. */
export const DEFAULT_FRAME_TEXT_SIZE = 100;
export const MIN_FRAME_TEXT_SIZE = 100;
export const MAX_FRAME_TEXT_SIZE = 150;

/** The panel's native size in pixels, each edge. FrameSettings.Min/MaxSize. */
export const MIN_FRAME_SIZE = 200;
export const MAX_FRAME_SIZE = 4000;

/** Widgets per free column; three is the most that stays legible. FrameSettings.MaxColumnWidgets. */
export const MAX_COLUMN_WIDGETS = 3;

/** Left column's share of the width, in percent. FrameSettings.Min/MaxSplit. */
export const MIN_SPLIT = 30;
export const MAX_SPLIT = 70;

/** Minutes Ambient and Photo keep a photo. FrameSettings.*PhotoIntervalMinutes. */
export const DEFAULT_PHOTO_INTERVAL_MINUTES = 60;
export const MIN_PHOTO_INTERVAL_MINUTES = 15;
export const MAX_PHOTO_INTERVAL_MINUTES = 1440;

/** Note mode's text. FrameSettings.MaxNoteTitle/Body/Lines. */
export const MAX_NOTE_TITLE = 80;
export const MAX_NOTE_BODY = 600;
export const MAX_NOTE_LINES = 12;

/** Guest mode's text and Wi-Fi. FrameSettings.MaxGuest*; an SSID is at most 32 bytes and a
 *  WPA passphrase 63 characters. */
export const MAX_GUEST_TITLE = 80;
export const MAX_GUEST_NETWORK = 32;
export const MAX_GUEST_PASSWORD = 63;
export const MAX_GUEST_NOTES = 300;
export const MAX_GUEST_NOTE_LINES = 6;

/** Modes one schedule rule holds — its first mode plus ModeRule.MaxRotate more. */
export const MAX_RULE_MODES = 6;
