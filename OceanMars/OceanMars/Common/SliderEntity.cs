using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace OceanMars.Common
{
    public class SliderEntity : Entity
    {
        public Point[] endPoints;
        
        public SliderEntity(Point[] endPoints)
        {
            endPoints = new Point[2];
            this.endPoints[0] = endPoints[0];
            this.endPoints[1] = endPoints[1];
        }
    }
}
