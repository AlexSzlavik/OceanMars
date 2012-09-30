using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using OceanMars.Common;
using OceanMars.Client;

namespace OceanMars.Common
{
    class EditorMan : FreeEntity
    {
        private Vector2? point1 = null;
        private Entity parent = null;

        public EditorMan(Entity parent) : base(new Vector2(21, 21), parent) {
            this.parent = parent;
        }

        // The user has choosen to create a point on the map
        // This point is either the beginning or the ending of a line
        public Entity pointSet()
        {
            Entity result = null;

            // If the first point is not set, set it now
            if (point1 == null)
            {
                point1 = new Vector2(worldTransform.Translation.X, inverseWorldTransform.Translation.Y);
            }
            // Otherwise, create the new wall entity
            else
            {
                Vector2 point2 = new Vector2(worldTransform.Translation.X, inverseWorldTransform.Translation.Y);
                TestWall w = new TestWall(this, (Vector2)point1, point2);
                parent.addChild(w);
                result = w;

                point1 = null;
            }

            return result;
        }
    }
}
