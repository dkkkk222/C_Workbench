using AutoMapper;
using LinqToDB;
using PPEC.Communication.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Workbench.Db.IService;
using Workbench.Db.Tables;

namespace Workbench.Db.Service
{
    public class CpService : ICpService
    {
        private readonly IMapper _mapper;
        public CpService(IMapper mapper)
        {
            _mapper = mapper;
        }
        public async Task<Chip> GetChipById(string id)
        {
            using (var db = new DbContext())
            {
                return await db.Chips.FirstOrDefaultAsync(t => t.Id == id);
            }
        }

        public async Task<List<RegisterMeta>> GetChipRegisters(string chipId)
        {
            var target = new List<RegisterMeta>();
            using (var db = new DbContext())
            {
                var registers = await db.Registers.Where(t => t.ChipId == chipId).ToListAsync();
                foreach (var register in registers)
                {
                    var meta = new RegisterMeta();
                    var addressInfo = _mapper.Map<RegisterAddrInfo>(register);

                    //bit field
                    var registerBits = await db.RegisterBits.Where(t => t.RegisterId == register.Id).ToListAsync();
                    var bitFields = new List<BitField>();
                    foreach (var rgb in registerBits)
                    {
                        var bitField = _mapper.Map<BitField>(rgb);

                        #region 公式计算赋值
                        bitField.FormParam.ParamA = rgb.ParamA==null?1:double.Parse(rgb.ParamA);
                        bitField.FormParam.ParamB = rgb.ParamB==null?0:double.Parse(rgb.ParamB);
                        bitField.FormParam.ParamC = rgb.ParamC;
                        bitField.FormParam.ParamName = rgb.ParamName;
                        bitField.FormParam.UnitName = rgb.UnitName;
                        #endregion

                        var registerBitOptions = await db.RegisterBitOptions.Where(t => t.RegisterBitId == rgb.Id).ToListAsync();
                        var bitOptions = _mapper.Map<ObservableCollection<BitOption>>(registerBitOptions);
                        bitField.Options = bitOptions;

                        bitFields.Add(bitField);
                    }

                    addressInfo.BitFields =new ObservableCollection<BitField> (bitFields);

                    meta.AddrInfo = addressInfo;
                    target.Add(meta);
                }
            }

            return target;
        }
    }
}
