import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';

import { BookDetail } from './book-detail';
import { Book } from '../book.model';
import { Category } from '../../categories/category.model';

describe('BookDetail', () => {
  let component: BookDetail;
  let fixture: ComponentFixture<BookDetail>;

  const categories: Category[] = [
    { id: 1, name: 'Fiction', description: null },
    { id: 2, name: 'History', description: null },
  ];

  const book: Book = {
    id: 10,
    categoryId: 1,
    title: 'The Road',
    isbn: '9780307277671',
    publicationYear: 2006,
    synopsis: null,
    createdAt: '2026-01-01T00:00:00Z',
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [BookDetail],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();

    fixture = TestBed.createComponent(BookDetail);
    component = fixture.componentInstance;

    // Both inputs are input.required(): bind them before the first change
    // detection run, otherwise ngOnInit throws NG0950.
    fixture.componentRef.setInput('book', book);
    fixture.componentRef.setInput('categories', categories);

    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should prefill the editable copy with the selected book', () => {
    const values = Array.from(
      fixture.nativeElement.querySelectorAll('input') as NodeListOf<HTMLInputElement>,
    ).map((input) => input.value);

    expect(values).toContain('The Road');
    expect(values).toContain('9780307277671');
    expect(values).toContain('2006');
  });

  it('should list every category in the dropdown', () => {
    const options = fixture.nativeElement.querySelectorAll('select option') as NodeListOf<HTMLOptionElement>;
    const labels = Array.from(options).map((option) => option.textContent?.trim());

    // One placeholder option plus one per category.
    expect(labels).toContain('Fiction');
    expect(labels).toContain('History');
  });
});
