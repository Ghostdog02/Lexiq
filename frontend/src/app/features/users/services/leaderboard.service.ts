import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { LeaderboardResponse, TimeFrame } from '../models/leaderboard.interface';

@Injectable({ providedIn: 'root' })
export class LeaderboardService {
  private httpClient = inject(HttpClient);

  async getLeaderboard(timeFrame: TimeFrame): Promise<LeaderboardResponse> {
    const result = await firstValueFrom(
      this.httpClient.get<LeaderboardResponse>(
        `/api/leaderboard?timeFrame=${timeFrame}`,
        { withCredentials: true }
      )
    );

    return result ?? { entries: [], currentUserEntry: null };
  }
}
