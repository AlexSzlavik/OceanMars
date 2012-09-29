using System;

namespace OceanMars.Common.NetCode
{

    /// <summary>
    /// Generic interface for a StateChangeListener.
    /// </summary>
    public interface StateChangeListener
    {
        void HandleStateChange(StateChange s);
    }

}
