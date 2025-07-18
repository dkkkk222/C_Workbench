using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net;
using LinqToDB;
using LinqToDB.Data;
using NPOI.SS.Formula.Functions;
using PPEC.Communication.DB.Model;
using PPEC.Communication.Interface.DB;

namespace PPEC.Communication.DB.Provided
{
    public class ChipService : IChipService
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(ChipService));
        private readonly SmlsContext dbContext;
        public ChipService(SmlsContext db)
        {
            dbContext = db;
        }
        public async Task<List<smls_chip>> GetChip()
        {
            return await dbContext.GetTable<smls_chip>().ToListAsync();
        }

        public async Task<int> AddChip(string name, string filePath)
        {
            try
            {
                var id = await dbContext.InsertWithInt32IdentityAsync(new smls_chip { Name = name, FilePath = filePath });
                return (int)id;
            }
            catch (Exception ex)
            {
                return -1;
            }
        }

        public async Task<int> UpdateChip(int id, string newName, string newFilePath)
        {
            try
            {
                return await dbContext.GetTable<smls_chip>()
                         .Where(c => c.Id == id)
                         .Set(c => c.Name, newName)
                         .Set(c => c.FilePath, newFilePath)
                         .UpdateAsync();

            }
            catch (Exception ex)
            {
                return -1;
            }
        }

        public async Task<int> DeleteChip(int id)
        {
            try
            {
                return await dbContext.GetTable<smls_chip>().DeleteAsync(c => c.Id == id);
            }
            catch (Exception ex)
            {
                return -1;
            }
        }
    }
}
