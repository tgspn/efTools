using System.Collections.Generic;

namespace Tools.Templates.SQL.Models
{
    public class InstanceClass
    {
        public string ClassName { get; set; }
        public List<InstaceClassProperty> Properties { get; set; } = new List<InstaceClassProperty>();
    }
}
