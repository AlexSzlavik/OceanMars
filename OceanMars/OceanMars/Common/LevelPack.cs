using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using OceanMars.Client.Screens;
using System.Xml.Serialization;
using System.IO;

namespace OceanMars.Common
{
    class LevelPack : Entity
    {
        public List<Level> levelList = new List<Level>();

        public LevelPack(Entity parent)
            : base(new Vector2(0, 0), parent)
        {
            // Collect the set of .xml files
            string[] filePaths = System.IO.Directory.GetFiles(@"../../../../OceanMarsContent/Levels/");

            foreach (string filePath in filePaths)
            {
                // Parse each .xml file and create a Level Entity
                XmlSerializer deserializer = new XmlSerializer(typeof(List<Vector2[]>));

                TextReader textReader = new StreamReader(filePath);
                List<Vector2[]> vectorList;
                vectorList = (List<Vector2[]>)deserializer.Deserialize(textReader);
                textReader.Close();

                levelList.Add(new Level(this, vectorList));
            }

            // Add each level to the scene graph
            foreach (Level l in levelList)
            {
                this.addChild(l);
            }
            
        }
    }
}
