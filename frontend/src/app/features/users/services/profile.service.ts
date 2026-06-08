import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { UserProfile } from '../models/user.model';

@Injectable({ providedIn: 'root' })
export class ProfileService {
  private httpClient = inject(HttpClient);

  getMyProfile(): Promise<UserProfile> {
    return firstValueFrom(
      this.httpClient.get<UserProfile>('/api/user/profile', {
        withCredentials: true,
      })
    );
  }

  uploadAvatar(file: File): Promise<void> {
    const formData = new FormData();
    formData.append('file', file);
    return firstValueFrom(
      this.httpClient.put<void>('/api/user/avatar', formData, {
        withCredentials: true,
      })
    );
  }
}
