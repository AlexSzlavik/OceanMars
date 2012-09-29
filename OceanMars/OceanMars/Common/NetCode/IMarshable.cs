using System;

namespace OceanMars.Common.NetCode
{

    /// <summary>
    /// Inherited interface that forces objects to implement a serialization method.
    /// </summary>
    public interface IMarshable
    {

        /// <summary>
        /// Return a byte array representing a class.
        /// </summary>
        /// <returns>An array of bytes representing the underlying class.</returns>
        byte[] GetByteArray();

    }
}
