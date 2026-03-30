import { FormArray, FormControl, FormGroup } from "@angular/forms";
import { Exercise, ExerciseForm, ExerciseFormValue, ExerciseType } from "./exercise.interface";

export type LessonStatus = 'locked' | 'available' | 'in-progress' | 'completed';

export interface Lesson {
  lessonId: string;
  courseId: string;
  courseName: string;
  title: string;
  description?: string;
  estimatedDurationMinutes?: number;
  orderIndex: number;
  lessonContent: string;
  isLocked: boolean;
  exercises: Exercise[];
  completedExercises?: number;
  earnedXp?: number;
  totalPossibleXp?: number;
  isCompleted?: boolean;
  // Derived client-side, not from API
  status: LessonStatus;
  icon?: string; // Assigned dynamically by UI
}

export interface LessonProgressSummary {
  completedExercises: number;
  totalExercises: number;
  earnedXp: number;
  totalPossibleXp: number;
  completionPercentage: number;
  meetsCompletionThreshold: boolean;
}

export interface ExerciseSubmitResult {
  isCorrect: boolean;
  pointsEarned: number;
  correctAnswer: string | null;
  explanation: string | null;
}

export interface SubmitAnswerResponse extends ExerciseSubmitResult {
  lessonProgress: LessonProgressSummary;
}

// Saved progress loaded from backend (maps exerciseId → progress)
export interface UserExerciseProgress {
  exerciseId: string;
  isCompleted: boolean;
  pointsEarned: number;
  completedAt: string | null;
}

export interface CompleteLessonResponse {
  currentLessonId: string;
  isCompleted: boolean;
  earnedXp: number;
  totalPossibleXp: number;
  completionPercentage: number;
  requiredThreshold: number;
  isLastInCourse: boolean;
  nextLesson: {
    id: string;
    title: string;
    courseId: string;
    wasUnlocked: boolean;
    isLocked: boolean;
  } | null;
}

export interface CreateLessonApiResponse {
  courseName: string;
  title: string;
  description?: string;
  estimatedDurationMinutes?: number;
  lessonContent: string;
  isLocked: boolean;
}

export interface UpdateLessonApiResponse {
  courseName: string;
  title: string;
  description?: string;
  estimatedDurationMinutes?: number;
  orderIndex: number;
  lessonContent: string;
}

export interface CreateLessonDto {
  title: string;
  description: string;
  estimatedDuration: number;
  content: string;
  courseId: string;
  exercises: ExerciseFormValue[];
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