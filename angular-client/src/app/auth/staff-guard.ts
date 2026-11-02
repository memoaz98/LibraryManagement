import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthState } from './auth-state';

/**
 * Lets only Librarian and Administrator through; anyone else goes home.
 * Use it after authGuard, which sends signed-out users to the login page.
 */
export const staffGuard: CanActivateFn = () => {
  const authState = inject(AuthState);
  const router = inject(Router);

  if (authState.isStaff()) {
    return true;
  }

  return router.createUrlTree(['/']);
};
