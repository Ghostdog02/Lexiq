export enum TimeFrame {
  Weekly = 'Weekly',
  Monthly = 'Monthly',
  AllTime = 'AllTime'
}

export interface LeaderboardEntry {
  rank: number;
  userId: string;
  userName: string;
  avatar: string | null;
  totalXp: number;
  currentStreak: number;
  longestStreak: number;
  level: number;
  change: number;
  isCurrentUser: boolean;
}

export interface LeaderboardResponse {
  entries: LeaderboardEntry[];
  currentUserEntry: LeaderboardEntry | null;
}
