import { CommonModule } from '@angular/common';
import { Component, DestroyRef, inject } from '@angular/core';
import { FormArray, ReactiveFormsModule, FormGroup } from '@angular/forms';
import { LessonForm } from './lesson.interface';
import { DifficultyLevel, Exercise, ExerciseForm, ExerciseType, MultipleChoiceExercise } from './exercise.interface';
import { LessonService } from './lesson.service';
import { LessonFormService } from './lesson-form.service';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { debounceTime } from 'rxjs/internal/operators/debounceTime';
import { distinctUntilChanged } from 'rxjs/internal/operators/distinctUntilChanged';
import { marked } from 'marked';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';

@Component({
  selector: 'app-lesson',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './lesson.component.html',
  styleUrl: './lesson.component.scss'
})
export class LessonComponent {
  private readonly formService = inject(LessonFormService);
  private readonly lessonService = inject(LessonService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly sanitizer = inject(DomSanitizer);

  lessonForm!: LessonForm;
  exerciseTypeDictionary: { label: string; value: ExerciseType }[];
  ExerciseType = ExerciseType;

  constructor() {
    this.exerciseTypeDictionary = Object.entries(ExerciseType).map(([key, value]) => ({
      label: key,
      value: value
    }));
  }

  ngOnInit(): void {
    this.initializeForm();
    this.setupFormValueChanges();
  }

  get lessonFormControls() {
    return this.lessonForm.controls;
  }

  get exercises(): FormArray<ExerciseForm> {
    return this.lessonForm.controls.exercises;
  }

  get exerciseControls() {
    return this.exercises.controls;
  }

  onDifficultyChange(event: Event, exerciseForm: ExerciseForm): void {
    const rangeValue = +(event.target as HTMLInputElement).value;

    // Map range value (1-3) to DifficultyLevel enum
    const difficultyMap: { [key: string]: DifficultyLevel } = {
      '1': DifficultyLevel.Beginner,
      '2': DifficultyLevel.Intermediate,
      '3': DifficultyLevel.Advanced
    };

    exerciseForm.get('difficultyLevel')?.setValue(difficultyMap[rangeValue]);
  }

  parseMarkdown(markdown: string): SafeHtml {
    if (!markdown) return this.sanitizer.bypassSecurityTrustHtml('');
    const html = marked(markdown, { async: false, breaks: true });
    return this.sanitizer.bypassSecurityTrustHtml(html as string);
  }

  replaceFillInBlank(question: string, answer: string): string {
    if (!question) return question;
    if (!answer) {
      return question.replace(/{%special%}/g, '');
    }
    const underscores = '_'.repeat(answer.length);
    return question.replace(/{%special%}/g, underscores);
  }

  addExercise(type: ExerciseType): void {
    const form: ExerciseForm = this.formService.createExerciseForm(type);
    this.formService.addExerciseToForm(this.lessonForm.controls.exercises);
  }

  removeExercise(index: number): void {
    if (this.confirmRemoval('exercise')) {
      this.formService.removeExerciseFromForm(this.exercises, index);
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
