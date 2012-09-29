using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OceanMars.Common
{
    public interface TransformChangeListener
    {
        void handleTransformChange(Entity e);
    }
}
