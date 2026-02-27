import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, ActivatedRoute } from '@angular/router';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { LessonService } from '../../services/lesson.service';
import { ContentParserService } from '../../../../shared/services/content-parser.service';
import { Lesson, SubmitAnswerResponse } from '../../models/lesson.interface';
import { AnyExercise } from '../../models/exercise.interface';
import { ExerciseViewerComponent } from '../exercise-viewer/exercise-viewer.component';

/**
 * Displays lesson content (instructions, textbook material) and delegates
 * exercise solving to ExerciseViewerComponent.
 */
@Component({
  selector: 'app-lesson-viewer',
  standalone: true,
  imports: [CommonModule, ExerciseViewerComponent],
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
  initialSubmissions: SubmitAnswerResponse[] = [];
  isLoading = true;
  error: string | null = null;
  currentView: 'content' | 'exercises' = 'content';

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
      const [apiLesson, submissions] = await Promise.all([
        this.lessonService.getLesson(lessonId),
        this.lessonService.getLessonSubmissions(lessonId)
      ]);

      if (!apiLesson) {
        this.error = 'Lesson not found';
        return;
      }

      this.lesson = apiLesson;
      this.initialSubmissions = submissions;
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
    this.currentView = 'exercises';
  }

  goToContent() {
    this.currentView = 'content';
  }

  goBack() {
    this.router.navigate(['/']);
  }
}
