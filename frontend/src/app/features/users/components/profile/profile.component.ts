import { Component, OnInit, inject } from '@angular/core';
import { Achievement, Language, UserProfile } from '../../models/user.model';
import { ProfileService } from '../../services/profile.service';

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [],
  templateUrl: './profile.component.html',
  styleUrls: ['./profile.component.scss']
})
export class ProfileComponent implements OnInit {
  private profileService = inject(ProfileService);

  profile: UserProfile | null = null;
  unlockedAchievements: Achievement[] = [];
  lockedAchievements: Achievement[] = [];
  enrolledLanguages: Language[] = [];
  isLoading = true;
  error: string | null = null;

  async ngOnInit(): Promise<void> {
    try {
      this.profile = await this.profileService.getMyProfile();
      this.unlockedAchievements = this.profile.achievements.filter(a => a.isUnlocked);
      this.lockedAchievements = this.profile.achievements.filter(a => !a.isUnlocked);
    } catch {
      this.error = 'Failed to load profile. Please try again.';
    } finally {
      this.isLoading = false;
    }
  }

  getXpProgress(achievement: Achievement): number {
    return Math.min(((this.profile?.totalXp ?? 0) / achievement.xpRequired) * 100, 100);
  }

  formatDate(dateStr: string): string {
    const date = new Date(dateStr);
    return new Intl.DateTimeFormat('en-US', {
      month: 'short',
      day: 'numeric',
      year: 'numeric'
    }).format(date);
  }

  getDaysSinceJoined(): number {
    const now = new Date();
    const joined = new Date(this.profile!.joinDate);
    const diff = now.getTime() - joined.getTime();
    return Math.floor(diff / (1000 * 60 * 60 * 24));
  }
}
