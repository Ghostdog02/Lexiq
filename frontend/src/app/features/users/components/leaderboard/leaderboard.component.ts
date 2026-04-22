import { Component, DestroyRef, inject, OnInit } from '@angular/core';
import { LeaderboardEntry, LeaderboardResponse, TimeFrame } from '../../models/leaderboard.interface';
import { LeaderboardService } from '../../services/leaderboard.service';

@Component({
  selector: 'app-leaderboard',
  standalone: true,
  imports: [],
  templateUrl: './leaderboard.component.html',
  styleUrls: ['./leaderboard.component.scss']
})
export class LeaderboardComponent implements OnInit {
  private leaderboardService = inject(LeaderboardService);

  TimeFrame = TimeFrame;
  timeFrame: TimeFrame = TimeFrame.AllTime;
  leaderboardEntries: LeaderboardEntry[] = [];
  currentUserEntry: LeaderboardEntry | null = null;
  isLoading = false;

  ngOnInit(): void {
    this.loadLeaderboard();
  }

  async setTimeFrame(timeFrame: TimeFrame): Promise<void> {
    this.timeFrame = timeFrame;
    await this.loadLeaderboard();
  }

  getAvatarUrl(entry: LeaderboardEntry): string {
    return entry.avatar ?? `https://api.dicebear.com/7.x/avataaars/svg?seed=${entry.userId}`;
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

  private async loadLeaderboard(): Promise<void> {
    this.isLoading = true;
    try {
      const response = await this.leaderboardService.getLeaderboard(this.timeFrame);
      this.leaderboardEntries = response.entries;
      this.currentUserEntry = response.currentUserEntry;
    } catch (error) {
      console.error('Failed to load leaderboard:', error);
      this.leaderboardEntries = [];
      this.currentUserEntry = null;
    } finally {
      this.isLoading = false;
    }
  }
}
