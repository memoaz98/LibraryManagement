import { Service, signal, computed, inject } from '@angular/core';
import { TokenStorage } from './token-storage';
import { isStaffRole, readTokenRoles } from './token-roles';

@Service()
export class AuthState {
  private readonly tokenStorage = inject(TokenStorage);

  private readonly accessToken = signal<string | null>(null);
  private readonly refreshToken = signal<string | null>(null);

  readonly isAuthenticated = computed(() => this.accessToken() !== null);

  // Roles shape the UI only; the API enforces them.
  readonly roles = computed(() => readTokenRoles(this.accessToken()));
  readonly isStaff = computed(() => this.roles().some(isStaffRole));

  constructor() {
    // Restore session from storage on startup
    this.accessToken.set(this.tokenStorage.getAccessToken());
    this.refreshToken.set(this.tokenStorage.getRefreshToken());
  }

  setAuthenticated(accessToken: string, refreshToken: string): void {
    this.tokenStorage.setTokens(accessToken, refreshToken);
    this.accessToken.set(accessToken);
    this.refreshToken.set(refreshToken);
  }

  clear(): void {
    this.tokenStorage.clear();
    this.accessToken.set(null);
    this.refreshToken.set(null);
  }

  getAccessToken(): string | null {
    return this.accessToken();
  }

  getRefreshToken(): string | null {
    return this.refreshToken();
  }
}
