using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PPEC.Communication.Common;
using PPEC.Communication.DB.Model;
using PPEC.Communication.Interface.DB;
using PPEC.Communication.Model;

namespace PPEC.Communication.DB
{
    public class InitDataModelService
    {
        private static readonly InitDataModelService _instance = new InitDataModelService();
        public static InitDataModelService Instance => _instance;

        public List<smls_chip> ListChip { get; set; } = new List<smls_chip>();
        public Dictionary<int, List<RegisterMeta>> DicChipAddress { get; set; } = new Dictionary<int, List<RegisterMeta>>();
        public InitDataModelService()
        {

        }

        public async Task InitChipList(IChipService chipService)
        {
            RegisterExcelParser rep = new RegisterExcelParser();
            //初始化芯片列表
            ListChip = await chipService.GetChip();            
            foreach(var chip in ListChip)
            {//初始化芯片寄存器对照信息
                var list=rep.Parse(chip.FilePath);
                if(!DicChipAddress.ContainsKey(chip.Id))
                {
                    DicChipAddress.Add(chip.Id, list);
                }
            }
        }
    }
}
