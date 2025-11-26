import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root'
})
export class CoursesService {
  httpClient = inject(HttpClient)

  getAllCourses() {
    return this.httpClient.get<Course[]>(GET_ALL_COURSES)
  }


}
