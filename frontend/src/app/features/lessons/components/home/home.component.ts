import { Component, HostListener, inject, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { LessonService } from '../../services/lesson.service';
import { AuthService } from '../../../../auth/auth.service';
import { Lesson, LessonStatus } from '../../models/lesson.interface';
import { CourseWithLessons } from '../../models/course.interface';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [],
  templateUrl: './home.component.html',
  styleUrl: './home.component.scss'
})
export class HomeComponent implements OnInit {
  courses: CourseWithLessons[] = [];
  currentLessonId: string = '';
  totalXp: number = 0;
  currentStreak: number = 0;
  isUserBelowLesson: boolean = false;
  isLoading: boolean = true;
  error: string | null = null;
  isAdmin: boolean = false;

  // Color palette for courses (cycles if more courses than colors)
  private readonly courseColors = ['#7c5cff', '#9178ff', '#5a3ce6', '#a78bfa', '#8b5cf6', '#7c3aed'];

  private router = inject(Router);
  private lessonService = inject(LessonService);
  private authService = inject(AuthService);

  ngOnInit() {
    this.isAdmin = this.authService.getIsAdmin();
    this.loadCoursesFromApi();
  }

  @HostListener('window:scroll')
  onScroll(): void {
    const currentLessonElement = document.getElementById(`lesson-${this.currentLessonId}`);
    if (currentLessonElement) {
      const rect = currentLessonElement.getBoundingClientRect();
      // If the top of the lesson is above the viewport, the user is below it.
      this.isUserBelowLesson = rect.top < 0;
    }
  }

  /**
   * Fetches courses from the API, then fetches lessons for each course.
   * Maps API responses to the UI model shape expected by the template.
   */
  async loadCoursesFromApi(): Promise<void> {
    this.isLoading = true;
    this.error = null;

    try {
      const coursesFromApi = await this.lessonService.getCourses();

      const coursesWithLessons: CourseWithLessons[] = await Promise.all(
        coursesFromApi.map(async (course, index) => {
          const lessonsFromApi = await this.lessonService.getLessonsByCourse(course.courseId);

          // Derive status from progress fields for each lesson
          const lessonsWithStatus = lessonsFromApi.map(lesson => ({
            ...lesson,
            status: this.deriveLessonStatus(lesson)
          }));

          return {
            ...course,
            color: this.courseColors[index % this.courseColors.length],
            lessons: lessonsWithStatus
          };
        })
      );

      this.courses = coursesWithLessons;
      this.updateTotalXp();

      // Set current lesson to first non-locked lesson
      this.setCurrentLesson();

    } catch (err) {
      console.error('❌ Error loading courses:', err);
      this.error = 'Failed to load courses. Please try again.';
    } finally {
      this.isLoading = false;
    }
  }

  /**
   * Sets currentLessonId to the first available (non-locked) lesson.
   */
  private setCurrentLesson(): void {
    const allLessons = this.getAllLessons();
    const firstAvailable = allLessons.find(l => l.status === 'available' || l.status === 'in-progress');
    if (firstAvailable?.lessonId) {
      this.currentLessonId = firstAvailable.lessonId;
    }
  }

  onLessonClick(lesson: Lesson) {
    if (lesson.status === 'available' || lesson.status === 'in-progress' || lesson.status === 'completed' || this.isAdmin) {
      if (lesson.lessonId) {
        this.router.navigate(['/lesson', lesson.lessonId]);
      }
    }
  }

  scrollToCurrentLesson() {
    const currentLesson = document.getElementById(`lesson-${this.currentLessonId}`);
    if (currentLesson) {
      currentLesson.scrollIntoView({ behavior: 'smooth', block: 'center' });
    }
  }

  getAllLessons(): Lesson[] {
    return this.courses.flatMap(course => course.lessons);
  }

  redirectToCreateExercise() {
    this.router.navigate(["/create-lesson"])
  }

  private deriveLessonStatus(lesson: Lesson): LessonStatus {
    if (lesson.isLocked) return 'locked';
    if (lesson.isCompleted) return 'completed';
    if ((lesson.completedExercises ?? 0) > 0) return 'in-progress';
    return 'available';
  }

  private updateTotalXp(): void {
    const allLessons = this.getAllLessons();
    this.totalXp = allLessons.reduce((sum, l) => sum + (l.earnedXp ?? 0), 0);
  }
}
