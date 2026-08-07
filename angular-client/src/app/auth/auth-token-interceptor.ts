import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { AuthState } from './auth-state';

export const authTokenInterceptor: HttpInterceptorFn = (req, next) => {
  const authState = inject(AuthState);
  const accessToken = authState.getAccessToken();

  if (accessToken) {
    const authReq = req.clone({
      setHeaders: { Authorization: `Bearer ${accessToken}` },
    });
    return next(authReq);
  }

  return next(req);
};
