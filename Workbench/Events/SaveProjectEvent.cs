using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prism.Events;
using Workbench.Models;

namespace Workbench.Events
{
    public class SaveProjectEvent : PubSubEvent<PpecProject>
    {
    }
}
