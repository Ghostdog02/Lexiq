import { Component, Input, Output, EventEmitter, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, NonNullableFormBuilder, FormArray } from '@angular/forms';
import { Router } from '@angular/router';
import {
  AnyExercise,
  ExerciseType,
  ExerciseAnswerForm,
  ExerciseViewerForm
} from '../../models/exercise.interface';
import { SubmitAnswerResponse } from '../../models/lesson.interface';
import { LessonService } from '../../services/lesson.service';

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

  @Input({ required: true }) exercises!: AnyExercise[];

  @Input({ required: true }) lessonId!: string;

  @Output() backToContent = new EventEmitter<void>();

  exerciseForm!: ExerciseViewerForm;

  submissionResults: Map<number, SubmitAnswerResponse> = new Map();

  currentExerciseIndex = 0;
  isSubmitting = false;

  earnedXp = 0;
  totalPossibleXp = 0;

  ExerciseType = ExerciseType;

  ngOnInit() {
    this.buildForm();
    this.calculateTotalXp();
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

  get exerciseProgress(): number {
    if (this.exercises.length === 0) return 0;
    return (this.submissionResults.size / this.exercises.length) * 100;
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
      this.currentExerciseIndex = index;
    }
  }

  selectMultipleChoiceOption(optionId: string) {
    if (!this.isCurrentSubmitted) {
      this.currentAnswerGroup.controls.answer.setValue(optionId);
    }
  }

  async submitAnswer() {
    const exercise = this.currentExercise;
    if (!exercise || this.isCurrentSubmitted || this.isSubmitting) return;

    const answerValue = this.currentAnswerValue;
    if (!answerValue) return;

    this.isSubmitting = true;

    try {
      const result = await this.lessonService.submitExerciseAnswer(exercise.id, answerValue);

      this.submissionResults.set(this.currentExerciseIndex, result);
      this.currentAnswerGroup.disable();

      this.earnedXp = result.lessonProgress.earnedXp;
      this.totalPossibleXp = result.lessonProgress.totalPossibleXp;
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
