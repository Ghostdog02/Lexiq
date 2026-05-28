import {
  Component,
  OnInit,
  OnDestroy,
  inject,
  DestroyRef,
} from '@angular/core';
import { trigger, transition, animate, keyframes, style } from '@angular/animations';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { CommonModule } from '@angular/common';
import { Router, ActivatedRoute } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom, fromEvent, filter } from 'rxjs';
import {
  AnyExercise,
  Exercise,
  ExerciseType,
  FillInBlankExercise,
  ListeningExercise,
  TrueFalseExercise,
} from '../../models/exercise.interface';
import { LessonSubmitResult } from '../../models/lesson.interface';
import { LessonService } from '../../services/lesson.service';
import { ExerciseViewerStateService } from '../../services/exercise-viewer-state.service';
import { AuthService } from '../../../../auth/auth.service';
import { MultipleChoiceExerciseComponent } from './multiple-choice-exercise/multiple-choice-exercise.component';
import { FillInBlankExerciseComponent } from './fill-in-blank-exercise/fill-in-blank-exercise.component';
import { ListeningExerciseComponent } from './listening-exercise/listening-exercise.component';
import { TrueFalseExerciseComponent } from './true-false-exercise/true-false-exercise.component';

/**
 * Container component for the exercise-solving flow.
 *
 * Answers are validated locally — no per-exercise backend call.
 * The lesson is submitted once at the end via LessonService.submitLesson().
 * ExerciseViewerStateService is the single source of truth for all exercise state.
 */
@Component({
  selector: 'app-exercise-viewer',
  standalone: true,
  imports: [
    CommonModule,
    MultipleChoiceExerciseComponent,
    FillInBlankExerciseComponent,
    ListeningExerciseComponent,
    TrueFalseExerciseComponent,
  ],
  providers: [ExerciseViewerStateService],
  templateUrl: './exercise-viewer.component.html',
  styleUrl: './exercise-viewer.component.scss',
  animations: [
    trigger('exerciseSwitch', [
      transition(':increment', [
        animate('600ms cubic-bezier(0.4, 0, 0.2, 1)', keyframes([
          style({ opacity: 1, transform: 'translateX(0)',     offset: 0    }),
          style({ opacity: 0, transform: 'translateX(-24px)', offset: 0.25 }),
          style({ opacity: 0, transform: 'translateX(24px)',  offset: 0.75 }),
          style({ opacity: 1, transform: 'translateX(0)',     offset: 1    }),
        ]))
      ]),
      transition(':decrement', [
        animate('600ms cubic-bezier(0.4, 0, 0.2, 1)', keyframes([
          style({ opacity: 1, transform: 'translateX(0)',     offset: 0    }),
          style({ opacity: 0, transform: 'translateX(24px)',  offset: 0.25 }),
          style({ opacity: 0, transform: 'translateX(-24px)', offset: 0.75 }),
          style({ opacity: 1, transform: 'translateX(0)',     offset: 1    }),
        ]))
      ]),
    ])
  ],
})
export class ExerciseViewerComponent implements OnInit, OnDestroy {
  private router = inject(Router);
  private route = inject(ActivatedRoute);
  private lessonService = inject(LessonService);
  private authService = inject(AuthService);
  private destroyRef = inject(DestroyRef);
  private http = inject(HttpClient);

  readonly state = inject(ExerciseViewerStateService);

  exercises: AnyExercise[] = [];
  lessonId = '';
  isLoading = true;
  isAdmin = false;
  isSubmitting = false;
  lessonSubmitResult: LessonSubmitResult | null = null;

  // Keyboard navigation state
  focusedOptionIndex = 0;

  // Expose enum to template
  readonly ExerciseType = ExerciseType;

  async ngOnInit(): Promise<void> {
    this.authService
      .getAdminStatusListener()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((isAdmin) => (this.isAdmin = isAdmin));

    this.setupKeyboardNavigation();

    const hearts = await this.fetchHearts();

    const lessonId = this.route.snapshot.paramMap.get('id');
    if (!lessonId) {
      console.error('No lesson ID in route params');
      await this.router.navigate(['/']);
      return;
    }

    this.lessonId = lessonId;

    this.isLoading = true;
    try {
      this.exercises = await this.lessonService.getExercisesByLesson(lessonId);
    } catch (err) {
      console.error('Error loading exercises:', err);
      await this.router.navigate(['/']);
      return;
    } finally {
      this.isLoading = false;
    }

    this.state.initialize(this.exercises, this.isAdmin, hearts);
  }

  ngOnDestroy(): void {
    this.state.reset();
  }

