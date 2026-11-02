const STAFF_ROLES: readonly string[] = ['Librarian', 'Administrator'];

/**
 * Reads the role names from an access token's `role` claim, which holds one
 * string or an array. A missing or malformed token gives no roles.
 *
 * Only for deciding what to show: the signature is not checked here, the API
 * checks it and enforces the roles.
 */
export function readTokenRoles(token: string | null): string[] {
  const payload = token?.split('.')[1];
  if (!payload) {
    return [];
  }

  try {
    const role: unknown = JSON.parse(atob(payload.replace(/-/g, '+').replace(/_/g, '/'))).role;
    if (typeof role === 'string') {
      return [role];
    }
    return Array.isArray(role) ? role.filter((name) => typeof name === 'string') : [];
  } catch {
    return [];
  }
}

export function isStaffRole(role: string): boolean {
  return STAFF_ROLES.includes(role);
}
