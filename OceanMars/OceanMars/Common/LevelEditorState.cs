using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace OceanMars.Common
{
    class LevelEditorState : State
    {
        public EditorMan player;

        public LevelEditorState()
        {
            player = new EditorMan(root);

            TestWall w = new TestWall (root, new Vector2(-100, -50), new Vector2(100, -50));

            root.addChild(player);
            root.addChild(w);
        }
    }
}
