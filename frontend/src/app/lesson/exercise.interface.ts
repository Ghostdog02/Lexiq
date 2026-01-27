import { FormArray, FormControl, FormGroup } from "@angular/forms";

export enum DifficultyLevel {
  Beginner = 'Beginner',
  Intermediate = 'Intermediate',
  Advanced = 'Advanced'
}

export enum ExerciseType {
  MultipleChoice = 'MultipleChoice',
  FillInBlank = 'FillInTheBlank',
  Listening = 'Listening',
  Translation = 'Translation'
}

export interface Exercise {
  title: string;
  instructions: string;
  question: string;
  estimatedDurationMinutes: number;
  difficultyLevel: DifficultyLevel;
  points: number;
  explanation?: string;
  exerciseType: ExerciseType;
}

export interface ExerciseFormControls {
  title: FormControl<string>;
  instructions: FormControl<string>;
  question: FormControl<string>;
  estimatedDurationMinutes: FormControl<number>;
  difficultyLevel: FormControl<DifficultyLevel>;
  points: FormControl<number>;
  explanation: FormControl<string>;
  exerciseType: FormControl<ExerciseType>;
}

export interface QuestionOption {
  optionText: string;
  isCorrect: boolean;
}

export interface QuestionOptionFormControls {
  optionText: FormControl<string>;
  isCorrect: FormControl<boolean>;
}

export interface MultipleChoiceExercise extends Exercise {
  exerciseType: ExerciseType.MultipleChoice;
  options: QuestionOption[];
}

export interface FillInBlankExercise extends Exercise {
  exerciseType: ExerciseType.FillInBlank;
  correctAnswer: string;
  acceptedAnswers?: string;
  caseSensitive: boolean;
  trimWhitespace: boolean;
}

export interface ListeningExercise extends Exercise {
  exerciseType: ExerciseType.Listening;
  correctAnswer: string;
  acceptedAnswers?: string;
  caseSensitive: boolean;
  audioUrl: string;
  maxReplays: number;
}

export interface TranslationExercise extends Exercise {
  exerciseType: ExerciseType.Translation;
  sourceText: string;
  targetText: string;
  sourceLanguageCode: string;
  targetLanguageCode: string;
  matchingThreshold: number;
}

export type AnyExercise =
  | MultipleChoiceExercise
  | FillInBlankExercise
  | TranslationExercise
  | ListeningExercise;

export interface MultipleChoiceFormControls extends ExerciseFormControls {
  exerciseType: FormControl<ExerciseType.MultipleChoice>;
  options: FormArray<QuestionOptionForm>;
}

export interface FillInBlankFormControls extends ExerciseFormControls {
  exerciseType: FormControl<ExerciseType.FillInBlank>;
  correctAnswer: FormControl<string>;
  acceptedAnswers?: FormControl<string>;
  caseSensitive: FormControl<boolean>;
  trimWhitespace: FormControl<boolean>;
}

export interface TranslationFormControls extends ExerciseFormControls {
  exerciseType: FormControl<ExerciseType.Translation>;
  sourceText: FormControl<string>;
  targetText: FormControl<string>;
  sourceLanguageCode: FormControl<string>;
  targetLanguageCode: FormControl<string>;
  matchingThreshold: FormControl<number>;
}

export interface ListeningFormControls extends ExerciseFormControls {
  exerciseType: FormControl<ExerciseType.Listening>;
  correctAnswer: FormControl<string>;
  acceptedAnswers?: FormControl<string>;
  caseSensitive: FormControl<boolean>;
  maxReplays: FormControl<number>;
  audioUrl: FormControl<string>;
}

export type MultipleChoiceForm = FormGroup<MultipleChoiceFormControls>;
export type FillInBlankForm = FormGroup<FillInBlankFormControls>;
export type TranslationForm = FormGroup<TranslationFormControls>;
export type ListeningForm = FormGroup<ListeningFormControls>;
export type ExerciseForm = FormGroup<any>;
export type QuestionOptionForm = FormGroup<QuestionOptionFormControls>;