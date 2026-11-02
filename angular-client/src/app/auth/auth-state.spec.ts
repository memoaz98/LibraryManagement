import { TestBed } from '@angular/core/testing';

import { AuthState } from './auth-state';

/** An unsigned token carrying `payload`: enough to read its claims. */
const tokenWith = (payload: object) => `header.${btoa(JSON.stringify(payload))}.signature`;

describe('AuthState', () => {
  let service: AuthState;

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({});
    service = TestBed.inject(AuthState);
  });

  afterEach(() => localStorage.clear());

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should read a single role from the access token', () => {
    service.setAuthenticated(tokenWith({ role: 'Reader' }), 'refresh');

    expect(service.roles()).toEqual(['Reader']);
    expect(service.isStaff()).toBe(false);
  });

  it('should treat a librarian as staff', () => {
    service.setAuthenticated(tokenWith({ role: ['Reader', 'Librarian'] }), 'refresh');

    expect(service.isStaff()).toBe(true);
  });

  it('should have no roles with a malformed token', () => {
    service.setAuthenticated('not-a-jwt', 'refresh');

    expect(service.roles()).toEqual([]);
  });
});
