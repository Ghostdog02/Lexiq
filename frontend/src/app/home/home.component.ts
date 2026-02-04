import { Component, HostListener, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { Lesson, LessonStatus } from '../lesson/lesson.interface';
import { HomeService, Unit, Quest, UserStats } from './home.service';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './home.component.html',
  styleUrl: './home.component.scss'
})
export class HomeComponent implements OnInit {
  readonly LessonStatus = LessonStatus; // expose the enum to the template
  
  units: Unit[] = [];
  currentLessonId: number = 3;
  userStats: UserStats = { totalXp: 0, currentStreak: 0 };
  isUserBelowLesson: boolean = false;
  dailyQuests: Quest[] = [];

  private router = inject(Router);
  private homeService = inject(HomeService);

  ngOnInit() {
    this.subscribeToHomeData();
  }

  private subscribeToHomeData() {
    this.homeService.units$.subscribe(units => {
      this.units = units;
    });

    this.homeService.userStats$.subscribe(stats => {
      this.userStats = stats;
    });

    this.homeService.dailyQuests$.subscribe(quests => {
      this.dailyQuests = quests;
    });
  }

  @HostListener('window:scroll') // FIX 
  onScroll(): void {
    const currentLessonElement = document.getElementById(`lesson-${this.currentLessonId}`);
    if (currentLessonElement) {
      const rect = currentLessonElement.getBoundingClientRect();
    
      this.isUserBelowLesson = rect.top < 0;
    }
  }

  onLessonClick(lesson: Lesson) {
    if (lesson.status === LessonStatus.Available ||
        lesson.status === LessonStatus.InProgress ||
        lesson.status === LessonStatus.Completed) {
      if (lesson.id) {
        this.router.navigate(['/lesson', lesson.id]);
      }
    }
  }

  scrollToCurrentLesson() {
    const currentLesson = document.getElementById(`lesson-${this.currentLessonId}`);

    if (currentLesson) {
      currentLesson.scrollIntoView({ behavior: 'smooth', block: 'center' });
    }
  }

  redirectToCreateExercise() {
    this.router.navigate(["/create-lesson"])
  }

  getQuestProgress(quest: Quest): number {
    return Math.min((quest.current / quest.target) * 100, 100);
  }
}
