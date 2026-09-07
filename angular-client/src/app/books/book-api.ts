import { Service, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Book } from './book.model';
import { CreateBook } from './create-book.model';
import { UpdateBook } from './update-book.model';

@Service()
export class BookApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = 'https://localhost:7281/api/books';

  getAll(): Observable<Book[]> {
    return this.http.get<Book[]>(this.baseUrl);
  }

  create(request: CreateBook): Observable<Book> {
    return this.http.post<Book>(this.baseUrl, request);
  }

  update(id: number, request: UpdateBook): Observable<Book> {
    return this.http.put<Book>(`${this.baseUrl}/${id}`, request);
  }
}
