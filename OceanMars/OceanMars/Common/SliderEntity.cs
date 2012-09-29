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

        public SliderEntity(Vector2 endPoint1, Vector2 endPoint2, Entity parent)
            : base(Vector2.Zero, parent)
        {
            endPoints = new Vector2[2];
            this.endPoints[0] = endPoint1;
            this.endPoints[1] = endPoint2;
        }
    }
}
