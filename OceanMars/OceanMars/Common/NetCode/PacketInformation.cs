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

        public Matrix GetMatrix()
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

    public class EntityStateData : IMarshallable
    {
        public int EntityID;
        public Entity.GroundState groundState;
        public MobileEntity.FacingState facingState;
        public MobileEntity.MovingState movingState;

        public EntityStateData(Entity e)
        {
            EntityID = e.id;
            groundState = e.groundState;
        }

        public EntityStateData(MobileEntity e)
        {
            EntityID = e.id;
            groundState = e.groundState;
            facingState = e.facing;
            movingState = e.moving;
        }

        /// <summary>
        /// Apply any data in the EntityStateData to the given entity
        /// </summary>
        public void apply(Entity e) {
            e.groundState = groundState;
        }

        /// <summary>
        /// Apply any data in the EntityStateData to the given entity
        /// </summary>
        public void apply(MobileEntity e)
        {
            e.groundState = groundState;
            e.moving = movingState;
            e.facing = facingState;
        }


        /// <summary>
        /// Reconstruct the Class from Network
        /// </summary>
        /// <param name="data"></param>
        public EntityStateData(byte[] data)
        {
            using (MemoryStream memoryStream = new MemoryStream(data))
            {
                using (BinaryReader binaryReader = new BinaryReader(memoryStream))
                {
                    EntityID = (int)binaryReader.ReadInt32();
                    groundState = (Entity.GroundState)binaryReader.ReadInt32();
                    facingState = (MobileEntity.FacingState)binaryReader.ReadInt32();
                    movingState = (MobileEntity.MovingState)binaryReader.ReadInt32();
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
                    binaryWriter.Write((int)groundState);
                    binaryWriter.Write((int)facingState);
                    binaryWriter.Write((int)movingState);

                    return memoryStream.ToArray();
                }
            }
        }
    }

    public class EntityData : IMarshallable
    {

        /// <summary>
        /// A list of ALL possible entities to be sent over the network.
        /// If you add a new type of entity, add it here
        /// </summary>
        public enum EntityType
        {
            TestMan = 0
        }

        public EntityType type;
        public TransformData transformData;

                /// <summary>
        /// Convinience Constructor to Marshal Matrix
        /// </summary>
        /// <param name="ID"></param>
        /// <param name="matrix"></param>
        public EntityData(EntityType type, int id, Matrix matrix)
        {
            this.type = type;
            this.transformData = new TransformData(id, matrix);
        }

        /// <summary>
        /// Reconstruct the Class from Network
        /// </summary>
        /// <param name="data"></param>
        public EntityData(byte[] data)
        {
            using (MemoryStream memoryStream = new MemoryStream(data))
            {
                using (BinaryReader binaryReader = new BinaryReader(memoryStream))
                {
                    type = (EntityType)binaryReader.ReadInt32();
                    int entityID = (int)binaryReader.ReadInt32();
                    float[] matrix = new float[16];
                    Matrix Matrix;
                    for (int i = 0; i < 16; i++)
                    {
                        matrix[i] = (float)binaryReader.ReadDouble();
                    }
                    Matrix = new Matrix(matrix[0], matrix[1], matrix[2], matrix[3], matrix[4], matrix[5], matrix[6], matrix[7], matrix[8], matrix[9], matrix[10], matrix[11], matrix[12], matrix[13], matrix[14], matrix[15]);
                    transformData = new TransformData(entityID, Matrix);
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
                    binaryWriter.Write((int)type);
                    binaryWriter.Write((int)transformData.EntityID);
                    foreach (float element in transformData.Matrix)
                        binaryWriter.Write((double)element);

                    return memoryStream.ToArray();
                }
            }
        }

    }
}
