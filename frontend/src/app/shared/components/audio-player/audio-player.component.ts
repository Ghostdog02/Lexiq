import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-audio-player',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './audio-player.component.html',
  styleUrl: './audio-player.component.scss',
})
export class AudioPlayerComponent {
  @Input({ required: true }) src!: string;
  /** 0 = unlimited */
  @Input() maxReplays: number = 0;

  audioElement: HTMLAudioElement | null = null;
  isPlaying = false;
  currentTime = 0;
  duration = 0;
  replayCount = 0;

  get scrubValue(): number {
    return (this.currentTime / (this.duration || 1)) * 100;
  }

  get replaysExhausted(): boolean {
    return this.maxReplays > 0 && this.replayCount >= this.maxReplays;
  }

  get replaysRemaining(): number {
    return this.maxReplays - this.replayCount;
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
    if (!this.audioElement || this.replaysExhausted) return;
    if (this.maxReplays > 0) this.replayCount++;
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
