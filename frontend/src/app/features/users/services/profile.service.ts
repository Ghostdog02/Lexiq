import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { UserProfile } from '../models/user.model';

@Injectable({ providedIn: 'root' })
export class ProfileService {
  private httpClient = inject(HttpClient);

  async getMyProfile(): Promise<UserProfile> {
    return firstValueFrom(
      this.httpClient.get<UserProfile>('/api/user/profile')
    );
  }
}
