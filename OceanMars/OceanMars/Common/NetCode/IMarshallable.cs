using System;

namespace OceanMars.Common.NetCode
{

    /// <summary>
    /// Inherited interface that forces objects to implement a marshallisation method.
    /// </summary>
    public interface IMarshallable
    {

        /// <summary>
        /// Return a byte array representing a class.
        /// </summary>
        /// <returns>An array of bytes representing the underlying class.</returns>
        byte[] GetByteArray();

    }
}
