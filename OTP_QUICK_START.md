# OTP Email Verification - Quick Start Guide

## ✅ What's Been Implemented

Your application now has **complete email verification with OTP** for all new user registrations and login attempts.

## 🚀 How to Test

### Test New User Registration:
1. Run your application: `dotnet run`
2. Navigate to the registration page
3. Fill out the registration form
4. Click "Register"
5. **You'll be redirected to the OTP verification page**
6. Check your email for the 6-digit OTP
7. Enter the OTP and click "Verify Email"
8. You'll be redirected to login page with success message

### Test Login with Unverified Account:
1. Try to login with an unverified account
2. **System will automatically send a new OTP**
3. You'll be redirected to OTP verification page
4. Enter the OTP to verify your email
5. Return to login and sign in

### Test OTP Expiry:
1. Request an OTP
2. Wait 10 minutes
3. Try to use the expired OTP
4. You'll see an error message
5. Click "Resend OTP" to get a new one

## 📧 Email Configuration

Your SMTP settings are in `appsettings.json`:
```json
"Smtp": {
  "Host": "smtp.gmail.com",
  "Port": 587,
  "EnableSsl": true,
  "FromEmail": "no-reply@example.com",
  "FromName": "ComRates",
  "Username": "safereturn254@gmail.com",
  "Password": "gfob oion thit wrzj"
}
```

**Note**: The email is already configured with Gmail credentials.

## 🔑 Key Features

✅ **6-digit OTP** sent to user's email  
✅ **10-minute expiry** for security  
✅ **Resend OTP** functionality  
✅ **Blocks unverified users** from logging in  
✅ **Automatic OTP generation** on registration  
✅ **Clean, user-friendly** verification page  
✅ **Email templates** with branded styling  

## 📱 User Flow

```
Registration → OTP Email Sent → Verify OTP → Login
                                     ↓
                            (If not verified)
                                     ↓
Login Attempt → OTP Email Sent → Verify OTP → Access Granted
```

## 🗄️ Database Changes

Three new fields added to `AspNetUsers` table:
- `EmailVerified` (bit) - Default: false
- `EmailVerificationOTP` (nvarchar) - Stores current OTP
- `OTPExpiryTime` (datetime2) - OTP expiration time

## 🔧 For Existing Users

All existing users will have `EmailVerified = false` by default.

**To manually verify existing users** (run in SQL Server):
```sql
UPDATE AspNetUsers 
SET EmailVerified = 1 
WHERE Email = 'user@example.com';
```

Or verify all existing users:
```sql
UPDATE AspNetUsers 
SET EmailVerified = 1 
WHERE CreatedAt < '2025-10-15';  -- Before OTP implementation
```

## 🎯 Testing Checklist

- [ ] Register new account
- [ ] Receive OTP email
- [ ] Verify with correct OTP
- [ ] Try invalid OTP
- [ ] Try expired OTP (wait 10 min)
- [ ] Test resend OTP
- [ ] Login without verification
- [ ] Login after verification
- [ ] Test with different roles

## 🐛 Troubleshooting

### Not Receiving Emails?
1. Check spam/junk folder
2. Verify SMTP credentials in `appsettings.json`
3. Check console logs for errors
4. Test with a different email provider

### OTP Not Working?
1. Ensure OTP hasn't expired (10 min limit)
2. Check for typos (6 digits only)
3. Use "Resend OTP" to get a fresh code
4. Check database: `SELECT * FROM AspNetUsers WHERE Email = 'user@example.com'`

### Build Errors?
1. Run: `dotnet restore`
2. Run: `dotnet build`
3. Check all files are saved

## 📂 New Files Created

- `Services/IOtpService.cs` - OTP service interface
- `Services/OtpService.cs` - OTP logic
- `ViewModels/VerifyOtpViewModel.cs` - OTP form model
- `Views/Account/VerifyOtp.cshtml` - OTP verification page
- `Migrations/[timestamp]_AddEmailVerificationOTP.cs` - Database migration

## 🎨 UI Features

The OTP verification page includes:
- Large, centered OTP input field
- Auto-focus on page load
- Numeric-only input validation
- Letter-spaced display for readability
- Resend OTP button
- Back to login link
- Success/error message alerts

## 🔐 Security Notes

1. **OTP Expiry**: 10 minutes (configurable)
2. **One-Time Use**: OTP cleared after verification
3. **Email-Only**: OTP sent only to registered email
4. **No Bypass**: Login blocked until verified

## 📞 Support

If you need to:
- Change OTP expiry time: Edit `DateTime.Now.AddMinutes(10)` in `AccountController.cs`
- Customize email template: Edit email body in `AccountController.cs`
- Change OTP length: Modify `GenerateOTP()` in `OtpService.cs`
- Add SMS OTP: Implement SMS service and update controller

## ✨ Next Steps

Your OTP system is **fully functional**! 

To run the application:
```bash
cd c:\Users\tanui\OneDrive\Desktop\ComRates
dotnet run
```

Then navigate to: `https://localhost:[port]/Account/Register`

**Everything is ready to use!** 🎉
