describe('Login page', () => {
  beforeEach(() => {
    // Relative path thanks to baseUrl. Runs before every test, and Cypress
    // clears cookies/localStorage between tests (testIsolation), so each
    // test starts from a genuinely anonymous session.
    cy.visit('/login');
  });

  it('renders an empty login form', () => {
    cy.getByCy('login-form').should('be.visible');

    cy.getByCy('login-email').should('be.visible').and('have.value', '');
    cy.getByCy('login-password').should('be.visible').and('have.value', '');

    cy.getByCy('login-submit').should('be.enabled').and('contain.text', 'Login');
  });


  it('does not show an error message before any attempt', () => {
    cy.getByCy('login-error').should('not.exist');
  });

  it('accepts the credentials typed by the user', () => {
    cy.getByCy('login-email').type('guille@test.com').should('have.value', 'guille@test.com');
    cy.getByCy('login-password').type('Test1234!').should('have.value', 'Test1234!');
  });

  it('navigates to the register page', () => {
    cy.getByCy('login-register-link').click();

    // Asserting on pathname instead of the full URL keeps the test independent
    // of host and port.
    cy.location('pathname').should('eq', '/register');
  });
});

// Shape of the sensitive values declared in cypress.config.ts -> env.
interface TestCredentials {
  testUserEmail: string;
  testUserPassword: string;
}

describe('Login flow against the API', () => {
  // Non-sensitive config: available synchronously.
  const apiUrl = Cypress.expose('apiUrl') as string;

  // Local helper. Sensitive values arrive through a chainable, so the whole
  // interaction happens inside .then(). In the next step this becomes cy.login().
  const loginAsTestUser = () => {
    cy.env<TestCredentials>(['testUserEmail', 'testUserPassword']).then(
      ({ testUserEmail, testUserPassword }) => {
        cy.getByCy('login-email').type(testUserEmail);
        cy.getByCy('login-password').type(testUserPassword, { log: false });
        cy.getByCy('login-submit').click();
      },
    );
  };

  beforeEach(() => {
    // Spies are registered BEFORE visiting, so no request escapes unobserved.
    cy.intercept('POST', `${apiUrl}/auth/login`).as('loginRequest');
    cy.intercept('GET', `${apiUrl}/categories`).as('categoriesRequest');

    cy.visit('/login');
  });

  it('logs the user in and lands on the categories page', () => {
    loginAsTestUser();

    cy.wait('@loginRequest').its('response.statusCode').should('eq', 200);

    cy.location('pathname').should('eq', '/categories');
    // The nav is auth-aware: Logout replaces the Login/Register links.
    cy.getByCy('nav-logout').should('be.visible');
    cy.getByCy('nav-login').should('not.exist');
  });

    it('attaches the bearer token to the next API call', () => {
    loginAsTestUser();

    // One wait consumes one interception. Both assertions must read from that
    // single object: a second cy.wait('@categoriesRequest') would sit there
    // waiting for a second, non-existent request.
    cy.wait('@categoriesRequest').then(({ request, response }) => {
      // End-to-end proof that authTokenInterceptor works.
      expect(request.headers.authorization).to.match(/^Bearer .+/);
      expect(response?.statusCode).to.eq(200);
    });
  });


  it('persists both tokens in localStorage', () => {
    loginAsTestUser();
    cy.wait('@loginRequest');

    cy.window().then((win) => {
      expect(win.localStorage.getItem('library.accessToken')).to.be.a('string').and.not.be.empty;
      expect(win.localStorage.getItem('library.refreshToken')).to.be.a('string').and.not.be.empty;
    });
  });

  it('shows an error and stays on the page when the password is wrong', () => {
    cy.env<TestCredentials>(['testUserEmail']).then(({ testUserEmail }) => {
      cy.getByCy('login-email').type(testUserEmail);
    });
    cy.getByCy('login-password').type('ThisIsNotThePassword1!', { log: false });
    cy.getByCy('login-submit').click();

    cy.wait('@loginRequest').its('response.statusCode').should('eq', 401);

    cy.getByCy('login-error').should('be.visible').and('contain.text', 'Invalid email or password');
    cy.location('pathname').should('eq', '/login');
  });
});

describe('Login error handling (stubbed API)', () => {
  const apiUrl = Cypress.expose('apiUrl') as string;

  beforeEach(() => {
    // The request never reaches .NET: Cypress answers it. This is the only
    // reliable way to test a 500 without breaking the real backend.
    cy.intercept('POST', `${apiUrl}/auth/login`, {
      statusCode: 500,
      body: { type: 'about:blank', title: 'An unexpected error occurred', status: 500 },
    }).as('loginServerError');

    cy.visit('/login');
  });

  it('surfaces an error message when the server fails', () => {
    // Credentials are irrelevant here: the response is stubbed either way.
    cy.getByCy('login-email').type('anyone@test.com');
    cy.getByCy('login-password').type('irrelevant', { log: false });
    cy.getByCy('login-submit').click();

    cy.wait('@loginServerError');

    cy.getByCy('login-error').should('be.visible');
    cy.location('pathname').should('eq', '/login');
  });
});
