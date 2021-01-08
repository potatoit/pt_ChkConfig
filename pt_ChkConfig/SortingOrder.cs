using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pt_ChkConfig
{
    public class SortingOrderDefinition
    {
        public string TableName { get; set; }
        public string SortingOrder { get; set; }
        public List<string> Fields { get; set; } = new List<string>();
    }
}
