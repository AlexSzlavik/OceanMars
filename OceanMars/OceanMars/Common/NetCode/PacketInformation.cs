using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using System.IO;

namespace OceanMars.Common.NetCode
{
    public class TransformData : IMarshallable
    {
        public int EntityID;

        public float [] Matrix = new float[16];

        public Matrix getMatrix()
        {
            Matrix ret = new Matrix(Matrix[0], Matrix[1], Matrix[2], Matrix[3], Matrix[4], Matrix[5], Matrix[6], Matrix[7], Matrix[8], Matrix[9], Matrix[10], Matrix[11], Matrix[12], Matrix[13], Matrix[14], Matrix[15]);
            return ret;
        }

        /// <summary>
        /// Convinience Constructor to Marshal Matrix
        /// </summary>
        /// <param name="ID"></param>
        /// <param name="matrix"></param>
        public TransformData(int ID, Matrix matrix)
        {
            EntityID = ID;
            Matrix[0] = matrix.M11;
            Matrix[1] = matrix.M12;
            Matrix[2] = matrix.M13;
            Matrix[3] = matrix.M14;
            Matrix[4] = matrix.M21;
            Matrix[5] = matrix.M22;
            Matrix[6] = matrix.M23;
            Matrix[7] = matrix.M24;
            Matrix[8] = matrix.M31;
            Matrix[9] = matrix.M32;
            Matrix[10] = matrix.M33;
            Matrix[11] = matrix.M34;
            Matrix[12] = matrix.M41;
            Matrix[13] = matrix.M42;
            Matrix[14] = matrix.M43;
            Matrix[15] = matrix.M44;
        }

        /// <summary>
        /// Reconstruct the Class from Network
        /// </summary>
        /// <param name="data"></param>
        public TransformData(byte[] data)
        {
            using (MemoryStream memoryStream = new MemoryStream(data))
            {
                using (BinaryReader binaryReader = new BinaryReader(memoryStream))
                {
                    EntityID = (int)binaryReader.ReadInt32();
                    for (int i = 0; i < 16; i++)
                        Matrix[i] = (float)binaryReader.ReadDouble();
                }
            }
        }

        /// <summary>
        /// Construct Network representation
        /// </summary>
        /// <returns></returns>
        public byte[] GetByteArray()
        {
            using (MemoryStream memoryStream = new MemoryStream())
            {
                using (BinaryWriter binaryWriter = new BinaryWriter(memoryStream))
                {
                    binaryWriter.Write((int)EntityID);
                    foreach(float element in Matrix)
                        binaryWriter.Write((double)element);

                    return memoryStream.ToArray();
                }
            }
        }
    }
}
