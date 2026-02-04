import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { Lesson, LessonApiResponse } from '../models/lesson.interface';
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

  // ---------------------------------------------------------------------------
  // Mappers
  // ---------------------------------------------------------------------------

  /**
   * Converts API response to UI model.
   * Centralizes the transformation so all components use the same logic.
   */
  mapApiToLesson(api: LessonApiResponse): Lesson {
    return {
      id: api.lessonId,
      title: api.title,
      description: api.description ?? '',
      estimatedDuration: api.estimatedDurationMinutes ?? 0,
      mediaUrl: api.lessonMediaUrl?.[0],
      content: api.lessonContent,
      courseId: api.courseId,
      exercises: [],
      icon: LESSON_ICONS[api.orderIndex % LESSON_ICONS.length],
      status: api.isLocked ? 'locked' : 'available',
      xp: api.exerciseCount * 25
    };
  }

  // ---------------------------------------------------------------------------
  // Courses
  // ---------------------------------------------------------------------------

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

  // ---------------------------------------------------------------------------
  // Lessons
  // ---------------------------------------------------------------------------

  async getLessonsByCourse(courseId: string): Promise<LessonApiResponse[]> {
    const result = await firstValueFrom(
      this.httpClient.get<LessonApiResponse[]>(`/api/lesson/course/${courseId}`, { withCredentials: true })
    );

    if (!result) {
      console.error(`❌ Failed to fetch lessons for course ${courseId}.`);
      return [];
    }

    return result;
  }

  async getLesson(lessonId: string): Promise<LessonApiResponse | null> {
    try {
      const result = await firstValueFrom(
        this.httpClient.get<LessonApiResponse>(`/api/lesson/${lessonId}`, { withCredentials: true })
      );
      return result;
    } catch (error) {
      console.error(`❌ Failed to fetch lesson ${lessonId}:`, error);
      return null;
    }
  }

  async createLesson(lesson: Lesson): Promise<LessonApiResponse> {
    const createLessonDto = {
      courseId: lesson.courseId,
      title: lesson.title,
      description: lesson.description || null,
      estimatedDurationMinutes: lesson.estimatedDuration || null,
      orderIndex: 0, // TODO: Make this dynamic based on existing lessons in course
      lessonMediaUrl: lesson.mediaUrl ? [lesson.mediaUrl] : null,
      content: lesson.content
    };

    const result = await firstValueFrom(
      this.httpClient.post<LessonApiResponse>('/api/lesson', createLessonDto, { withCredentials: true })
    );

    console.log('✅ Lesson created:', result);
    return result;
  }

  async updateLesson(lessonId: string, updates: Partial<Lesson>): Promise<LessonApiResponse | null> {
    try {
      const updateDto = {
        title: updates.title,
        description: updates.description,
        estimatedDurationMinutes: updates.estimatedDuration,
        lessonMediaUrl: updates.mediaUrl ? [updates.mediaUrl] : undefined,
        lessonContent: updates.content
      };

      const result = await firstValueFrom(
        this.httpClient.put<LessonApiResponse>(`/api/lesson/${lessonId}`, updateDto, { withCredentials: true })
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
      await firstValueFrom(
        this.httpClient.delete(`/api/lesson/${lessonId}`, { withCredentials: true })
      );
      console.log('✅ Lesson deleted:', lessonId);
      return true;
    } catch (error) {
      console.error(`❌ Failed to delete lesson ${lessonId}:`, error);
      return false;
    }
  }
}