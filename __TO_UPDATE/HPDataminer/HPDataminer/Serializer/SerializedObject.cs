using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HellpointDataminer
{
    [DM_Serialized]
    public abstract class SerializedObject
    {
        public abstract void Apply(object obj);
        public abstract void Serialize(object obj);
    }
}
