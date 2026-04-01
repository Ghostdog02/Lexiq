import { Component, OnInit, OnDestroy, inject, DestroyRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, NonNullableFormBuilder } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import {
  AnyExercise,
  ExerciseType,
  MultipleChoiceExercise,
  FillInBlankExercise,
  TranslationExercise,
  ListeningExercise
} from '../../models/exercise.interface';
import { LessonService } from '../../services/lesson.service';
import { ExerciseService } from '../../services/exercise.service';
import { ExerciseViewerStateService } from '../../services/exercise-viewer-state.service';
import { AuthService } from '../../../../auth/auth.service';

/**
 * Container component for the exercise-solving flow.
 * Uses ExerciseViewerStateService to manage all state (view models, form controls, submissions).
 * Handles answer submission via ExerciseService and lesson completion via LessonService.
 *
 * All four exercise types (MultipleChoice, FillInBlank, Translation, Listening)
 * are rendered within this single component — the template switches UI controls
 * based on `exerciseType`.
 */
@Component({
  selector: 'app-exercise-viewer',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  providers: [ExerciseViewerStateService], // Component-scoped service
  templateUrl: './exercise-viewer.component.html',
  styleUrl: './exercise-viewer.component.scss'
})
export class ExerciseViewerComponent implements OnInit, OnDestroy {
  private router = inject(Router);
  private route = inject(ActivatedRoute);
  private fb = inject(NonNullableFormBuilder);
  private lessonService = inject(LessonService);
  private exerciseService = inject(ExerciseService);
  private authService = inject(AuthService);
  private destroyRef = inject(DestroyRef);

  // State service manages all exercise state
  readonly state = inject(ExerciseViewerStateService);

  exercises: AnyExercise[] = [];
  lessonId: string = '';
  isLoading = true;

  isAdmin = false;
  isSubmitting = false;

  // Audio player state
  audioElement: HTMLAudioElement | null = null;
  isPlaying = false;
  currentTime = 0;
  duration = 0;
  progressPercentage = 0;

  ExerciseType = ExerciseType;

  async ngOnInit() {
    // Subscribe to admin status
    this.authService.getAdminStatusListener()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((isAdmin) => this.isAdmin = isAdmin);

    // Get lessonId from route params
    const lessonId = this.route.snapshot.paramMap.get('id');
    if (!lessonId) {
      console.error('❌ No lesson ID in route params');
      this.router.navigate(['/']);
      return;
    }

    this.lessonId = lessonId;

    // Load lesson to get exercises
    await this.loadLesson();

    // Initialize state and build forms
    await this.initializeState();
    this.buildForms();
  }

  private async loadLesson(): Promise<void> {
    this.isLoading = true;

    try {
      const lesson = await this.lessonService.getLesson(this.lessonId);

      if (!lesson) {
        console.error('❌ Lesson not found');
        this.router.navigate(['/']);
        return;
      }

      this.exercises = lesson.exercises as AnyExercise[];
    } catch (err) {
      console.error('❌ Error loading lesson:', err);
      this.router.navigate(['/']);
    } finally {
      this.isLoading = false;
    }
  }

  ngOnDestroy() {
    this.state.reset();
    if (this.audioElement) {
      this.audioElement.pause();
      this.audioElement = null;
    }
  }

  /**
   * Initialize the state service with exercises and restore previous progress.
   */
  private async initializeState() {
    const submissions = await this.lessonService.getLessonSubmissions(this.lessonId);
    this.state.initialize(this.exercises, submissions, this.isAdmin);
  }

  /**
   * Build reactive form controls for all exercises.
   * Disables controls for already-submitted exercises.
   */
  private buildForms() {
    this.state.viewModels.forEach(vm => {
      const control = this.fb.control('');

      // Disable if already submitted
      if (vm.isSubmitted) {
        control.disable();
      }

      vm.formControl = control;
    });
  }

  // Getters that delegate to state service

  get currentExercise(): AnyExercise | null {
    return this.state.currentViewModel?.exercise || null;
  }

  get currentSubmission() {
    return this.state.currentViewModel?.submission || null;
  }

  /** True only when the current exercise was answered correctly. Wrong attempts keep the form enabled for retry. */
  get isCurrentSubmitted(): boolean {
    return this.state.currentViewModel?.isSubmitted || false;
  }

  get isCurrentLocked(): boolean {
    const exercise = this.currentExercise;
    return !!exercise?.isLocked && !this.isAdmin;
  }

