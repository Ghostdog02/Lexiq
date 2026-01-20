import { FormArray, FormControl, FormGroup } from "@angular/forms";
import { Question, QuestionForm } from "./question.interface";

export enum DifficultyLevel {
  Beginner = 'Beginner',
  Intermediate = 'Intermediate',
  Advanced = 'Advanced'
}

export interface Exercise {
  title: string;
  instructions: string;
  estimatedDurationMinutes: number;
  difficultyLevel: DifficultyLevel;
  points: number;
  questions: Question[];
}

export interface ExerciseFormControls {
  title: FormControl<string>;
  instructions: FormControl<string>;
  estimatedDurationMinutes: FormControl<number>;
  difficultyLevel: FormControl<DifficultyLevel>;
  points: FormControl<number>;
  questions: FormArray<QuestionForm>;
}

export type ExerciseForm = FormGroup<ExerciseFormControls>;