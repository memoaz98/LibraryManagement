import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';

import { Categories } from './categories';
import { Category } from './category.model';

const CATEGORIES_URL = 'https://localhost:7281/api/categories';

describe('Categories', () => {
  let component: Categories;
  let fixture: ComponentFixture<Categories>;
  let httpMock: HttpTestingController;

  const categories: Category[] = [
    { id: 1, name: 'Fiction', description: 'Novels and short stories' },
    { id: 2, name: 'History', description: null },
  ];

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Categories],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();

    httpMock = TestBed.inject(HttpTestingController);

    fixture = TestBed.createComponent(Categories);
    component = fixture.componentInstance;

    // The component loads the list in its constructor, so the request is
    // already pending by the time createComponent() returns.
    httpMock.expectOne(CATEGORIES_URL).flush(categories);

    await fixture.whenStable();
  });

  afterEach(() => {
    // Fails the test if the component issued a request no assertion covered.
    httpMock.verify();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should render one row per category returned by the API', () => {
    const rows = fixture.nativeElement.querySelectorAll('tbody tr') as NodeListOf<HTMLTableRowElement>;

    expect(rows.length).toBe(2);
    expect(rows[0].textContent).toContain('Fiction');
    expect(rows[1].textContent).toContain('History');
  });
});
