import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ListeningExercise } from '../../../models/exercise.interface';
import { BaseExerciseComponent } from '../base-exercise/base-exercise.component';

@Component({
  selector: 'app-listening-exercise',
  standalone: true,
  imports: [CommonModule, BaseExerciseComponent],
  templateUrl: './listening-exercise.component.html',
  styleUrl: './listening-exercise.component.scss',
})
export class ListeningExerciseComponent {
  @Input({ required: true }) exercise!: ListeningExercise;

  readonly BAR_HEIGHTS = [
    5, 10, 15, 8, 18, 11, 14, 7, 17, 12, 9, 19, 11, 16, 9, 13, 6, 17, 12, 8, 18, 10, 15, 7,
    5, 10, 15, 8, 18, 11, 14, 7, 17, 12, 9, 19, 11, 16, 9, 13, 6, 17, 12, 8, 18, 10, 15, 7,
  ];

  audioElement: HTMLAudioElement | null = null;
  isPlaying = false;
  currentTime = 0;
  duration = 0;
  replayCount = 0;

  get playedBars(): number {
    if (!this.duration) return 0;
    return Math.round((this.currentTime / this.duration) * this.BAR_HEIGHTS.length);
  }

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
  }

  onEnded(): void {
    this.isPlaying = false;
  }

  replay(): void {
    if (!this.audioElement) return;
    if (this.exercise.maxReplays > 0 && this.replayCount >= this.exercise.maxReplays) return;
    this.replayCount++;
    this.audioElement.currentTime = 0;
    this.audioElement.play();
    this.isPlaying = true;
  }

  seekByWaveform(event: MouseEvent): void {
    if (!this.audioElement) return;
    const el = event.currentTarget as HTMLElement;
    const rect = el.getBoundingClientRect();
    this.audioElement.currentTime =
      ((event.clientX - rect.left) / rect.width) * this.audioElement.duration;
  }

  formatTime(seconds: number): string {
    if (isNaN(seconds) || seconds === 0) return '0:00';
    const mins = Math.floor(seconds / 60);
    const secs = Math.floor(seconds % 60);
    return `${mins}:${secs.toString().padStart(2, '0')}`;
  }
}
