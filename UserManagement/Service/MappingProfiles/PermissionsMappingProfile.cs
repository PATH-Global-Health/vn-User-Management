using AutoMapper;
using Data.MongoCollections;
using Data.ViewModels;
namespace Service.MappingProfiles
{
    public class PermissionsMappingProfile : Profile
    {
        public PermissionsMappingProfile()
        {
            CreateMap<ResourcePermissionCreateModel, ResourcePermission>()
                .ForMember(dm => dm.NormalizedMethod, map => map.MapFrom(vm => vm.Method.Trim().ToUpper()))
                .ForMember(dm => dm.NormalizedUrl, map => map.MapFrom(vm => vm.Url.Trim().ToUpper()))
                ;

            CreateMap<UiPermissionCreateModel, UiPermission>()
                .ForMember(dm => dm.Type, map => map.MapFrom(vm => vm.PermissionType))
                ;

            CreateMap<ResourcePermission, ResourcePermissionModel>()
                ;

            CreateMap<UiPermission, UiPermissionModel>();

        }
    }
}
