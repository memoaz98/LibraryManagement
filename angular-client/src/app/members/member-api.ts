import { Service, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Member } from './member.model';

@Service()
export class MemberApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = 'https://localhost:7281/api/members';

  /**
   * Staff only. Matches first name, last name or email; no term returns every
   * member. Inactive members are included.
   */
  search(term?: string): Observable<Member[]> {
    const search = term?.trim();
    return this.http.get<Member[]>(this.baseUrl, { params: search ? { search } : {} });
  }
}
