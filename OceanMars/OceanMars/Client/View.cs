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
        public Entity avatar;
        State state;

        public Dictionary<int, Sprite> sprites = new Dictionary<int,Sprite>();
        public Dictionary<String, Texture2D> textureDict = new Dictionary<string,Texture2D>();
        public Matrix centreTransform = Matrix.CreateTranslation(new Vector3(1280 / 2, 720 / 2, 0)); //TODO: use resolution?

        public View(State s, Entity avatar)
        {
            this.state = s;
            this.avatar = avatar;
        }

        public void draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            Entity root = state.root;
            foreach (Entity e in root.children)
            {
                if (sprites.ContainsKey(e.id)) {
                    sprites[e.id].draw(gameTime, spriteBatch);
                }
            }
        }
    }
}
