import { FormControl, FormGroup } from "@angular/forms";
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

export interface LessonFormControls {
  title: FormControl<string>;
  description: FormControl<string>;
  estimatedDuration: FormControl<number>;
  content: FormControl<string>;
  courseId: FormControl<string>;
  exercises: FormControl<Exercise[]>;
}

export type LessonForm = FormGroup<LessonFormControls>;