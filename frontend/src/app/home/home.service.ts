import { Injectable, inject, DestroyRef } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { LessonService } from '../lesson/lesson.service';
import { Lesson, LessonStatus } from '../lesson/lesson.interface';

export interface Unit {
  id: number;
  title: string;
  description: string;
  lessons: Lesson[];
  color: string;
}

export interface Quest {
  id: number;
  icon: string;
  description: string;
  current: number;
  target: number;
}

export interface UserStats {
  totalXp: number;
  currentStreak: number;
}

@Injectable({
  providedIn: 'root'
})
export class HomeService {
  private lessonService = inject(LessonService);
  private destroyRef = inject(DestroyRef);

  private unitsSubject = new BehaviorSubject<Unit[]>([]);
  private userStatsSubject = new BehaviorSubject<UserStats>({ totalXp: 150, currentStreak: 5 });
  private dailyQuestsSubject = new BehaviorSubject<Quest[]>([
    { id: 1, icon: '🎯', description: 'Complete 3 lessons', current: 2, target: 3 },
    { id: 2, icon: '⭐', description: 'Earn 100 XP', current: 25, target: 100 },
    { id: 3, icon: '🗣️', description: 'Practice speaking for 5 minutes', current: 4, target: 5 }
  ]);

  units$: Observable<Unit[]> = this.unitsSubject.asObservable();
  userStats$: Observable<UserStats> = this.userStatsSubject.asObservable();
  dailyQuests$: Observable<Quest[]> = this.dailyQuestsSubject.asObservable();

  constructor() {
    this.initializeLearningPath();
    this.subscribeToCreatedLessons();
  }

  private initializeLearningPath(): void {
    const units: Unit[] = [
      {
        id: 1,
        title: 'Unit 1',
        description: 'Greetings & Basics',
        color: '#7c5cff',
        lessons: [
          { id: 1, title: 'Hello!', description: 'Learn basic greetings', icon: '👋', status: LessonStatus.Completed, xp: 50, estimatedDuration: 10, content: '', courseId: '1', exercises: [] },
          { id: 2, title: 'Introductions', description: 'Introduce yourself', icon: '🙋', status: LessonStatus.Completed, xp: 50, estimatedDuration: 15, content: '', courseId: '1', exercises: [] },
          { id: 3, title: 'Numbers', description: 'Count from 1 to 10', icon: '🔢', status: LessonStatus.InProgress, xp: 50, progress: 65, estimatedDuration: 10, content: '', courseId: '1', exercises: [] },
          { id: 4, title: 'Colors', description: 'Learn basic colors', icon: '🎨', status: LessonStatus.Available, xp: 50, estimatedDuration: 10, content: '', courseId: '1', exercises: [] },
        ]
      },
      {
        id: 2,
        title: 'Unit 2',
        description: 'Daily Conversations',
        color: '#9178ff',
        lessons: [
          { id: 5, title: 'Family', description: 'Talk about family', icon: '👨‍👩‍👧‍👦', status: LessonStatus.Locked, xp: 75, estimatedDuration: 20, content: '', courseId: '2', exercises: [] },
          { id: 6, title: 'Food', description: 'Order food', icon: '🍕', status: LessonStatus.Locked, xp: 75, estimatedDuration: 15, content: '', courseId: '2', exercises: [] },
          { id: 7, title: 'Shopping', description: 'Go shopping', icon: '🛍️', status: LessonStatus.Locked, xp: 75, estimatedDuration: 20, content: '', courseId: '2', exercises: [] },
        ]
      },
      {
        id: 3,
        title: 'Unit 3',
        description: 'Grammar Foundations',
        color: '#5a3ce6',
        lessons: [
          { id: 8, title: 'Verbs', description: 'Present tense verbs', icon: '⚡', status: LessonStatus.Locked, xp: 100, estimatedDuration: 25, content: '', courseId: '3', exercises: [] },
          { id: 9, title: 'Adjectives', description: 'Describe things', icon: '✨', status: LessonStatus.Locked, xp: 100, estimatedDuration: 20, content: '', courseId: '3', exercises: [] },
          { id: 10, title: 'Questions', description: 'Ask questions', icon: '❓', status: LessonStatus.Locked, xp: 100, estimatedDuration: 20, content: '', courseId: '3', exercises: [] },
        ]
      },
      {
        id: 4,
        title: 'Unit 4',
        description: 'Real World Practice',
        color: '#a78bfa',
        lessons: [
          { id: 11, title: 'Travel', description: 'Navigate around', icon: '✈️', status: LessonStatus.Locked, xp: 125, estimatedDuration: 30, content: '', courseId: '4', exercises: [] },
          { id: 12, title: 'Work', description: 'Professional talk', icon: '💼', status: LessonStatus.Locked, xp: 125, estimatedDuration: 25, content: '', courseId: '4', exercises: [] },
          { id: 13, title: 'Hobbies', description: 'Discuss interests', icon: '🎸', status: LessonStatus.Locked, xp: 125, estimatedDuration: 25, content: '', courseId: '4', exercises: [] },
        ]
      }
    ];

    this.unitsSubject.next(units);
    this.loadExistingLessons();
  }

