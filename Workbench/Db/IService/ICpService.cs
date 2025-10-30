using PPEC.Communication.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Workbench.Db.Tables;

namespace Workbench.Db.IService
{
    public interface ICpService
    {
        Task<Chip> GetChipById(string id);

        Task<List<RegisterMeta>> GetChipRegisters(string chipId);

        Task<List<TelemetryCode>> GetTeleList(string chipId);
    }
}
