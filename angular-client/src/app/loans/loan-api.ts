import { Service, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Loan } from './loan.model';
import { CreateLoan } from './create-loan.model';
import { LoanQuery, LoanStatusFilter } from './loan-query.model';

@Service()
export class LoanApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = 'https://localhost:7281/api/loans';
  private readonly myLoansUrl = 'https://localhost:7281/api/members/me/loans';

  /** Staff only. Every loan, newest first, optionally filtered. */
  getAll(query: LoanQuery = {}): Observable<Loan[]> {
    const params: Record<string, string | number> = {};
    if (query.memberId !== undefined) {
      params['memberId'] = query.memberId;
    }
    if (query.bookCopyId !== undefined) {
      params['bookCopyId'] = query.bookCopyId;
    }
    if (query.status !== undefined) {
      params['status'] = query.status;
    }
    return this.http.get<Loan[]>(this.baseUrl, { params });
  }

  /** A Reader gets a 404 for a loan that is not theirs. */
  getById(id: number): Observable<Loan> {
    return this.http.get<Loan>(`${this.baseUrl}/${id}`);
  }

  /** The current user's loans; empty when the account has no linked member. */
  getMine(status?: LoanStatusFilter): Observable<Loan[]> {
    return this.http.get<Loan[]>(this.myLoansUrl, { params: status ? { status } : {} });
  }

  /** Staff only. Fails with a 409 and a reason code when a rule refuses the loan. */
  checkout(request: CreateLoan): Observable<Loan> {
    return this.http.post<Loan>(this.baseUrl, request);
  }

  /** Staff only. Fails with a 409 (`loan_not_open`) when the loan is already closed. */
  returnLoan(id: number): Observable<Loan> {
    return this.http.post<Loan>(`${this.baseUrl}/${id}/return`, null);
  }
}
