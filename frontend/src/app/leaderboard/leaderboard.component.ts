import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { LeaderboardService } from './leaderboard.service';
import { LeaderboardEntry } from './leaderboard.interface';

@Component({
  selector: 'app-leaderboard',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './leaderboard.component.html',
  styleUrls: ['./leaderboard.component.scss']
})
export class LeaderboardComponent implements OnInit {
  leaders: LeaderboardEntry[] = [];
  topThree: LeaderboardEntry[] = [];
  restOfList: LeaderboardEntry[] = [];

  constructor(private leaderboardService: LeaderboardService) {}

  ngOnInit(): void {
    this.leaderboardService.getTopLearners().subscribe(data => {
      this.leaders = data;
      this.topThree = data.slice(0, 3);
      this.restOfList = data.slice(3);
    });
  }

  getRankClass(rank: number): string {
    if (rank === 1) return 'rank-1';
    if (rank === 2) return 'rank-2';
    if (rank === 3) return 'rank-3';
    return '';
  }
}