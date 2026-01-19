import { Question } from "./question.interface";

export interface Exercise {
  title: string;
  instructions: string;
  estimatedDurationMinutes: number;
  difficultyLevel: 'Beginner' | 'Intermediate' | 'Advanced';
  points: number;
  lessonName: string;
  questions: Question[];
}

export interface Lesson {
  title: string;
  description: string;
  estimatedDuration: number;
  content: string;
  courseName: string;
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