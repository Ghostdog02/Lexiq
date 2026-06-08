import {
  Component,
  ElementRef,
  OnInit,
  ViewChild,
  inject,
} from '@angular/core';
import { Achievement, Language, UserProfile } from '../../models/user.model';
import { ProfileService } from '../../services/profile.service';

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [],
  templateUrl: './profile.component.html',
  styleUrls: ['./profile.component.scss'],
})
export class ProfileComponent implements OnInit {
  private profileService = inject(ProfileService);

  @ViewChild('avatarInput') avatarInput!: ElementRef<HTMLInputElement>;

  profile: UserProfile | null = null;
  enrolledLanguages: Language[] = [];

  unlockedAchievements: Achievement[] = [];
  nextLockedAchievements: Achievement[] = [];
  xpProgressMap = new Map<string, number>();
  unlockedDateMap = new Map<string, string>();

  formattedJoinDate = '';
  daysSinceJoined = 0;

  isLoading = true;
  isUploadingAvatar = false;
  error: string | null = null;

  async ngOnInit(): Promise<void> {
    try {
      const profile = await this.profileService.getMyProfile();
      this.profile = profile;
      this.enrolledLanguages = [];

      this.unlockedAchievements = profile.achievements.filter(
        (a) => a.isUnlocked
      );
      this.nextLockedAchievements = profile.achievements
        .filter((a) => !a.isUnlocked)
        .slice(0, 3);

      for (const a of profile.achievements) {
        const pct = Math.min(
          ((profile.totalXp / a.xpRequired) * 100),
          100
        );
        this.xpProgressMap.set(a.id, pct);

        if (a.isUnlocked && a.unlockedAt) {
          this.unlockedDateMap.set(a.id, this.formatDate(a.unlockedAt));
        }
      }

      this.formattedJoinDate = this.formatDate(profile.joinDate);
      const now = new Date();
      const joined = new Date(profile.joinDate);
      this.daysSinceJoined = Math.floor(
        (now.getTime() - joined.getTime()) / (1000 * 60 * 60 * 24)
      );
    } catch {
      this.error = 'Failed to load profile. Please try again.';
    } finally {
      this.isLoading = false;
    }
  }

  triggerAvatarUpload(): void {
    this.avatarInput.nativeElement.click();
  }

  async onAvatarFileSelected(event: Event): Promise<void> {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file || !this.profile) return;

    this.isUploadingAvatar = true;
    try {
      await this.profileService.uploadAvatar(file);
      const previewUrl = URL.createObjectURL(file);
      this.profile = { ...this.profile, avatarUrl: previewUrl };
    } catch {
      // silently ignore — could surface via toastr if needed
    } finally {
      this.isUploadingAvatar = false;
      input.value = '';
    }
  }

  private formatDate(dateStr: string): string {
    const date = new Date(dateStr);
    return new Intl.DateTimeFormat('en-US', {
      month: 'long',
      day: 'numeric',
      year: 'numeric',
    }).format(date);
  }
}
