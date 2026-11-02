import { ComponentFixture, TestBed } from '@angular/core/testing';

import { LoanFilters } from './loan-filters';
import { LoanQuery } from '../loan-query.model';
import { Member } from '../../members/member.model';

describe('LoanFilters', () => {
  let component: LoanFilters;
  let fixture: ComponentFixture<LoanFilters>;
  let emitted: LoanQuery | undefined;

  const members: Member[] = [
    { id: 1, firstName: 'Ana', lastName: 'Mora', email: 'ana@test.com', isActive: true },
  ];

  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [LoanFilters] }).compileComponents();

    fixture = TestBed.createComponent(LoanFilters);
    component = fixture.componentInstance;
    fixture.componentRef.setInput('members', members);
    emitted = undefined;
    component.queryChange.subscribe((query) => (emitted = query));
  });

  function choose(dataCy: string, value: string): void {
    const select = fixture.nativeElement.querySelector(
      `[data-cy="${dataCy}"]`,
    ) as HTMLSelectElement;
    select.value = value;
    select.dispatchEvent(new Event('change'));
  }

  it('should report the status filter, keeping the member filter', async () => {
    fixture.componentRef.setInput('query', { memberId: 1 });
    await fixture.whenStable();

    choose('loan-filter-status', 'active');

    expect(emitted).toEqual({ memberId: 1, status: 'active' });
  });

  it('should clear the member filter when "All members" is chosen', async () => {
    fixture.componentRef.setInput('query', { memberId: 1, status: 'active' });
    await fixture.whenStable();

    choose('loan-filter-member', '');

    expect(emitted).toEqual({ status: 'active' });
  });
});
