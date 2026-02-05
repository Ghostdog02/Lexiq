import { CommonModule } from '@angular/common';
import { Component, DestroyRef, inject, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { FormArray, ReactiveFormsModule, FormGroup, AbstractControl } from '@angular/forms';
import { CreateLessonDto, Lesson, LessonForm } from '../../models/lesson.interface';
import {
  DifficultyLevel,
  ExerciseForm,
  ExerciseType,
  FillInBlankExercise,
  TranslationExercise,
  ListeningExercise,
  MultipleChoiceExercise,
  AnyExercise
} from '../../models/exercise.interface';
import { LessonService } from '../../services/lesson.service';
import { LessonFormService } from '../../services/lesson-form.service';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { debounceTime } from 'rxjs/operators';
import { distinctUntilChanged } from 'rxjs/operators';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { EditorComponent } from '../../../../shared/components/editor/editor.component';
import { ContentParserService } from '../../../../shared/services/content-parser.service';
import { Course } from '../../models/course.interface';

@Component({
  selector: 'app-lesson-editor',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, EditorComponent],
  templateUrl: './lesson-editor.component.html',
  styleUrl: './lesson-editor.component.scss'
})
export class LessonEditorComponent implements OnInit {
  readonly formService = inject(LessonFormService);
  private readonly lessonService = inject(LessonService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly sanitizer = inject(DomSanitizer);
  private readonly contentParser = inject(ContentParserService);
  private readonly router = inject(Router);
  lessonForm!: LessonForm;
  exerciseTypeDictionary: { label: string; value: ExerciseType }[];
  // courses: { id: string; title: string }[] = [];
  courses: Course[] = [];
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
    this.loadCourses();
  }

