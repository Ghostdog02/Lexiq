import { Injectable } from '@angular/core';
import { FormControl } from '@angular/forms';
import { AnyExercise } from '../models/exercise.interface';
import { SubmitAnswerResponse } from '../models/lesson.interface';

/**
 * ViewModel that combines exercise data with its runtime state.
 * All state for a single exercise is co-located here.
 *
 * Note: formControl is created by the component after initialization.
 */
export interface ExerciseViewModel {
  exercise: AnyExercise;
  formControl: FormControl<string> | null; // Created by component
  submission: SubmitAnswerResponse | null;
  isSubmitted: boolean;
  isAccessible: boolean;
}

/**
 * State management service for exercise viewer component.
 * Manages exercise view models, answers, submissions, and navigation.
 *
 * Uses ID-based tracking (not array indices) to remain robust when exercises are reordered.
 * Relies on Angular's change detection—no observables needed.
 */
@Injectable()
export class ExerciseViewerStateService {
  viewModels: ExerciseViewModel[] = [];
  currentExerciseId: string | null = null;

  /**
   * Initialize state with exercises and restore previous submissions.
   * FormControls should be created by the component after this initialization.
   *
   * @param exercises Array of exercises (sorted by orderIndex)
   * @param submissions Previous submission results (sorted by orderIndex, includes all exercises)
   * @param isAdmin Whether user has admin privileges (bypasses locks)
   */
  initialize(exercises: AnyExercise[], submissions: SubmitAnswerResponse[], isAdmin: boolean): void {
    // Build view models with initial state (without form controls)
    this.viewModels = exercises.map((exercise, index) => {
      const submission = submissions[index];

      // Determine if exercise was actually attempted
      // Backend returns a response for ALL exercises, so we need to filter
      const wasAttempted = submission && (
        submission.isCorrect === true ||
        submission.correctAnswer !== null
      );

      return {
        exercise: { ...exercise }, // Defensive copy to prevent external mutations
        formControl: null, // Created by component
        submission: wasAttempted ? submission : null,
        isSubmitted: wasAttempted || false,
        isAccessible: !exercise.isLocked || isAdmin
      };
    });

    // Set current exercise to first incomplete unlocked one
    const firstIncomplete = this.viewModels.find(vm => !vm.isSubmitted && vm.isAccessible);
    this.currentExerciseId = firstIncomplete?.exercise.id || this.viewModels[0]?.exercise.id || null;
  }

  /**
   * Update the answer for a specific exercise (user typing/selecting).
   */
  updateAnswer(exerciseId: string, answer: string): void {
    const viewModel = this.getViewModelById(exerciseId);
    if (!viewModel || viewModel.isSubmitted || !viewModel.formControl) return;

    viewModel.formControl.setValue(answer);
  }

  /**
   * Get the current answer value for an exercise.
   */
  getAnswer(exerciseId: string): string {
    const viewModel = this.getViewModelById(exerciseId);
    return viewModel?.formControl?.value || '';
  }

  /**
   * Record a submission result and update exercise state.
   * Also unlocks the next exercise if answer was correct.
   */
  submitAnswer(exerciseId: string, result: SubmitAnswerResponse): void {
    const viewModel = this.getViewModelById(exerciseId);
    if (!viewModel) return;

    viewModel.submission = result;
    viewModel.isSubmitted = true;

    // Disable form control if it exists
    if (viewModel.formControl) {
      viewModel.formControl.disable();
    }

    // Unlock next exercise if answer was correct
    if (result.isCorrect) {
      this.unlockNextExercise(exerciseId);
    }
  }

  /**
   * Unlock the exercise that follows the given exercise (by orderIndex).
   */
  private unlockNextExercise(currentExerciseId: string): void {
    const currentIndex = this.viewModels.findIndex(vm => vm.exercise.id === currentExerciseId);

    if (currentIndex >= 0 && currentIndex < this.viewModels.length - 1) {
      const nextViewModel = this.viewModels[currentIndex + 1];
      nextViewModel.exercise.isLocked = false;
      nextViewModel.isAccessible = true;
    }
  }

  /**
   * Set the current exercise being viewed.
   * Only allows navigation to accessible exercises.
   */
  setCurrentExercise(exerciseId: string): void {
    const viewModel = this.getViewModelById(exerciseId);
    if (viewModel && viewModel.isAccessible) {
      this.currentExerciseId = exerciseId;
    }
  }

  /**
   * Navigate to the next exercise in order.
   */
  goToNext(): void {
    const currentIndex = this.viewModels.findIndex(vm => vm.exercise.id === this.currentExerciseId);

    if (currentIndex >= 0 && currentIndex < this.viewModels.length - 1) {
      this.currentExerciseId = this.viewModels[currentIndex + 1].exercise.id;
    }
  }

  /**
   * Navigate to the previous exercise in order.
   */
  goToPrevious(): void {
    const currentIndex = this.viewModels.findIndex(vm => vm.exercise.id === this.currentExerciseId);

    if (currentIndex > 0) {
      this.currentExerciseId = this.viewModels[currentIndex - 1].exercise.id;
    }
  }

  /**
   * Get view model by exercise ID.
   */
  getViewModelById(exerciseId: string): ExerciseViewModel | null {
    return this.viewModels.find(vm => vm.exercise.id === exerciseId) || null;
  }

  // Computed properties (no storage needed)

  get currentViewModel(): ExerciseViewModel | null {
    return this.currentExerciseId
      ? this.getViewModelById(this.currentExerciseId)
      : null;
  }

  get currentFormControl(): FormControl<string> | null {
    const vm = this.currentViewModel;
    return vm?.formControl || null;
  }

  get currentAnswer(): string {
    return this.currentFormControl?.value || '';
  }

  get currentIndex(): number {
    return this.viewModels.findIndex(vm => vm.exercise.id === this.currentExerciseId);
  }

  get earnedXp(): number {
    const lastSubmission = this.viewModels
      .filter(vm => vm.submission)
      .map(vm => vm.submission!)
      .pop(); // Get the last one which has the cumulative total

    return lastSubmission?.lessonProgress?.earnedXp || 0;
  }

  get totalPossibleXp(): number {
    const lastSubmission = this.viewModels
      .filter(vm => vm.submission)
      .map(vm => vm.submission!)
      .pop();

    return lastSubmission?.lessonProgress?.totalPossibleXp
      || this.viewModels.reduce((sum, vm) => sum + (vm.exercise.points || 10), 0);
  }

  get progressPercentage(): number {
    if (this.viewModels.length === 0) return 0;
    const completedCount = this.viewModels.filter(vm => vm.isSubmitted).length;
    return (completedCount / this.viewModels.length) * 100;
  }

  get canGoNext(): boolean {
    return this.currentIndex < this.viewModels.length - 1;
  }

  get canGoPrevious(): boolean {
    return this.currentIndex > 0;
  }

  /**
   * Clean up state (call on component destroy).
   */
  reset(): void {
    this.viewModels = [];
    this.currentExerciseId = null;
  }
}
