using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;

namespace OceanMars.Common.NetCode
{
    public enum StateChangeType { CREATE_LEVEL, CREATE_PLAYER }
    public enum StateProperties { ENTITY_ID, PARENT_ID, TRANSFORM, LEVEL_TYPE, SIZE_X, SIZE_Y }

    public class StateChange : IMarshable
    {
        public StateChangeType type;
        public Dictionary<StateProperties, int> intProperties = new Dictionary<StateProperties, int>();
        public Dictionary<StateProperties, String> stringProperties = new Dictionary<StateProperties, String>();
        public Dictionary<StateProperties, double> doubleProperties = new Dictionary<StateProperties, double>();
        public Dictionary<StateProperties, Matrix> matrixProperties = new Dictionary<StateProperties, Matrix>();

        public StateChange() { }

        public byte[] GetByteArray()
        {
            MemoryStream ms = new MemoryStream();
            BinaryWriter bb = new BinaryWriter(ms);
            bb.Write((byte)type);

            bb.Write((byte)intProperties.Count);
            foreach (KeyValuePair<StateProperties, int> kvp in intProperties)
            {
                bb.Write((byte)kvp.Key);
                bb.Write((int)kvp.Value);
            }

            bb.Write((byte)stringProperties.Count);
            foreach (KeyValuePair<StateProperties, String> kvp in stringProperties)
            {
                bb.Write((byte)kvp.Key);
                bb.Write(kvp.Value);
            }

            bb.Write((byte)doubleProperties.Count);
            foreach (KeyValuePair<StateProperties, double> kvp in doubleProperties)
            {
                bb.Write((byte)kvp.Key);
                bb.Write((double)kvp.Value);
            }

            bb.Write((byte)matrixProperties.Count);
            foreach (KeyValuePair<StateProperties, Matrix> kvp in matrixProperties)
            {
                bb.Write((byte)kvp.Key);
                bb.Write((double)kvp.Value.M11);
                bb.Write((double)kvp.Value.M12);
                bb.Write((double)kvp.Value.M13);
                bb.Write((double)kvp.Value.M14);
                bb.Write((double)kvp.Value.M21);
                bb.Write((double)kvp.Value.M22);
                bb.Write((double)kvp.Value.M23);
                bb.Write((double)kvp.Value.M24);
                bb.Write((double)kvp.Value.M31);
                bb.Write((double)kvp.Value.M32);
                bb.Write((double)kvp.Value.M33);
                bb.Write((double)kvp.Value.M34);
                bb.Write((double)kvp.Value.M41);
                bb.Write((double)kvp.Value.M42);
                bb.Write((double)kvp.Value.M43);
                bb.Write((double)kvp.Value.M44);
            }

            return ms.ToArray();
        }

        public StateChange(byte[] buffer)
        {
            MemoryStream ms = new MemoryStream(buffer);
            BinaryReader br = new BinaryReader(ms);

            this.type = (StateChangeType)br.ReadByte();

            byte nums = br.ReadByte();
            for (int i = 0; i < nums; i++)
            {
                this.intProperties[(StateProperties)br.ReadByte()] = br.ReadInt32();
            }

            nums = br.ReadByte();
            for (int i = 0; i < nums; i++)
            {
                this.stringProperties[(StateProperties)br.ReadByte()] = br.ReadString();
            }

            nums = br.ReadByte();
            for (int i = 0; i < nums; i++)
            {
                this.doubleProperties[(StateProperties)br.ReadByte()] = br.ReadDouble();
            }

            nums = br.ReadByte();
            for (int i = 0; i < nums; i++)
            {
                StateProperties sp = (StateProperties)br.ReadByte();
                Matrix n = new Matrix((float)br.ReadDouble(), (float)br.ReadDouble(), (float)br.ReadDouble(), (float)br.ReadDouble(),
                    (float)br.ReadDouble(), (float)br.ReadDouble(), (float)br.ReadDouble(), (float)br.ReadDouble(),
                    (float)br.ReadDouble(), (float)br.ReadDouble(), (float)br.ReadDouble(), (float)br.ReadDouble(),
                    (float)br.ReadDouble(), (float)br.ReadDouble(), (float)br.ReadDouble(), (float)br.ReadDouble());
                this.matrixProperties[sp] = n;
            }
        }

        public static StateChange GetStateData(byte[] b)
        {
            StateChange ret = new StateChange(b);

            return ret;
        }
    }
}
