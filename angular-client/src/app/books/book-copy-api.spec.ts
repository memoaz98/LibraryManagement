import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';

import { BookCopyApi } from './book-copy-api';

describe('BookCopyApi', () => {
  const copiesUrl = 'https://localhost:7281/api/books/4/copies';
  let service: BookCopyApi;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(BookCopyApi);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it("should get all of a book's copies", () => {
    service.getByBook(4).subscribe();

    const req = http.expectOne(copiesUrl);
    expect(req.request.method).toBe('GET');
    expect(req.request.params.has('status')).toBe(false);
  });

  it('should ask only for available copies when requested', () => {
    service.getByBook(4, true).subscribe();

    const req = http.expectOne((r) => r.url === copiesUrl);
    expect(req.request.params.get('status')).toBe('available');
  });
});
