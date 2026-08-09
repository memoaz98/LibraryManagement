import { Component, inject, signal } from '@angular/core';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthApi } from '../auth-api';
import { AuthState } from '../auth-state';

@Component({
  selector: 'app-login',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './login.html',
  styleUrl: './login.scss',
})
export class Login {
  private readonly authApi = inject(AuthApi);
  private readonly authState = inject(AuthState);
  private readonly router = inject(Router);
  private readonly formBuilder = inject(FormBuilder);

  protected readonly error = signal<string | null>(null);
  protected readonly isLoading = signal(false);

  protected readonly form = this.formBuilder.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required]],
  });

  submit(): void {
    if (this.form.invalid) {
      return;
    }

    this.error.set(null);
    this.isLoading.set(true);

    const { email, password } = this.form.getRawValue();

    this.authApi.login({ email, password }).subscribe({
      next: (response) => {
        this.authState.setAuthenticated(response.accessToken, response.refreshToken);
        this.router.navigateByUrl('/categories');
      },
      error: () => {
        this.error.set('Invalid email or password.');
        this.isLoading.set(false);
      },
    });
  }

  testCredentials(): void {
    this.form.setValue({
      email: 'guille@test.com',
      password: 'Test1234!',
    });
  }

}
