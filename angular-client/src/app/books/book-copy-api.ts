import { Service, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { BookCopy } from './book-copy.model';

@Service()
export class BookCopyApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = 'https://localhost:7281/api/books';

  /** Staff only. A book's copies, sorted by inventory code. */
  getByBook(bookId: number, availableOnly = false): Observable<BookCopy[]> {
    return this.http.get<BookCopy[]>(`${this.baseUrl}/${bookId}/copies`, {
      params: availableOnly ? { status: 'available' } : {},
    });
  }
}
