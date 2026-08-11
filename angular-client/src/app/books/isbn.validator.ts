import { AbstractControl, ValidationErrors } from '@angular/forms';

/**
 * Validates the ISBN-13 check digit (the last of the 13 digits).
 *
 * The algorithm: multiply the first 12 digits alternately by 1 and 3,
 * sum them, and the check digit is (10 - sum % 10) % 10.
 */
export function isbn13Validator(control: AbstractControl): ValidationErrors | null {
  const value = control.value as string | null;

  // Empty is not this validator's problem: Validators.required already covers it.
  if (!value) {
    return null;
  }

  // Wrong shape is not this validator's problem either: Validators.pattern covers it.
  // Bailing out here avoids showing two error messages for the same input.
  if (!/^\d{13}$/.test(value)) {
    return null;
  }

  const digits = [...value].map(Number);
  const sum = digits
    .slice(0, 12)
    .reduce((total, digit, index) => total + digit * (index % 2 === 0 ? 1 : 3), 0);
  const checkDigit = (10 - (sum % 10)) % 10;

  return checkDigit === digits[12] ? null : { isbn13: true };
}
