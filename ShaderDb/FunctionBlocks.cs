using FrostySdk.IO;
using System;
using System.Collections.Generic;

namespace ShaderDataPlugin
{
    public class ConstantFunctionBlock
    {
        public class Constant
        {
            // maps to ShaderConstantFunction
            public uint constFunction;
            // index into the cbuffer
            public uint index;
            public uint parameter;
            public uint arraySize;
            public uint matrixDims;

            public Constant(NativeReader reader)
            {
                switch ((ShaderDBVersion)ShaderDb.Version)
                {
                    case ShaderDBVersion.Battlefield4:
                    case ShaderDBVersion.DragonAgeInquisition:
                    case ShaderDBVersion.PvZGardenWarfare1:
                    case ShaderDBVersion.NFSRivals:
                        {
                            constFunction = reader.ReadUInt();
                            index = reader.ReadByte();
                            parameter = reader.ReadByte();
                            arraySize = reader.ReadByte();
                            matrixDims = reader.ReadByte();
                            break;
                        }
                    case ShaderDBVersion.NFS2015_PvZGardenWarfare2:
                    case ShaderDBVersion.StarWarsBattlefront1:
                    case ShaderDBVersion.NFSPayback_MECatalyst:
                    case ShaderDBVersion.Battlefield1:
                    case ShaderDBVersion.BattlefieldV:
                        {
                            constFunction = reader.ReadByte();
                            parameter = reader.ReadByte();
                            index = reader.ReadUShort();
                            arraySize = reader.ReadUShort();
                            matrixDims = reader.ReadUShort();
                            break;
                        }
                    case ShaderDBVersion.Anthem:
                    case ShaderDBVersion.StarWarsSquadrons:
                    case ShaderDBVersion.PvZBattleForNeighborville:
                    case ShaderDBVersion.NFSHeat:
                        {
                            constFunction = reader.ReadByte();
                            parameter = reader.ReadByte();
                            index = reader.ReadUShort();
                            arraySize = reader.ReadUShort();
                            break;
                        }
                    default:
                        break;
                }
            }
        }

        public uint RegisterCount;
        public List<Constant> Constants = new List<Constant>();

        public ConstantFunctionBlock(NativeReader reader)
        {
            uint constantCount = reader.ReadUShort();
            RegisterCount = reader.ReadUShort();

            for (int i = 0; i < constantCount; ++i)
            {
                Constants.Add(new Constant(reader));
            }
        }
    }

    public class TextureFunctionBlock
    {
        public class Texture
        {
            // maps to ShaderConstantFunction
            public uint constFunction;
            // maps to ShaderValueType
            public uint valueType;
            public uint index;
            public uint parameter;

            public Texture(NativeReader reader)
            {
                constFunction = reader.ReadByte();
                valueType = reader.ReadByte();
                index = reader.ReadByte();
                parameter = reader.ReadByte();
            }
        }

        public List<Texture> Textures = new List<Texture>();

        public TextureFunctionBlock(NativeReader reader)
        {
            uint textureCount = reader.ReadUInt();

            for (int i = 0; i < textureCount; ++i)
            {
                Textures.Add(new Texture(reader));
            }
        }
    }

    public class BufferFunctionBlock
    {
        public class Buffer
        {
            // maps to ShaderConstantFunction
            public uint constFunction;
            // maps to ShaderValueType
            public uint valueType;
            public uint index;

            public Buffer(NativeReader reader)
            {
                constFunction = reader.ReadByte();
                valueType = reader.ReadByte();
                index = reader.ReadByte();
            }
        }

        public List<Buffer> Buffers = new List<Buffer>();

        public BufferFunctionBlock(NativeReader reader)
        {
            uint bufferCount = reader.ReadUInt();

            for (int i = 0; i < bufferCount; ++i)
            {
                Buffers.Add(new Buffer(reader));
            }
        }
    }
}