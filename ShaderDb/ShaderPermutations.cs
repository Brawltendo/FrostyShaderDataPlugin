using FrostySdk.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShaderDataPlugin
{
    public class ShaderPermutation
    {
        public Guid ShaderBytecodeGuid = default;
        public uint ShaderSize = 0;
        public uint ConstantFunctionBlocksIndex = 0;
        public uint TextureFunctionBlocksIndex = 0;
        public uint BufferFunctionBlocksIndex = 0;

        public long DbOffset = 0;
        public string DbPath = "";

        public ShaderPermutation()
        {
        }

        public ShaderPermutation(NativeReader reader, string pathName)
        {
            // shader GUID
            if (ShaderDb.Version != (int)ShaderDBVersion.Anthem)
                reader.ReadGuid();
            if (ShaderDb.Version == (int)ShaderDBVersion.PvZBattleForNeighborville
            || ShaderDb.Version == (int)ShaderDBVersion.NFSHeat
            || ShaderDb.Version == (int)ShaderDBVersion.NFSUnbound)
            {
                // newer games don't embed bytecode in the database anymore
                // in its place is a GUID which is the name of a resource containing the shader bytecode
                ShaderBytecodeGuid = reader.ReadGuid();
            }
            else
            {
                ShaderSize = reader.ReadUInt();
                // get current offset and db path so we can return here and grab the embedded DXBC bytecode
                DbOffset = reader.Position;
                DbPath = pathName;
                reader.ReadBytes((int)ShaderSize);
            }

            if (ShaderDb.Version != (int)ShaderDBVersion.Anthem)
            {
                // constantsIndex
                reader.ReadUInt();
                ConstantFunctionBlocksIndex = reader.ReadUInt();
                TextureFunctionBlocksIndex = reader.ReadUInt();
                if (ShaderDb.Version > (int)ShaderDBVersion.NFSRivals)
                    BufferFunctionBlocksIndex = reader.ReadUInt();
            }
        }
    }

    public class VertexShaderPermutation : ShaderPermutation
    {
        public VertexShaderPermutation()
        {
        }

        public VertexShaderPermutation(NativeReader reader, string pathName)
            : base(reader, pathName)
        {
            switch ((ShaderDBVersion)ShaderDb.Version)
            {
                case ShaderDBVersion.Battlefield4:
                case ShaderDBVersion.DragonAgeInquisition:
                case ShaderDBVersion.PvZGardenWarfare1:
                case ShaderDBVersion.NFSRivals:
                case ShaderDBVersion.NFS2015_PvZGardenWarfare2:
                case ShaderDBVersion.StarWarsBattlefront1:
                case ShaderDBVersion.NFSPayback_MECatalyst:
                case ShaderDBVersion.MassEffectAndromeda:
                case ShaderDBVersion.Battlefield1:
                case ShaderDBVersion.BattlefieldV:
                    {
                        // more bytecode, not really sure what exactly it's used for
                        uint count = reader.ReadUInt();
                        reader.ReadBytes((int)count);

                        // some struct array; the first member is an int32 that represents the index
                        count = reader.ReadUInt();
                        reader.ReadBytes((int)count * 24);

                        if (ShaderDb.Version < (int)ShaderDBVersion.NFS2015_PvZGardenWarfare2)
                        {
                            // list of strings presumably for each texcoord; haven't seen them say anything other than TEXCOORD
                            count = reader.ReadUInt();
                            for (int strIt = 0; strIt < count; ++strIt)
                                reader.ReadNullTerminatedString();
                        }

                        if (ShaderDb.Version != (int)ShaderDBVersion.NFSRivals)
                        {
                            // some array of 4 byte structs
                            count = reader.ReadUInt();
                            reader.ReadBytes((int)count * 4);
                        }
                        reader.ReadBytes(4);
                        break;
                    }
                case ShaderDBVersion.Anthem:
                case ShaderDBVersion.PvZBattleForNeighborville:
                case ShaderDBVersion.NFSHeat:
                case ShaderDBVersion.NFSUnbound:
                    // permutation ends here for newer games
                    break;
                default:
                    break;
            }
        }
    }

    public class PixelShaderPermutation : ShaderPermutation
    {
        public PixelShaderPermutation()
        {
        }

        public PixelShaderPermutation(NativeReader reader, string pathName)
            : base(reader, pathName)
        {
            switch ((ShaderDBVersion)ShaderDb.Version)
            {
                case ShaderDBVersion.Battlefield4:
                case ShaderDBVersion.DragonAgeInquisition:
                case ShaderDBVersion.PvZGardenWarfare1:
                case ShaderDBVersion.NFSRivals:
                case ShaderDBVersion.NFS2015_PvZGardenWarfare2:
                case ShaderDBVersion.StarWarsBattlefront1:
                case ShaderDBVersion.NFSPayback_MECatalyst:
                case ShaderDBVersion.MassEffectAndromeda:
                case ShaderDBVersion.Battlefield1:
                case ShaderDBVersion.BattlefieldV:
                    // unknown
                    reader.ReadUInt();
                    break;
                case ShaderDBVersion.Anthem:
                case ShaderDBVersion.PvZBattleForNeighborville:
                case ShaderDBVersion.NFSHeat:
                case ShaderDBVersion.NFSUnbound:
                    break;
                default:
                    break;
            }
        }
    }

    public class GeometryShaderPermutation
    {
        public GeometryShaderPermutation()
        {
        }

        public GeometryShaderPermutation(NativeReader reader)
        {
            reader.ReadGuid();
            // size of DXBC bytecode
            uint size = reader.ReadUInt();
            // DXBC bytecode for this shader
            reader.ReadBytes((int)size);
            // unknown data
            reader.ReadBytes(4);
        }
    }

    public class HullShaderPermutation : ShaderPermutation
    {
        public HullShaderPermutation()
        {
        }

        public HullShaderPermutation(NativeReader reader, string pathName)
            : base(reader, pathName)
        {
            switch ((ShaderDBVersion)ShaderDb.Version)
            {
                case ShaderDBVersion.Battlefield4:
                case ShaderDBVersion.DragonAgeInquisition:
                case ShaderDBVersion.PvZGardenWarfare1:
                case ShaderDBVersion.NFSRivals:
                case ShaderDBVersion.NFS2015_PvZGardenWarfare2:
                case ShaderDBVersion.StarWarsBattlefront1:
                case ShaderDBVersion.NFSPayback_MECatalyst:
                case ShaderDBVersion.MassEffectAndromeda:
                case ShaderDBVersion.Battlefield1:
                case ShaderDBVersion.BattlefieldV:
                    // unknown
                    reader.ReadUInt();
                    break;
                case ShaderDBVersion.Anthem:
                case ShaderDBVersion.PvZBattleForNeighborville:
                case ShaderDBVersion.NFSHeat:
                case ShaderDBVersion.NFSUnbound:
                    break;
                default:
                    break;
            }
        }
    }

    public class DomainShaderPermutation : ShaderPermutation
    {
        public DomainShaderPermutation()
        {
        }

        public DomainShaderPermutation(NativeReader reader, string pathName)
            : base(reader, pathName)
        {
            switch ((ShaderDBVersion)ShaderDb.Version)
            {
                case ShaderDBVersion.Battlefield4:
                case ShaderDBVersion.DragonAgeInquisition:
                case ShaderDBVersion.PvZGardenWarfare1:
                case ShaderDBVersion.NFSRivals:
                case ShaderDBVersion.NFS2015_PvZGardenWarfare2:
                case ShaderDBVersion.StarWarsBattlefront1:
                case ShaderDBVersion.NFSPayback_MECatalyst:
                case ShaderDBVersion.MassEffectAndromeda:
                case ShaderDBVersion.Battlefield1:
                case ShaderDBVersion.BattlefieldV:
                    // unknown
                    reader.ReadUInt();
                    break;
                case ShaderDBVersion.Anthem:
                case ShaderDBVersion.PvZBattleForNeighborville:
                case ShaderDBVersion.NFSHeat:
                case ShaderDBVersion.NFSUnbound:
                    break;
                default:
                    break;
            }
        }
    }

    public class AnthemUnkStruct
    {
        public AnthemUnkStruct(NativeReader reader)
        {
            // probably some index
            reader.ReadByte();
            reader.ReadGuid();
            // unknown
            reader.ReadByte();
            reader.ReadByte();
            reader.ReadByte();
            // likely the shader constants index
            reader.ReadUInt();
            // likely the constant function block index
            reader.ReadUInt();
            // likely the texture function block index
            reader.ReadUInt();
            // likely the buffer function block index
            reader.ReadUInt();
        }
    }

    public class AnthemUnkStruct1
    {
        public AnthemUnkStruct1(NativeReader reader)
        {
            // some array of ints
            int count = reader.ReadInt();
            reader.ReadBytes(count * 4);

            // 8 arrays that seem to contain indices
            for (int i = 0; i < 8; ++i)
            {
                count = reader.ReadInt();
                reader.ReadBytes(count * 4);
            }

            // unknown
            count = reader.ReadInt();
            reader.ReadBytes(count);
        }
    }

    public class BFVUnkShaderPermutation : ShaderPermutation
    {
        public BFVUnkShaderPermutation()
        {
        }

        public BFVUnkShaderPermutation(NativeReader reader, string pathName)
            : base(reader, pathName)
        {
            // unknown
            reader.ReadUInt();
            reader.ReadUInt();
        }
    }

    public class BFVUnkShaderPermutation1 : ShaderPermutation
    {
        public BFVUnkShaderPermutation1()
        {
        }

        public BFVUnkShaderPermutation1(NativeReader reader, string pathName)
            : base(reader, pathName)
        {
            // unknown
            reader.ReadUInt();
            reader.ReadUInt();
            reader.ReadUInt();
            reader.ReadUInt();
            reader.ReadUInt();
        }
    }
}
