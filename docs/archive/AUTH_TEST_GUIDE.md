# Authentication Testing Guide

## Setup
- Backend running on http://localhost:5000
- Frontend running on http://localhost:5173

## Steps to Test

1. **Open the Application**
   - Open http://localhost:5173 in your browser
   - Press F12 to open Developer Tools
   - Go to Console tab to see logs

2. **Login**
   - Email: `dev@example.com`
   - Password: `devpassword`
   - Click "Sign in"

3. **Watch the Console for These Logs**

   **Expected Sequence:**

```
[AuthContext] Login attempt for: dev@example.com
[AuthContext] Login response received: 200
[AuthContext] Response data keys: accessToken,expiresIn,user,tokenType
[AuthContext] Token from response: eyJhbGciOiJIUzI1NiIs...
[AuthContext] User from response: {id, email, name, timezone, createdAt}
[AuthContext] Saved to localStorage
[AuthContext] Set Authorization header
[AuthContext] isAuthenticated should be: true
[ProtectedRoute] Check - isAuthenticated: true loading: false
[apiClient] Request to /entries - Authorization header set
[apiClient] Response from /entries - 200
```

4. **Check localStorage**
   - Open Developer Tools > Storage > Local Storage
   - Look for `auth_token` - should have JWT value
   - Look for `auth_user` - should have user object
   - Both should be present

5. **Verify Page Stays on Dashboard**
   - After login, you should see the Timeline/Dashboard
   - Refresh the page (F5)
   - You should STAY logged in (not redirected to login)
   - Check console for: `[AuthContext] Initializing...` and `[AuthContext] Found token in storage: true`

## Troubleshooting

If you see a redirect back to login, check console for:

- **`[apiClient] Error on /entries - 401`** → Token not being sent with request
- **`[ProtectedRoute] Not authenticated`** → User or token is null
- **`[AuthContext] NO token in auth_token`** → localStorage not being read
- **API errors** → Check Network tab to see actual response

## Browser Developer Tools Help

- **Console**: F12 → Console tab
- **localStorage**: F12 → Storage/Application
- **Network**: F12 → Network tab (refresh page to see requests)
