import { Component } from '@angular/core';
import { LeaderboardUser } from '../../models/leaderboard.interface';

@Component({
  selector: 'app-leaderboard',
  standalone: true,
  imports: [],
  templateUrl: './leaderboard.component.html',
  styleUrls: ['./leaderboard.component.scss']
})
export class LeaderboardComponent {
  currentUser = {
    name: 'Alex Rodriguez',
    rank: 4
  };

  timeFrame: 'weekly' | 'monthly' | 'allTime' = 'allTime';

  leaderboardUsers: LeaderboardUser[] = [
    {
      rank: 1,
      name: 'Sarah Chen',
      avatar: 'https://api.dicebear.com/7.x/avataaars/svg?seed=Sarah',
      totalXp: 45800,
      currentStreak: 127,
      longestStreak: 145,
      level: 24,
      change: 2
    },
    {
      rank: 2,
      name: 'Marcus Johnson',
      avatar: 'https://api.dicebear.com/7.x/avataaars/svg?seed=Marcus',
      totalXp: 42300,
      currentStreak: 98,
      longestStreak: 120,
      level: 22,
      change: -1
    },
    {
      rank: 3,
      name: 'Emma Wilson',
      avatar: 'https://api.dicebear.com/7.x/avataaars/svg?seed=Emma',
      totalXp: 38950,
      currentStreak: 156,
      longestStreak: 156,
      level: 21,
      change: 1
    },
    {
      rank: 4,
      name: 'Alex Rodriguez',
      avatar: 'https://api.dicebear.com/7.x/avataaars/svg?seed=Alex',
      totalXp: 35200,
      currentStreak: 47,
      longestStreak: 89,
      level: 19,
      change: -2
    },
    {
      rank: 5,
      name: 'Liam Zhang',
      avatar: 'https://api.dicebear.com/7.x/avataaars/svg?seed=Liam',
      totalXp: 32800,
      currentStreak: 73,
      longestStreak: 95,
      level: 18,
      change: 0
    },
    {
      rank: 6,
      name: 'Olivia Martinez',
      avatar: 'https://api.dicebear.com/7.x/avataaars/svg?seed=Olivia',
      totalXp: 29400,
      currentStreak: 61,
      longestStreak: 88,
      level: 17,
      change: 3
    },
    {
      rank: 7,
      name: 'Noah Kim',
      avatar: 'https://api.dicebear.com/7.x/avataaars/svg?seed=Noah',
      totalXp: 27100,
      currentStreak: 42,
      longestStreak: 76,
      level: 16,
      change: -1
    },
    {
      rank: 8,
      name: 'Sophia Patel',
      avatar: 'https://api.dicebear.com/7.x/avataaars/svg?seed=Sophia',
      totalXp: 24800,
      currentStreak: 55,
      longestStreak: 82,
      level: 15,
      change: 0
    },
    {
      rank: 9,
      name: 'James Anderson',
      avatar: 'https://api.dicebear.com/7.x/avataaars/svg?seed=James',
      totalXp: 22500,
      currentStreak: 38,
      longestStreak: 65,
      level: 14,
      change: 1
    },
    {
      rank: 10,
      name: 'Isabella Garcia',
      avatar: 'https://api.dicebear.com/7.x/avataaars/svg?seed=Isabella',
      totalXp: 20300,
      currentStreak: 29,
      longestStreak: 58,
      level: 13,
      change: -3
    }
  ];

  setTimeFrame(timeFrame: 'weekly' | 'monthly' | 'allTime'): void {
    this.timeFrame = timeFrame;
  }

  isCurrentUser(user: LeaderboardUser): boolean {
    return user.name === this.currentUser.name;
  }

  getRankBadgeClass(rank: number): string {
    if (rank === 1) return 'gold';
    if (rank === 2) return 'silver';
    if (rank === 3) return 'bronze';
    return '';
  }

  getRankIcon(rank: number): string {
    if (rank === 1) return '🥇';
    if (rank === 2) return '🥈';
    if (rank === 3) return '🥉';
    return '';
  }

  getChangeIcon(change: number): string {
    if (change > 0) return '↑';
    if (change < 0) return '↓';
    return '−';
  }

  getChangeClass(change: number): string {
    if (change > 0) return 'up';
    if (change < 0) return 'down';
    return 'neutral';
  }
}
