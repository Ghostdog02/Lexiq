import { HttpClient, HttpHeaders } from '@angular/common/http';
import { inject, Injectable, OnInit } from '@angular/core';

import { BehaviorSubject, firstValueFrom } from 'rxjs';
import { Router } from '@angular/router';
import { FormGroup } from '@angular/forms';

const BACKEND_API_URL = import.meta.env.BACKEND_API_URL + '/auth';

@Injectable({ providedIn: 'root' })
export class AuthService implements OnInit {
  private expiresIn = 3600; // 1 hour

  private httpClient = inject(HttpClient);
  private router = inject(Router);

  private authStatusListener = new BehaviorSubject<boolean>(false);
  
  private tokenTimer?: ReturnType<typeof setTimeout>;

  public isLogged: boolean = false;

  async ngOnInit() {
    this.isLogged = await this.getInitialValue();                                                           
    
    if (this.isLogged) {
      this.changeAuthStatus(true);
      this.checkIfAuthTokenIsExpired();
    } else {
      this.changeAuthStatus(false);
    }
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
        this.httpClient.get<{ message: string; isLogged: boolean }>(BACKEND_API_URL + '/auth-status', { withCredentials: true })
      );

      return response?.isLogged ?? false;
    } catch (error) {
      return false;
    }
  }

  setExpirationDate(issuedAt: number) {
    const expirationDate = new Date((issuedAt + this.expiresIn) * 1000);
    console.log('Expiration Date Set:', expirationDate);

    localStorage.setItem('expiration-date', expirationDate.toISOString());
  }

  clearExpirationDate() {
    localStorage.removeItem('expiration-date');
  }

  checkIfAuthTokenIsExpired() {
    const expirationDate = localStorage.getItem('expiration-date');

    const now = new Date();

    if (!expirationDate) {
      this.changeAuthStatus(false);
      return;
    }

    const expiresIn = new Date(expirationDate).getTime() - now.getTime();

    if (expiresIn > 0) {
      this.setAuthTimer(expiresIn / 1000);
      this.changeAuthStatus(true);
    } else {
      this.clearExpirationDate();
      this.changeAuthStatus(false);
    }
  }

  changeAuthStatus(status: boolean) {
    this.authStatusListener.next(status);
    this.isLogged = status;
  }

  async loginUserWithGoogle(googleToken: any) {
    try {
      const response = await firstValueFrom(
        this.httpClient.post<{ message: string; user: any; issuedAt: number }>(
          BACKEND_API_URL + '/google-login',
          { idToken: googleToken },
          { withCredentials: true }
        )
      );

      const responseMessage = response.message;

      if (!response) {
        throw new Error(`Login failed: ${responseMessage}`);
      }

      this.setExpirationDate(response.issuedAt);

      this.setAuthTimer(this.expiresIn);

      this.changeAuthStatus(true);

      this.router.navigateByUrl('/');
    } catch (error) {
      this.changeAuthStatus(false);
    }
  }

  async logoutUser() {
    try {
      const response = await firstValueFrom(
        this.httpClient.post<{ message: string }>(BACKEND_API_URL + '/logout', {
          headers: new HttpHeaders({ 'Content-Type': 'application/json' }),
          withCredentials: true,
          observe: 'response' as 'response',
        })
      );

      const resMessage = response.message;

      this.clearExpirationDate();

      if (response) {
        this.changeAuthStatus(false);
      } else {
        throw new Error(`An error ocurred during logout: ${resMessage}`);
      }

      this.router.navigate(['/']);
    } catch (error: any) {
      throw new Error(`An error ocurred during logout: ${error.message}`);
    }
  }

  private setAuthTimer(duration: number) {
    console.log('Settinng timer ' + duration);
    this.tokenTimer = setTimeout(() => {
      this.logoutUser();
    }, duration * 1000);
  }

  hasValidationErrors(fieldName: string, form: FormGroup) {
    const field = form.get(fieldName);

    return !!(field?.errors && (field?.touched || field?.dirty));
  }
}
