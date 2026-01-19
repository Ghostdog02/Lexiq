import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Lesson } from './lesson.interface';

@Injectable({
  providedIn: 'root'
})
export class LessonService {
  private apiUrl = 'https://your-api.com/api/lessons';

  constructor(private httpClient: HttpClient) {}

  createLesson(lesson: Lesson): Observable<Lesson> {
    return this.httpClient.post<Lesson>(this.apiUrl, lesson);
  }
}