import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of, Subject } from 'rxjs';
import { tap } from 'rxjs/operators';
import { Lesson } from './lesson.interface';

@Injectable({
  providedIn: 'root'
})
export class LessonService {
  private apiUrl = 'http://localhost:5000/api/lessons';

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
    // For now, log the lesson data and return it (until backend endpoint exists)
    console.log('Creating lesson:', JSON.stringify(lesson, null, 2));

    // TODO: Uncomment when backend endpoint is ready
    // return this.httpClient.post<Lesson>(this.apiUrl, lesson);

    // Temporary: return the lesson as-is for testing
    return of(lesson).pipe(
      tap((createdLesson) => {
        console.log('Lesson would be sent to:', this.apiUrl);
        // Store the lesson
        this.createdLessons.push(createdLesson);
        // Emit the created lesson to subscribers
        this.lessonCreatedSubject.next(createdLesson);
      })
    );
  }
}