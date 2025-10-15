# OTP Email Verification Flow Diagram

## 📋 Complete User Journey

### 1️⃣ NEW USER REGISTRATION FLOW

```
┌─────────────────────────────────────────────────────────────┐
│                    USER REGISTERS                            │
│  (Fills form with Name, Email, Password, etc.)              │
└────────────────────┬────────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────────┐
│              ACCOUNT CREATED IN DATABASE                     │
│  - User record saved                                         │
│  - EmailVerified = FALSE                                     │
│  - Role assigned (Buyer/Seller/DeliveryService/Admin)       │
└────────────────────┬────────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────────┐
│                  OTP GENERATED                               │
│  - 6-digit random number (100000-999999)                    │
│  - Saved to: EmailVerificationOTP                           │
│  - Expiry: DateTime.Now + 10 minutes                        │
└────────────────────┬────────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────────┐
│                EMAIL SENT TO USER                            │
│  Subject: "Verify Your Email - ComRates"                    │
│  Body: Welcome message + OTP code                           │
└────────────────────┬────────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────────┐
│         REDIRECT TO /Account/VerifyOtp                       │
│  - Shows verification page                                   │
│  - Email pre-filled                                          │
│  - OTP input field ready                                     │
└────────────────────┬────────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────────┐
│              USER ENTERS OTP                                 │
│  - Types 6-digit code from email                            │
│  - Clicks "Verify Email" button                             │
└────────────────────┬────────────────────────────────────────┘
                     │
                     ▼
              ┌──────┴──────┐
              │   VALIDATE  │
              │     OTP     │
              └──────┬──────┘
                     │
         ┌───────────┴───────────┐
         │                       │
         ▼                       ▼
    ┌────────┐            ┌──────────┐
    │ VALID  │            │ INVALID  │
    └────┬───┘            └─────┬────┘
         │                      │
         ▼                      ▼
┌─────────────────┐    ┌──────────────────┐
│ EMAIL VERIFIED  │    │  ERROR MESSAGE   │
│ - Set to TRUE   │    │  - Wrong OTP     │
│ - OTP cleared   │    │  - Expired OTP   │
│ - Redirect to   │    │  - Show resend   │
│   Login page    │    │    option        │
└─────────────────┘    └──────────────────┘
```

---

### 2️⃣ UNVERIFIED USER LOGIN ATTEMPT

```
┌─────────────────────────────────────────────────────────────┐
│           USER TRIES TO LOGIN                                │
│  (Enters Email + Password)                                   │
└────────────────────┬────────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────────┐
│              CHECK USER EXISTS                               │
│  - Find user by email                                        │
│  - Verify password is correct                                │
└────────────────────┬────────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────────┐
│            CHECK ACCOUNT STATUS                              │
│  ✓ Is Banned? → Block with message                          │
│  ✓ Is Suspended? → Block with message                       │
│  ✓ Email Verified? → Check below                            │
└────────────────────┬────────────────────────────────────────┘
                     │
                     ▼
              ┌──────┴──────┐
              │   EMAIL     │
              │  VERIFIED?  │
              └──────┬──────┘
                     │
         ┌───────────┴───────────┐
         │                       │
         ▼                       ▼
    ┌─────────┐          ┌──────────────┐
    │   YES   │          │      NO      │
    └────┬────┘          └──────┬───────┘
         │                      │
         ▼                      ▼
┌─────────────────┐    ┌──────────────────┐
│  LOGIN SUCCESS  │    │  GENERATE NEW    │
│  - Sign in user │    │      OTP         │
│  - Redirect to  │    │  - 6 digits      │
│    dashboard    │    │  - 10 min expiry │
└─────────────────┘    └──────┬───────────┘
                              │
                              ▼
                     ┌──────────────────┐
                     │   SEND OTP VIA   │
                     │      EMAIL       │
                     └──────┬───────────┘
                              │
                              ▼
                     ┌──────────────────┐
                     │   REDIRECT TO    │
                     │   VerifyOtp      │
                     │      PAGE        │
                     └──────────────────┘
```

---

### 3️⃣ OTP VERIFICATION PROCESS

