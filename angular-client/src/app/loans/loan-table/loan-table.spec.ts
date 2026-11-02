import { ComponentFixture, TestBed } from '@angular/core/testing';

import { LoanTable } from './loan-table';
import { Loan } from '../loan.model';

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

describe('LoanTable', () => {
  let component: LoanTable;
  let fixture: ComponentFixture<LoanTable>;

  const open = aLoan({ id: 1 });
  const overdue = aLoan({ id: 2, isOverdue: true });
  const returned = aLoan({ id: 3, status: 'Returned', returnDate: '2026-01-20' });

  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [LoanTable] }).compileComponents();

    fixture = TestBed.createComponent(LoanTable);
    component = fixture.componentInstance;
    fixture.componentRef.setInput('loans', [open, overdue, returned]);
    await fixture.whenStable();
  });

  function returnButtons(): HTMLButtonElement[] {
    return Array.from(fixture.nativeElement.querySelectorAll('[data-cy="loan-return"]'));
  }

  it('should offer Return only on open loans', () => {
    expect(returnButtons().length).toBe(2);
  });

  it('should report the loan to return', () => {
    let emitted: Loan | undefined;
    component.returnLoan.subscribe((loan) => (emitted = loan));

    returnButtons()[0].click();

    expect(emitted).toEqual(open);
  });

  it('should highlight overdue loans', () => {
    const overdueRows = fixture.nativeElement.querySelectorAll(
      'tr.overdue',
    ) as NodeListOf<HTMLElement>;

    expect(overdueRows.length).toBe(1);
    expect(overdueRows[0].textContent).toContain('Overdue');
  });

  it('should disable Return while a loan is being returned', async () => {
    fixture.componentRef.setInput('returningId', 1);
    await fixture.whenStable();

    expect(returnButtons().every((button) => button.disabled)).toBe(true);
    expect(returnButtons()[0].textContent?.trim()).toBe('Returning...');
  });
});
