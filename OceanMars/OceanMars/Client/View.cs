using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OceanMars.Common;
using Microsoft.Xna.Framework;
using OceanMars.Client.GameStateManager;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;

namespace OceanMars.Client
{
    public class View
    {
        public TestMan avatar;
        State state;

        public Dictionary<int, Sprite> sprites = new Dictionary<int,Sprite>();
        public Dictionary<String, Texture2D> textureDict = new Dictionary<string,Texture2D>();
        public Matrix centreTransform = Matrix.CreateTranslation(new Vector3(1280 / 2, 720 / 2, 0)); //TODO: use resolution?

        public View(State s, Entity avatar)
        {
            this.state = s;
            this.avatar = (TestMan)avatar;

            s.registerEntityAdd(this.OnAddEntity);
            s.registerEntityRemove(this.OnRemoveEntity);
        }

        /// <summary>
        /// Initialize sprite lists from entities already created. Should be called after content is loaded.
        /// </summary>
        public void InitFromState()
        {
            // Add all entities already in state
            foreach (Entity e in state.entities.Values)
            {
                OnAddEntity(e);
            }
        }

        public void draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            foreach (int id in sprites.Keys)
            {
                sprites[id].draw(gameTime, spriteBatch);
            }
        }

        public void OnAddEntity(Entity e)
        {
            if (e is DefaultLevel)
            {
                Sprite s = new DefaultLevelSprite(this, (DefaultLevel)e);
                sprites.Add(e.id, s);
            }
            else if (e is TestMan)
            {
                Sprite s = new TestManSprite(this, (TestMan)e);
                sprites.Add(e.id, s);
            }
            else if (e is TestWall)
            {
                Sprite s = new TestWallSprite(this, (TestWall)e);
                sprites.Add(e.id, s);
            }
        }

        public void OnRemoveEntity(Entity e)
        {
            sprites.Remove(e.id);
        }
    }
}
