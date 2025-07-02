using AutoMapper;
using PPEC.Communication.Model;
using Workbench.Db.Tables;

namespace Workbench.Profiles
{
    public class RegisterProfile : Profile
    {
        public RegisterProfile()
        {
            CreateMap<RegisterAddrInfo, Register>();
            CreateMap<Register, RegisterAddrInfo>();

            CreateMap<BitField, RegisterBit>();
            CreateMap<RegisterBit, BitField>();

            CreateMap<BitOption, RegisterBitOption>();
            CreateMap<RegisterBitOption, BitOption>();
        }
    }
}
