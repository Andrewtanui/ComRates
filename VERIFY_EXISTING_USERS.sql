-- SQL Script to Handle Existing Users After OTP Implementation
-- Run this in SQL Server Management Studio or Azure Data Studio

USE TanuiAppDB;
GO

-- Option 1: Verify ALL existing users (if you trust them)
-- This will mark all current users as verified
UPDATE AspNetUsers 
SET EmailVerified = 1 
WHERE EmailVerified = 0;
GO

-- Option 2: Verify only users created before OTP implementation
-- Replace the date with your OTP implementation date
UPDATE AspNetUsers 
SET EmailVerified = 1 
WHERE EmailVerified = 0 
  AND CreatedAt < '2025-10-15';
GO

-- Option 3: Verify specific users by email
UPDATE AspNetUsers 
SET EmailVerified = 1 
WHERE Email IN (
    'admin@example.com',
    'user1@example.com',
    'user2@example.com'
);
GO

-- Option 4: Verify users by role (e.g., all admins)
UPDATE AspNetUsers 
SET EmailVerified = 1 
WHERE UserRole = 3;  -- 3 = SystemAdmin
GO

-- Check verification status of all users
SELECT 
    Id,
    Email,
    FullName,
    UserRole,
    EmailVerified,
    CreatedAt,
    EmailVerificationOTP,
    OTPExpiryTime
FROM AspNetUsers
ORDER BY CreatedAt DESC;
GO

-- Count verified vs unverified users
SELECT 
    EmailVerified,
    COUNT(*) as UserCount
FROM AspNetUsers
GROUP BY EmailVerified;
GO

-- Find users who need verification
SELECT 
    Email,
    FullName,
    UserRole,
    CreatedAt
FROM AspNetUsers
WHERE EmailVerified = 0
ORDER BY CreatedAt DESC;
GO

-- Clean up any expired OTPs (optional maintenance)
UPDATE AspNetUsers
SET 
    EmailVerificationOTP = NULL,
    OTPExpiryTime = NULL
WHERE OTPExpiryTime < GETDATE();
GO
