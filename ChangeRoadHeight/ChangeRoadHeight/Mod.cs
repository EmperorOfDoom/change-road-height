﻿using ICities;

namespace ChangeRoadHeight
{
    public class Mod : IUserMod {

        public string Name
        {
            get
            {
                return "Change Road Height v14";
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
