import { HttpClient } from '@angular/common/http';
import { inject, Injectable, OnInit } from '@angular/core';

import { BehaviorSubject, firstValueFrom } from 'rxjs';
import { Router } from '@angular/router';
import { FormGroup } from '@angular/forms';

const BACKEND_API_URL = import.meta.env.BACKEND_API_URL || 'http://localhost:8080/api';
const AUTH_API_URL = (BACKEND_API_URL || '/api') + '/auth';

@Injectable({ providedIn: 'root' })
export class AuthService implements OnInit {
  private httpClient = inject(HttpClient);
  private router = inject(Router);

  private authStatusListener = new BehaviorSubject<boolean>(false);

  public isLogged: boolean = false;

  async ngOnInit() {
    this.isLogged = await this.getInitialValue();
    this.changeAuthStatus(this.isLogged);
  }

  getAuthStatusListener() {
    return this.authStatusListener.asObservable();
  }

  getIsAuth() {
    return this.isLogged;
  }

  async getInitialValue() {
    try {
      const response = await firstValueFrom(
        this.httpClient.get<{ message: string; isLogged: boolean }>(
          AUTH_API_URL + '/auth-status',
          { withCredentials: true }
        )
      );

      return response?.isLogged ?? false;
    } catch {
      return false;
    }
  }

  changeAuthStatus(status: boolean) {
    this.authStatusListener.next(status);
    this.isLogged = status;
  }

  async loginUserWithGoogle(googleToken: string) {
    try {
      await firstValueFrom(
        this.httpClient.post(
          AUTH_API_URL + '/google-login',
          { idToken: googleToken },
          { withCredentials: true }
        )
      );

      this.changeAuthStatus(true);
      this.router.navigateByUrl('/');
    } catch {
      this.changeAuthStatus(false);
    }
  }

  async logoutUser() {
    try {
      await firstValueFrom(
        this.httpClient.post(AUTH_API_URL + '/logout', null, {
          withCredentials: true,
        })
      );

      this.changeAuthStatus(false);
      this.router.navigate(['/']);
    } catch (error: any) {
      throw new Error(`An error occurred during logout: ${error.message}`);
    }
  }

  hasValidationErrors(fieldName: string, form: FormGroup) {
    const field = form.get(fieldName);

    return !!(field?.errors && (field?.touched || field?.dirty));
  }
}
