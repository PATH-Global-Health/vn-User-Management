using AutoMapper;
using Data.MongoCollections;
using Data.ViewModels;
namespace Service.MappingProfiles
{
    public class RoleMappingProfile : Profile
    {
        public RoleMappingProfile()
        {
            CreateMap<Role, RoleModel>()
                ;

            CreateMap<RoleCreateModel, Role>()
                ;

            CreateMap<RoleUpdateModel, Role>();
        }
    }
}
