import { inject, Injectable } from '@angular/core';
import { FormArray, FormBuilder, FormControl, FormGroup, NonNullableFormBuilder, Validators } from '@angular/forms';
import {
  ExerciseType,
  DifficultyLevel,
  MultipleChoiceFormControls,
  FillInBlankFormControls,
  TranslationFormControls,
  ListeningFormControls,
  ExerciseForm,
  ExerciseFormControls,
  QuestionOptionForm
} from './exercise.interface';
import { LessonForm } from './lesson.interface';

@Injectable({
  providedIn: 'root'
})
export class LessonFormService {
  private fb = inject(NonNullableFormBuilder);

  createLessonForm(): LessonForm {
    const group = this.fb.group({
      title: this.fb.control('', {
        validators: [Validators.required, Validators.minLength(3), Validators.maxLength(100)]
      }),
      description: this.fb.control('', {
        validators: [Validators.required, Validators.minLength(10), Validators.maxLength(500)]
      }),
      estimatedDuration: this.fb.control(30, {
        validators: [Validators.min(1), Validators.max(300)]
      }),
      mediaUrl: this.fb.control(''),
      content: this.fb.control('', {
        validators: Validators.required
      }),
      courseId: this.fb.control(''),
      exercises: this.fb.array<ExerciseForm>([]),
      exerciseType: this.fb.control<ExerciseType | ''>('' as ExerciseType | '')
    });

    return group;
  }

  createExerciseForm(type: ExerciseType): ExerciseForm {
    switch (type) {
      case ExerciseType.MultipleChoice:
        return this.createMultipleChoiceForm();

      case ExerciseType.FillInBlank:
        return this.createFillInTheBlankForm();

      case ExerciseType.Translation:
        return this.createTranslationForm();

      case ExerciseType.Listening:
        return this.createListeningForm();

      default:
        throw new Error(`Unknown exercise type: ${type}`);
    }
  }

  private createBaseExerciseControls() {
    return {
      title: this.fb.control(''),
      instructions: this.fb.control<string>(''),
      estimatedDurationMinutes: this.fb.control<number>(1),
      question: this.fb.control('', {
        validators: Validators.required
      }),
      difficultyLevel: this.fb.control(DifficultyLevel.Beginner),
      points: this.fb.control<number>(1),
      explanation: this.fb.control<string>(''),
      exerciseType: this.fb.control(ExerciseType.FillInBlank, {
        validators: Validators.required
      }),
    } as ExerciseFormControls;
  }

  private createMultipleChoiceForm(): FormGroup<MultipleChoiceFormControls> {
    return this.fb.group({
      ...this.createBaseExerciseControls(),
      options: this.fb.array<QuestionOptionForm>([]),
      exerciseType: this.fb.control<ExerciseType.MultipleChoice>(ExerciseType.MultipleChoice, {
        validators: Validators.required
      })
    });
  }

  private createFillInTheBlankForm(): FormGroup<FillInBlankFormControls> {
    return this.fb.group({
      ...this.createBaseExerciseControls(),
      exerciseType: this.fb.control<ExerciseType.FillInBlank>(ExerciseType.FillInBlank, {
        validators: Validators.required
      }),
      correctAnswer: this.fb.control<string>('', {
        validators: Validators.required
      }),
      acceptedAnswers: this.fb.control<string>(''),
      caseSensitive: this.fb.control<boolean>(true),
      trimWhitespace: this.fb.control<boolean>(true),
    }) as FormGroup<FillInBlankFormControls>;
  }

  private createTranslationForm(): FormGroup<TranslationFormControls> {
    return this.fb.group({
      ...this.createBaseExerciseControls(),
      exerciseType: this.fb.control(ExerciseType.Translation, {
        validators: Validators.required
      }),
      sourceLanguageCode: this.fb.control<string>('en', {
        validators: [Validators.required, Validators.pattern('[a-z]{2}')]
      }),
      targetLanguageCode: this.fb.control<string>('it', {
        validators: [Validators.required, Validators.pattern('[a-z]{2}')]
      }),
      matchingThreshold: this.fb.control<number>(0.1, {
        validators: [Validators.required, Validators.min(0.1), Validators.max(1)]
      }),
      sourceText: this.fb.control<string>('', {
        validators: [Validators.required, Validators.maxLength(1000)]
      }),
      targetText: this.fb.control<string>('', {
        validators: [Validators.required, Validators.maxLength(1000)]
      }),
    }) as FormGroup<TranslationFormControls>;
  }

  private createListeningForm(): FormGroup<ListeningFormControls> {
    return this.fb.group({
      ...this.createBaseExerciseControls(),
      exerciseType: this.fb.control(ExerciseType.Listening, {
        validators: Validators.required
      }),
      correctAnswer: this.fb.control<string>('', {
        validators: Validators.required
      }),
      acceptedAnswers: this.fb.control<string>(''),
      caseSensitive: this.fb.control<boolean>(true),
      maxReplays: this.fb.control<number>(1, {
        validators: [Validators.required, Validators.min(1), Validators.max(3)]
      }),
      audioUrl: this.fb.control<string>('', {
        validators: [Validators.required, Validators.maxLength(500)]
      }),
    }) as FormGroup<ListeningFormControls>;
  }

  addExerciseToForm(exercises: FormArray<ExerciseForm>, type: ExerciseType = ExerciseType.FillInBlank): void {
    const newExercise = this.createExerciseForm(type);
    exercises.push(newExercise);
  }

  removeExerciseFromForm(exercises: FormArray<ExerciseForm>, index: number): void {
    exercises.removeAt(index);
  }

  addOptionToMultipleChoice(form: FormGroup<MultipleChoiceFormControls>): void {
    const options = form.controls.options;
    const questionOptionForm = this.fb.group({
      optionText: this.fb.control<string>('', { validators: Validators.required }),
      isCorrect: this.fb.control<boolean>(false, { validators: Validators.required })
    }) as QuestionOptionForm;

    options.push(questionOptionForm);
  }

  removeOptionFromMultipleChoice(form: FormGroup<MultipleChoiceFormControls>, index: number): void {
    const options = form.controls.options;
    if (options.length > 2) { // Keep at least 2 options
      options.removeAt(index);
    }
  }
}
