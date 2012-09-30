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
        public Level(Entity parent, List<Vector2[]> vectorList) : base (new Vector2(0, 0), parent)
        {
            TestWall w = null;
            foreach (Vector2[] v in vectorList)
            {
                w = new TestWall(this, v[0], v[1]);
                this.addChild(w);
            }
        }

        public Level(Entity parent, string filePath)
            : base(new Vector2(0, 0), parent)
        {
            XmlSerializer deserializer = new XmlSerializer(typeof(List<Vector2[]>));

            TextReader textReader = new StreamReader(filePath);
            List<Vector2[]> vectorList;
            vectorList = (List<Vector2[]>)deserializer.Deserialize(textReader);
            textReader.Close();

            //this.addChild(new Level(this, vectorList));

            TestWall w = null;
            foreach (Vector2[] v in vectorList)
            {
                w = new TestWall(this, v[0], v[1]);
                this.addChild(w);
            }
        }
    }
}
