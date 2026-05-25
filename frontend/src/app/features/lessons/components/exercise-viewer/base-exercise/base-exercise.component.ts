import { Component, Input, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ExerciseOption } from '../../../models/exercise.interface';
import { ExerciseViewerStateService } from '../../../services/exercise-viewer-state.service';

/**
 * Renders an options list for any exercise type that uses option-based answers
 * (MultipleChoice, TrueFalse, FillInBlank word tiles, Listening).
 *
 * State is driven entirely by ExerciseViewerStateService — no @Input/@Output
 * for selection or result state.
 */
@Component({
  selector: 'app-base-exercise',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './base-exercise.component.html',
  styleUrl: './base-exercise.component.scss',
})
export class BaseExerciseComponent {
  @Input({ required: true }) exerciseId!: string;

  readonly state = inject(ExerciseViewerStateService);

  readonly letters = ['A', 'B', 'C', 'D', 'E', 'F'];

  get viewModel() {
    return this.state.getViewModelById(this.exerciseId);
  }

  get exercise() {
    return this.viewModel?.exercise;
  }

  get options(): ExerciseOption[] {
    return this.exercise?.options ?? [];
  }

  selectOption(optionId: string): void {
    const vm = this.viewModel;
    if (!vm || vm.isSubmitted || !vm.isAccessible) return;
    this.state.selectOption(this.exerciseId, optionId);
  }

  getOptionClass(option: ExerciseOption): string {
    const vm = this.viewModel;
    if (!vm) return '';

    const isSelected = vm.selectedOptionId === option.id;

    if (!vm.isSubmitted) {
      return isSelected ? 'selected' : '';
    }

    if (isSelected) {
      return vm.isCorrect ? 'correct' : 'incorrect';
    }

    // Show correct answer when user got it wrong
    if (!vm.isCorrect && option.isCorrect) {
      return 'correct';
    }

    return '';
  }
}