  // Type-safe getters for specific exercise types

  get currentMultipleChoice(): MultipleChoiceExercise | null {
    const exercise = this.currentExercise;
    return exercise?.type === ExerciseType.MultipleChoice
      ? exercise as MultipleChoiceExercise
      : null;
  }

  get currentFillInBlank(): FillInBlankExercise | null {
    const exercise = this.currentExercise;
    return exercise?.type === ExerciseType.FillInBlank
      ? exercise as FillInBlankExercise
      : null;
  }

  get currentTranslation(): TranslationExercise | null {
    const exercise = this.currentExercise;
    return exercise?.type === ExerciseType.Translation
      ? exercise as TranslationExercise
      : null;
  }

  get currentListening(): ListeningExercise | null {
    const exercise = this.currentExercise;
    return exercise?.type === ExerciseType.Listening
      ? exercise as ListeningExercise
      : null;
  }

  // Navigation methods

  previousExercise() {
    this.state.goToPrevious();
  }

  nextExercise() {
    this.state.goToNext();
  }

  goToExercise(exerciseId: string) {
    this.state.setCurrentExercise(exerciseId);
  }

  isExerciseAccessible(exerciseId: string): boolean {
    const viewModel = this.state.getViewModelById(exerciseId);
    return viewModel?.isAccessible || false;
  }

  // User interaction methods

  selectMultipleChoiceOption(optionId: string) {
    if (!this.isCurrentSubmitted && !this.isCurrentLocked) {
      this.state.updateAnswer(this.state.currentExerciseId!, optionId);
    }
  }

  async submitAnswer() {
    const exercise = this.currentExercise;
    if (!exercise || this.isCurrentSubmitted || this.isSubmitting || this.isCurrentLocked) return;

    const answerValue = this.state.currentAnswer;
    if (!answerValue) return;

    this.isSubmitting = true;

    try {
      const result = await this.exerciseService.submitExerciseAnswer(exercise.id, answerValue);
      this.state.submitAnswer(exercise.id, result);
    } catch (err) {
      console.error('❌ Failed to submit answer:', err);
    } finally {
      this.isSubmitting = false;
    }
  }

  async finishLesson() {
    try {
      const result = await this.lessonService.completeLesson(this.lessonId);

      if (result.isCompleted) {
        this.router.navigate(['/']);
      } else {
        alert(
          `You need ${Math.round(result.requiredThreshold * 100)}% to complete this lesson. ` +
          `You scored ${Math.round(result.completionPercentage * 100)}%.`
        );
      }
    } catch (err) {
      console.error('❌ Failed to complete lesson:', err);
    }
  }

  onBackToContent() {
    this.router.navigate(['/lesson', this.lessonId]);
  }

  // Helper methods for multiple choice options

  isOptionSelected(optionId: string): boolean {
    return this.state.currentAnswer === optionId;
  }

  getOptionClass(optionId: string, isCorrect: boolean): string {
    if (!this.currentSubmission) {
      return this.isOptionSelected(optionId) ? 'selected' : '';
    }

    if (isCorrect) return 'correct';
    if (this.isOptionSelected(optionId) && !isCorrect) return 'incorrect';
    return '';
  }

  getSubmission(exerciseId: string) {
    return this.state.getViewModelById(exerciseId)?.submission;
  }

  // Audio player methods

  onAudioLoaded(audio: HTMLAudioElement) {
    this.audioElement = audio;
    this.duration = audio.duration;
  }

  togglePlayPause() {
    if (!this.audioElement) return;

    if (this.isPlaying) {
      this.audioElement.pause();
      this.isPlaying = false;
    } else {
      this.audioElement.play();
      this.isPlaying = true;
    }
  }

  onTimeUpdate(audio: HTMLAudioElement) {
    this.currentTime = audio.currentTime;
    this.progressPercentage = (audio.currentTime / audio.duration) * 100;
  }

  seekAudio(event: MouseEvent) {
    if (!this.audioElement) return;

    const progressBar = event.currentTarget as HTMLElement;
    const rect = progressBar.getBoundingClientRect();
    const clickX = event.clientX - rect.left;
    const percentage = clickX / rect.width;

    this.audioElement.currentTime = percentage * this.audioElement.duration;
  }

  formatTime(seconds: number): string {
    if (isNaN(seconds)) return '0:00';
    const mins = Math.floor(seconds / 60);
    const secs = Math.floor(seconds % 60);
    return `${mins}:${secs.toString().padStart(2, '0')}`;
  }
}
