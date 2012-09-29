using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OceanMars.Common;
using OceanMars.Common.NetCode;

namespace OceanMars.Server
{
    public class HeadlessServer
    {
        public State state;

        private NetworkServer networkServer;
        private StateChange createLevel;

        // Do we need a record of ownership or do we assume clients are honest?

        public HeadlessServer(NetworkServer ns, Entity level)
        {
            this.networkServer = ns;

            state.root.addChild(level);

            StateChange createLevel = new StateChange();
            createLevel.type = StateChangeType.CREATE_LEVEL;

            if (level is TestLevel)
            {
                createLevel.stringProperties.Add(StateProperties.LEVEL_TYPE, "testlevel");
            }
        }

        public void processEvents()
        {
        }

        public void handlePlayerJoined(int id)
        {
            // Send the level creation event

            // Send creation events for everything outside our level

            // Send a spawn location
        }
    }
}
