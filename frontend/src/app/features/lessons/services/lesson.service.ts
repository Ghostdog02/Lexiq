import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import {
  CreateLessonApiResponse,
  CreateLessonDto,
  Lesson,
  LessonSubmitResult,
  UpdateLessonApiResponse,
} from '../models/lesson.interface';
import {
  AnyExercise,
  CreateExerciseDto,
  ExerciseFormValue,
  ExerciseType,
} from '../models/exercise.interface';
import { Course } from '../models/course.interface';

export interface UserXp {
  userId: string;
  totalXp: number;
  completedExercises: number;
  lastActivityAt: string | null;
}

/**
 * Service for managing lessons and courses.
 * All data is fetched from the API — no local caching.
 */
@Injectable({
  providedIn: 'root',
})
export class LessonService {
  private httpClient = inject(HttpClient);

  async getCourses(): Promise<Course[]> {
    const result = await firstValueFrom(
      this.httpClient.get<Course[]>('/api/courses')
    );

    if (!result) {
      console.error('Failed to fetch courses from backend.');
      return [];
    }

    return result;
  }

  async getLessonsByCourse(courseId: string): Promise<Lesson[]> {
    const result = await firstValueFrom(
      this.httpClient.get<Lesson[]>(`/api/lessons/course/${courseId}`)
    );

    if (!result) {
      console.error(`Failed to fetch lessons for course ${courseId}.`);
      return [];
    }

    return result;
  }

  async getLesson(lessonId: string): Promise<Lesson | null> {
    try {
      return await firstValueFrom(
        this.httpClient.get<Lesson>(`/api/lessons/${lessonId}`)
      );
    } catch (error) {
      console.error(`Failed to fetch lesson ${lessonId}:`, error);
      return null;
    }
  }

  /**
   * Fetch all exercises for a lesson.
   * Used by exercise-viewer to avoid fetching the full lesson.
   */
  async getExercisesByLesson(lessonId: string): Promise<AnyExercise[]> {
    try {
      return await firstValueFrom(
        this.httpClient.get<AnyExercise[]>(
          `/api/lessons/${lessonId}/exercises`,
          { withCredentials: true }
        )
      );
    } catch (error) {
      console.error(`Failed to fetch exercises for lesson ${lessonId}:`, error);
      return [];
    }
  }

  /**
   * Submit all exercise answers for a lesson at once.
   * Returns server-validated results and lesson summary.
   */
  async submitLesson(
    lessonId: string,
    answers: { exerciseId: string; selectedOptionId: string | null }[]
  ): Promise<LessonSubmitResult> {
    return await firstValueFrom(
      this.httpClient.post<LessonSubmitResult>(
        `/api/lessons/${lessonId}/submit`,
        { answers },
        { withCredentials: true }
      )
    );
  }

  async createLesson(lesson: CreateLessonDto): Promise<CreateLessonApiResponse> {
    const payload = {
      courseId: lesson.courseId,
      title: lesson.title,
      description: lesson.description ?? null,
      estimatedDurationMinutes: lesson.estimatedDuration ?? null,
      orderIndex: null,
      content: lesson.content,
      exercises: lesson.exercises.map((ex, i) => this.mapFormToCreateDto(ex, i)),
    };

    const result = await firstValueFrom(
      this.httpClient.post<CreateLessonApiResponse>('/api/lessons', payload)
    );

    console.log('Lesson created:', result);
    return result;
  }

  private mapFormToCreateDto(formValue: ExerciseFormValue, _index: number): CreateExerciseDto {
    const options = formValue.options.map((opt) => ({
      optionText: opt.optionText,
      isCorrect: opt.isCorrect,
      explanation: opt.explanation,
    }));

    switch (formValue.exerciseType) {
      case ExerciseType.MultipleChoice:
        return {
          type: 'MultipleChoice',
          instructions: formValue.instructions,
          difficultyLevel: formValue.difficultyLevel,
          points: formValue.points,
          explanation: formValue.explanation,
          options,
        };

      case ExerciseType.FillInBlank:
        return {
          type: 'FillInBlank',
          instructions: formValue.instructions,
          difficultyLevel: formValue.difficultyLevel,
          points: formValue.points,
          explanation: formValue.explanation,
          text: formValue.text,
          options,
        };

      case ExerciseType.Listening:
        return {
          type: 'Listening',
          instructions: formValue.instructions,
          difficultyLevel: formValue.difficultyLevel,
          points: formValue.points,
          explanation: formValue.explanation,
          audioUrl: formValue.audioUrl,
          maxReplays: formValue.maxReplays,
          options,
        };

      case ExerciseType.TrueFalse:
        return {
          type: 'TrueFalse',
          instructions: formValue.instructions,
          difficultyLevel: formValue.difficultyLevel,
          points: formValue.points,
          explanation: formValue.explanation,
          statement: formValue.statement,
          imageUrl: formValue.imageUrl || undefined,
          options,
        };
    }
  }

  async updateLesson(
    lessonId: string,
    updates: Lesson
  ): Promise<UpdateLessonApiResponse | null> {
    try {
      const updateDto = {
        title: updates.title,
        description: updates.description,
        estimatedDurationMinutes: updates.estimatedDurationMinutes,
        lessonContent: updates.lessonContent,
      };

      const result = await firstValueFrom(
        this.httpClient.put<UpdateLessonApiResponse>(
          `/api/lessons/${lessonId}`,
          updateDto
        )
      );

      console.log('Lesson updated:', result);
      return result;
    } catch (error) {
      console.error(`Failed to update lesson ${lessonId}:`, error);
      return null;
    }
  }

  async deleteLesson(lessonId: string): Promise<boolean> {
    try {
      const response = await firstValueFrom(
        this.httpClient.delete<{ isSuccessful: boolean; message: string }>(
          `/api/lessons/${lessonId}`
        )
      );
      console.log('Lesson deleted:', response);
      return true;
    } catch (error) {
      console.error(`Failed to delete lesson ${lessonId}:`, error);
      return false;
    }
  }

  /**
   * Get current authenticated user's total XP and stats.
   */
  async getCurrentUserXp(): Promise<UserXp | null> {
    try {
      return await firstValueFrom(
        this.httpClient.get<UserXp>('/api/user/xp')
      );
    } catch (error) {
      console.error('Failed to fetch user XP:', error);
      return null;
    }
  }
}
