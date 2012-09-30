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
        private List<Vector2[]> walls = null;

        public EditorMan(Entity parent) : base(new Vector2(21, 21), parent) {
            this.parent = parent;
            walls = new List<Vector2[]>();
        }

        // The user has choosen to create a point on the map
        // This point is either the beginning or the ending of a line
        public Entity pointSet()
        {
            
            Entity result = null;

            // If the first point is not set, set it now
            if (point1 == null)
            {
                System.Diagnostics.Debug.WriteLine("point 1 set");
                point1 = new Vector2(worldTransform.Translation.X, inverseWorldTransform.Translation.Y);
            }
            // Otherwise, create the new wall entity
            else
            {
                System.Diagnostics.Debug.WriteLine("point 2 set");
                Vector2 point2 = new Vector2(worldTransform.Translation.X, inverseWorldTransform.Translation.Y);
                TestWall w = new TestWall(this, (Vector2)point1, point2);
                parent.addChild(w);
                result = w;
                // Add the walls to the list of vector2 arrays for saving later
                Vector2[] worldWallEndpoints = new Vector2[] {
                        Vector2.Transform(w.endPoints[0], w.worldTransform),
                        Vector2.Transform(w.endPoints[1], w.worldTransform)
                                                };
                walls.Add(worldWallEndpoints);

                point1 = null;
            }

            return result;
        }

        public void saveWalls()
        {
            System.Diagnostics.Debug.WriteLine("Save the Game");
            foreach (Vector2[] w in walls)
            {
                System.Diagnostics.Debug.WriteLine(w[0]);
                System.Diagnostics.Debug.WriteLine(w[1]);
            }
        }
    }
}
