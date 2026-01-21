import { inject, Injectable } from '@angular/core';
import { FormArray, FormBuilder, FormControl, FormGroup, NonNullableFormBuilder, Validators } from '@angular/forms';
import {
  ExerciseType,
  DifficultyLevel,
  MultipleChoiceFormControls,
  FillInBlankFormControls,
  TranslationFormControls,
  ListeningFormControls,
  ExerciseForm
} from './exercise.interface';
import { LessonForm } from './lesson.interface';

@Injectable({
  providedIn: 'root'
})
export class LessonFormService {
  private fb = inject(NonNullableFormBuilder);

  createLessonForm(): LessonForm {
    const group = this.fb.group({
      title: ['', Validators.required],
      description: [''],
      estimatedDuration: [0],
      content: ['', Validators.required],
      courseId: [''],
      exercises: this.fb.array<ExerciseForm>([])
    });

    return group;
  }

  // Factory method to create exercise form based on type
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
      exerciseType: this.fb.control(ExerciseType.FillInBlank, {
        validators: Validators.required
      }),
      question: this.fb.control('', {
        validators: Validators.required
      }),
      difficultyLevel: this.fb.control(DifficultyLevel.Beginner, {
        
      }),
      title: this.fb.control('', {  }),
      instructions: this.fb.control('', {  }),
      estimatedDurationMinutes: this.fb.control(5, {  }),
      points: this.fb.control(10, {  }),
      explanation: this.fb.control<string | undefined>(undefined)
    };
  }

  private createMultipleChoiceForm(): FormGroup<MultipleChoiceFormControls> {
    return this.fb.group({
      ...this.createBaseExerciseControls(),
      exerciseType: this.fb.control(ExerciseType.MultipleChoice, {
        
        validators: Validators.required
      }),
      options: this.fb.array<FormControl<string>>([
        this.fb.control('', {  }),
        this.fb.control('', {  })
      ]),
      correctAnswer: this.fb.control('', {
        
        validators: Validators.required
      })
    });
  }

  private createFillInTheBlankForm(): FormGroup<FillInBlankFormControls> {
    return this.fb.group({
      ...this.createBaseExerciseControls(),
      exerciseType: this.fb.control(ExerciseType.FillInBlank, {
        
        validators: Validators.required
      }),
      answer: this.fb.control('', {
        
        validators: Validators.required
      })
    });
  }

  private createTranslationForm(): FormGroup<TranslationFormControls> {
    return this.fb.group({
      ...this.createBaseExerciseControls(),
      exerciseType: this.fb.control(ExerciseType.Translation, {
        
        validators: Validators.required
      }),
      answer: this.fb.control('', {
        
        validators: Validators.required
      })
    });
  }

  private createListeningForm(): FormGroup<ListeningFormControls> {
    return this.fb.group({
      ...this.createBaseExerciseControls(),
      exerciseType: this.fb.control(ExerciseType.Listening, {
        
        validators: Validators.required
      }),
      audioUrl: this.fb.control('', {
        
        validators: Validators.required
      }),
      answer: this.fb.control('', {
        
        validators: Validators.required
      })
    });
  }

  addExerciseToForm(exercises: FormArray<ExerciseForm>, type: ExerciseType = ExerciseType.FillInBlank): void {
    const newExercise = this.createExerciseForm(type);
    exercises.push(newExercise);
  }

  removeExerciseFromForm(exercises: FormArray<ExerciseForm>, index: number): void {
    exercises.removeAt(index);
  }

  // Helper to add option to multiple choice
  addOptionToMultipleChoice(form: FormGroup<MultipleChoiceFormControls>): void {
    const options = form.controls.options;
    options.push(this.fb.control(''));
  }

  removeOptionFromMultipleChoice(form: FormGroup<MultipleChoiceFormControls>, index: number): void {
    const options = form.controls.options;
    if (options.length > 2) { // Keep at least 2 options
      options.removeAt(index);
    }
  }
}