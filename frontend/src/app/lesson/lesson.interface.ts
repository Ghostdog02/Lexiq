import { FormArray, FormControl, FormGroup } from "@angular/forms";
import { Exercise, ExerciseForm, ExerciseType } from "./exercise.interface";

export type LessonStatus = 'locked' | 'available' | 'in-progress' | 'completed';

export interface Lesson {
  // Core lesson data
  title: string;
  description: string;
  estimatedDuration: number;
  content: string;
  courseId: string;
  exercises: Exercise[];

  // Path display properties (optional - set when adding to learning path)
  id?: number;
  icon?: string;
  status?: LessonStatus;
  xp?: number;
  progress?: number;
}

export interface LessonFormControls {
  title: FormControl<string>;
  description: FormControl<string>;
  estimatedDuration: FormControl<number>;
  content: FormControl<string>;
  courseId: FormControl<string>;
  exercises: FormArray<ExerciseForm>;
  exerciseType: FormControl<ExerciseType | ''>;
}

export type LessonForm = FormGroup<LessonFormControls>;