import { Component, Input, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FillInBlankExercise } from '../../../models/exercise.interface';
import { BaseExerciseComponent } from '../base-exercise/base-exercise.component';
import { ExerciseViewerStateService } from '../../../services/exercise-viewer-state.service';

/**
 * Renders a fill-in-the-blank exercise.
 *
 * Displays the `text` with the blank placeholder highlighted,
 * then shows the options list (via BaseExerciseComponent) for the user to select.
 * The selected option fills the blank.
 */
@Component({
  selector: 'app-fill-in-blank-exercise',
  standalone: true,
  imports: [CommonModule, BaseExerciseComponent],
  templateUrl: './fill-in-blank-exercise.component.html',
  styleUrl: './fill-in-blank-exercise.component.scss',
})
export class FillInBlankExerciseComponent implements OnInit {
  @Input({ required: true }) exercise!: FillInBlankExercise;

  readonly state = inject(ExerciseViewerStateService);

  textBefore = '';
  textAfter = '';
  hasBlank = false;

  ngOnInit(): void {
    const parsed = this.parseText(this.exercise.text);
    this.textBefore = parsed.before;
    this.textAfter = parsed.after;
    this.hasBlank = parsed.hasBlank;
  }

  private parseText(text: string): {
    before: string;
    after: string;
    hasBlank: boolean;
  } {
    const placeholder = '____';
    const index = text.indexOf(placeholder);
    if (index === -1) return { before: text, after: '', hasBlank: false };
    return {
      before: text.substring(0, index),
      after: text.substring(index + placeholder.length),
      hasBlank: true,
    };
  }

  get selectedOptionText(): string {
    const vm = this.state.getViewModelById(this.exercise.id);
    if (!vm?.selectedOptionId) return '...';
    return (
      this.exercise.options.find((o) => o.id === vm.selectedOptionId)
        ?.optionText ?? '...'
    );
  }

  get isSubmitted(): boolean {
    return this.state.getViewModelById(this.exercise.id)?.isSubmitted ?? false;
  }

  get isCorrect(): boolean {
    return this.state.getViewModelById(this.exercise.id)?.isCorrect ?? false;
  }
}
