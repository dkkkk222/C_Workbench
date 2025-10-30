using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LinqToDB.Mapping;
using Prism.Mvvm;

namespace PPEC.Communication.Model
{
    [Table("t_TelemetryCode")]
    public class TelemetryCode:BindableBase
    {
        [PrimaryKey]
        [Column("id", CanBeNull = false)]
        public string Id { get; set; }

        [Column("chip_id")]
        public string ChipId { get; set; }


        [Column("name", Length = 64)]
        public string Name { get; set; }

        [Column("code", Length = 64)]
        public string Code { get; set; }

        [Column("type", Length = 64)]
        public string Type { get; set; }

        [Column("length", Length = 64)]
        public string Length { get; set; }

        private bool _isChecked = false;
        public bool IsChecked
        {
            get => _isChecked;
            set => SetProperty(ref _isChecked, value);
        }
    }
}
