# Automatic Logout for Banned/Suspended Users - Implementation Summary

## Overview
This implementation ensures that when a user is banned or suspended by an admin, they are immediately logged out from all active sessions across all devices.

## Implementation Details

### 1. **BanCheckMiddleware** (`Middleware/BanCheckMiddleware.cs`)
- Custom middleware that runs on every HTTP request
- Checks if the authenticated user is banned or suspended
- If banned/suspended, immediately signs out the user and redirects to login page with appropriate error message
- Runs after authentication middleware to access user identity

### 2. **Security Stamp Validation** (`Program.cs`)
- Configured `SecurityStampValidatorOptions.ValidationInterval = TimeSpan.Zero`
- This validates the user's security stamp on every request
- When a user is banned/suspended, their security stamp is updated via `UpdateSecurityStampAsync()`
- This invalidates all existing authentication cookies/sessions immediately

### 3. **Admin Actions Updated** (`Controllers/AdminDashboardController.cs`)
- **BanUser**: Now calls `UpdateSecurityStampAsync()` before updating user
- **SuspendUser**: Now calls `UpdateSecurityStampAsync()` before updating user
- **UnsuspendUser**: Now calls `UpdateSecurityStampAsync()` to allow user to log in again
- This ensures all active sessions are invalidated when the action is taken

### 4. **Login Controller Enhanced** (`Controllers/AccountController.cs`)
- Login action now accepts query parameters: `banned`, `suspended`, and `reason`
- Displays appropriate error messages when users are redirected after being logged out
- Existing login checks for banned/suspended status remain in place as a secondary safeguard

## How It Works

### When Admin Bans a User:
1. Admin clicks "Ban User" in the dashboard
2. `BanUser` action sets `IsBanned = true` and updates the security stamp
3. User's security stamp changes, invalidating all existing sessions
4. On the banned user's next request:
   - Security stamp validation fails (if using cookies)
   - BanCheckMiddleware detects `IsBanned = true`
   - User is immediately signed out
   - User is redirected to login page with ban message

### When Admin Suspends a User:
1. Admin clicks "Suspend User" in the dashboard
2. `SuspendUser` action sets `IsSuspended = true` and updates the security stamp
3. User's security stamp changes, invalidating all existing sessions
4. On the suspended user's next request:
   - Security stamp validation fails (if using cookies)
   - BanCheckMiddleware detects `IsSuspended = true`
   - User is immediately signed out
   - User is redirected to login page with suspension message

### When Admin Unsuspends a User:
1. Admin clicks "Unsuspend User" in the dashboard
2. `UnsuspendUser` action sets `IsSuspended = false` and updates the security stamp
3. User's products are reactivated
4. User receives notification and email about account restoration
5. User can now log in again successfully

## Multi-Layer Protection

The implementation provides **three layers** of protection:

1. **Security Stamp Validation**: Invalidates authentication cookies immediately
2. **BanCheckMiddleware**: Checks ban/suspension status on every request
3. **Login Checks**: Prevents banned/suspended users from logging in again

## Performance Considerations

- **Security Stamp Validation**: Set to `TimeSpan.Zero` means validation on every request. This is appropriate for security-critical scenarios but adds a small database query overhead.
- **BanCheckMiddleware**: Only queries the database for authenticated users, minimal overhead
- For high-traffic applications, consider caching user ban status with a short TTL (e.g., 5-10 seconds)

## Testing

To test the implementation:

1. Create a test user account and log in
2. From admin dashboard, ban or suspend the test user
3. The test user should be immediately logged out on their next page navigation or refresh
4. Attempting to log in again should show the ban/suspension message

## Files Modified

1. **Created**: `Middleware/BanCheckMiddleware.cs`
2. **Modified**: `Program.cs` - Added middleware registration and security stamp validation
3. **Modified**: `Controllers/AdminDashboardController.cs` - Added security stamp updates to Ban/Suspend/Unsuspend actions
4. **Modified**: `Controllers/AccountController.cs` - Enhanced login action with query parameters
5. **Modified**: `Views/AdminDashboard/Index.cshtml` - Added Unsuspend button UI and JavaScript handler
6. **Modified**: `Views/AdminDashboard/Reports.cshtml` - Added Unsuspend button UI and JavaScript handler

## Notes

- The middleware runs after authentication, so it only affects authenticated users
- Anonymous users are not checked (no performance impact on public pages)
- The implementation is compatible with all authentication schemes (cookies, JWT, etc.)
- Logging is included for security auditing purposes
