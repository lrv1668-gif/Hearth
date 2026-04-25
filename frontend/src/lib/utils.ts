/**
 * Converts a 24-hour "HH:MM" string to 12-hour local format, e.g. "4:00PM".
 * The minutes are cut off if they're 0, e.g. 13:00 become 1PM.
 * @param time The time in the HH:MM format, in 24-hour time.
 * @returns
 */
export function formatTime(time: string): string {
  const [h, m] = time.split(":").map(Number);
  const period = h >= 12 ? "PM" : "AM";
  const hour = h % 12 || 12;

  if (m !== 0) {
    return `${hour}:${String(m).padStart(2, "0")}${period}`;
  }

  return `${hour}${period}`;
}
