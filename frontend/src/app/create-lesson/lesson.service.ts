import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Lesson, LessonResponse } from './lesson.interface';

@Injectable({
  providedIn: 'root'
})
export class LessonService {
  private apiUrl = 'https://your-api.com/api/lessons';

  constructor(private http: HttpClient) {}

  createLesson(lesson: Lesson): Observable<LessonResponse> {
    return this.http.post<LessonResponse>(this.apiUrl, lesson);
  }
}