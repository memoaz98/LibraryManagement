import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';

import { Login } from './login';

describe('Login', () => {
  let component: Login;
  let fixture: ComponentFixture<Login>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Login],
      providers: [
        // The template links to /register with routerLink, which needs ActivatedRoute.
        provideRouter([]),
        // Login -> AuthApi -> HttpClient.
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(Login);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should render the email and password fields', () => {
    const element = fixture.nativeElement as HTMLElement;

    expect(element.querySelector('input[type="email"]')).not.toBeNull();
    expect(element.querySelector('input[type="password"]')).not.toBeNull();
  });
});
