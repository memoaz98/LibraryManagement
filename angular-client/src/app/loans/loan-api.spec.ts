import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';

import { LoanApi } from './loan-api';

describe('LoanApi', () => {
  const baseUrl = 'https://localhost:7281/api/loans';
  let service: LoanApi;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(LoanApi);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('should get all loans without filters', () => {
    service.getAll().subscribe();

    const req = http.expectOne(baseUrl);
    expect(req.request.method).toBe('GET');
    expect(req.request.params.keys()).toEqual([]);
  });

  it('should send only the filters that are set', () => {
    service.getAll({ memberId: 3, status: 'active' }).subscribe();

    const req = http.expectOne((r) => r.url === baseUrl);
    expect(req.request.params.get('memberId')).toBe('3');
    expect(req.request.params.get('status')).toBe('active');
    expect(req.request.params.has('bookCopyId')).toBe(false);
  });

  it('should get one loan by id', () => {
    service.getById(7).subscribe();

    expect(http.expectOne(`${baseUrl}/7`).request.method).toBe('GET');
  });

  it("should get the current user's loans with an optional status", () => {
    service.getMine('returned').subscribe();

    const req = http.expectOne((r) => r.url === 'https://localhost:7281/api/members/me/loans');
    expect(req.request.method).toBe('GET');
    expect(req.request.params.get('status')).toBe('returned');
  });

  it('should post a checkout', () => {
    service.checkout({ memberId: 3, bookCopyId: 9 }).subscribe();

    const req = http.expectOne(baseUrl);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ memberId: 3, bookCopyId: 9 });
  });

  it('should post a return without a body', () => {
    service.returnLoan(7).subscribe();

    const req = http.expectOne(`${baseUrl}/7/return`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toBeNull();
  });
});
