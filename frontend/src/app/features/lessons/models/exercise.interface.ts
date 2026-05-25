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
  TrueFalse = 'TrueFalse'
}

export interface ExerciseOption {
  id: string;
  optionText: string;
  isCorrect: boolean;
  explanation: string;
}

// Base Exercise = concrete MultipleChoice type (instructions + options list)
export interface Exercise {
  id: string;
  lessonId: string;
  instructions: string;
  difficultyLevel: DifficultyLevel | number;
  points: number;
  explanation?: string;
  type: ExerciseType;
  isLocked: boolean;
  options: ExerciseOption[];
}

export interface FillInBlankExercise extends Exercise {
  type: ExerciseType.FillInBlank;
  text: string;
}

export interface ListeningExercise extends Exercise {
  type: ExerciseType.Listening;
  audioUrl: string;
  maxReplays: number;
}

export interface TrueFalseExercise extends Exercise {
  type: ExerciseType.TrueFalse;
  statement: string;
  imageUrl?: string;
}

export type AnyExercise = Exercise | FillInBlankExercise | ListeningExercise | TrueFalseExercise;

// ---------------------------------------------------------------------------
// Form controls
// ---------------------------------------------------------------------------

export interface ExerciseFormControls {
  instructions: FormControl<string>;
  difficultyLevel: FormControl<DifficultyLevel>;
  points: FormControl<number>;
  explanation: FormControl<string>;
  exerciseType: FormControl<ExerciseType>;
}

export interface OptionFormControls {
  optionText: FormControl<string>;
  isCorrect: FormControl<boolean>;
  explanation: FormControl<string>;
}

export type OptionForm = FormGroup<OptionFormControls>;

export interface MultipleChoiceFormControls extends ExerciseFormControls {
  exerciseType: FormControl<ExerciseType.MultipleChoice>;
  options: FormArray<OptionForm>;
}

export interface FillInBlankFormControls extends ExerciseFormControls {
  exerciseType: FormControl<ExerciseType.FillInBlank>;
  text: FormControl<string>;
  options: FormArray<OptionForm>;
}

export interface ListeningFormControls extends ExerciseFormControls {
  exerciseType: FormControl<ExerciseType.Listening>;
  audioUrl: FormControl<string>;
  maxReplays: FormControl<number>;
  options: FormArray<OptionForm>;
}

export interface TrueFalseFormControls extends ExerciseFormControls {
  exerciseType: FormControl<ExerciseType.TrueFalse>;
  statement: FormControl<string>;
  imageUrl: FormControl<string>;
  options: FormArray<OptionForm>;
}

export type MultipleChoiceForm = FormGroup<MultipleChoiceFormControls>;
export type FillInBlankForm = FormGroup<FillInBlankFormControls>;
export type ListeningForm = FormGroup<ListeningFormControls>;
export type TrueFalseForm = FormGroup<TrueFalseFormControls>;
export type ExerciseForm = FormGroup<any>;

// ---------------------------------------------------------------------------
// Form value types — the shape returned by FormGroup.getRawValue()
// ---------------------------------------------------------------------------

interface BaseExerciseFormValue {
  instructions: string;
  difficultyLevel: DifficultyLevel;
  points: number;
  explanation: string;
  options: { optionText: string; isCorrect: boolean; explanation: string }[];
}

export interface MultipleChoiceFormValue extends BaseExerciseFormValue {
  exerciseType: ExerciseType.MultipleChoice;
}

export interface FillInBlankFormValue extends BaseExerciseFormValue {
  exerciseType: ExerciseType.FillInBlank;
  text: string;
}

export interface ListeningFormValue extends BaseExerciseFormValue {
  exerciseType: ExerciseType.Listening;
  audioUrl: string;
  maxReplays: number;
}

export interface TrueFalseFormValue extends BaseExerciseFormValue {
  exerciseType: ExerciseType.TrueFalse;
  statement: string;
  imageUrl: string;
}

export type ExerciseFormValue =
  | MultipleChoiceFormValue
  | FillInBlankFormValue
  | ListeningFormValue
  | TrueFalseFormValue;

// ---------------------------------------------------------------------------
// Backend Create DTOs — the shape the API expects
// ---------------------------------------------------------------------------

export interface CreateExerciseBase {
  difficultyLevel: DifficultyLevel;
  points: number;
  explanation: string;
}

export interface CreateOptionDto {
  optionText: string;
  isCorrect: boolean;
  explanation: string;
}

export interface CreateMultipleChoiceDto extends CreateExerciseBase {
  type: 'MultipleChoice';
  instructions: string;
  options: CreateOptionDto[];
}

export interface CreateFillInBlankDto extends CreateExerciseBase {
  type: 'FillInBlank';
  instructions: string;
  text: string;
  options: CreateOptionDto[];
}

export interface CreateListeningDto extends CreateExerciseBase {
  type: 'Listening';
  instructions: string;
  audioUrl: string;
  maxReplays: number;
  options: CreateOptionDto[];
}

export interface CreateTrueFalseDto extends CreateExerciseBase {
  type: 'TrueFalse';
  instructions: string;
  statement: string;
  imageUrl?: string;
  options: CreateOptionDto[];
}

export type CreateExerciseDto =
  | CreateMultipleChoiceDto
  | CreateFillInBlankDto
  | CreateListeningDto
  | CreateTrueFalseDto;