  private loadExistingLessons(): void {
    const existingLessons = this.lessonService.getCreatedLessons();
    const createdLessons = this.createdLessonsSubject.value;

    existingLessons.forEach(lesson => {
      if (!createdLessons.find(l => l.id === lesson.id)) {
        createdLessons.push(lesson);
        this.addLessonToUnits(lesson);
      }
    });

    this.createdLessonsSubject.next(createdLessons);
  }

  /**
   * Subscribe to newly created lessons from LessonService
   */
  private subscribeToCreatedLessons(): void {
    this.lessonService.lessonCreated$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((createdLesson) => {
        const createdLessons = this.createdLessonsSubject.value;

        // Only add if not already in createdLessons
        if (!createdLessons.find(l => l.id === createdLesson.id)) {
          createdLessons.push(createdLesson);
          this.addLessonToUnits(createdLesson);
          this.createdLessonsSubject.next(createdLessons);
        }
      });
  }

  /**
   * Add a created lesson to the "My Lessons" unit
   */
  private addLessonToUnits(lesson: Lesson): void {
    const units = this.unitsSubject.value;
    let myLessonsUnit = units.find(u => u.id === 0);

    if (!myLessonsUnit) {
      myLessonsUnit = {
        id: 0,
        title: 'My Lessons',
        description: 'Your created lessons',
        color: '#10b981',
        lessons: []
      };
      units.unshift(myLessonsUnit);
    }

    // Generate a unique ID for the new lesson if it doesn't have one
    if (lesson.id === undefined) {
      const allLessons = this.getAllLessons();
      const maxId = allLessons.length > 0 ? Math.max(...allLessons.map(l => l.id ?? 0)) : 0;
      lesson.id = maxId + 1;
    }

    // Add path display properties to the lesson
    const lessonWithPath: Lesson = {
      ...lesson,
      icon: lesson.icon || '📚',
      status: LessonStatus.Available,
      xp: lesson.exercises.length * 25
    };

    // Check if already in unit to avoid duplicates
    if (!myLessonsUnit.lessons.find(l => l.id === lesson.id)) {
      myLessonsUnit.lessons.push(lessonWithPath);
    }

    this.unitsSubject.next(units);
  }

  /**
   * Get all lessons across all units
   */
  getAllLessons(): Lesson[] {
    return this.unitsSubject.value.flatMap(unit => unit.lessons);
  }

  /**
   * Get the current units
   */
  getUnits(): Unit[] {
    return this.unitsSubject.value;
  }

  /**
   * Get the current user stats
   */
  getUserStats(): UserStats {
    return this.userStatsSubject.value;
  }

  /**
   * Update user stats
   */
  updateUserStats(stats: Partial<UserStats>): void {
    const currentStats = this.userStatsSubject.value;
    this.userStatsSubject.next({ ...currentStats, ...stats });
  }

  /**
   * Get daily quests
   */
  getDailyQuests(): Quest[] {
    return this.dailyQuestsSubject.value;
  }

  /**
   * Update progress for a specific quest
   */
  updateQuestProgress(questId: number, current: number): void {
    const quests = this.dailyQuestsSubject.value.map(quest =>
      quest.id === questId ? { ...quest, current } : quest
    );
    this.dailyQuestsSubject.next(quests);
  }

  /**
   * Update progress for all quests (useful for bulk updates)
   */
  updateDailyQuests(quests: Quest[]): void {
    this.dailyQuestsSubject.next(quests);
  }

  /**
   * Complete a lesson and update user progress
   */
  completeLesson(lessonId: number, xpEarned: number): void {
    // Update lesson status
    const units = this.unitsSubject.value;
    const updatedUnits = units.map(unit => ({
      ...unit,
      lessons: unit.lessons.map(lesson =>
        lesson.id === lessonId ? { ...lesson, status: LessonStatus.Completed } : lesson
      )
    }));
    this.unitsSubject.next(updatedUnits);

    // Update user XP
    const currentStats = this.userStatsSubject.value;
    this.updateUserStats({ totalXp: currentStats.totalXp + xpEarned });

    // TODO: Send to backend when API is ready
  }

  /**
   * Update lesson progress
   */
  updateLessonProgress(lessonId: number, progress: number): void {
    const units = this.unitsSubject.value;
    const updatedUnits = units.map(unit => ({
      ...unit,
      lessons: unit.lessons.map(lesson =>
        lesson.id === lessonId ? { ...lesson, progress, status: LessonStatus.InProgress } : lesson
      )
    }));
    this.unitsSubject.next(updatedUnits);

    // TODO: Send to backend when API is ready
  }
}