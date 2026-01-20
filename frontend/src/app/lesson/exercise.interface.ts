import { FormArray, FormControl, FormGroup } from "@angular/forms";
import { Question, QuestionForm } from "./question.interface";

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
  questions: Question[];
  exerciseType: ExerciseType;
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

export interface QuestionOption {
  optionText: string;
  isCorrect: boolean;
  orderIndex: number;
}

export interface MultipleChoiceExercise extends Question {
  questionType: QuestionType.MultipleChoice;
  options: QuestionOption[];
}

export interface FillInBlankQuestion extends Question {
  questionType: QuestionType.FillInBlank;
  correctAnswer: string;
  acceptedAnswers?: string;
  caseSensitive: boolean;
  trimWhitespace: boolean;
}

export interface ListeningQuestion extends Question {
  questionType: QuestionType.Listening;
  correctAnswer: string;
  acceptedAnswers?: string;
  caseSensitive: boolean;
  maxReplays: number;
}

export interface TranslationQuestion extends Question {
  questionType: QuestionType.Translation;
  sourceLanguageCode: string;
  targetLanguageCode: string;
  matchingThreshold: number;
}