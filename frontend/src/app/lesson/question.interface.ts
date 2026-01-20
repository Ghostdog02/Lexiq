import { FormControl, FormGroup } from "@angular/forms";

export interface Question {
  exerciseName: string;
  questionText: string;
  orderIndex: number;
  points: number;
  explanation?: string;
  questionType: QuestionType;
}

export enum QuestionType {
  MultipleChoice = 'MultipleChoice',
  FillInBlank = 'FillInBlank',
  Listening = 'Listening',
  Translation = 'Translation'
}

export interface QuestionFormControls {
  exerciseName: FormControl<string>;
  questionText: FormControl<string>;
  orderIndex: FormControl<number>;
  points: FormControl<number>;
  explanation: FormControl<string>;
  questionType: FormControl<QuestionType>;
}

export type QuestionForm = FormGroup<QuestionFormControls>;

export interface QuestionOption {
  optionText: string;
  isCorrect: boolean;
  orderIndex: number;
}

export interface MultipleChoiceQuestion extends Question {
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
