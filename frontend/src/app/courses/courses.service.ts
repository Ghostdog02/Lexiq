import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { URLS } from '../Constants/urls';
import { Course } from './courses.component';

@Injectable({
  providedIn: 'root'
})
export class CoursesService {
  httpClient = inject(HttpClient);

  getAllCourses(): Observable<Course[]> {
    return this.httpClient.get<Course[]>(URLS.GET_ALL_COURSES);
  }
}
