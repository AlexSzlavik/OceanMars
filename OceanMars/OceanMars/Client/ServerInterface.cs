using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OceanMars.Common.NetCode;
using OceanMars.Common;

namespace OceanMars.Client
{
    class ServerInterface : TransformChangeListener
    {
        // Reference to server communication layer
        // Queue of state changes owned by us

        public ServerInterface()
        {
        }

        public virtual void handleTransformChange(Entity e)
        {
            // Generate a transform change packet, put it on stack
        }

        // Kind of events we have to handle:
            // Entity transform change
                // Only send most recent in queue
            // ____Entity creation (i.e. powerup, bullets, etc)

    }
}
