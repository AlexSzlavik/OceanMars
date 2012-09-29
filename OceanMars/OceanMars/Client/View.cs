using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OceanMars.Common;
using Microsoft.Xna.Framework;
using OceanMars.Client.GameStateManager;
using Microsoft.Xna.Framework.Graphics;

namespace OceanMars.Client
{
    public class View
    {
        Entity avatar;
        State state;

        public Dictionary<int, Sprite> sprites;
        public Dictionary<String, Texture2D> textureDict;

        void draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            Entity root = state.root;
            foreach (Entity e in root.children)
            {
                if (sprites.ContainsKey(e.id)) { sprites[e.id].draw(gameTime, spriteBatch); }
            }
        }
    }
}
