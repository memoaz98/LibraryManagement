import { TestBed } from '@angular/core/testing';
import {
  ActivatedRouteSnapshot,
  CanActivateFn,
  RouterStateSnapshot,
  UrlTree,
  provideRouter,
} from '@angular/router';

import { staffGuard } from './staff-guard';
import { AuthState } from './auth-state';

/** An unsigned token carrying `payload`: enough to read its claims. */
const tokenWith = (payload: object) => `header.${btoa(JSON.stringify(payload))}.signature`;

describe('staffGuard', () => {
  const executeGuard: CanActivateFn = (...guardParameters) =>
    TestBed.runInInjectionContext(() => staffGuard(...guardParameters));
  const route = {} as ActivatedRouteSnapshot;
  const state = {} as RouterStateSnapshot;
  let authState: AuthState;

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({ providers: [provideRouter([])] });
    authState = TestBed.inject(AuthState);
  });

  afterEach(() => localStorage.clear());

  it('should let a librarian through', () => {
    authState.setAuthenticated(tokenWith({ role: 'Librarian' }), 'refresh');

    expect(executeGuard(route, state)).toBe(true);
  });

  it('should send a reader to the home page', () => {
    authState.setAuthenticated(tokenWith({ role: 'Reader' }), 'refresh');

    const result = executeGuard(route, state);

    expect(result).toBeInstanceOf(UrlTree);
    expect(String(result)).toBe('/');
  });
});
