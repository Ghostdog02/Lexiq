import { CommonModule } from '@angular/common';
import { Component, DestroyRef, inject } from '@angular/core';
import { FormArray, ReactiveFormsModule, FormGroup } from '@angular/forms';
import { LessonForm } from './lesson.interface';
import { ExerciseForm } from './exercise.interface';
import { LessonService } from './lesson.service';
import { LessonFormService } from './lesson-form.service';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { debounceTime } from 'rxjs/internal/operators/debounceTime';
import { distinctUntilChanged } from 'rxjs/internal/operators/distinctUntilChanged';

@Component({
  selector: 'app-create-exercise',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './lesson.component.html',
  styleUrl: './lesson.component.scss'
})
export class LessonComponent {
  private readonly formService = inject(LessonFormService);
  private readonly lessonService = inject(LessonService);
  private readonly destroyRef = inject(DestroyRef);

  lessonForm!: LessonForm;

  ngOnInit(): void {
    this.initializeForm();
    this.setupFormValueChanges();
  }

  get formControls() { 
    return this.lessonForm.controls; 
  }
  
  get exercises(): FormArray<ExerciseForm> { 
    return this.lessonForm.controls.exercises;
  }

  addExercise(): void {
    this.formService.addExerciseToForm(this.exercises);
  }

  removeExercise(index: number): void {
    if (this.confirmRemoval('exercise')) {
      this.formService.removeExerciseFromForm(this.exercises, index);
    }
  }

  getExerciseQuestions(exerciseIndex: number): FormArray<QuestionForm> {
    return this.exercises.at(exerciseIndex).controls.questions;
  }

  // Question management
  addQuestion(exerciseIndex: number): void {
    const questions = this.getExerciseQuestions(exerciseIndex);
    this.formService.addQuestionToExercise(questions);
  }

  removeQuestion(exerciseIndex: number, questionIndex: number): void {
    if (this.confirmRemoval('question')) {
      const questions = this.getExerciseQuestions(exerciseIndex);
      this.formService.removeQuestionFromExercise(questions, questionIndex);
    }
  }

  // Form submission
  onSubmit(): void {
    if (this.lessonForm.invalid) {
      this.markFormGroupTouched();
      return;
    }

    const lessonData = this.lessonForm.getRawValue();
    console.log('Lesson submitted:', lessonData);

    this.lessonService.createLesson(lessonData)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (response) => {
          console.log('Lesson created successfully:', response);
          this.resetForm();
        },
        error: (error) => {
          console.error('Error creating lesson:', error);
          // Handle error (show toast, etc.)
        }
      });
  }

  // Private methods
  private initializeForm(): void {
    this.lessonForm = this.formService.createLessonForm();
  }

  private setupFormValueChanges(): void {
    // Example: Auto-save draft
    this.lessonForm.valueChanges
      .pipe(
        debounceTime(1000),
        distinctUntilChanged(),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe((value) => {
        // Auto-save logic here
        console.log('Form changed:', value);
      });
  }

  private markFormGroupTouched(): void {
    Object.keys(this.lessonForm.controls).forEach(key => {
      const control = this.lessonForm.get(key);
      control?.markAsTouched();

      if (control instanceof FormArray) {
        control.controls.forEach(c => {
          if (c instanceof FormGroup) {
            Object.keys(c.controls).forEach(k => c.get(k)?.markAsTouched());
          }
        });
      }
    });
  }

  private confirmRemoval(type: string): boolean {
    return confirm(`Are you sure you want to remove this ${type}?`);
  }

  private resetForm(): void {
    this.lessonForm.reset();
    this.exercises.clear();
  }
}
