import { inject, Injectable } from '@angular/core';
import { FormArray, FormGroup, NonNullableFormBuilder, Validators } from '@angular/forms';
import {
  DifficultyLevel,
  ExerciseForm,
  ExerciseFormControls,
  ExerciseType,
  FillInBlankFormControls,
  ListeningFormControls,
  MultipleChoiceFormControls,
  OptionForm,
  OptionFormControls,
  TrueFalseFormControls,
} from '../models/exercise.interface';
import { LessonForm } from '../models/lesson.interface';

@Injectable({ providedIn: 'root' })
export class LessonFormService {
  private fb = inject(NonNullableFormBuilder);

  createLessonForm(): LessonForm {
    return this.fb.group({
      title: this.fb.control('', {
        validators: [
          Validators.required,
          Validators.minLength(3),
          Validators.maxLength(100),
        ],
      }),
      description: this.fb.control('', {
        validators: [
          Validators.required,
          Validators.minLength(10),
          Validators.maxLength(500),
        ],
      }),
      estimatedDuration: this.fb.control(30, {
        validators: [Validators.min(1), Validators.max(300)],
      }),
      mediaUrl: this.fb.control(''),
      content: this.fb.control('', { validators: Validators.required }),
      courseId: this.fb.control('', {
        validators: [Validators.required, Validators.minLength(1)],
      }),
      exercises: this.fb.array<ExerciseForm>([]),
      exerciseType: this.fb.control<ExerciseType | ''>('' as ExerciseType | ''),
    });
  }

  createExerciseForm(type: ExerciseType): ExerciseForm {
    switch (type) {
      case ExerciseType.MultipleChoice:
        return this.createMultipleChoiceForm();
      case ExerciseType.FillInBlank:
        return this.createFillInBlankForm();
      case ExerciseType.Listening:
        return this.createListeningForm();
      case ExerciseType.TrueFalse:
        return this.createTrueFalseForm();
      default:
        throw new Error(`Unknown exercise type: ${type}`);
    }
  }

  private createBaseControls(): ExerciseFormControls {
    return {
      instructions: this.fb.control('', { validators: Validators.required }),
      difficultyLevel: this.fb.control(DifficultyLevel.Beginner),
      points: this.fb.control<number>(10),
      explanation: this.fb.control(''),
      exerciseType: this.fb.control(ExerciseType.MultipleChoice, {
        validators: Validators.required,
      }),
    } as ExerciseFormControls;
  }

  private createOptionForm(): OptionForm {
    return this.fb.group<OptionFormControls>({
      optionText: this.fb.control('', { validators: Validators.required }),
      isCorrect: this.fb.control(false),
      explanation: this.fb.control(''),
    });
  }

  private createMultipleChoiceForm(): FormGroup<MultipleChoiceFormControls> {
    return this.fb.group({
      ...this.createBaseControls(),
      exerciseType: this.fb.control<ExerciseType.MultipleChoice>(
        ExerciseType.MultipleChoice,
        { validators: Validators.required }
      ),
      options: this.fb.array<OptionForm>([]),
    });
  }

  private createFillInBlankForm(): FormGroup<FillInBlankFormControls> {
    return this.fb.group({
      ...this.createBaseControls(),
      exerciseType: this.fb.control<ExerciseType.FillInBlank>(
        ExerciseType.FillInBlank,
        { validators: Validators.required }
      ),
      text: this.fb.control('', { validators: Validators.required }),
      options: this.fb.array<OptionForm>([]),
    });
  }

  private createListeningForm(): FormGroup<ListeningFormControls> {
    return this.fb.group({
      ...this.createBaseControls(),
      exerciseType: this.fb.control<ExerciseType.Listening>(
        ExerciseType.Listening,
        { validators: Validators.required }
      ),
      audioUrl: this.fb.control('', { validators: Validators.required }),
      maxReplays: this.fb.control(3, {
        validators: [Validators.min(1), Validators.max(10)],
      }),
      options: this.fb.array<OptionForm>([]),
    });
  }

  private createTrueFalseForm(): FormGroup<TrueFalseFormControls> {
    return this.fb.group({
      ...this.createBaseControls(),
      exerciseType: this.fb.control<ExerciseType.TrueFalse>(
        ExerciseType.TrueFalse,
        { validators: Validators.required }
      ),
      statement: this.fb.control('', { validators: Validators.required }),
      imageUrl: this.fb.control(''),
      options: this.fb.array<OptionForm>([
        this.fb.group<OptionFormControls>({
          optionText: this.fb.control('True'),
          isCorrect: this.fb.control(false),
          explanation: this.fb.control(''),
        }),
        this.fb.group<OptionFormControls>({
          optionText: this.fb.control('False'),
          isCorrect: this.fb.control(true),
          explanation: this.fb.control(''),
        }),
      ]),
    });
  }

  addExerciseToForm(
    exercises: FormArray<ExerciseForm>,
    type: ExerciseType = ExerciseType.MultipleChoice
  ): void {
    exercises.push(this.createExerciseForm(type));
  }

  removeExerciseFromForm(
    exercises: FormArray<ExerciseForm>,
    index: number
  ): void {
    exercises.removeAt(index);
  }

  addOption(form: FormGroup): void {
    const options = form.get('options') as FormArray | null;
    options?.push(this.createOptionForm());
  }

  removeOption(form: FormGroup, index: number): void {
    const options = form.get('options') as FormArray | null;
    if (options && options.length > 2) {
      options.removeAt(index);
    }
  }
}
