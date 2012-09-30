using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using OceanMars.Common;
using OceanMars.Client;
using System.Xml.Serialization;
using System.IO;

namespace OceanMars.Common
{
    class EditorMan : FreeEntity
    {
        private Vector2? point1 = null;
        private Entity parent = null;
        private List<Vector2[]> walls = null;
        private List<Vector2> spawns = null;

        public EditorMan(Entity parent) : base(new Vector2(21, 21), parent) {
            this.parent = parent;
            walls = new List<Vector2[]>();
            spawns = new List<Vector2>();
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

        // Write the contents of the wall list to a file
        public void saveWalls()
        {
            string filePath = @"../../../../OceanMarsContent/Levels/custom.lvl";
            XmlSerializer serializer = new XmlSerializer(typeof(List<Vector2[]>));

            TextWriter textWriter = new StreamWriter(filePath);

            // Create a list containing all of the spawn and wall details
            List<Vector2[]> outList = new List<Vector2[]>();
            Vector2[] spawnVectors = new Vector2[spawns.Count];
            for (int i = 0; i < spawns.Count; i++)
            {
                spawnVectors[i] = spawns[i];
            }
            outList.Add(spawnVectors);
            foreach (Vector2[] v in walls)
            {
                outList.Add(v);
            }

            // Write out the list and close the file
            serializer.Serialize(textWriter, outList);
            textWriter.Close();

        }

        public void loadLevel(string filePath, Entity root)
        {
            XmlSerializer deserializer = new XmlSerializer(typeof(List<Vector2[]>));

            TextReader textReader = new StreamReader(filePath);
            List<Vector2[]> level = (List<Vector2[]>)deserializer.Deserialize(textReader);
            textReader.Close();

            TestWall w = null;
            SpawnPointEntity s = null;
            Vector2[] v;

            // First, create the set of spawn points
            v = level[0];
            for (int i = 0; i < v.Length; i++)
            {
                spawns.Add(v[i]);
                s = new SpawnPointEntity(root, v[i]);
                root.addChild(s);
            }

            // Next, create the set of walls
            for (int i = 1; i < level.Count; i++)
            {
                v = level[i];
                walls.Add(v);
                w = new TestWall(root, v[0], v[1]);
                root.addChild(w);
            }
        }
    }
}
