import { FormArray, FormControl, FormGroup } from "@angular/forms";
import { Exercise, ExerciseForm, ExerciseType } from "./exercise.interface";

export type LessonStatus = 'locked' | 'available' | 'in-progress' | 'completed';

export interface Lesson {
  // Core lesson data
  title: string;
  description: string;
  estimatedDuration: number;
  mediaUrl?: string; // Cover image URL
  content: string;   // Editor.js JSON content
  courseId: number;  // Course ID (numeric)
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
  mediaUrl: FormControl<string>;
  content: FormControl<string>;
  courseId: FormControl<number>;  // Changed to number
  exercises: FormArray<ExerciseForm>;
  exerciseType: FormControl<ExerciseType | ''>;
}

export type LessonForm = FormGroup<LessonFormControls>;