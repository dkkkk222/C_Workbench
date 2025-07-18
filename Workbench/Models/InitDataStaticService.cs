using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LinqToDB;
using PPEC.Communication.DB;
using Workbench.Db;

namespace Workbench.Models
{
    public class InitDataStaticService
    {
        private static readonly InitDataStaticService _instance = new InitDataStaticService();
        public static InitDataStaticService Instance => _instance;

        public ObservableCollection<ValueName> ChipTypeSource { get; set; }=new ObservableCollection<ValueName>();

        public async Task GetChipType()
        {
            using (var db = new DbContext())
            {
                var chips = await db.Chips.Where(t => t.IsDeleted == "A").ToListAsync();
                foreach (var chip in chips)
                {
                    ChipTypeSource.Add(new ValueName { Value = chip.Id, Name = chip.Name, Label = chip.FileName });
                }
            }
        }
    }
}
