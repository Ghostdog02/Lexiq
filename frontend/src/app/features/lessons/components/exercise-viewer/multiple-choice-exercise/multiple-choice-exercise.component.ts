import { Component, Input, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Exercise } from '../../../models/exercise.interface';
import { BaseExerciseComponent } from '../base-exercise/base-exercise.component';
import { ExerciseViewerStateService } from '../../../services/exercise-viewer-state.service';

/**
 * Renders a multiple-choice exercise.
 *
 * Displays options list (via BaseExerciseComponent) for the user to select.
 * Instructions are shown in the exercise-viewer header.
 */
@Component({
  selector: 'app-multiple-choice-exercise',
  standalone: true,
  imports: [CommonModule, BaseExerciseComponent],
  templateUrl: './multiple-choice-exercise.component.html',
  styleUrl: './multiple-choice-exercise.component.scss',
})
export class MultipleChoiceExerciseComponent {
  @Input({ required: true }) exercise!: Exercise;

  readonly state = inject(ExerciseViewerStateService);
}
