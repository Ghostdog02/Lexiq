import { Component, Input } from '@angular/core';
import { ListeningExercise } from '../../../models/exercise.interface';
import { BaseExerciseComponent } from '../base-exercise/base-exercise.component';
import { AudioPlayerComponent } from '../../../../../shared/components/audio-player/audio-player.component';

@Component({
  selector: 'app-listening-exercise',
  standalone: true,
  imports: [BaseExerciseComponent, AudioPlayerComponent],
  templateUrl: './listening-exercise.component.html',
  styleUrl: './listening-exercise.component.scss',
})
export class ListeningExerciseComponent {
  @Input({ required: true }) exercise!: ListeningExercise;
}
