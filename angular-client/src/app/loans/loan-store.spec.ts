import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';

import { LoanStore } from './loan-store';
import { Loan } from './loan.model';
import { LoanQuery } from './loan-query.model';

const LOANS_URL = 'https://localhost:7281/api/loans';
const CONFLICT = { status: 409, statusText: 'Conflict' };

function aLoan(overrides: Partial<Loan> = {}): Loan {
  return {
    id: 1,
    memberId: 3,
    memberName: 'Ana Mora',
    bookCopyId: 9,
    inventoryCode: 'LIB-0001',
    bookId: 2,
    bookTitle: 'Clean Code',
    status: 'Active',
    loanDate: '2026-01-15',
    dueDate: '2026-01-29',
    returnDate: null,
    isOverdue: false,
    ...overrides,
  };
}

describe('LoanStore', () => {
  let store: LoanStore;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    store = TestBed.inject(LoanStore);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  async function loadWith(loans: Loan[], query: LoanQuery = {}): Promise<void> {
    const loading = store.load(query);
    http.expectOne((request) => request.url === LOANS_URL).flush(loans);
    await loading;
  }

  it('should load the loans with the given filters', async () => {
    const loading = store.load({ status: 'active' });
    const request = http.expectOne((r) => r.url === LOANS_URL);
    expect(request.request.params.get('status')).toBe('active');
    request.flush([aLoan()]);
    await loading;

    expect(store.loans()).toEqual([aLoan()]);
    expect(store.query()).toEqual({ status: 'active' });
    expect(store.isLoading()).toBe(false);
  });

  it('should explain a failed load', async () => {
    const loading = store.load();
    http.expectOne(LOANS_URL).flush(null, { status: 500, statusText: 'Server Error' });
    await loading;

    expect(store.error()).toContain('Could not load the loans');
  });

  it('should count the overdue loans', async () => {
    await loadWith([aLoan({ id: 1, isOverdue: true }), aLoan({ id: 2 })]);

    expect(store.overdueCount()).toBe(1);
  });

  it('should add a new loan at the top and confirm it', async () => {
    await loadWith([aLoan({ id: 1 })]);

    const checkingOut = store.checkout({ memberId: 3, bookCopyId: 9 });
    http.expectOne(LOANS_URL).flush(aLoan({ id: 2 }));

    expect(await checkingOut).toBe(true);
    expect(store.loans().map((loan) => loan.id)).toEqual([2, 1]);
    expect(store.notice()).toContain('Clean Code');
  });

  it('should explain a refused checkout and keep the list', async () => {
    await loadWith([aLoan({ id: 1 })]);

    const checkingOut = store.checkout({ memberId: 3, bookCopyId: 9 });
    http.expectOne(LOANS_URL).flush({ status: 409, code: 'loan_limit_reached' }, CONFLICT);

    expect(await checkingOut).toBe(false);
    expect(store.error()).toBe('This member already has the maximum number of open loans.');
    expect(store.loans().map((loan) => loan.id)).toEqual([1]);
  });

  it('should update a returned loan in place', async () => {
    await loadWith([aLoan({ id: 1 }), aLoan({ id: 2 })]);

    const returning = store.returnLoan(1);
    http
      .expectOne(`${LOANS_URL}/1/return`)
      .flush(aLoan({ id: 1, status: 'Returned', returnDate: '2026-01-20' }));

    expect(await returning).toBe(true);
    expect(store.loans().map((loan) => loan.status)).toEqual(['Returned', 'Active']);
  });

  it('should drop a returned loan from an open-loans list', async () => {
    await loadWith([aLoan({ id: 1 }), aLoan({ id: 2 })], { status: 'active' });

    const returning = store.returnLoan(1);
    http
      .expectOne(`${LOANS_URL}/1/return`)
      .flush(aLoan({ id: 1, status: 'Returned', returnDate: '2026-01-20' }));
    await returning;

    expect(store.loans().map((loan) => loan.id)).toEqual([2]);
  });

  it('should explain a refused return and reload the list', async () => {
    await loadWith([aLoan({ id: 1 })]);

    const returning = store.returnLoan(1);
    http.expectOne(`${LOANS_URL}/1/return`).flush({ status: 409, code: 'loan_not_open' }, CONFLICT);

    expect(await returning).toBe(false);
    expect(store.error()).toBe('This loan is already closed, so it cannot be returned again.');
    http.expectOne(LOANS_URL).flush([aLoan({ id: 1, status: 'Returned' })]);
  });
});