```
┌─────────────────────────────────────────────────────────────┐
│              VERIFY OTP PAGE LOADED                          │
│  - Email address displayed                                   │
│  - OTP input field (6 digits)                               │
│  - Resend OTP button available                              │
└────────────────────┬────────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────────┐
│              USER ENTERS OTP                                 │
│  - Types 6-digit code                                        │
│  - Submits form                                              │
└────────────────────┬────────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────────┐
│              VALIDATION CHECKS                               │
│  1. Does OTP match stored value?                            │
│  2. Is OTP still valid (not expired)?                       │
│  3. Is current time < OTPExpiryTime?                        │
└────────────────────┬────────────────────────────────────────┘
                     │
         ┌───────────┴───────────┐
         │                       │
         ▼                       ▼
    ┌─────────┐          ┌──────────────┐
    │  VALID  │          │   INVALID    │
    └────┬────┘          └──────┬───────┘
         │                      │
         ▼                      ▼
┌─────────────────┐    ┌──────────────────┐
│  UPDATE USER    │    │  SHOW ERROR      │
│  - EmailVerified│    │                  │
│    = TRUE       │    │  If Expired:     │
│  - Clear OTP    │    │  "OTP expired"   │
│  - Clear Expiry │    │                  │
└────┬────────────┘    │  If Wrong:       │
     │                 │  "Invalid OTP"   │
     ▼                 │                  │
┌─────────────────┐    │  Options:        │
│  SUCCESS MSG    │    │  - Try again     │
│  "Email         │    │  - Resend OTP    │
│   verified!"    │    └──────────────────┘
└────┬────────────┘
     │
     ▼
┌─────────────────┐
│  REDIRECT TO    │
│  LOGIN PAGE     │
└─────────────────┘
```

---

### 4️⃣ RESEND OTP FLOW

```
┌─────────────────────────────────────────────────────────────┐
│          USER CLICKS "RESEND OTP"                            │
└────────────────────┬────────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────────┐
│              FIND USER BY EMAIL                              │
└────────────────────┬────────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────────┐
│          GENERATE NEW OTP                                    │
│  - New 6-digit code                                          │
│  - New expiry time (10 min from now)                        │
│  - Overwrites old OTP                                        │
└────────────────────┬────────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────────┐
│              SEND NEW EMAIL                                  │
│  Subject: "New Verification OTP - ComRates"                 │
└────────────────────┬────────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────────┐
│          SHOW SUCCESS MESSAGE                                │
│  "A new OTP has been sent to your email"                    │
└────────────────────┬────────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────────┐
│          STAY ON VERIFY PAGE                                 │
│  User can now enter the new OTP                             │
└─────────────────────────────────────────────────────────────┘
```

---

## 🔄 State Transitions

```
User States:
┌──────────────┐
│  REGISTERED  │ ──────────────┐
│  (Unverified)│               │
└──────┬───────┘               │
       │                       │
       │ Enter OTP             │ OTP Expires
       │                       │ (10 minutes)
       ▼                       │
┌──────────────┐               │
│   VERIFIED   │               │
│  (Can Login) │◄──────────────┘
└──────────────┘    Resend OTP
```

---

## 📊 Database State Changes

```
REGISTRATION:
AspNetUsers Record:
├── EmailVerified: FALSE
├── EmailVerificationOTP: "123456"
└── OTPExpiryTime: "2025-10-15 16:00:00"

AFTER VERIFICATION:
AspNetUsers Record:
├── EmailVerified: TRUE
├── EmailVerificationOTP: NULL
└── OTPExpiryTime: NULL
```

---

## ⏱️ Timing

- **OTP Generation**: Instant
- **Email Delivery**: 1-30 seconds (depends on SMTP)
- **OTP Validity**: 10 minutes
- **Verification**: Instant (after OTP entered)

---

## 🎯 Key Decision Points

1. **Registration** → Always send OTP
2. **Login** → Check if verified, send OTP if not
3. **OTP Entry** → Validate and verify
4. **Expired OTP** → Allow resend
5. **Invalid OTP** → Allow retry or resend

---

## 🔐 Security Checkpoints

✅ User must exist in database  
✅ Password must be correct (for login)  
✅ OTP must match exactly  
✅ OTP must not be expired  
✅ Account must not be banned/suspended  
✅ Email must be verified to access app  
