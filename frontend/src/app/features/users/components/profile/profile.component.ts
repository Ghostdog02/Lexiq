import {
  Component,
  DestroyRef,
  ElementRef,
  OnInit,
  ViewChild,
  inject,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { firstValueFrom, interval } from 'rxjs';
import { HttpClient } from '@angular/common/http';
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
  private http = inject(HttpClient);
  private destroyRef = inject(DestroyRef);

  @ViewChild('avatarInput') avatarInput!: ElementRef<HTMLInputElement>;

  profile: UserProfile | null = null;
  enrolledLanguages: Language[] = [];

  unlockedAchievements: Achievement[] = [];
  nextLockedAchievements: Achievement[] = [];
  xpProgressMap = new Map<string, number>();
  unlockedDateMap = new Map<string, string>();

  formattedJoinDate = '';
  daysSinceJoined = 0;

  hearts: number | null = null;
  nextRefillAt: Date | null = null;
  heartsCountdown = signal('');

  isLoading = true;
  isUploadingAvatar = false;
  error: string | null = null;

  async ngOnInit(): Promise<void> {
    try {
      const [profile] = await Promise.all([
        this.profileService.getMyProfile(),
        this.loadHearts(),
      ]);
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

  private async loadHearts(): Promise<void> {
    try {
      const response = await firstValueFrom(
        this.http.get<{ hearts: number; nextRefillAt: string | null }>('/api/user/hearts', {
          withCredentials: true,
        })
      );
      this.hearts = response.hearts;
      this.nextRefillAt = response.nextRefillAt ? new Date(response.nextRefillAt) : null;

      if (this.hearts < 5) {
        this.heartsCountdown.set(this.computeHeartCountdown());
        interval(1_000)
          .pipe(takeUntilDestroyed(this.destroyRef))
          .subscribe(() => { this.heartsCountdown.set(this.computeHeartCountdown()); });
      }
    } catch {
      this.hearts = null;
    }
  }

  private computeHeartCountdown(): string {
    if (!this.nextRefillAt) return '';
    const ms = Math.max(0, this.nextRefillAt.getTime() - Date.now());
    const totalSeconds = Math.ceil(ms / 1000);
    const hours = Math.floor(totalSeconds / 3600);
    const minutes = Math.floor((totalSeconds % 3600) / 60);
    const seconds = totalSeconds % 60;
    if (hours === 0 && minutes === 0) return `${seconds}s`;
    if (hours === 0) return `${minutes}m ${seconds}s`;
    return `${hours}h ${minutes}m ${seconds}s`;
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
