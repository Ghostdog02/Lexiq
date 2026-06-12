import { CommonModule } from '@angular/common';
import { Component, DestroyRef, ElementRef, inject, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { Router } from '@angular/router';
import { FormArray, ReactiveFormsModule, FormGroup, AbstractControl } from '@angular/forms';
import { CreateLessonDto, LessonForm } from '../../models/lesson.interface';
import {
  DifficultyLevel,
  ExerciseForm,
  ExerciseType,
} from '../../models/exercise.interface';
import { LessonService } from '../../services/lesson.service';
import { LessonFormService } from '../../services/lesson-form.service';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { debounceTime } from 'rxjs/operators';
import { EditorComponent } from '../../../../shared/components/editor/editor.component';
import { AudioPlayerComponent } from '../../../../shared/components/audio-player/audio-player.component';
import { Course } from '../../models/course.interface';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { ConfirmDialogService } from '../../../../shared/components/confirm-dialog/confirm-dialog.service';
import { ToastrService } from 'ngx-toastr';

@Component({
  selector: 'app-lesson-editor',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, EditorComponent, AudioPlayerComponent],
  templateUrl: './lesson-editor.component.html',
  styleUrl: './lesson-editor.component.scss'
})
export class LessonEditorComponent implements OnInit, OnDestroy {
  @ViewChild(EditorComponent) private editorComponent!: EditorComponent;
  @ViewChild('globalAudioInput') private globalAudioInput!: ElementRef<HTMLInputElement>;

