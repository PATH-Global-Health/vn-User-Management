using Data.ViewModels;
using System;

namespace Service.Helper
{
    public static class OTPHepler
    {
        public static OTP GenerateOTP()
        {
            return new OTP()
            {
                Value = new Random().Next(100000, 999999).ToString(),
                ExpiredTime = DateTime.Now.AddSeconds(60),
                AccessFailedCount = 0,
            };
        }

        public static bool ValidateOTP(string checkOTP, OTP otp)
        {
            if (otp == null || otp.ExpiredTime < DateTime.Now || checkOTP != otp.Value || otp.AccessFailedCount >= 3)
                return false;
            return true;
        }
    }
}