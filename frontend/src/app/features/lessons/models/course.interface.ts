import { Lesson } from './lesson.interface';

// API response shape (matches backend CourseDto)
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