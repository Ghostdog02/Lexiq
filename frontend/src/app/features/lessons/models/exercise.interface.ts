import { FormArray, FormControl, FormGroup } from "@angular/forms";

export enum DifficultyLevel {
  Beginner = 'Beginner',
  Intermediate = 'Intermediate',
  Advanced = 'Advanced'
}

export enum ExerciseType {
  MultipleChoice = 'MultipleChoice',
  FillInBlank = 'FillInBlank',
  Listening = 'Listening',
  Translation = 'Translation'
}

export interface Exercise {
  id: string;
  lessonId: string;
  title: string;
  instructions: string;
  estimatedDurationMinutes: number;
  difficultyLevel: DifficultyLevel | number;
  points: number;
  orderIndex: number;
  explanation?: string;
  type: ExerciseType;  // Backend returns "type" not "exerciseType"
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
  id: string;
  optionText: string;
  isCorrect: boolean;
  orderIndex: number;
}

export interface QuestionOptionFormControls {
  optionText: FormControl<string>;
  isCorrect: FormControl<boolean>;
}

export interface MultipleChoiceExercise extends Exercise {
  type: ExerciseType.MultipleChoice;
  options: QuestionOption[];
}

export interface FillInBlankExercise extends Exercise {
  type: ExerciseType.FillInBlank;
  text: string;
  correctAnswer: string;
  acceptedAnswers?: string | null;
  caseSensitive: boolean;
  trimWhitespace: boolean;
}

export interface ListeningExercise extends Exercise {
  type: ExerciseType.Listening;
  audioUrl: string;
  correctAnswer: string;
  acceptedAnswers?: string | null;
  caseSensitive: boolean;
  maxReplays: number;
}

export interface TranslationExercise extends Exercise {
  type: ExerciseType.Translation;
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

// ---------------------------------------------------------------------------
// Exercise Answer Forms (for solving exercises)
// ---------------------------------------------------------------------------

/**
 * Form controls for a single exercise answer.
 * The `answer` control holds:
 *   - MultipleChoice: selected option ID (radio button value)
 *   - FillInBlank: typed text (input)
 *   - Translation: typed translation (textarea)
 *   - Listening: typed transcription (input)
 */
export interface ExerciseAnswerControls {
  answer: FormControl<string>;
}

export type ExerciseAnswerForm = FormGroup<ExerciseAnswerControls>;

/**
 * Top-level form for the exercise viewer.
 * Contains a FormArray with one ExerciseAnswerForm per exercise in the lesson.
 */
export interface ExerciseViewerFormControls {
  exercises: FormArray<ExerciseAnswerForm>;
}

export type ExerciseViewerForm = FormGroup<ExerciseViewerFormControls>;