import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import {
  CreateLessonApiResponse,
  CreateLessonDto,
  Lesson,
  UpdateLessonApiResponse,
  SubmitAnswerResponse,
  CompleteLessonResponse,
  UserExerciseProgress
} from '../models/lesson.interface';
import {
  Exercise,
  MultipleChoiceExercise,
  FillInBlankExercise,
  TranslationExercise,
  ListeningExercise
} from '../models/exercise.interface';
import { Course } from '../models/course.interface';

/** Icon set that cycles based on lesson order */
const LESSON_ICONS = ['📚', '✏️', '🎯', '💡', '🔤', '🗣️', '📝', '🎓'];

/**
 * Service for managing lessons and courses.
 * All data is fetched from the API — no local caching.
 */
@Injectable({
  providedIn: 'root'
})
export class LessonService {
  constructor(private httpClient: HttpClient) {}

  async getCourses(): Promise<Course[]> {
    const result = await firstValueFrom(
      this.httpClient.get<Course[]>('/api/course', { withCredentials: true })
    );

    if (!result) {
      console.error('❌ Failed to fetch courses from backend.');
      return [];
    }

    return result;
  }

  async getLessonsByCourse(courseId: string): Promise<Lesson[]> {
    const result = await firstValueFrom(
      this.httpClient.get<Lesson[]>(`/api/lesson/course/${courseId}`, { withCredentials: true })
    );

    if (!result) {
      console.error(`❌ Failed to fetch lessons for course ${courseId}.`);
      return [];
    }

    return result;
  }

  async getLesson(lessonId: string): Promise<Lesson | null> {
    try {
      const result = await firstValueFrom(
        this.httpClient.get<Lesson>(`/api/lesson/${lessonId}`, { withCredentials: true })
      );
      return result;
    } catch (error) {
      console.error(`❌ Failed to fetch lesson ${lessonId}:`, error);
      return null;
    }
  }

  async createLesson(lesson: CreateLessonDto): Promise<CreateLessonApiResponse> {
    const createLessonDto = {
      courseId: lesson.courseId,
      title: lesson.title,
      description: lesson.description || null,
      estimatedDurationMinutes: lesson.estimatedDuration || null,
      orderIndex: null,
      content: lesson.content,
      exercises: lesson.exercises.map((ex, i) => ({
        lessonId: '',
        title: ex.title,
        instructions: ex.instructions,
        estimatedDurationMinutes: ex.estimatedDurationMinutes,
        difficultyLevel: ex.difficultyLevel,
        points: ex.points,
        orderIndex: i,
        explanation: ex.explanation,
        type: ex.type,
        ...this.mapExerciseTypeFields(ex)
      }))
    };

    const result = await firstValueFrom(
      this.httpClient.post<CreateLessonApiResponse>('/api/lesson', createLessonDto, { withCredentials: true })
    );

    console.log('✅ Lesson created:', result);
    return result;
  }

  private mapExerciseTypeFields(ex: Exercise): Record<string, unknown> {
    switch (ex.type) {
      case 'MultipleChoice': {
        const mc = ex as MultipleChoiceExercise;
        return { options: mc.options };
      }
      case 'FillInBlank': {
        const fib = ex as FillInBlankExercise;
        return {
          text: fib.text,
          correctAnswer: fib.correctAnswer,
          acceptedAnswers: fib.acceptedAnswers,
          caseSensitive: fib.caseSensitive,
          trimWhitespace: fib.trimWhitespace
        };
      }
      case 'Translation': {
        const t = ex as TranslationExercise;
        return {
          sourceText: t.sourceText,
          targetText: t.targetText,
          sourceLanguageCode: t.sourceLanguageCode,
          targetLanguageCode: t.targetLanguageCode,
          matchingThreshold: t.matchingThreshold
        };
      }
      case 'Listening': {
        const l = ex as ListeningExercise;
        return {
          audioUrl: l.audioUrl,
          correctAnswer: l.correctAnswer,
          acceptedAnswers: l.acceptedAnswers,
          caseSensitive: l.caseSensitive,
          maxReplays: l.maxReplays
        };
      }
      default:
        return {};
    }
  }

  async updateLesson(lessonId: string, updates: Lesson): Promise<UpdateLessonApiResponse | null> {
    try {
      const updateDto = {
        title: updates.title,
        description: updates.description,
        estimatedDurationMinutes: updates.estimatedDurationMinutes,
        lessonContent: updates.lessonContent
      };

      const result = await firstValueFrom(
        this.httpClient.put<UpdateLessonApiResponse>(`/api/lesson/${lessonId}`, updateDto, { withCredentials: true })
      );

      console.log('✅ Lesson updated:', result);
      return result;
    } catch (error) {
      console.error(`❌ Failed to update lesson ${lessonId}:`, error);
      return null;
    }
  }

  async deleteLesson(lessonId: string): Promise<boolean> {
    try {
      const response = await firstValueFrom(
        this.httpClient.delete<{isSuccessful: boolean, message: string}>(`/api/lesson/${lessonId}`,
          { withCredentials: true }
        )
      );
      console.log('✅ Lesson deleted:', response);
      return true;
    } catch (error) {
      console.error(`❌ Failed to delete lesson ${lessonId}:`, error);
      return false;
    }
  }

  /**
   * Submit an answer for a specific exercise.
   * @param exerciseId The ID of the exercise
   * @param answer The user's answer (option ID for multiple choice, text for others)
   * @returns Server response with correctness, points, and lesson progress
   */
  async submitExerciseAnswer(exerciseId: string, answer: string): Promise<SubmitAnswerResponse> {
    return await firstValueFrom(
      this.httpClient.post<SubmitAnswerResponse>(
        `/api/exercise/${exerciseId}/submit`,
        { answer },
        { withCredentials: true }
      )
    );
  }

  /**
   * Mark a lesson as completed.
   * @param lessonId The ID of the lesson to complete
   * @returns Completion status, XP earned, and next lesson info
   */
  async completeLesson(lessonId: string): Promise<CompleteLessonResponse> {
    return await firstValueFrom(
      this.httpClient.post<CompleteLessonResponse>(
        `/api/lesson/${lessonId}/complete`,
        {},
        { withCredentials: true }
      )
    );
  }

  /**
   * Get user's saved exercise progress for a lesson.
   * Used to restore progress state after page refresh.
   * @param lessonId The ID of the lesson
   * @returns Array of exercise progress records
   */
  async getLessonProgress(lessonId: string): Promise<UserExerciseProgress[]> {
    try {
      return await firstValueFrom(
        this.httpClient.get<UserExerciseProgress[]>(
          `/api/exercise/lesson/${lessonId}/progress`,
          { withCredentials: true }
        )
      );
    } catch (error) {
      console.error(`❌ Failed to fetch progress for lesson ${lessonId}:`, error);
      return [];
    }
  }

  /**
   * Get all previous submission results for a lesson's exercises, ordered by exercise OrderIndex.
   * Used to restore the submissionResults map after page refresh.
   * @param lessonId The ID of the lesson
   * @returns Array of SubmitAnswerResponse in exercise order
   */
  async getLessonSubmissions(lessonId: string): Promise<SubmitAnswerResponse[]> {
    try {
      return await firstValueFrom(
        this.httpClient.get<SubmitAnswerResponse[]>(
          `/api/exercise/lesson/${lessonId}/submissions`,
          { withCredentials: true }
        )
      );
    } catch (error) {
      console.error(`❌ Failed to fetch submissions for lesson ${lessonId}:`, error);
      return [];
    }
  }
}