export function formatCountdown(nextRefillAt: Date): string {
  const ms = Math.max(0, nextRefillAt.getTime() - Date.now());
  const totalSeconds = Math.ceil(ms / 1000);
  const hours = Math.floor(totalSeconds / 3600);
  const minutes = Math.floor((totalSeconds % 3600) / 60);
  const seconds = totalSeconds % 60;
  if (hours === 0 && minutes === 0) return `${seconds}s`;
  if (hours === 0) return `${minutes}m ${seconds}s`;
  return `${hours}h ${minutes}m ${seconds}s`;
}
