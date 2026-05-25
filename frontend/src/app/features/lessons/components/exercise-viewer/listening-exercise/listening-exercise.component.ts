import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ListeningExercise } from '../../../models/exercise.interface';
import { BaseExerciseComponent } from '../base-exercise/base-exercise.component';

/**
 * Renders a listening exercise.
 *
 * Shows a custom audio player above the options list (via BaseExerciseComponent).
 * Audio state is managed locally — no global state needed for playback.
 */
@Component({
  selector: 'app-listening-exercise',
  standalone: true,
  imports: [CommonModule, BaseExerciseComponent],
  templateUrl: './listening-exercise.component.html',
  styleUrl: './listening-exercise.component.scss',
})
export class ListeningExerciseComponent {
  @Input({ required: true }) exercise!: ListeningExercise;

  audioElement: HTMLAudioElement | null = null;
  isPlaying = false;
  currentTime = 0;
  duration = 0;
  progressPercentage = 0;

  onAudioLoaded(audio: HTMLAudioElement): void {
    this.audioElement = audio;
    this.duration = audio.duration;
  }

  togglePlayPause(): void {
    if (!this.audioElement) return;
    if (this.isPlaying) {
      this.audioElement.pause();
      this.isPlaying = false;
    } else {
      this.audioElement.play();
      this.isPlaying = true;
    }
  }

  onTimeUpdate(audio: HTMLAudioElement): void {
    this.currentTime = audio.currentTime;
    this.progressPercentage = (audio.currentTime / audio.duration) * 100;
  }

  onEnded(): void {
    this.isPlaying = false;
  }

  seekAudio(event: MouseEvent): void {
    if (!this.audioElement) return;
    const el = event.currentTarget as HTMLElement;
    const rect = el.getBoundingClientRect();
    this.audioElement.currentTime =
      ((event.clientX - rect.left) / rect.width) * this.audioElement.duration;
  }

  formatTime(seconds: number): string {
    if (isNaN(seconds)) return '0:00';
    const mins = Math.floor(seconds / 60);
    const secs = Math.floor(seconds % 60);
    return `${mins}:${secs.toString().padStart(2, '0')}`;
  }
}
