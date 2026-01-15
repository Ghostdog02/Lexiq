export interface Exercise {
  type: 'multiple-choice' | 'fill-in-blank' | 'translation';
  question: string;
  options?: string[]; // Optional, only for multiple-choice
  correctAnswer?: string; // Optional, for multiple-choice
  answer?: string; // Optional, for fill-in-blank and translation
}

export interface Lesson {
  title: string;
  description: string;
  content: string;
  difficulty: number;
  exercises: Exercise[];
}

export interface LessonResponse {
  id: string;
  title: string;
  description: string;
  content: string;
  difficulty: number;
  exercises: Exercise[];
  createdAt: string;
  updatedAt: string;
}