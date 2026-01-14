export interface LeaderboardEntry {
  rank: number;
  userId: string;
  username: string;
  avatarUrl?: string; 
  xp: number;
  streak: number;
  language: string;
  isCurrentUser: boolean;
}