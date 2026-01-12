import { Component, HostListener, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';

export interface Lesson {
  id: number;
  title: string;
  description: string;
  icon: string;
  status: 'locked' | 'available' | 'in-progress' | 'completed';
  xp: number;
  progress?: number; // 0-100 for in-progress lessons
}

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
  units: Unit[] = [];
  currentLessonId: number = 3;
  totalXp: number = 150;
  currentStreak: number = 5;
  isUserBelowLesson: boolean = false;

  ngOnInit() {
    this.initializeLearningPath();
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
    this.units = [
      {
        id: 1,
        title: 'Unit 1',
        description: 'Greetings & Basics',
        color: '#7c5cff',
        lessons: [
          { id: 1, title: 'Hello!', description: 'Learn basic greetings', icon: '👋', status: 'completed', xp: 50 },
          { id: 2, title: 'Introductions', description: 'Introduce yourself', icon: '🙋', status: 'completed', xp: 50 },
          { id: 3, title: 'Numbers', description: 'Count from 1 to 10', icon: '🔢', status: 'in-progress', xp: 50, progress: 65 },
          { id: 4, title: 'Colors', description: 'Learn basic colors', icon: '🎨', status: 'available', xp: 50 },
        ]
      },
      {
        id: 2,
        title: 'Unit 2',
        description: 'Daily Conversations',
        color: '#9178ff',
        lessons: [
          { id: 5, title: 'Family', description: 'Talk about family', icon: '👨‍👩‍👧‍👦', status: 'locked', xp: 75 },
          { id: 6, title: 'Food', description: 'Order food', icon: '🍕', status: 'locked', xp: 75 },
          { id: 7, title: 'Shopping', description: 'Go shopping', icon: '🛍️', status: 'locked', xp: 75 },
        ]
      },
      {
        id: 3,
        title: 'Unit 3',
        description: 'Grammar Foundations',
        color: '#5a3ce6',
        lessons: [
          { id: 8, title: 'Verbs', description: 'Present tense verbs', icon: '⚡', status: 'locked', xp: 100 },
          { id: 9, title: 'Adjectives', description: 'Describe things', icon: '✨', status: 'locked', xp: 100 },
          { id: 10, title: 'Questions', description: 'Ask questions', icon: '❓', status: 'locked', xp: 100 },
        ]
      },
      {
        id: 4,
        title: 'Unit 4',
        description: 'Real World Practice',
        color: '#a78bfa',
        lessons: [
          { id: 11, title: 'Travel', description: 'Navigate around', icon: '✈️', status: 'locked', xp: 125 },
          { id: 12, title: 'Work', description: 'Professional talk', icon: '💼', status: 'locked', xp: 125 },
          { id: 13, title: 'Hobbies', description: 'Discuss interests', icon: '🎸', status: 'locked', xp: 125 },
        ]
      }
    ];
  }

  onLessonClick(lesson: Lesson) {
    if (lesson.status === 'available' || lesson.status === 'in-progress') {
      console.log('Starting lesson:', lesson.title);
      // Navigate to lesson view - implement routing here
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
    return this.units.flatMap(unit => unit.lessons);
  }

  getLessonPosition(lessonIndex: number): { x: number, y: number } {
    const y = lessonIndex * 180 + 100;
    const x = lessonIndex % 2 === 0 ? 50 : -50;
    return { x, y };
  }
}
