import { Component, Input, Output, EventEmitter, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, NonNullableFormBuilder, FormArray } from '@angular/forms';
import { Router } from '@angular/router';
import {
  AnyExercise,
  ExerciseType,
  ExerciseAnswerForm,
  ExerciseViewerForm,
  MultipleChoiceExercise,
  FillInBlankExercise,
  TranslationExercise,
  ListeningExercise
} from '../../models/exercise.interface';
import { SubmitAnswerResponse } from '../../models/lesson.interface';
import { LessonService } from '../../services/lesson.service';
import { AuthService } from '../../../../auth/auth.service';

/**
 * Container component for the exercise-solving flow.
 * Manages a Reactive Form (FormArray of exercise answers), navigation between
 * exercises, answer submission via LessonService, and progress tracking.
 *
 * All four exercise types (MultipleChoice, FillInBlank, Translation, Listening)
 * are rendered within this single component — the template switches UI controls
 * (radio buttons, input, textarea) based on `exerciseType`.
 */
@Component({
  selector: 'app-exercise-viewer',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './exercise-viewer.component.html',
  styleUrl: './exercise-viewer.component.scss'
})
export class ExerciseViewerComponent implements OnInit {
  private router = inject(Router);
  private fb = inject(NonNullableFormBuilder);
  private lessonService = inject(LessonService);
  private authService = inject(AuthService);

  @Input({ required: true }) exercises!: AnyExercise[];

  @Input({ required: true }) lessonId!: string;

  isAdmin!: boolean;

  @Output() backToContent = new EventEmitter<void>();

  exerciseForm!: ExerciseViewerForm;

  submissionResults: Map<number, SubmitAnswerResponse> = new Map();

  currentExerciseIndex = 0;
  isSubmitting = false;

  earnedXp = 0;
  totalPossibleXp = 0;

  ExerciseType = ExerciseType;

  async ngOnInit() {
    this.buildForm();
    this.calculateTotalXp();
    await this.restorePreviousProgress();
    this.authService.getAuthStatusListener()
      .subscribe((isAdmin) => this.isAdmin = isAdmin);
  }

  /**
   * Builds the ExerciseViewerForm with one ExerciseAnswerForm per exercise.
   * Each answer control starts as an empty string.
   */
  private buildForm() {
    const answerGroups = this.exercises.map(() =>
      this.fb.group({ answer: this.fb.control('') })
    );

    this.exerciseForm = this.fb.group({
      exercises: this.fb.array(answerGroups)
    }) as ExerciseViewerForm;
  }

  private calculateTotalXp() {
    this.totalPossibleXp = this.exercises.reduce((sum, ex) => sum + (ex.points || 10), 0);
  }

  private async restorePreviousProgress() {
    const submissions = await this.lessonService.getLessonSubmissions(this.lessonId);

    submissions.forEach((response, index) => {
      // Skip exercises that were never attempted (no correctAnswer means no attempt)
      const wasAttempted = response.isCorrect || response.correctAnswer !== null;
      if (!wasAttempted) return;

      this.submissionResults.set(index, response);
      this.exercisesFormArray.at(index).disable();
    });

    // Update XP from the shared lessonProgress summary
    const lastSubmission = submissions.find(s => s.lessonProgress);
    if (lastSubmission) {
      this.earnedXp = lastSubmission.lessonProgress.earnedXp;
    }

    // Start at first incomplete unlocked exercise
    const firstIncomplete = this.exercises.findIndex(
      (ex, i) => !this.submissionResults.has(i) && !ex.isLocked
    );
    if (firstIncomplete >= 0) {
      this.currentExerciseIndex = firstIncomplete;
    }
  }

  get exercisesFormArray(): FormArray<ExerciseAnswerForm> {
    return this.exerciseForm.controls.exercises;
  }

  get currentAnswerGroup(): ExerciseAnswerForm {
    return this.exercisesFormArray.at(this.currentExerciseIndex);
  }

  get currentAnswerValue(): string {
    return this.currentAnswerGroup.controls.answer.value;
  }

  get currentExercise(): AnyExercise | null {
    if (this.exercises.length === 0) return null;
    return this.exercises[this.currentExerciseIndex];
  }

  get currentSubmission(): SubmitAnswerResponse | null {
    return this.submissionResults.get(this.currentExerciseIndex) ?? null;
  }

