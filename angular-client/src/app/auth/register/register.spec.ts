import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';

import { Register } from './register';

describe('Register', () => {
  let component: Register;
  let fixture: ComponentFixture<Register>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Register],
      providers: [
        // The template links to /login with routerLink, which needs ActivatedRoute.
        provideRouter([]),
        // Register -> AuthApi -> HttpClient.
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(Register);
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
