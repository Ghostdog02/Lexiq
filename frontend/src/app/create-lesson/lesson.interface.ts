import { Question } from "./question.interface";

export interface Exercise {
  title: string;
  instructions: string;
  estimatedDurationMinutes: number;
  difficultyLevel: 'Beginner' | 'Intermediate' | 'Advanced';
  points: number;
  questions: Question[];
}

export interface Lesson {
  title: string;
  description: string;
  estimatedDuration: number;
  content: string;
  courseId: string;
  exercises: Exercise[];
}