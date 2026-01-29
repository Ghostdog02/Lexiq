import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of, Subject } from 'rxjs';
import { tap } from 'rxjs/operators';
import { Lesson } from './lesson.interface';

@Injectable({
  providedIn: 'root'
})
export class LessonService {
  private apiUrl = `${import.meta.env.BACKEND_API_URL || 'http://localhost:8080'}/api/lesson`;

  // Store created lessons in memory
  private createdLessons: Lesson[] = [];

  // Subject to emit newly created lessons
  private lessonCreatedSubject = new Subject<Lesson>();

  // Observable that components can subscribe to
  lessonCreated$ = this.lessonCreatedSubject.asObservable();

  constructor(private httpClient: HttpClient) {}

  // Get all created lessons (for loading on init)
  getCreatedLessons(): Lesson[] {
    return [...this.createdLessons];
  }

  createLesson(lesson: Lesson): Observable<Lesson> {
    console.log('📤 Creating lesson:', lesson);

    // Map frontend Lesson interface to backend CreateLessonDto format
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

    // Make actual API call to backend
    return this.httpClient.post<any>(this.apiUrl, createLessonDto).pipe(
      tap((response) => {
        console.log('✅ Lesson created successfully:', response);

        // Store the created lesson
        const createdLesson: Lesson = {
          ...lesson,
          id: response.id
        };
        this.createdLessons.push(createdLesson);

        // Emit the created lesson to subscribers
        this.lessonCreatedSubject.next(createdLesson);
      })
    );
  }
}