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

  audioElement: HTMLAudioElement | null = null;
  isPlaying = false;
  currentTime = 0;
  duration = 0;
  replayCount = 0;

  get scrubValue(): number {
    return (this.currentTime / (this.duration || 1)) * 100;
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

  onScrub(event: Event): void {
    if (!this.audioElement) return;
    const input = event.target as HTMLInputElement;
    this.audioElement.currentTime = (Number(input.value) / 100) * this.duration;
  }

  formatTime(seconds: number): string {
    if (isNaN(seconds) || seconds === 0) return '0:00';
    const mins = Math.floor(seconds / 60);
    const secs = Math.floor(seconds % 60);
    return `${mins}:${secs.toString().padStart(2, '0')}`;
  }
}
