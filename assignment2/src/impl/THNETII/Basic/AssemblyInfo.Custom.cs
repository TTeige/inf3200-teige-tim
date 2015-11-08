using System;
using System.Reflection;

namespace THNETII
{
    public partial class AssemblyInfo
    {
        private AssemblyName assem_name;
        private bool load_name;

        public AssemblyName AssemblyName
        { get { return this.GetPropertyValue(ref load_name, ref assem_name, () => this.assem_obj.GetName()); } }
    }
}