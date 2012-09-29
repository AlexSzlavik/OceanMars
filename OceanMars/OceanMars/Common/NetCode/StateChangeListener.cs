using System;

namespace OceanMars.NetCode
{
    public interface StateChangeListener
    {
        void handleStateChange(StateChange s);
    }
}