  private async fetchHearts(): Promise<number> {
    try {
      const response = await firstValueFrom(
        this.http.get<{ hearts: number }>('/api/user/hearts', {
          withCredentials: true,
        })
      );
      return response.hearts;
    } catch (err) {
      console.error('Failed to fetch hearts:', err);
      return 0;
    }
  }

  private setupKeyboardNavigation(): void {
    fromEvent<KeyboardEvent>(document, 'keydown')
      .pipe(
        filter(
          () => this.currentExercise?.type === ExerciseType.MultipleChoice
        ),
        filter(() => !this.isCurrentSubmitted),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe((event) => {
        const options = this.currentExercise?.options ?? [];
        if (options.length === 0) return;

        switch (event.key) {
          case 'ArrowDown':
            event.preventDefault();
            this.focusedOptionIndex =
              (this.focusedOptionIndex + 1) % options.length;
            this.state.selectOption(
              this.state.currentExerciseId!,
              options[this.focusedOptionIndex].id
            );
            break;

          case 'ArrowUp':
            event.preventDefault();
            this.focusedOptionIndex =
              (this.focusedOptionIndex - 1 + options.length) % options.length;
            this.state.selectOption(
              this.state.currentExerciseId!,
              options[this.focusedOptionIndex].id
            );
            break;

          case 'Enter':
            event.preventDefault();
            if (
              this.isCurrentSubmitted &&
              this.state.canGoNext &&
              this.state.hearts > 0
            ) {
              this.nextExercise();
            }
            break;
        }
      });
  }

  // ── Getters ───────────────────────────────────────────────────────────────

  get currentExercise(): AnyExercise | null {
    return this.state.currentViewModel?.exercise ?? null;
  }

  get isCurrentSubmitted(): boolean {
    return this.state.currentViewModel?.isSubmitted ?? false;
  }

  get isCurrentCorrect(): boolean {
    return this.state.currentViewModel?.isCorrect ?? false;
  }

  get currentMultipleChoice(): Exercise | null {
    const ex = this.currentExercise;
    return ex?.type === ExerciseType.MultipleChoice ? (ex as Exercise) : null;
  }

  get currentFillInBlank(): FillInBlankExercise | null {
    const ex = this.currentExercise;
    return ex?.type === ExerciseType.FillInBlank
      ? (ex as FillInBlankExercise)
      : null;
  }

  get currentListening(): ListeningExercise | null {
    const ex = this.currentExercise;
    return ex?.type === ExerciseType.Listening
      ? (ex as ListeningExercise)
      : null;
  }

  get currentTrueFalse(): TrueFalseExercise | null {
    const ex = this.currentExercise;
    return ex?.type === ExerciseType.TrueFalse
      ? (ex as TrueFalseExercise)
      : null;
  }

  get isLastExercise(): boolean {
    return !this.state.canGoNext;
  }

  get allSubmitted(): boolean {
    return this.state.viewModels.every((vm) => vm.isSubmitted);
  }

  get currentExerciseIndex(): number {
    return this.state.viewModels.findIndex(
      (vm) => vm.exercise.id === this.state.currentExerciseId
    );
  }

  // Converts exercise type enum to readable label
  getExerciseTypeLabel(type: ExerciseType): string {
    return type.replace(/([A-Z])/g, ' $1').trim();
  }

  // ── Navigation ────────────────────────────────────────────────────────────

  nextExercise(): void {
    this.state.goToNext();
    this.focusedOptionIndex = 0;
  }

  previousExercise(): void {
    this.state.goToPrevious();
    this.focusedOptionIndex = 0;
  }

  goToExercise(exerciseId: string): void {
    this.state.setCurrentExercise(exerciseId);
  }

  isExerciseAccessible(exerciseId: string): boolean {
    return this.state.getViewModelById(exerciseId)?.isAccessible ?? false;
  }

  // ── Actions ───────────────────────────────────────────────────────────────

  checkAnswer(): void {
    if (!this.currentExercise?.id || !this.state.currentHasSelection) return;
    this.state.submitAnswer(this.currentExercise.id);
  }

  onBackToContent(): void {
    this.router.navigate(['/lesson', this.lessonId]);
  }

  async finishLesson(): Promise<void> {
    if (this.isSubmitting) return;
    this.isSubmitting = true;

    try {
      const payload = this.state.buildSubmitPayload();
      this.lessonSubmitResult = await this.lessonService.submitLesson(
        this.lessonId,
        payload
      );
    } catch (err) {
      console.error('Failed to submit lesson:', err);
    } finally {
      this.isSubmitting = false;
    }
  }

  dismissResults(): void {
    this.router.navigate(['/']);
  }
}
