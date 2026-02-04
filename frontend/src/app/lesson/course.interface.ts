export interface Course {
  courseId: string;
  title: string;
  languageName: string;
  description?: string;
  estimatedDurationHours: number;
  orderIndex: number;
  lessonCount: number;
}