import { Lesson } from './lesson.interface';

export interface Course {
  courseId: string;
  title: string;
  languageName: string;
  description?: string;
  estimatedDurationHours: number;
  orderIndex: number;
  lessonCount: number;
}

// Extended course with lessons loaded for UI display
export interface CourseWithLessons extends Course {
  lessons: Lesson[];
  color: string;
}

export interface CoursesWithProgressResponse {
  courses: HomeCourse[];
  totalXp: number;
  hearts: number;
  nextHeartRefillAt: string | null;
}

export interface HomeCourse {
  courseId: string;
  languageName: string;
  title: string;
  description?: string;
  estimatedDurationHours?: number;
  orderIndex: number;
  lessonCount: number;
  lessons: HomeLesson[];
}

export interface HomeLesson {
  lessonId: string;
  courseId: string;
  courseName: string;
  title: string;
  estimatedDurationMinutes?: number;
  orderIndex: number;
  isLocked: boolean;
  completedExercises?: number;
  earnedXp?: number;
  totalPossibleXp?: number;
  isCompleted?: boolean;
}