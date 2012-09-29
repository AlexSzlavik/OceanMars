using System;

namespace OceanMars.Common.NetCode
{
    public interface StateChangeListener
    {
        void handleStateChange(StateChange s);
    }
}
