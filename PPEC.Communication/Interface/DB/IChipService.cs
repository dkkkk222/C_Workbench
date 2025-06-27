using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PPEC.Communication.DB.Model;

namespace PPEC.Communication.Interface.DB
{
    public interface IChipService
    {
        Task<List<smls_chip>> GetChip(); 
        Task<int> AddChip(string name, string filePath);
        Task<int> UpdateChip(int id, string newName, string newFilePath);
        Task<int> DeleteChip(int id);
    }
}
