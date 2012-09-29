using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace OceanMars.Common
{
    public class SliderEntity : Entity
    {
        public Vector2[] endPoints;

        public SliderEntity(Vector2[] endPoints)
            : base(Vector2.Zero)
        {
            endPoints = new Vector2[2];
            this.endPoints[0] = endPoints[0];
            this.endPoints[1] = endPoints[1];
        }
    }
}
