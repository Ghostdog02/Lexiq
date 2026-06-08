import { Component, Input, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TrueFalseExercise } from '../../../models/exercise.interface';
import { BaseExerciseComponent } from '../base-exercise/base-exercise.component';
import { ExerciseViewerStateService } from '../../../services/exercise-viewer-state.service';

/**
 * Renders a true/false exercise.
 *
 * Displays options list (via BaseExerciseComponent) for the user to select.
 * Instructions (statement) are shown in the exercise-viewer header.
 */
@Component({
  selector: 'app-true-false-exercise',
  standalone: true,
  imports: [CommonModule, BaseExerciseComponent],
  templateUrl: './true-false-exercise.component.html',
  styleUrl: './true-false-exercise.component.scss',
})
export class TrueFalseExerciseComponent {
  @Input({ required: true }) exercise!: TrueFalseExercise;

  readonly state = inject(ExerciseViewerStateService);
}
