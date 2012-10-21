using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OceanMars.Common
{

    /// <summary>
    /// An interface representing objects that can listen to updating state.
    /// </summary>
    public interface IStatePhaseListener
    {

        void HandleStatePhaseChange(State.PHASE phase);

    }

}
