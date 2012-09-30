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

            root.addChild(player);
        }
    }
}
