import { Component, OnInit } from '@angular/core';
import { Language, Achievement } from '../../models/user.model';

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [],
  templateUrl: './profile.component.html',
  styleUrls: ['./profile.component.scss']
})
export class ProfileComponent implements OnInit {
  user = {
    name: 'Alex Rodriguez',
    email: 'alex.rodriguez@example.com',
    joinDate: new Date('2024-01-15'),
    totalXp: 12450,
    currentStreak: 47,
    longestStreak: 89,
    avatar: 'https://api.dicebear.com/7.x/avataaars/svg?seed=Alex'
  };

  enrolledLanguages: Language[] = [
    {
      id: '1',
      name: 'Spanish',
      flag: '🇪🇸',
      level: 8,
      xp: 450,
      totalXp: 4250,
      progress: 75
    },
    {
      id: '2',
      name: 'French',
      flag: '🇫🇷',
      level: 5,
      xp: 280,
      totalXp: 2800,
      progress: 56
    },
    {
      id: '3',
      name: 'Japanese',
      flag: '🇯🇵',
      level: 3,
      xp: 120,
      totalXp: 1400,
      progress: 24
    }
  ];

  achievements: Achievement[] = [
    {
      id: '1',
      name: 'First Steps',
      description: 'Earned your first 100 XP',
      xpRequired: 100,
      icon: '🌱',
      unlocked: true,
      unlockedDate: new Date('2024-01-16')
    },
    {
      id: '2',
      name: 'Getting Started',
      description: 'Reached 500 XP',
      xpRequired: 500,
      icon: '🚀',
      unlocked: true,
      unlockedDate: new Date('2024-01-25')
    },
    {
      id: '3',
      name: 'Dedicated Learner',
      description: 'Accumulated 1,000 XP',
      xpRequired: 1000,
      icon: '📚',
      unlocked: true,
      unlockedDate: new Date('2024-02-10')
    },
    {
      id: '4',
      name: 'Rising Star',
      description: 'Reached 2,500 XP',
      xpRequired: 2500,
      icon: '⭐',
      unlocked: true,
      unlockedDate: new Date('2024-03-05')
    },
    {
      id: '5',
      name: 'Expert Explorer',
      description: 'Achieved 5,000 XP',
      xpRequired: 5000,
      icon: '🏆',
      unlocked: true,
      unlockedDate: new Date('2024-04-20')
    },
    {
      id: '6',
      name: 'Master Linguist',
      description: 'Accumulated 10,000 XP',
      xpRequired: 10000,
      icon: '👑',
      unlocked: true,
      unlockedDate: new Date('2024-06-15')
    },
    {
      id: '7',
      name: 'Legend',
      description: 'Reached 25,000 XP',
      xpRequired: 25000,
      icon: '💎',
      unlocked: false
    },
    {
      id: '8',
      name: 'Polyglot Pro',
      description: 'Earned 50,000 XP',
      xpRequired: 50000,
      icon: '🌟',
      unlocked: false
    }
  ];

  unlockedAchievements: Achievement[] = [];
  lockedAchievements: Achievement[] = [];

  ngOnInit(): void {
    this.unlockedAchievements = this.achievements.filter(a => a.unlocked);
    this.lockedAchievements = this.achievements.filter(a => !a.unlocked);
  }

  getXpProgress(achievement: Achievement): number {
    return Math.min((this.user.totalXp / achievement.xpRequired) * 100, 100);
  }

  formatDate(date: Date): string {
    return new Intl.DateTimeFormat('en-US', {
      month: 'short',
      day: 'numeric',
      year: 'numeric'
    }).format(date);
  }

  getDaysSinceJoined(): number {
    const now = new Date();
    const diff = now.getTime() - this.user.joinDate.getTime();
    return Math.floor(diff / (1000 * 60 * 60 * 24));
  }
}
