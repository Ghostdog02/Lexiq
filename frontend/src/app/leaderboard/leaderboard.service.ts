import { Injectable } from '@angular/core';
import { Observable, of } from 'rxjs';
import { LeaderboardEntry } from './leaderboard.interface';

@Injectable({
  providedIn: 'root'
})
export class LeaderboardService {
  getTopLearners(): Observable<LeaderboardEntry[]> {
    const data: LeaderboardEntry[] = [
      { rank: 1, userId: '1', username: 'PolyglotMaster', xp: 15400, streak: 45, language: 'Spanish', isCurrentUser: false },
      { rank: 2, userId: '2', username: 'Sarah Learns', xp: 14200, streak: 32, language: 'French', isCurrentUser: false },
      { rank: 3, userId: '3', username: 'DevHiker', xp: 12800, streak: 12, language: 'German', isCurrentUser: false },
      { rank: 4, userId: '4', username: 'CodeNinja', xp: 11500, streak: 105, language: 'Japanese', isCurrentUser: true },
      { rank: 5, userId: '5', username: 'Traveler_Joe', xp: 10200, streak: 5, language: 'Italian', isCurrentUser: false },
      { rank: 6, userId: '6', username: 'LinguaFranca', xp: 9800, streak: 14, language: 'Spanish', isCurrentUser: false },
      { rank: 7, userId: '7', username: 'NewbieDev', xp: 8500, streak: 2, language: 'Python', isCurrentUser: false },
    ];
    return of(data);
  }
}