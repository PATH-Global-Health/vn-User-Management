using System;
using System.Collections.Generic;
using System.Text;

namespace Data.ViewModels.ProfileAPIs
{
    public class CreateProfileRequest
    {
        public string fullname { get; set; }
        public bool gender { get; set; } = false;
        public DateTime dateOfBirth { get; set; }
        public string phoneNumber { get; set; }
        public string email { get; set; }
        public string vaccinationCode { get; set; }
        public string identityCard { get; set; }
        public string address { get; set; }
        public string province { get; set; }
        public string district { get; set; }
        public string ward { get; set; }
        public string passportNumber { get; set; }
        public string nation { get; set; }
    }
}
