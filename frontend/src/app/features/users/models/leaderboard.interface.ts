export interface LeaderboardUser {
  rank: number;
  name: string;
  avatar: string;
  totalXp: number;
  currentStreak: number;
  longestStreak: number;
  level: number;
  change: number; // Position change (positive = moved up, negative = moved down, 0 = no change)
}