  get isCurrentSubmitted(): boolean {
    return this.submissionResults.has(this.currentExerciseIndex);
  }

  get isCurrentLocked(): boolean {
    return this.currentExercise?.isLocked && !this.isAdmin ? true : false;
  }

  get exerciseProgress(): number {
    if (this.exercises.length === 0) return 0;
    return (this.submissionResults.size / this.exercises.length) * 100;
  }

  /**
   * Type-safe getter for MultipleChoiceExercise.
   * Returns the current exercise as MultipleChoiceExercise if type matches.
   */
  get currentMultipleChoice(): MultipleChoiceExercise | null {
    const exercise = this.currentExercise;
    return exercise?.type === ExerciseType.MultipleChoice
      ? exercise as MultipleChoiceExercise
      : null;
  }

  /**
   * Type-safe getter for FillInBlankExercise.
   * Returns the current exercise as FillInBlankExercise if type matches.
   */
  get currentFillInBlank(): FillInBlankExercise | null {
    const exercise = this.currentExercise;
    return exercise?.type === ExerciseType.FillInBlank
      ? exercise as FillInBlankExercise
      : null;
  }

  /**
   * Type-safe getter for TranslationExercise.
   * Returns the current exercise as TranslationExercise if type matches.
   */
  get currentTranslation(): TranslationExercise | null {
    const exercise = this.currentExercise;
    return exercise?.type === ExerciseType.Translation
      ? exercise as TranslationExercise
      : null;
  }

  /**
   * Type-safe getter for ListeningExercise.
   * Returns the current exercise as ListeningExercise if type matches.
   */
  get currentListening(): ListeningExercise | null {
    const exercise = this.currentExercise;
    return exercise?.type === ExerciseType.Listening
      ? exercise as ListeningExercise
      : null;
  }

  previousExercise() {
    if (this.currentExerciseIndex > 0) {
      this.currentExerciseIndex--;
    }
  }

  nextExercise() {
    if (this.currentExerciseIndex < this.exercises.length - 1) {
      this.currentExerciseIndex++;
    }
  }

  goToExercise(index: number) {
    if (index >= 0 && index < this.exercises.length) {
      // Only allow navigation to unlocked exercises or already attempted ones
      const targetExercise = this.exercises[index];
      if (!targetExercise.isLocked || this.submissionResults.has(index)) {
        this.currentExerciseIndex = index;
      }
    }
  }

  isExerciseAccessible(index: number): boolean {
    const exercise = this.exercises[index];
    return !exercise.isLocked || this.submissionResults.has(index);
  }

  selectMultipleChoiceOption(optionId: string) {
    if (!this.isCurrentSubmitted && !this.isCurrentLocked) {
      this.currentAnswerGroup.controls.answer.setValue(optionId);
    }
  }

  async submitAnswer() {
    const exercise = this.currentExercise;
    if (!exercise || this.isCurrentSubmitted || this.isSubmitting || this.isCurrentLocked) return;

    const answerValue = this.currentAnswerValue;
    if (!answerValue) return;

    this.isSubmitting = true;

    try {
      const result = await this.lessonService.submitExerciseAnswer(exercise.id, answerValue);

      this.submissionResults.set(this.currentExerciseIndex, result);
      this.currentAnswerGroup.disable();

      this.earnedXp = result.lessonProgress.earnedXp;
      this.totalPossibleXp = result.lessonProgress.totalPossibleXp;

      // If answered correctly and there's a next exercise, unlock it in the local state
      if (result.isCorrect && this.currentExerciseIndex < this.exercises.length - 1) {
        this.exercises[this.currentExerciseIndex + 1].isLocked = false;
      }
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
    this.backToContent.emit();
  }

  isOptionSelected(optionId: string): boolean {
    return this.currentAnswerValue === optionId;
  }

  getOptionClass(optionId: string, isCorrect: boolean): string {
    if (!this.isCurrentSubmitted) {
      return this.isOptionSelected(optionId) ? 'selected' : '';
    }

    if (isCorrect) return 'correct';
    if (this.isOptionSelected(optionId) && !isCorrect) return 'incorrect';
    return '';
  }

  getSubmission(index: number): SubmitAnswerResponse | undefined {
    return this.submissionResults.get(index);
  }
}
