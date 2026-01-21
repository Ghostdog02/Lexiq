import { FormControl, FormGroup } from "@angular/forms";

export enum DifficultyLevel {
  Beginner = 'Beginner',
  Intermediate = 'Intermediate',
  Advanced = 'Advanced'
}

export enum ExerciseType {
  MultipleChoice = 'MultipleChoice',
  FillInTheBlank = 'FillInTheBlank',
  Listening = 'Listening',
  Translation = 'Translation'
}

export interface Exercise {
  title: string;
  instructions: string;
  estimatedDurationMinutes: number;
  difficultyLevel: DifficultyLevel;
  points: number;
  explanation?: string;
  exerciseType: ExerciseType;
}

export interface ExerciseFormControls {
  title: FormControl<string>;
  instructions: FormControl<string>;
  estimatedDurationMinutes: FormControl<number>;
  difficultyLevel: FormControl<DifficultyLevel>;
  points: FormControl<number>;
  explanation?: FormControl<string>;
  exerciseType: FormControl<ExerciseType>;
}

export type ExerciseForm = FormGroup<ExerciseFormControls>;

export interface QuestionOption {
  optionText: string;
  isCorrect: boolean;
  orderIndex: number;
}

export interface MultipleChoiceExercise extends Exercise {
  questionType: ExerciseType.MultipleChoice;
  options: QuestionOption[];
}

export interface FillInBlankExercise extends Exercise {
  questionType: ExerciseType.FillInTheBlank;
  correctAnswer: string;
  acceptedAnswers?: string;
  caseSensitive: boolean;
  trimWhitespace: boolean;
}

export interface ListeningExercise extends Exercise {
  questionType: ExerciseType.Listening;
  correctAnswer: string;
  acceptedAnswers?: string;
  caseSensitive: boolean;
  maxReplays: number;
}

export interface TranslationExercise extends Exercise {
  questionType: ExerciseType.Translation;
  sourceLanguageCode: string;
  targetLanguageCode: string;
  matchingThreshold: number;
}