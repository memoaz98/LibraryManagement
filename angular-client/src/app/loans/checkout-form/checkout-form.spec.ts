import { ComponentFixture, TestBed } from '@angular/core/testing';

import { CheckoutForm } from './checkout-form';
import { CreateLoan } from '../create-loan.model';
import { Member } from '../../members/member.model';
import { Book } from '../../books/book.model';
import { BookCopy } from '../../books/book-copy.model';

describe('CheckoutForm', () => {
  let component: CheckoutForm;
  let fixture: ComponentFixture<CheckoutForm>;

  const members: Member[] = [
    { id: 1, firstName: 'Ana', lastName: 'Mora', email: 'ana@test.com', isActive: true },
    { id: 2, firstName: 'Bea', lastName: 'Quesada', email: 'bea@test.com', isActive: false },
  ];
  const books: Book[] = [
    {
      id: 5,
      categoryId: 1,
      title: 'Clean Code',
      isbn: '9780132350884',
      publicationYear: 2008,
      synopsis: null,
      createdAt: '2026-01-01T00:00:00Z',
    },
  ];
  const copies: BookCopy[] = [
    {
      id: 9,
      bookId: 5,
      inventoryCode: 'LIB-0003',
      status: 'Available',
      acquisitionDate: '2025-01-15',
    },
  ];

  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [CheckoutForm] }).compileComponents();

    fixture = TestBed.createComponent(CheckoutForm);
    component = fixture.componentInstance;
    fixture.componentRef.setInput('members', members);
    fixture.componentRef.setInput('books', books);
    fixture.componentRef.setInput('copies', copies);
    await fixture.whenStable();
  });

  it('should emit the picked member and copy', () => {
    let emitted: CreateLoan | undefined;
    component.checkout.subscribe((request) => (emitted = request));

    // Picking the book first, because a new book clears the copy.
    component['form'].patchValue({ memberId: 1, bookId: 5 });
    component['form'].patchValue({ bookCopyId: 9 });
    component.submit();

    expect(emitted).toEqual({ memberId: 1, bookCopyId: 9 });
  });

  it('should not let an inactive member be picked', () => {
    const options = Array.from(
      fixture.nativeElement.querySelectorAll(
        '[data-cy="checkout-member"] option',
      ) as NodeListOf<HTMLOptionElement>,
    );
    const inactive = options.find((option) => option.textContent?.includes('Quesada'));

    expect(inactive?.disabled).toBe(true);
    expect(inactive?.textContent).toContain('inactive');
  });

  it('should ask for the copies of a newly picked book', () => {
    const picked: (number | null)[] = [];
    component.bookSelected.subscribe((bookId) => picked.push(bookId));

    component['form'].controls.bookId.setValue(5);

    expect(picked).toEqual([5]);
  });

  it('should report a member search', () => {
    let term: string | undefined;
    component.searchMembers.subscribe((value) => (term = value));

    component['form'].controls.search.setValue('mora');
    component.search();

    expect(term).toBe('mora');
  });

  it('should say when a book has no available copies', async () => {
    fixture.componentRef.setInput('copies', []);
    await fixture.whenStable();

    expect(fixture.nativeElement.querySelector('[data-cy="checkout-no-copies"]')).not.toBeNull();
  });
});
