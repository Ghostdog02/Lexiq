import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom, Observable, of, Subject } from 'rxjs';
import { tap } from 'rxjs/operators';
import { Lesson } from './lesson.interface';
import { Course } from './course.interface';

@Injectable({
  providedIn: 'root'
})
export class LessonService {
  private apiUrl = import.meta.env.BACKEND_API_URL;

  private createdLessons: Lesson[] = [];

  private lessonCreatedSubject = new Subject<Lesson>();

  lessonCreated$ = this.lessonCreatedSubject.asObservable();

  constructor(private httpClient: HttpClient) {}

  getCreatedLessons(): Lesson[] {
    return [...this.createdLessons];
  }

  async getCourses(): Promise<Course[]> {
    const result = await firstValueFrom(
      this.httpClient.get<Course[]>(`${this.apiUrl}/course`)
    );
    
    if (!result) {
      console.error('❌ Failed to fetch courses from backend.');
    }

    return result;
  }

  createLesson(lesson: Lesson): Observable<Lesson> {
    console.log('📤 Creating lesson:', lesson);

    const createLessonDto = {
      courseId: lesson.courseId,
      title: lesson.title,
      description: lesson.description || null,
      estimatedDurationMinutes: lesson.estimatedDuration || null,
      orderIndex: 0, // TODO: Make this dynamic based on existing lessons in course
      lessonMediaUrl: lesson.mediaUrl ? [lesson.mediaUrl] : null,
      content: lesson.content  // Editor.js JSON string
    };

    console.log('📡 Sending to backend:', this.apiUrl, createLessonDto);

    return this.httpClient.post<any>(`${this.apiUrl}/lesson`, createLessonDto).pipe(
      tap((response) => {
        console.log('✅ Lesson created successfully:', response);

        const createdLesson: Lesson = {
          ...lesson,
          id: response.id
        };
        this.createdLessons.push(createdLesson);

        this.lessonCreatedSubject.next(createdLesson);
      })
    );
  }
}