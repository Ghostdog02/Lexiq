import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, ActivatedRoute } from '@angular/router';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { LessonService } from '../../services/lesson.service';
import { ContentParserService } from '../../../../shared/services/content-parser.service';
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
  private sanitizer = inject(DomSanitizer);
  private lessonService = inject(LessonService);
  private contentParser = inject(ContentParserService);

  lesson: Lesson | null = null;
  parsedContent: SafeHtml | null = null;
  isLoading = true;
  error: string | null = null;

  ngOnInit() {
    const lessonId = this.route.snapshot.paramMap.get('id');
    if (lessonId) {
      this.loadLesson(lessonId);
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

  startExercises() {
    if (this.lesson) {
      this.router.navigate(['/lesson', this.lesson.lessonId, 'exercises']);
    }
  }

  goBack() {
    this.router.navigate(['/']);
  }
}
