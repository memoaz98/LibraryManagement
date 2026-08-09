import { Component, inject } from '@angular/core';
import { RouterOutlet, RouterLink, RouterLinkActive, Router } from '@angular/router';
import { AuthState } from './auth/auth-state';
import { AuthApi } from './auth/auth-api';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  templateUrl: './app.html',
  styleUrl: './app.scss',
})
export class App {
  private readonly authApi = inject(AuthApi);
  private readonly router = inject(Router);
  protected readonly authState = inject(AuthState);

  logout(): void {
    const refreshToken = this.authState.getRefreshToken();
    if (refreshToken) {
      this.authApi.logout({ refreshToken }).subscribe();
    }
    this.authState.clear();
    this.router.navigateByUrl('/login');
  }
}
