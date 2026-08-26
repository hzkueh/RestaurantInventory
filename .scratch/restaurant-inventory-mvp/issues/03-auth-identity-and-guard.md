# 03 — Auth: Identity, seeded Manager, cookie auth, login/logout, redirect guard

**What to build:** The Manager signs in with a seeded account and stays signed in across page loads; signing out leaves the terminal safe. Any attempt to reach an inventory page while unauthenticated redirects to Login, so no inventory data is reachable without signing in. Establishes the authenticated app shell and navigation the later screens hang off.

**Blocked by:** 01.

**Status:** ready-for-agent

- [ ] ASP.NET Core Identity configured with cookie auth; one Manager account is seeded on startup.
- [ ] Login page: the seeded Manager can sign in.
- [ ] The session persists across page loads via the auth cookie — no re-authentication on every action.
- [ ] Sign-out control ends the session.
- [ ] All inventory pages require authentication; hitting any of them while unauthenticated redirects to Login.
- [ ] An authenticated layout / navigation shell exists for subsequent screens to plug into.
- [ ] Covers user stories 1–4.
