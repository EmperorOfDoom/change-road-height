using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChangeRoadHeight.Enums
{
    public enum ToolError
    {
        None,
        Unknown,
        AlreadyBuilt,
        AlreadyTwoway,
        SameDirection,
        CannotUpgradeThisType,
        OutOfArea
    }
}