  readonly formService = inject(LessonFormService);
  private readonly lessonService = inject(LessonService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly router = inject(Router);
  private readonly httpClient = inject(HttpClient);
  private readonly toastr = inject(ToastrService);
  private readonly confirmDialog = inject(ConfirmDialogService);

  lessonForm!: LessonForm;
  exerciseTypeDictionary: { label: string; value: ExerciseType }[] = [];
  courses: Course[] = [];
  ExerciseType = ExerciseType;

  private pendingAudioIndex = -1;
  private pendingAudioFiles = new Map<string, File>();

  ngOnInit(): void {
    this.exerciseTypeDictionary = Object.entries(ExerciseType).map(
      ([key, value]) => ({ label: key, value: value as ExerciseType })
    );
    this.initializeForm();
    this.setupFormValueChanges();
    this.loadCourses();
  }

  ngOnDestroy(): void {
    this.pendingAudioFiles.forEach((_, blobUrl) => URL.revokeObjectURL(blobUrl));
    this.pendingAudioFiles.clear();
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
    if (!control || !control.errors || !control.touched) return '';
    const errors = control.errors;
    if (errors['required']) return 'This field is required';
    if (errors['minlength']) return `Minimum length is ${errors['minlength'].requiredLength} characters`;
    if (errors['maxlength']) return `Maximum length is ${errors['maxlength'].requiredLength} characters`;
    if (errors['min']) return `Minimum value is ${errors['min'].min}`;
    if (errors['max']) return `Maximum value is ${errors['max'].max}`;
    if (errors['pattern']) {
      const pattern = errors['pattern'].requiredPattern;
      if (pattern === '[a-z]{2}') return 'Language code must be 2 lowercase letters (e.g. en, es, fr)';
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

  addExercise(type: ExerciseType | ''): void {
    if (!type) return;
    const form: ExerciseForm = this.formService.createExerciseForm(type);
    this.lessonForm.controls.exercises.push(form);
    this.lessonForm.controls.exerciseType.reset();
  }

  removeExercise(index: number): void {
    if (this.confirmRemoval('exercise')) {
      const ctrl = this.exercises.at(index).get('audioUrl');
      const url = ctrl?.value as string;
      if (url?.startsWith('blob:')) {
        URL.revokeObjectURL(url);
        this.pendingAudioFiles.delete(url);
      }
      this.formService.removeExerciseFromForm(this.exercises, index);
    }
  }

  // ── Audio upload ──────────────────────────────────────────────────────────

  triggerAudioUpload(index: number): void {
    this.pendingAudioIndex = index;
    this.globalAudioInput.nativeElement.value = '';
    this.globalAudioInput.nativeElement.click();
  }

  onAudioFileSelected(event: Event): void {
    const file = (event.target as HTMLInputElement).files?.[0];
    if (!file || this.pendingAudioIndex < 0) return;
    if (file.size > 10 * 1024 * 1024) {
      this.toastr.error('Audio file must be under 10 MB', 'Upload error', { toastClass: 'ngx-toastr toast-auth' });
      return;
    }
    const blobUrl = URL.createObjectURL(file);
    this.pendingAudioFiles.set(blobUrl, file);
    this.exercises.at(this.pendingAudioIndex).get('audioUrl')?.setValue(blobUrl);
    this.pendingAudioIndex = -1;
  }

  removeAudio(index: number): void {
    const ctrl = this.exercises.at(index).get('audioUrl');
    const url = ctrl?.value as string;
    if (url?.startsWith('blob:')) {
      URL.revokeObjectURL(url);
      this.pendingAudioFiles.delete(url);
    }
    ctrl?.setValue('');
  }

  isBlobUrl(url: string | null | undefined): boolean {
    return !!url?.startsWith('blob:');
  }

  getAudioFileName(blobUrl: string): string {
    return this.pendingAudioFiles.get(blobUrl)?.name ?? 'audio file';
  }

  getAudioFileSize(blobUrl: string): string {
    const bytes = this.pendingAudioFiles.get(blobUrl)?.size ?? 0;
    return bytes > 1024 * 1024
      ? `${(bytes / 1024 / 1024).toFixed(1)} MB`
      : `${Math.round(bytes / 1024)} KB`;
  }

  // ── Form submission ───────────────────────────────────────────────────────

  async onSubmit(): Promise<void> {
    if (this.lessonForm.invalid) {
      this.markFormGroupTouched();
      return;
    }
    try {
      const rawContent = this.lessonForm.controls.content.value ?? '';
      const finalContent = await this.editorComponent.uploadPendingFiles(rawContent);
      this.lessonForm.controls.content.setValue(finalContent, { emitEvent: false });

      await this.uploadPendingAudioFiles();

      const lessonData = this.buildLessonPayload();
      await this.lessonService.createLesson(lessonData);
      this.toastr.success('Lesson created successfully', 'Success', { toastClass: 'ngx-toastr toast-auth' });
      this.resetForm();
      this.router.navigate(['/']);
    } catch {
      // HTTP error toasted by interceptor
    }
  }

  onDiscard(): void {
    if (!this.lessonForm.dirty) {
      this.router.navigate(['/']);
      return;
    }
    this.confirmDialog
      .confirm({ message: 'Do you want to leave lesson creation? All unsaved changes will be lost.', confirmLabel: 'Yes, leave' })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(confirmed => { if (confirmed) this.router.navigate(['/']); });
  }

  // ── Private ───────────────────────────────────────────────────────────────

  private async uploadPendingAudioFiles(): Promise<void> {
    for (let i = 0; i < this.exercises.length; i++) {
      const ctrl = this.exercises.at(i).get('audioUrl');
      const url = ctrl?.value as string;
      if (!url?.startsWith('blob:')) continue;
      const file = this.pendingAudioFiles.get(url);
      if (!file) continue;
      const fd = new FormData();
      fd.append('file', file);
      const res = await firstValueFrom(
        this.httpClient.post<{ success: number; file: { url: string } }>(
          '/api/uploads/audio', fd, { withCredentials: true }
        )
      );
      URL.revokeObjectURL(url);
      this.pendingAudioFiles.delete(url);
      ctrl!.setValue(res.file.url);
    }
  }

  private buildLessonPayload(): CreateLessonDto {
    const formValue = this.lessonForm.getRawValue();
    return {
      title: formValue.title,
      description: formValue.description,
      estimatedDurationMinutes: formValue.estimatedDuration,
      content: formValue.content,
      courseId: formValue.courseId,
      exercises: this.exercises.controls.map(ex => ex.getRawValue())
    };
  }

  private initializeForm(): void {
    this.lessonForm = this.formService.createLessonForm();
  }

  private setupFormValueChanges(): void {
    this.lessonForm.controls.content.valueChanges
      .pipe(debounceTime(500), takeUntilDestroyed(this.destroyRef))
      .subscribe((contentJson) => {
        console.log('📝 Editor content changed (length):', contentJson?.length || 0);
      });
  }

  private markFormGroupTouched(): void {
    const markTouched = (control: AbstractControl): void => {
      control.markAsTouched();
      if (control instanceof FormGroup) {
        Object.keys(control.controls).forEach(key => markTouched(control.get(key)!));
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
