using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace Workbench.Converters
{
    public class AddOneConverter : IValueConverter
    {
        public object Convert(object value, Type t, object p, CultureInfo c)
        { 
            return ((int)value + 1).ToString(); 
        }
        public object ConvertBack(object v, Type t, object p, CultureInfo c)
        { 
          throw new NotImplementedException();
        }
      
    }
}
