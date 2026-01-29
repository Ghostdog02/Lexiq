import { CommonModule } from '@angular/common';
import { Component, DestroyRef, inject, AfterViewInit, OnInit, OnDestroy } from '@angular/core';
import { Router } from '@angular/router';
import { FormArray, ReactiveFormsModule, FormGroup, AbstractControl } from '@angular/forms';
import { Lesson, LessonForm } from './lesson.interface';
import {
  DifficultyLevel,
  ExerciseForm,
  ExerciseType,
  FillInBlankExercise,
  TranslationExercise,
  ListeningExercise,
  MultipleChoiceExercise,
  AnyExercise
} from './exercise.interface';
import { LessonService } from './lesson.service';
import { LessonFormService } from './lesson-form.service';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { debounceTime } from 'rxjs/internal/operators/debounceTime';
import { distinctUntilChanged } from 'rxjs/internal/operators/distinctUntilChanged';
import { marked } from 'marked';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { EditorComponent } from '../editor/editor.component';

@Component({
  selector: 'app-lesson',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, EditorComponent],
  templateUrl: './lesson.component.html',
  styleUrl: './lesson.component.scss'
})
export class LessonComponent implements OnInit {
  readonly formService = inject(LessonFormService);
  private readonly lessonService = inject(LessonService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly sanitizer = inject(DomSanitizer);
  private readonly router = inject(Router);
  lessonForm!: LessonForm;
  exerciseTypeDictionary: { label: string; value: ExerciseType }[];
  courses: { id: string; title: string }[] = [];
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

  private loadCourses(): void {
    this.lessonService.getCourses()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (courses) => {
          this.courses = courses.map(c => ({
            id: c.id,
            title: c.title
          }));
          console.log('📚 Loaded courses:', this.courses);
        },
        error: (error) => {
          console.error('Error loading courses:', error);
          // Set empty array on error so form can still be used
          this.courses = [];
        }
      });
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
    if (!content) return this.sanitizer.bypassSecurityTrustHtml('');

    try {
      const data = JSON.parse(content);
      if (data && data.blocks && Array.isArray(data.blocks)) {
        let html = '';
        data.blocks.forEach((block: any) => {
          switch (block.type) {
            case 'header':
              html += `<h${block.data.level}>${block.data.text}</h${block.data.level}>`;
              break;
            case 'paragraph':
              html += `<p>${block.data.text}</p>`;
              break;
            case 'List':
            case 'list':
              const tag = block.data.style === 'ordered' ? 'ol' : 'ul';
              html += `<${tag}>`;
              block.data.items.forEach((item: string) => {
                html += `<li>${item}</li>`;
              });
              html += `</${tag}>`;
              break;
            case 'delimiter':
              html += '<hr />';
              break;
            case 'table':
              html += '<table>';
              if (block.data.withHeadings && block.data.content.length > 0) {
                html += '<thead><tr>';
                block.data.content[0].forEach((cell: string) => {
                  html += `<th>${cell}</th>`;
                });
                html += '</tr></thead><tbody>';
                block.data.content.slice(1).forEach((row: string[]) => {
                  html += '<tr>';
                  row.forEach((cell: string) => {
                    html += `<td>${cell}</td>`;
                  });
                  html += '</tr>';
                });
                html += '</tbody>';
              } else {
                html += '<tbody>';
                block.data.content.forEach((row: string[]) => {
                  html += '<tr>';
                  row.forEach((cell: string) => {
                    html += `<td>${cell}</td>`;
                  });
                  html += '</tr>';
                });
                html += '</tbody>';
              }
              html += '</table>';
              break;
            default:
              break;
          }
        });
        return this.sanitizer.bypassSecurityTrustHtml(html);
      }
    } catch (e) {
      // Not JSON, fallback to markdown
    }

    const html = marked(content, { async: false, breaks: true });
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

  // Form submission
  onSubmit(): void {
    if (this.lessonForm.invalid) {
      this.markFormGroupTouched();
      return;
    }

    const lessonData = this.buildLessonPayload();
    console.log('Lesson submitted:', lessonData);

    this.lessonService.createLesson(lessonData)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (response) => {
          console.log('Lesson created successfully:', response);
          this.resetForm();
          this.router.navigate(['/']);
        },
        error: (error) => {
          console.error('Error creating lesson:', error);
        }
      });
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

  private buildLessonPayload(): Lesson {
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

  // Private methods
  private initializeForm(): void {
    this.lessonForm = this.formService.createLessonForm();
  }

  private setupFormValueChanges(): void {
    // Monitor content changes specifically
    this.lessonForm.controls.content.valueChanges
      .pipe(
        debounceTime(500),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe((contentJson) => {
        console.log('📝 Editor content changed (length):', contentJson?.length || 0);
        // You can parse and preview the content here if needed
        try {
          if (contentJson) {
            const parsed = JSON.parse(contentJson);
            console.log('📦 Editor.js blocks:', parsed.blocks?.length || 0);
          }
        } catch (e) {
          // Not valid JSON yet
        }
      });

    // Monitor all form changes for auto-save
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
    // Helper function to recursively mark all controls as touched
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

    // Mark all controls in the form as touched
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
