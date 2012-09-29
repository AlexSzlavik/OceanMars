using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace OceanMars.Common
{
    public class DefaultLevel : Entity
    {
        public DefaultLevel(Entity parent)
            : base(new Vector2(2400, 1000), parent)
        {
        }
    }
}
