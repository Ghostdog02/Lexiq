import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, ActivatedRoute } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { LessonService } from '../../services/lesson.service';
import { ContentParserService } from '../../../../shared/services/content-parser.service';
import { Lesson, SubmitAnswerResponse } from '../../models/lesson.interface';
import {
  AnyExercise,
  ExerciseType,
  MultipleChoiceExercise,
} from '../../models/exercise.interface';

interface ExerciseAnswer {
  answer: string | string[];
  isSubmitted: boolean;
  isCorrect: boolean | null;
  correctAnswer?: string | null;
  explanation?: string | null;
}

@Component({
  selector: 'app-lesson-viewer',
  standalone: true,
  imports: [CommonModule, FormsModule],
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
  isLoading = true;
  error: string | null = null;
  currentView: 'content' | 'exercises' = 'content';
  currentExerciseIndex = 0;
  answers: Map<number, ExerciseAnswer> = new Map();
  isSubmitting = false;

  // For tracking completion
  earnedXp = 0;
  totalPossibleXp = 0;

  // Expose enum to template
  ExerciseType = ExerciseType;

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
      this.initializeAnswers();
      this.calculateTotalXp();
    } catch (err) {
      console.error('❌ Error loading lesson:', err);
      this.error = 'Failed to load lesson';
    } finally {
      this.isLoading = false;
    }
  }

  private initializeAnswers() {
    if (!this.lesson) return;

    console.log(this.lesson);

    this.lesson.exercises.forEach((_, index) => {
      this.answers.set(index, {
        answer: '',
        isSubmitted: false,
        isCorrect: null
      });
    });
  }

  private calculateTotalXp() {
    if (!this.lesson) return;
    this.totalPossibleXp = this.lesson.exercises.reduce((sum, ex) => sum + (ex.points || 10), 0);
  }

  parseContent(content: string): SafeHtml {
    return this.sanitizer.bypassSecurityTrustHtml(this.contentParser.parse(content));
  }

  // Navigation
  get currentExercise(): AnyExercise | null {
    if (!this.lesson || this.lesson.exercises.length === 0) return null;
    return this.lesson.exercises[this.currentExerciseIndex] as AnyExercise;
  }

  get currentAnswer(): ExerciseAnswer | null {
    return this.answers.get(this.currentExerciseIndex) || null;
  }

  get exerciseProgress(): number {
    if (!this.lesson || this.lesson.exercises.length === 0) return 0;
    const completed = Array.from(this.answers.values()).filter(a => a.isSubmitted).length;
    return (completed / this.lesson.exercises.length) * 100;
  }

  get allExercisesCompleted(): boolean {
    if (!this.lesson) return false;
    return Array.from(this.answers.values()).every(a => a.isSubmitted);
  }

  startExercises() {
    this.currentView = 'exercises';
    this.currentExerciseIndex = 0;
  }

  goToContent() {
    this.currentView = 'content';
  }

  previousExercise() {
    if (this.currentExerciseIndex > 0) {
      this.currentExerciseIndex--;
    }
  }

  nextExercise() {
    if (this.lesson && this.currentExerciseIndex < this.lesson.exercises.length - 1) {
      this.currentExerciseIndex++;
    }
  }

  // Answer handling
  updateAnswer(value: string) {
    const current = this.answers.get(this.currentExerciseIndex);
    if (current && !current.isSubmitted) {
      current.answer = value;
    }
  }

  selectMultipleChoiceOption(optionId: string) {
    const current = this.answers.get(this.currentExerciseIndex);
    if (current && !current.isSubmitted) {
      current.answer = optionId;
    }
  }

  async submitAnswer() {
    const current = this.answers.get(this.currentExerciseIndex);
    const exercise = this.currentExercise;
    if (!current || !exercise || current.isSubmitted || this.isSubmitting) return;

    this.isSubmitting = true;

    try {
      const answerValue = Array.isArray(current.answer) ? current.answer[0] : current.answer;

      const result: SubmitAnswerResponse = await this.lessonService.submitExerciseAnswer(
        exercise.id,
        answerValue
      );

      current.isSubmitted = true;
      current.isCorrect = result.isCorrect;
      current.correctAnswer = result.correctAnswer;
      current.explanation = result.explanation;

      // Update XP from backend
      this.earnedXp = result.lessonProgress.earnedXp;
      this.totalPossibleXp = result.lessonProgress.totalPossibleXp;
    } catch (err) {
      console.error('Failed to submit answer:', err);
    } finally {
      this.isSubmitting = false;
    }
  }

  // Completion
  async finishLesson() {
    if (!this.lesson) return;

    try {
      const result = await this.lessonService.completeLesson(this.lesson.lessonId);

      if (result.isCompleted) {
        this.router.navigate(['/']);
      } else {
        alert(
          `You need ${Math.round(result.requiredThreshold * 100)}% to complete this lesson. ` +
          `You scored ${Math.round(result.completionPercentage * 100)}%.`
        );
      }
    } catch (err) {
      console.error('Failed to complete lesson:', err);
    }
  }

  goBack() {
    this.router.navigate(['/']);
  }

  // Helper for multiple choice
  isOptionSelected(optionId: string): boolean {
    const current = this.currentAnswer;
    return current?.answer === optionId;
  }

  getOptionClass(optionId: string, isCorrect: boolean): string {
    const current = this.currentAnswer;
    if (!current?.isSubmitted) {
      return this.isOptionSelected(optionId) ? 'selected' : '';
    }

    // After submission
    if (isCorrect) return 'correct';
    if (this.isOptionSelected(optionId) && !isCorrect) return 'incorrect';
    return '';
  }
}
