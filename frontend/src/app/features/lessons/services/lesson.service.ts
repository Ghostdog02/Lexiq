import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import {
  CreateLessonApiResponse,
  CreateLessonDto,
  Lesson,
  UpdateLessonApiResponse,
  SubmitAnswerResponse,
  CompleteLessonResponse
} from '../models/lesson.interface';
import { Course } from '../models/course.interface';
import { AnyExercise, ExerciseType, DifficultyLevel } from '../models/exercise.interface';

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

  /**
   * Converts API response to UI model.
   * Centralizes the transformation so all components use the same logic.
   */
  // mapApiToLesson(api: Lesson): Lesson {
  //   return {
  //     id: api.lessonId,
  //     title: api.title,
  //     description: api.description ?? '',
  //     estimatedDuration: api.estimatedDurationMinutes ?? 0,
  //     content: api.lessonContent,
  //     courseId: api.courseId,
  //     exerciseCount: api.exerciseCount,
  //     exercises: [],
  //     status: api.isLocked ? 'locked' : 'available',
  //   };
  // }

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
      orderIndex: 0, // TODO: Make this dynamic based on existing lessons in course
      content: lesson.content
    };

    const result = await firstValueFrom(
      this.httpClient.post<CreateLessonApiResponse>('/api/lesson', createLessonDto, { withCredentials: true })
    );

    console.log('✅ Lesson created:', result);
    return result;
  }

  async updateLesson(lessonId: string, updates: Lesson): Promise<UpdateLessonApiResponse | null> {
    try {
      const updateDto = {
        title: updates.title,
        description: updates.description,
        estimatedDurationMinutes: updates.estimatedDurationMinutes,
        lessonContent: updates.content
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

  async submitExerciseAnswer(exerciseId: string, answer: string): Promise<SubmitAnswerResponse> {
    return await firstValueFrom(
      this.httpClient.post<SubmitAnswerResponse>(
        `/api/exercise/${exerciseId}/submit`,
        { answer },
        { withCredentials: true }
      )
    );
  }

  async completeLesson(lessonId: string): Promise<CompleteLessonResponse> {
    return await firstValueFrom(
      this.httpClient.post<CompleteLessonResponse>(
        `/api/lesson/${lessonId}/complete`,
        {},
        { withCredentials: true }
      )
    );
  }
}