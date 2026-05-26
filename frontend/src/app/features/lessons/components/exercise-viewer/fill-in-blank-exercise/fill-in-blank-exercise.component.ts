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

  // Animation retrigger mechanism: track the last selected option and toggle flag
  private lastSelectedId = '';
  private animationToggle = false;

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

  /**
   * Tracks selection changes and toggles animation flag.
   * When a different option is selected, flip the toggle so the template
   * alternates between .filled and .filled-alt classes, retriggering the animation.
   */
  get animationKey(): string {
    const currentId = this.state.getViewModelById(this.exercise.id)?.selectedOptionId ?? '';
    if (currentId !== this.lastSelectedId && currentId) {
      this.lastSelectedId = currentId;
      this.animationToggle = !this.animationToggle; // Flip to retrigger animation
    }
    return currentId;
  }

  /**
   * Returns which animation variant to use.
   * Template binds this to alternate between .filled and .filled-alt,
   * forcing CSS to restart the animation on each selection change.
   */
  get useAlternateAnimation(): boolean {
    this.animationKey; // Ensure toggle logic runs
    return this.animationToggle;
  }
}
