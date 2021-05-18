using System;
using Data.ViewModels;

namespace Service.Interfaces
{
    public interface IUserProfileService
    {
        ResultModel Create(UserProfileCreateModel model);
        PagingModel Search(string name, string phoneNumber, string email, DateTime? dateOfBirth, bool hasYearOfBirthOnly, int pageSize, int pageIndex);
        ResultModel Delete(string key);
    }
}
