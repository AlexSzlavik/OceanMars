using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using System.Xml.Serialization;
using System.IO;

namespace OceanMars.Common
{
    public class Level : Entity
    {
        public List<SpawnPointEntity> spawnPoints = new List<SpawnPointEntity>();

        public Level(Entity parent, List<Vector2[]> vectorList) : base (new Vector2(0, 0), parent)
        {
            constructLevel(vectorList);
        }

        public Level(Entity parent, string filePath)
            : base(new Vector2(0, 0), parent)
        {
            XmlSerializer deserializer = new XmlSerializer(typeof(List<Vector2[]>));

            TextReader textReader = new StreamReader(filePath);
            List<Vector2[]> vectorList;
            vectorList = (List<Vector2[]>)deserializer.Deserialize(textReader);
            textReader.Close();


            constructLevel(vectorList);
        }

        private void constructLevel(List<Vector2[]> vectorList)
        {
            TestWall w = null;
            SpawnPointEntity s = null;
            Vector2[] v;

            // First, create the set of spawn points
            v = vectorList[0];
            for (int i = 0; i < v.Length; i++)
            {
                s = new SpawnPointEntity(this, v[i]);
                this.addChild(s);
                spawnPoints.Add(s);
            }

            // Next, create the set of walls
            for (int i = 1; i < vectorList.Count; i++ )
            {
                v = vectorList[i];
                w = new TestWall(this, v[0], v[1]);
                this.addChild(w);
            }
        }
    }
}
