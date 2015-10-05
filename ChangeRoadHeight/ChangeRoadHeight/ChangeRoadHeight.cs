using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ICities;

namespace ChangeRoadHeight
{
    public class ChangeRoadHeight : IUserMod
    {
        public string Name
        {
            get
            {
                return "Change Road Height";
            }
        }
        public string Description
        {
            get
            {
                return "Move existing roads in height.";
            }
        }
    }
}
