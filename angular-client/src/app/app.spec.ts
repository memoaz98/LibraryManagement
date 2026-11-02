import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';

import { App } from './app';

/** An unsigned token carrying `payload`: enough to read its claims. */
const tokenWith = (payload: object) => `header.${btoa(JSON.stringify(payload))}.signature`;

describe('App', () => {
  beforeEach(async () => {
    // AuthState restores the session from localStorage in its constructor,
    // so each test must start from a clean, unauthenticated state.
    localStorage.clear();

    await TestBed.configureTestingModule({
      imports: [App],
      providers: [
        // The template uses routerLink/routerLinkActive, which inject ActivatedRoute.
        provideRouter([]),
        // App -> AuthApi -> HttpClient. The testing backend keeps requests off the network.
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    }).compileComponents();
  });

  it('should create the app', () => {
    const fixture = TestBed.createComponent(App);

    expect(fixture.componentInstance).toBeTruthy();
  });

  it('should render the main navigation links', async () => {
    const fixture = TestBed.createComponent(App);
    await fixture.whenStable();

    const labels = Array.from(
      fixture.nativeElement.querySelectorAll('nav a') as NodeListOf<HTMLAnchorElement>,
    ).map((link) => link.textContent?.trim());

    expect(labels).toContain('Home');
    expect(labels).toContain('Categories');
    expect(labels).toContain('Books');
  });

  it('should show the Loans link to staff', async () => {
    localStorage.setItem('library.accessToken', tokenWith({ role: 'Librarian' }));

    const fixture = TestBed.createComponent(App);
    await fixture.whenStable();

    expect(fixture.nativeElement.querySelector('[data-cy="nav-loans"]')).not.toBeNull();
  });

  it('should hide the Loans link from readers', async () => {
    localStorage.setItem('library.accessToken', tokenWith({ role: 'Reader' }));

    const fixture = TestBed.createComponent(App);
    await fixture.whenStable();

    expect(fixture.nativeElement.querySelector('[data-cy="nav-loans"]')).toBeNull();
  });

  it('should offer Login and Register while the user is not authenticated', async () => {
    const fixture = TestBed.createComponent(App);
    await fixture.whenStable();

    const nav = fixture.nativeElement.querySelector('nav') as HTMLElement;

    expect(nav.textContent).toContain('Login');
    expect(nav.textContent).toContain('Register');
    // The Logout button only exists in the authenticated branch of the template.
    expect(nav.querySelector('button')).toBeNull();
  });
});
