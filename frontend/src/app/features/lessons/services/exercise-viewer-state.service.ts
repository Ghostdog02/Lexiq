import { Injectable } from '@angular/core';
import { AnyExercise, ExerciseOption } from '../models/exercise.interface';

/**
 * ViewModel that combines exercise data with its runtime state.
 * All state for a single exercise is co-located here.
 */
export interface ExerciseViewModel {
  exercise: AnyExercise;
  selectedOptionId: string | null;
  isCorrect: boolean;
  isSubmitted: boolean;
  isAccessible: boolean;
}

/**
 * State management service for exercise viewer component.
 * Manages exercise view models, local validation, hearts, and navigation.
 *
 * Answers are validated locally using option.isCorrect — no per-exercise backend call.
 * The lesson is submitted once at the end via buildSubmitPayload().
 */
@Injectable()
export class ExerciseViewerStateService {
  viewModels: ExerciseViewModel[] = [];
  currentExerciseId: string | null = null;
  hearts = 0;

  /**
   * Initialize state with exercises.
   *
   * @param exercises  Array of exercises (sorted by orderIndex)
   * @param isAdmin    Whether user has admin privileges (bypasses locks)
   * @param hearts     Current hearts count from backend
   */
  initialize(exercises: AnyExercise[], isAdmin: boolean, hearts: number): void {
    this.hearts = hearts;

    this.viewModels = exercises.map((exercise) => ({
      exercise: { ...exercise },
      selectedOptionId: null,
      isCorrect: false,
      isSubmitted: false,
      isAccessible: true,
    }));

    // Set current exercise to first accessible one
    const firstAccessible = this.viewModels.find((vm) => vm.isAccessible);
    this.currentExerciseId =
      firstAccessible?.exercise.id ?? this.viewModels[0]?.exercise.id ?? null;
  }

  /**
   * Record option selection for an exercise (does NOT submit).
   * User can change selection multiple times before calling submitAnswer().
   */
  selectOption(exerciseId: string, optionId: string): void {
    const vm = this.getViewModelById(exerciseId);
    if (!vm || vm.isSubmitted || !vm.isAccessible) return;

    // Just record selection, don't validate yet
    vm.selectedOptionId = optionId;
  }

  /**
   * Submit and validate the selected answer.
   * Marks exercise as submitted — no going back.
   * Decrements hearts on wrong answer.
   */
  submitAnswer(exerciseId: string): void {
    const vm = this.getViewModelById(exerciseId);
    if (!vm || vm.isSubmitted || !vm.isAccessible || !vm.selectedOptionId) return;

    const option = vm.exercise.options.find((o) => o.id === vm.selectedOptionId);
    const isCorrect = option?.isCorrect ?? false;

    vm.isCorrect = isCorrect;
    vm.isSubmitted = true;

    if (!isCorrect && this.hearts > 0) {
      this.hearts--;
    }
  }

  /**
   * Build the payload for POST /api/lessons/{id}/submit.
   */
  buildSubmitPayload(): { exerciseId: string; selectedOptionId: string | null }[] {
    return this.viewModels.map((vm) => ({
      exerciseId: vm.exercise.id,
      selectedOptionId: vm.selectedOptionId,
    }));
  }

  /**
   * Set the current exercise being viewed.
   * Only allows navigation to accessible exercises.
   */
  setCurrentExercise(exerciseId: string): void {
    const vm = this.getViewModelById(exerciseId);
    if (vm?.isAccessible) {
      this.currentExerciseId = exerciseId;
    }
  }

  goToNext(): void {
    const currentIndex = this.viewModels.findIndex(
      (vm) => vm.exercise.id === this.currentExerciseId
    );
    if (currentIndex >= 0 && currentIndex < this.viewModels.length - 1) {
      this.currentExerciseId = this.viewModels[currentIndex + 1].exercise.id;
    }
  }

  goToPrevious(): void {
    const currentIndex = this.viewModels.findIndex(
      (vm) => vm.exercise.id === this.currentExerciseId
    );
    if (currentIndex > 0) {
      this.currentExerciseId = this.viewModels[currentIndex - 1].exercise.id;
    }
  }

  getViewModelById(exerciseId: string): ExerciseViewModel | null {
    return this.viewModels.find((vm) => vm.exercise.id === exerciseId) ?? null;
  }

  reset(): void {
    this.viewModels = [];
    this.currentExerciseId = null;
    this.hearts = 0;
  }

  // ── Computed getters ──────────────────────────────────────────────────────

  get currentViewModel(): ExerciseViewModel | null {
    return this.currentExerciseId
      ? this.getViewModelById(this.currentExerciseId)
      : null;
  }

  get currentIndex(): number {
    return this.viewModels.findIndex(
      (vm) => vm.exercise.id === this.currentExerciseId
    );
  }

  get progressPercentage(): number {
    if (this.viewModels.length === 0) return 0;
    const submitted = this.viewModels.filter((vm) => vm.isSubmitted).length;
    return (submitted / this.viewModels.length) * 100;
  }

  get earnedXp(): number {
    return this.viewModels
      .filter((vm) => vm.isSubmitted && vm.isCorrect)
      .reduce((sum, vm) => sum + (vm.exercise.points ?? 0), 0);
  }

  get totalPossibleXp(): number {
    return this.viewModels.reduce(
      (sum, vm) => sum + (vm.exercise.points ?? 0),
      0
    );
  }

  get canGoNext(): boolean {
    return this.currentIndex < this.viewModels.length - 1;
  }

  get canGoPrevious(): boolean {
    return this.currentIndex > 0;
  }

  get currentHasSelection(): boolean {
    return this.currentViewModel?.selectedOptionId !== null;
  }
}
