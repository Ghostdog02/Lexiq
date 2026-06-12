import {Component, HostListener, inject, OnInit} from '@angular/core';
import {Router} from '@angular/router';
import {HttpClient} from '@angular/common/http';
import {firstValueFrom} from 'rxjs';
import {AuthService} from '../../../../auth/auth.service';
import {Lesson, LessonStatus} from '../../models/lesson.interface';
import {CourseWithLessons, CoursesWithProgressResponse, HomeLesson} from '../../models/course.interface';

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
  hearts: number = 0;
  isUserBelowLesson: boolean = false;
  isLoading: boolean = true;
  error: string | null = null;
  isAdmin: boolean = false;
  isContentCreator: boolean = false;
  userIsAuthenticated: boolean = false;

  // Color palette for courses (cycles if more courses than colors)
  private readonly courseColors = ['#7c5cff', '#9178ff', '#5a3ce6', '#a78bfa', '#8b5cf6', '#7c3aed'];

  // Icon set for lessons (cycles if more lessons than icons)
  private readonly lessonIcons = ['📚', '✏️', '🎯', '💡', '🔤', '🗣️', '📝', '🎓'];

  private router = inject(Router);
  private authService = inject(AuthService);
  private http = inject(HttpClient);

  async ngOnInit() {
    this.isAdmin = this.authService.getIsAdmin();
    this.isContentCreator = this.authService.getIsContentCreator();
    this.userIsAuthenticated = this.authService.getIsAuth();
    await this.loadCoursesFromApi();
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
      const data = await firstValueFrom(
        this.http.get<CoursesWithProgressResponse>('/api/courses/with-progress')
      );

      this.totalXp = data.totalXp;
      this.hearts = data.hearts;

      this.courses = data.courses.map((course, index) => ({
        courseId: course.courseId,
        title: course.title,
        languageName: course.languageName,
        description: course.description,
        estimatedDurationHours: course.estimatedDurationHours ?? 0,
        orderIndex: course.orderIndex,
        lessonCount: course.lessonCount,
        color: this.courseColors[index % this.courseColors.length],
        lessons: course.lessons.map((lesson, lessonIndex) => ({
          ...lesson,
          lessonContent: '',
          exercises: [],
          status: this.deriveLessonStatus(lesson),
          icon: this.lessonIcons[lessonIndex % this.lessonIcons.length],
        })),
      }));

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
    if ((lesson.status !== 'locked' || this.isAdmin) && lesson.lessonId) {
      this.router.navigate(['/lesson', lesson.lessonId]);
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

  private deriveLessonStatus(lesson: HomeLesson): LessonStatus {
    if (lesson.isLocked) return 'locked';
    if (lesson.isCompleted) return 'completed';
    if ((lesson.completedExercises ?? 0) > 0) return 'in-progress';
    return 'available';
  }

}
