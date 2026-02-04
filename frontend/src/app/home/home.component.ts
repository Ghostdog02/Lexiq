import { Component, DestroyRef, HostListener, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { LessonService } from '../lesson/lesson.service';
import { Lesson } from '../lesson/lesson.interface';

export interface Unit {
  id: number;
  title: string;
  description: string;
  lessons: Lesson[];
  color: string;
}

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './home.component.html',
  styleUrl: './home.component.scss'
})
export class HomeComponent implements OnInit {
  courses: Unit[] = [];
  createdLessons: Lesson[] = [];
  currentLessonId: number = 3;
  totalXp: number = 150;
  currentStreak: number = 5;
  isUserBelowLesson: boolean = false;

  private router = inject(Router);
  private lessonService = inject(LessonService);
  private destroyRef = inject(DestroyRef);

  ngOnInit() {
    this.initializeLearningPath();
    this.loadExistingLessons();
    this.subscribeToCreatedLessons();
  }

  private loadExistingLessons() {
    // Load lessons that were created before this component initialized
    const existingLessons = this.lessonService.getCreatedLessons();
    existingLessons.forEach(lesson => {
      this.createdLessons.push(lesson);
      this.addLessonToUnits(lesson);
    });
  }

  private subscribeToCreatedLessons() {
    this.lessonService.lessonCreated$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((createdLesson) => {
        console.log('New lesson received in home:', createdLesson);
        // Only add if not already in createdLessons (avoid duplicates from loadExistingLessons)
        if (!this.createdLessons.includes(createdLesson)) {
          this.createdLessons.push(createdLesson);
          this.addLessonToUnits(createdLesson);
        }
      });
  }

  private addLessonToUnits(lesson: Lesson) {
    // Find or create "My Lessons" unit for user-created content
    let myLessonsUnit = this.courses.find(u => u.id === 1);

    if (!myLessonsUnit) {
      myLessonsUnit = {
        id: 0,
        title: 'My Lessons',
        description: 'Your created lessons',
        color: '#10b981',
        lessons: []
      };
      // Add at the beginning of units
      this.courses.unshift(myLessonsUnit);
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
      icon: '📚',
      status: 'available',
      xp: lesson.exercises.length * 25
    };

    // Check if already in unit to avoid duplicates
    if (!myLessonsUnit.lessons.find(l => l.id === lesson.id)) {
      myLessonsUnit.lessons.push(lessonWithPath);
    }
  }

  @HostListener('window:scroll', ['$event'])
  onScroll(event: Event): void {
    const currentLessonElement = document.getElementById(`lesson-${this.currentLessonId}`);
    if (currentLessonElement) {
      const rect = currentLessonElement.getBoundingClientRect();
      // If the top of the lesson is above the viewport, the user is below it.
      this.isUserBelowLesson = rect.top < 0;
    }
  }

  initializeLearningPath() {
    this.courses = [
      {
        id: 1,
        title: 'Unit 1',
        description: 'Greetings & Basics',
        color: '#7c5cff',
        lessons: [
          { id: 1, title: 'Hello!', description: 'Learn basic greetings', icon: '👋', status: 'completed', xp: 50, estimatedDuration: 10, content: '', courseId: '1', exercises: [] },
          { id: 2, title: 'Introductions', description: 'Introduce yourself', icon: '🙋', status: 'completed', xp: 50, estimatedDuration: 15, content: '', courseId: '1', exercises: [] },
          { id: 3, title: 'Numbers', description: 'Count from 1 to 10', icon: '🔢', status: 'in-progress', xp: 50, progress: 65, estimatedDuration: 10, content: '', courseId: '1', exercises: [] },
          { id: 4, title: 'Colors', description: 'Learn basic colors', icon: '🎨', status: 'available', xp: 50, estimatedDuration: 10, content: '', courseId: '1', exercises: [] },
        ]
      },
      {
        id: 2,
        title: 'Unit 2',
        description: 'Daily Conversations',
        color: '#9178ff',
        lessons: [
          { id: 5, title: 'Family', description: 'Talk about family', icon: '👨‍👩‍👧‍👦', status: 'locked', xp: 75, estimatedDuration: 20, content: '', courseId: '2', exercises: [] },
          { id: 6, title: 'Food', description: 'Order food', icon: '🍕', status: 'locked', xp: 75, estimatedDuration: 15, content: '', courseId: '2', exercises: [] },
          { id: 7, title: 'Shopping', description: 'Go shopping', icon: '🛍️', status: 'locked', xp: 75, estimatedDuration: 20, content: '', courseId: '2', exercises: [] },
        ]
      },
      {
        id: 3,
        title: 'Unit 3',
        description: 'Grammar Foundations',
        color: '#5a3ce6',
        lessons: [
          { id: 8, title: 'Verbs', description: 'Present tense verbs', icon: '⚡', status: 'locked', xp: 100, estimatedDuration: 25, content: '', courseId: '3', exercises: [] },
          { id: 9, title: 'Adjectives', description: 'Describe things', icon: '✨', status: 'locked', xp: 100, estimatedDuration: 20, content: '', courseId: '3', exercises: [] },
          { id: 10, title: 'Questions', description: 'Ask questions', icon: '❓', status: 'locked', xp: 100, estimatedDuration: 20, content: '', courseId: '3', exercises: [] },
        ]
      },
      {
        id: 4,
        title: 'Unit 4',
        description: 'Real World Practice',
        color: '#a78bfa',
        lessons: [
          { id: 11, title: 'Travel', description: 'Navigate around', icon: '✈️', status: 'locked', xp: 125, estimatedDuration: 30, content: '', courseId: '4', exercises: [] },
          { id: 12, title: 'Work', description: 'Professional talk', icon: '💼', status: 'locked', xp: 125, estimatedDuration: 25, content: '', courseId: '4', exercises: [] },
          { id: 13, title: 'Hobbies', description: 'Discuss interests', icon: '🎸', status: 'locked', xp: 125, estimatedDuration: 25, content: '', courseId: '4', exercises: [] },
        ]
      }
    ];
  }

  onLessonClick(lesson: Lesson) {
    if (lesson.status === 'available' || lesson.status === 'in-progress' || lesson.status === 'completed') {
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

  getPathPoints(): string {
    // Generate SVG path points for a straight centered path
    const lessons = this.getAllLessons();
    let path = '';
    const x = 100; // Center of the 200px wide viewbox

    if (lessons.length === 0) {
      return '';
    }

    // Move to the starting point of the first lesson
    path += `M ${x},${1 * 180 + 100}`;

    // Draw lines to subsequent lessons
    for (let i = 1; i < lessons.length; i++) {
      const y = i * 180 + 100;
      path += ` L ${x},${y}`;
    }

    return path;
  }

  getAllLessons(): Lesson[] {
    return this.courses.flatMap(unit => unit.lessons);
  }

  getLessonPosition(lessonIndex: number): { x: number, y: number } {
    const y = lessonIndex * 180 + 100;
    const x = lessonIndex % 2 === 0 ? 50 : -50;
    return { x, y };
  }

  redirectToCreateExercise() {
    this.router.navigate(["/create-lesson"])
  }
}
