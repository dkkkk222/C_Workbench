using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PPEC.Communication.Interface.DB;
using Unity.Storage;

namespace PPEC.Communication.DB
{
    public class MainServices
    {
        public IChipService ChipService { get; }
        public MainServices(IChipService chipService)
        {
            ChipService = chipService;
        }
    }
}
