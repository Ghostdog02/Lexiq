import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { SubmitAnswerResponse } from '../models/lesson.interface';

/**
 * Service for managing exercise-related operations.
 * Handles answer submission and progress retrieval.
 */
@Injectable({
  providedIn: 'root'
})
export class ExerciseService {
  private httpClient = inject(HttpClient);

  /**
   * Submit an answer for a specific exercise.
   * @param exerciseId The ID of the exercise
   * @param answer The user's answer (option ID for multiple choice, text for others)
   * @returns Server response with correctness, points, and lesson progress
   */
  async submitExerciseAnswer(exerciseId: string, answer: string): Promise<SubmitAnswerResponse> {
    return await firstValueFrom(
      this.httpClient.post<SubmitAnswerResponse>(
        `/api/exercises/${exerciseId}/submit`,
        { answer },
        { withCredentials: true }
      )
    );
  }
}
