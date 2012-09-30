using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace OceanMars.Common.NetCode
{
    public class TransformData : IMarshallable
    {
        public int EntityID;
        public float [] Matrix = new float[16]; 

        /// <summary>
        /// Convinience Constructor to Marshal Matrix
        /// </summary>
        /// <param name="ID"></param>
        /// <param name="matrix"></param>
        public TransformData(int ID, Matrix matrix)
        {

        }

        /// <summary>
        /// Reconstruct the Class from Network
        /// </summary>
        /// <param name="data"></param>
        public TransformData(byte[] data)
        {

        }

        /// <summary>
        /// Construct Network representation
        /// </summary>
        /// <returns></returns>
        public byte[] GetByteArray()
        {
            throw new NotImplementedException();
        }
    }
}