  private async loadCourses(): Promise<void> {
    this.courses = await this.lessonService.getCourses();
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

  getErrorMessage(control: AbstractControl | null): string {
    if (!control || !control.errors || !control.touched) {
      return '';
    }

    const errors = control.errors;

    if (errors['required']) {
      return 'This field is required';
    }
    if (errors['minlength']) {
      return `Minimum length is ${errors['minlength'].requiredLength} characters`;
    }
    if (errors['maxlength']) {
      return `Maximum length is ${errors['maxlength'].requiredLength} characters`;
    }
    if (errors['min']) {
      return `Minimum value is ${errors['min'].min}`;
    }
    if (errors['max']) {
      return `Maximum value is ${errors['max'].max}`;
    }
    if (errors['pattern']) {
      // Check if this is a language code pattern error
      const pattern = errors['pattern'].requiredPattern;
      if (pattern === '[a-z]{2}') {
        return 'Language code must be 2 lowercase letters (e.g. en, es, fr)';
      }
      return 'Invalid format';
    }

    return 'Invalid value';
  }

  hasError(control: AbstractControl | null): boolean {
    return !!(control && control.invalid && control.touched);
  }

  onDifficultyChange(event: Event, exerciseForm: any): void {
    const rangeValue = +(event.target as HTMLInputElement).value;

    const difficultyMap: { [key: string]: DifficultyLevel } = {
      '1': DifficultyLevel.Beginner,
      '2': DifficultyLevel.Intermediate,
      '3': DifficultyLevel.Advanced
    };

    if (exerciseForm && exerciseForm.get) {
      exerciseForm.get('difficultyLevel')?.setValue(difficultyMap[rangeValue]);
    }
  }

  onImageUploaded(url: string): void {
    this.lessonForm.patchValue({ mediaUrl: url });
  }

  parseContent(content: string): SafeHtml {
    return this.sanitizer.bypassSecurityTrustHtml(this.contentParser.parse(content));
  }

  replaceFillInBlank(question: string, answer: string): string {
    if (!question) return question;
    if (!answer) {
      return question.replace(/{%special%}/g, '');
    }
    const underscores = '_'.repeat(answer.length);
    return question.replace(/{%special%}/g, underscores);
  }

  addExercise(type: ExerciseType | ''): void {
    if (!type) return;
    const form: ExerciseForm = this.formService.createExerciseForm(type);
    this.lessonForm.controls.exercises.push(form);
    this.lessonForm.controls.exerciseType.reset();
  }

  removeExercise(index: number): void {
    if (this.confirmRemoval('exercise')) {
      this.formService.removeExerciseFromForm(this.exercises, index);
    }
  }

  async onSubmit(): Promise<void> {
    if (this.lessonForm.invalid) {
      this.markFormGroupTouched();
      return;
    }

    const lessonData = this.buildLessonPayload();

    try {
      const created = await this.lessonService.createLesson(lessonData);
      console.log('✅ Lesson created:', created);
      this.resetForm();
      this.router.navigate(['/']);
    } catch (error) {
      console.error('❌ Error creating lesson:', error);
      // TODO: Show user-friendly error notification
    }
  }

  onDiscard(): void {
    const hasContent = this.lessonForm.dirty;

    if (hasContent) {
      const confirmed = confirm('Are you sure you want to discard all changes? This cannot be undone.');
      if (!confirmed) {
        return;
      }
    }

    this.router.navigate(['/']);
  }

  private buildLessonPayload(): CreateLessonDto {
    const formValue = this.lessonForm.getRawValue();

    const exercises: AnyExercise[] = this.exercises.controls.map(exerciseForm => {
      const exerciseValue = exerciseForm.getRawValue();
      const baseExercise = {
        title: exerciseValue.title,
        instructions: exerciseValue.instructions,
        estimatedDurationMinutes: exerciseValue.estimatedDurationMinutes,
        difficultyLevel: exerciseValue.difficultyLevel,
        points: exerciseValue.points,
        explanation: exerciseValue.explanation,
        exerciseType: exerciseValue.exerciseType,
        question: exerciseValue.question
      };

      switch (exerciseValue.exerciseType) {
        case ExerciseType.FillInBlank:
          return {
            ...baseExercise,
            exerciseType: ExerciseType.FillInBlank,
            correctAnswer: exerciseValue.correctAnswer,
            acceptedAnswers: exerciseValue.acceptedAnswers,
            caseSensitive: exerciseValue.caseSensitive,
            trimWhitespace: exerciseValue.trimWhitespace
          } as FillInBlankExercise;

        case ExerciseType.Translation:
          return {
            ...baseExercise,
            exerciseType: ExerciseType.Translation,
            sourceText: exerciseValue.sourceText,
            targetText: exerciseValue.targetText,
            sourceLanguageCode: exerciseValue.sourceLanguageCode,
            targetLanguageCode: exerciseValue.targetLanguageCode,
            matchingThreshold: exerciseValue.matchingThreshold
          } as TranslationExercise;

        case ExerciseType.Listening:
          return {
            ...baseExercise,
            exerciseType: ExerciseType.Listening,
            correctAnswer: exerciseValue.correctAnswer,
            acceptedAnswers: exerciseValue.acceptedAnswers,
            caseSensitive: exerciseValue.caseSensitive,
            maxReplays: exerciseValue.maxReplays,
            audioUrl: exerciseValue.audioUrl
          } as ListeningExercise;

        case ExerciseType.MultipleChoice:
          return {
            ...baseExercise,
            exerciseType: ExerciseType.MultipleChoice,
            options: exerciseValue.options || []
          } as MultipleChoiceExercise;

        default:
          throw new Error(`Unknown exercise type: ${exerciseValue.exerciseType}`);
      }
    });

    return {
      title: formValue.title,
      description: formValue.description,
      estimatedDuration: formValue.estimatedDuration,
      mediaUrl: formValue.mediaUrl,
      content: formValue.content,
      courseId: formValue.courseId,
      exercises
    };
  }

  private initializeForm(): void {
    this.lessonForm = this.formService.createLessonForm();
  }

  private setupFormValueChanges(): void {
    this.lessonForm.controls.content.valueChanges
      .pipe(
        debounceTime(500),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe((contentJson) => {
        console.log('📝 Editor content changed (length):', contentJson?.length || 0);
        try {
          if (contentJson) {
            const parsed = JSON.parse(contentJson);
            console.log('📦 Editor.js blocks:', parsed.blocks?.length || 0);
          }
        } catch (e) {
          // Not valid JSON yet
        }
      });

    this.lessonForm.valueChanges
      .pipe(
        debounceTime(1000),
        distinctUntilChanged(),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe((value) => {
        console.log('Form changed - Title:', value.title, '| Content length:', value.content?.length || 0);
      });
  }

  private markFormGroupTouched(): void {
    const markTouched = (control: AbstractControl): void => {
      control.markAsTouched();

      if (control instanceof FormGroup) {
        Object.keys(control.controls).forEach(key => {
          markTouched(control.get(key)!);
        });
      } else if (control instanceof FormArray) {
        control.controls.forEach(c => markTouched(c));
      }
    };

    markTouched(this.lessonForm);
  }

  private confirmRemoval(type: string): boolean {
    return confirm(`Are you sure you want to remove this ${type}?`);
  }

  private resetForm(): void {
    this.lessonForm.reset();
    this.exercises.clear();
  }
}
