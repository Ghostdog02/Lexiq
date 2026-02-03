export interface Course {
  id: string;
  title: string;
  LanguageName: string;
  description?: string;
  estimatedDurationHours: number;
  orderIndex: number;
  lessonCount: number;
}