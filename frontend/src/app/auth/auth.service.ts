import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';

import { BehaviorSubject, firstValueFrom } from 'rxjs';
import { Router } from '@angular/router';

const BACKEND_API_URL = import.meta.env.BACKEND_API_URL || 'http://localhost:8080/api';
const AUTH_API_URL = (BACKEND_API_URL || '/api') + '/auth';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private httpClient = inject(HttpClient);
  private router = inject(Router);

  private authStatusListener = new BehaviorSubject<boolean>(false);
  private adminStatusListener = new BehaviorSubject<boolean>(false);

  public isLogged: boolean = false;
  public isAdmin: boolean = false;

  /**
   * Initialize auth state by checking backend /auth-status endpoint.
   * Called by APP_INITIALIZER before app starts.
   */
  async initializeAuthState(): Promise<void> {
    this.isLogged = await this.getInitialValue();
    this.changeAuthStatus(this.isLogged);

    if (this.isLogged) {
      this.isAdmin = await this.checkAdminStatus();
      this.adminStatusListener.next(this.isAdmin);
    }
  }

  getAuthStatusListener() {
    return this.authStatusListener.asObservable();
  }

  getAdminStatusListener() {
    return this.adminStatusListener.asObservable();
  }

  getIsAuth() {
    return this.isLogged;
  }

  getIsAdmin() {
    return this.isAdmin;
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

  async checkAdminStatus(): Promise<boolean> {
    try {
      const response = await firstValueFrom(
        this.httpClient.get<{ isAdmin: boolean; roles: string[] }>(
          AUTH_API_URL + '/is-admin',
          { withCredentials: true }
        )
      );

      return response?.isAdmin ?? false;
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
      this.isAdmin = await this.checkAdminStatus();
      this.adminStatusListener.next(this.isAdmin);
      this.router.navigateByUrl('/');
    } catch {
      this.changeAuthStatus(false);
      this.isAdmin = false;
      this.adminStatusListener.next(false);
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
      this.isAdmin = false;
      this.adminStatusListener.next(false);
      this.router.navigate(['/']);
    } catch (error: any) {
      throw new Error(`An error occurred during logout: ${error.message}`);
    }
  }
}
