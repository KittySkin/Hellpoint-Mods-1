using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace HellpointDataminer.DM_Definitions
{
    [XmlRoot(ElementName = "DM_Definitions.ItemDefinition")]
    public class ItemDefinition : SerializedObject
    {
        public string Title;
        public string Description;

        public override void Apply(object obj)
        {
            throw new NotImplementedException("TODO");
            //var item = (Definitions.ItemDefinition)obj;
            
            //if (this.Title != null)
            //    item.title = new Localization.TextID { temporary = Title };
        }

        public override void Serialize(object obj)
        {
            if (obj is Definitions.ItemDefinition itemDef)
            {
                Title = itemDef.Title;
                if (string.IsNullOrEmpty(Title))
                {
                    Title = itemDef.title.ToString();
                }

                Description = itemDef.Description;
            }
            else
            {
                HPDataminer.Log("Error - item '" + obj.ToString() + "' not a valid ItemDefinition or cannot cast it!");
            }
        }
    }
}
