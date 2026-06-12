import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, ActivatedRoute } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { firstValueFrom } from 'rxjs';
import { formatCountdown } from '../../../../shared/utils/time.utils';
import { ToastrService } from 'ngx-toastr';
import { LessonService } from '../../services/lesson.service';
import { ContentParserService } from '../../../../shared/services/content-parser.service';
import { AuthService } from '../../../../auth/auth.service';
import { Lesson } from '../../models/lesson.interface';

/**
 * Displays lesson content (instructions, textbook material).
 * Navigates to separate exercise-viewer route for exercises.
 */
@Component({
  selector: 'app-lesson-viewer',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './lesson-viewer.component.html',
  styleUrl: './lesson-viewer.component.scss'
})
export class LessonViewerComponent implements OnInit {
  private router = inject(Router);
  private route = inject(ActivatedRoute);
  private http = inject(HttpClient);
  private sanitizer = inject(DomSanitizer);
  private lessonService = inject(LessonService);
  private contentParser = inject(ContentParserService);
  private authService = inject(AuthService);
  private toastr = inject(ToastrService);

  lesson: Lesson | null = null;
  parsedContent: SafeHtml | null = null;
  isLoading = true;
  error: string | null = null;
  private hearts = 5;
  private nextRefillAt: Date | null = null;

  async ngOnInit() {
    const lessonId = this.route.snapshot.paramMap.get('id');
    if (lessonId) {
      await Promise.all([this.loadLesson(lessonId), this.loadHearts()]);
    } else {
      this.error = 'No lesson ID provided';
      this.isLoading = false;
    }
  }

  private async loadLesson(lessonId: string): Promise<void> {
    this.isLoading = true;
    this.error = null;

    try {
      const apiLesson = await this.lessonService.getLesson(lessonId);

      if (!apiLesson) {
        this.error = 'Lesson not found';
        return;
      }

      this.lesson = apiLesson;
      if (apiLesson.lessonContent) {
        this.parsedContent = this.sanitizer.bypassSecurityTrustHtml(
          this.contentParser.parse(apiLesson.lessonContent)
        );
      }
    } catch (err) {
      console.error('❌ Error loading lesson:', err);
      this.error = 'Failed to load lesson';
    } finally {
      this.isLoading = false;
    }
  }

  private async loadHearts(): Promise<void> {
    if (this.authService.getIsAdmin() || this.authService.getIsContentCreator() || !this.authService.getIsAuth()) return;
    try {
      const response = await firstValueFrom(
        this.http.get<{ hearts: number; nextRefillAt: string | null }>('/api/user/hearts', { withCredentials: true })
      );
      this.hearts = response.hearts;
      this.nextRefillAt = response.nextRefillAt ? new Date(response.nextRefillAt) : null;
    } catch {
      // Non-critical — default allows start
    }
  }

  startExercises() {
    if (!this.lesson) return;

    if (this.hearts <= 0) {
      const timeLabel = this.nextRefillAt ? formatCountdown(this.nextRefillAt) : '4h';
      this.toastr.error(`Try again in ${timeLabel}.`, 'Not enough hearts', { toastClass: 'ngx-toastr toast-auth' });
      return;
    }

    this.router.navigate(['/lesson', this.lesson.lessonId, 'exercises']);
  }

  goBack() {
    this.router.navigate(['/courses']);
  }
}
