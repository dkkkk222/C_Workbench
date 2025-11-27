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
        Task<List<TelemetryMonit>> GetTeleMoniteList(string chipId);
        Task<int> UpdateTelemetryAsync(TelemetryCode item);
        Task<string> AddTelemetryAsync(string chipId, TelemetryCode item);
        Task<int> DeleteTeleByChipAsync(string codeId);
        Task SaveTeleListAsync(string chipId, IEnumerable<TelemetryCode> items);
        Task SaveParamsListAsync(string chipId, IEnumerable<ParamDict> items);
        Task SaveTeleMonListAsync(string chipId, IEnumerable<TelemetryMonit> items);
        Task SaveTeleTagListAsync(string chipId, IEnumerable<TelemetryTagTable> items);
        Task ExportTelemetryExcelAsync(string chipId, string xlsxPath);
    }
}
