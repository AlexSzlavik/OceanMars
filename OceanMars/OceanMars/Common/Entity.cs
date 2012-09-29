using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace OceanMars.Common
{
    public class Entity
    {
        int id;
        Vector2 collisionBox;
        Entity parent;
        List<Entity> children;
        Matrix transform;
        Vector2 velocity;

        virtual Matrix getWorldTransform() { return parent.getWorldTransform() * transform; }
    }
}
