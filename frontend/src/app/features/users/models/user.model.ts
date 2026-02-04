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
  unlocked: boolean;
  unlockedDate?: Date;
}
