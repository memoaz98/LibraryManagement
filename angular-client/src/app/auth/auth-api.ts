import { Service, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { LoginRequest, RegisterRequest, RefreshTokenRequest, AuthResponse } from './auth.models';

@Service()
export class AuthApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = 'https://localhost:7281/api/auth';

  login(request: LoginRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.baseUrl}/login`, request);
  }

  register(request: RegisterRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.baseUrl}/register`, request);
  }

  refresh(request: RefreshTokenRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.baseUrl}/refresh`, request);
  }

  logout(request: RefreshTokenRequest): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/logout`, request);
  }
}
