﻿using Data.ViewModels;
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
                ExpiredTime = DateTime.Now.AddMinutes(5),
            };
        }

        public static bool ValidateOTP(string checkOTP, OTP otp)
        {
            if (otp.ExpiredTime < DateTime.Now || checkOTP != otp.Value)
                return false;
            return true;
        }
    }
}