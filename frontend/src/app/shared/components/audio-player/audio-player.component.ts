import { Component, Input, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-audio-player',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './audio-player.component.html',
  styleUrl: './audio-player.component.scss',
})
export class AudioPlayerComponent implements OnDestroy {
  @Input({ required: true }) src!: string;
  /** 0 = unlimited */
  @Input() maxReplays: number = 0;

  audioElement: HTMLAudioElement | null = null;
  isPlaying = false;
  currentTime = 0;
  duration = 0;
  replayCount = 0;
  loadError = false;

  get scrubValue(): number {
    return (this.currentTime / (this.duration || 1)) * 100;
  }

  get replaysExhausted(): boolean {
    return this.maxReplays > 0 && this.replayCount >= this.maxReplays;
  }

  get replaysRemaining(): number {
    return this.maxReplays - this.replayCount;
  }

  ngOnDestroy(): void {
    if (this.audioElement) {
      this.audioElement.pause();
      this.audioElement.src = '';
      this.audioElement.load();
    }
  }

  onAudioLoaded(audio: HTMLAudioElement): void {
    this.audioElement = audio;
    this.duration = audio.duration;
  }

  onAudioError(): void {
    this.loadError = true;
    this.isPlaying = false;
  }

  togglePlayPause(): void {
    if (!this.audioElement) return;
    if (this.isPlaying) {
      this.audioElement.pause();
      this.isPlaying = false;
    } else {
      if (this.replaysExhausted) return;
      if (this.maxReplays > 0) this.replayCount++;
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

  formatTime(seconds: number): string {
    if (isNaN(seconds) || seconds === 0) return '0:00';
    const mins = Math.floor(seconds / 60);
    const secs = Math.floor(seconds % 60);
    return `${mins}:${secs.toString().padStart(2, '0')}`;
  }
}
