import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';

import { Books } from './books';
import { Book } from './book.model';
import { Category } from '../categories/category.model';

const BOOKS_URL = 'https://localhost:7281/api/books';
const CATEGORIES_URL = 'https://localhost:7281/api/categories';

describe('Books', () => {
  let component: Books;
  let fixture: ComponentFixture<Books>;
  let httpMock: HttpTestingController;

  const categories: Category[] = [{ id: 1, name: 'Fiction', description: null }];

  const books: Book[] = [
    {
      id: 10,
      categoryId: 1,
      title: 'The Road',
      isbn: '9780307277671',
      publicationYear: 2006,
      synopsis: null,
      createdAt: '2026-01-01T00:00:00Z',
    },
  ];

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Books],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();

    httpMock = TestBed.inject(HttpTestingController);

    fixture = TestBed.createComponent(Books);
    component = fixture.componentInstance;

    // The constructor loads books and categories; both requests are pending here.
    httpMock.expectOne(BOOKS_URL).flush(books);
    httpMock.expectOne(CATEGORIES_URL).flush(categories);

    await fixture.whenStable();
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should render the books resolving the category name', () => {
    const rows = fixture.nativeElement.querySelectorAll('tbody tr') as NodeListOf<HTMLTableRowElement>;

    expect(rows.length).toBe(1);
    expect(rows[0].textContent).toContain('The Road');
    // categoriesById() maps categoryId 1 to its name instead of showing the raw id.
    expect(rows[0].textContent).toContain('Fiction');
  });
});
