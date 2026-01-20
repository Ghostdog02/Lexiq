import { FormArray, FormControl, FormGroup } from "@angular/forms";
import { Exercise, ExerciseForm } from "./exercise.interface";

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
  exercises: FormArray<ExerciseForm>;
}

export type LessonForm = FormGroup<LessonFormControls>;