import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';

import { Loans } from './loans';
import { Loan } from './loan.model';

const LOANS_URL = 'https://localhost:7281/api/loans';
const MEMBERS_URL = 'https://localhost:7281/api/members';
const BOOKS_URL = 'https://localhost:7281/api/books';

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

describe('Loans', () => {
  let component: Loans;
  let fixture: ComponentFixture<Loans>;
  let http: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Loans],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();

    http = TestBed.inject(HttpTestingController);
    fixture = TestBed.createComponent(Loans);
    component = fixture.componentInstance;

    // The constructor loads the loans, the members and the books.
    http.expectOne(LOANS_URL).flush([aLoan()]);
    http.expectOne(MEMBERS_URL).flush([]);
    http.expectOne(BOOKS_URL).flush([]);
    await fixture.whenStable();
  });

  afterEach(() => http.verify());

  function text(dataCy: string): string | undefined {
    return fixture.nativeElement.querySelector(`[data-cy="${dataCy}"]`)?.textContent?.trim();
  }

  it('should show the loans it loaded', () => {
    expect(fixture.nativeElement.querySelectorAll('[data-cy="loan-row"]').length).toBe(1);
    expect(text('loans-summary')).toBe('1 loans shown, 0 overdue.');
  });

  it('should show why a checkout was refused', async () => {
    const checkingOut = component.checkout({ memberId: 3, bookCopyId: 9 });
    http
      .expectOne(LOANS_URL)
      .flush({ status: 409, code: 'member_inactive' }, { status: 409, statusText: 'Conflict' });
    await checkingOut;
    await fixture.whenStable();

    expect(text('loans-error')).toBe('This member is inactive and cannot borrow books.');
  });

  it('should confirm a return', async () => {
    const returning = component.returnLoan(aLoan());
    http
      .expectOne(`${LOANS_URL}/1/return`)
      .flush(aLoan({ status: 'Returned', returnDate: '2026-01-20' }));
    await returning;
    await fixture.whenStable();

    expect(text('loans-notice')).toBe('"Clean Code" (copy LIB-0001) was returned.');
  });
});
