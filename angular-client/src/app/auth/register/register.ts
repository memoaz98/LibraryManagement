import { Component, inject, signal } from '@angular/core';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthApi } from '../auth-api';
import { AuthState } from '../auth-state';

@Component({
  selector: 'app-register',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './register.html',
  styleUrl: './register.scss',
})
export class Register {
  private readonly authApi = inject(AuthApi);
  private readonly authState = inject(AuthState);
  private readonly router = inject(Router);
  private readonly formBuilder = inject(FormBuilder);

  protected readonly error = signal<string | null>(null);
  protected readonly isLoading = signal(false);

  protected readonly form = this.formBuilder.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(6)]],
  });

  submit(): void {
    if (this.form.invalid) {
      return;
    }

    this.error.set(null);
    this.isLoading.set(true);

    const { email, password } = this.form.getRawValue();

    this.authApi.register({ email, password }).subscribe({
      next: (response) => {
        this.authState.setAuthenticated(response.accessToken, response.refreshToken);
        this.router.navigateByUrl('/categories');
      },
      error: () => {
        this.error.set('Registration failed. The email may already be in use, or the password does not meet the requirements.');
        this.isLoading.set(false);
      },
    });
  }
}
