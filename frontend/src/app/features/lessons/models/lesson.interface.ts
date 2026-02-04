import { FormArray, FormControl, FormGroup } from "@angular/forms";
import { Exercise, ExerciseForm, ExerciseType } from "./exercise.interface";

export type LessonStatus = 'locked' | 'available' | 'in-progress' | 'completed';

// API response shape (matches backend LessonDto)
export interface LessonApiResponse {
  lessonId: string;
  courseId: string;
  courseName: string;
  title: string;
  description?: string;
  estimatedDurationMinutes?: number;
  orderIndex: number;
  lessonMediaUrl?: string[];
  lessonContent: string;
  lessonTextUrl?: string;
  isLocked: boolean;
  exerciseCount: number;
}

// UI model used for lesson creation forms and display
export interface Lesson {
  // Core lesson data
  title: string;
  description: string;
  estimatedDuration: number;
  mediaUrl?: string; // Cover image URL
  content: string;   // Editor.js JSON content
  courseId: string;
  exercises: Exercise[];

  // Path display properties (optional - set when adding to learning path)
  id?: string;
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
  courseId: FormControl<string>;
  exercises: FormArray<ExerciseForm>;
  exerciseType: FormControl<ExerciseType | ''>;
}

export type LessonForm = FormGroup<LessonFormControls>;