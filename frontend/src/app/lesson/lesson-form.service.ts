import { Injectable, inject } from '@angular/core';
import { FormArray, NonNullableFormBuilder, Validators } from '@angular/forms';
import { LessonForm } from './lesson.interface';
import { ExerciseForm, DifficultyLevel, ExerciseType } from './exercise.interface';
import { QuestionForm, QuestionType } from './question.interface';
import { MIN_DURATION, MIN_POINTS } from './lesson-form.constants';

@Injectable({
  providedIn: 'root',
})
export class LessonFormService {
  private readonly fb = inject(NonNullableFormBuilder);

  createLessonForm(): LessonForm {
    return this.fb.group({
      title: ['', Validators.required],
      description: [''],
      estimatedDuration: [0, [Validators.required, Validators.min(MIN_DURATION)]],
      content: [''],
      courseId: ['', Validators.required],
      exercises: this.fb.array<ExerciseForm>([])
    });
  }

  createExerciseForm(): ExerciseForm {
    return this.fb.group({
      title: ['', Validators.required],
      instructions: [''],
      estimatedDurationMinutes: [0, [Validators.required, Validators.min(MIN_DURATION)]],
      difficultyLevel: [DifficultyLevel.Beginner, Validators.required],
      points: [0, [Validators.required, Validators.min(MIN_POINTS)]],
      explanation: ['', Validators.maxLength(1000)],
      exerciseType: [ExerciseType.MultipleChoice, Validators.required]
    });
  }

  // createQuestionForm(): QuestionForm {
  //   return this.fb.group({
  //     exerciseName: ['', Validators.required],
  //     questionText: ['', Validators.required],
  //     orderIndex: [0, [Validators.required, Validators.min(0)]],
  //     points: [0, [Validators.required, Validators.min(MIN_POINTS)]],
  //     explanation: [''],
  //     questionType: [QuestionType.FillInBlank, Validators.required]
  //   });
  // }

  addExerciseToForm(exercises: FormArray<ExerciseForm>): void {
    exercises.push(this.createExerciseForm());
  }

  removeExerciseFromForm(exercises: FormArray<ExerciseForm>, index: number): void {
    if (index >= 0 && index < exercises.length) {
      exercises.removeAt(index);
    }
  }

  addQuestionToExercise(questions: FormArray<QuestionForm>): void {
    questions.push(this.createQuestionForm());
  }

  removeQuestionFromExercise(questions: FormArray<QuestionForm>, index: number): void {
    if (index >= 0 && index < questions.length) {
      questions.removeAt(index);
    }
  }
}
