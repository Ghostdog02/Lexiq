export interface Language {
  id: string;
  name: string;
  flag: string;
  level: number;
  xp: number;
  totalXp: number;
  progress: number;
}

export interface Achievement {
  id: string;
  name: string;
  description: string;
  xpRequired: number;
  icon: string;
  isUnlocked: boolean;
  unlockedAt?: string;
}

export interface UserProfile {
  userId: string;
  userName: string;
  joinDate: string;
  totalXp: number;
  level: number;
  currentStreak: number;
  longestStreak: number;
  avatarUrl: string | null;
  achievements: Achievement[];
}
