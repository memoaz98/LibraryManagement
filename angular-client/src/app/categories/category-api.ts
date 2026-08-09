import { Service, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Category } from './category.model';
import { CreateCategory } from './create-category.model';
import { UpdateCategory } from './update-category.model';

@Service()
export class CategoryApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = 'https://localhost:7281/api/categories';

  getAll(): Observable<Category[]> {
    return this.http.get<Category[]>(this.baseUrl);
  }

  create(request: CreateCategory): Observable<Category> {
    return this.http.post<Category>(this.baseUrl, request);
  }

  update(id: number, request: UpdateCategory): Observable<Category> {
    return this.http.put<Category>(`${this.baseUrl}/${id}`, request);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }


}
