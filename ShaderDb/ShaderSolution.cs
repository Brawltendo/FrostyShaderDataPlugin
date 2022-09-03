using FrostySdk;
using FrostySdk.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShaderDataPlugin
{
    public class ShaderSolution
    {
        // hash of solution state
        public ulong stateHash;
        public byte unk;
        // maps to SurfaceShaderType
        public byte surfaceType;
        // maps to ShaderBlendMode
        public byte blendMode;
        // maps to DispatchBlendMode
        public byte dispatchBlendMode;
        public uint pad;
        public ulong state;
        public ulong vertexPermutationIndex;
        public ulong pixelPermutationIndex;
        public ulong geometryPermutationIndex;
        public ulong hullPermutationIndex;
        public ulong domainPermutationIndex;
        public ulong vertexConstantsIndex;
        public ulong pixelConstantsIndex;
        public ulong hullConstantsIndex;
        public ulong domainConstantsIndex;
        public ulong geometryConstantsIndex;
        public byte[] data;
        public Guid genericShaderSolutionHash;

        public ShaderSolution(NativeReader reader)
        {
            // BF1 doesn't have a solution state hash
            if (ShaderDb.Version != (int)ShaderDBVersion.Battlefield1)
            {
                if (ShaderDb.Version == (int)ShaderDBVersion.BattlefieldV || (ShaderDb.Version == (int)ShaderDBVersion.NFSPayback_MECatalyst && ProfilesLibrary.DataVersion == (int)ProfileVersion.MirrorsEdgeCatalyst))
                    reader.ReadUInt(); // seems to be some sort of index
                else
                    stateHash = reader.ReadULong();
            }
            if (ShaderDb.Version == (int)ShaderDBVersion.Anthem || ShaderDb.Version == (int)ShaderDBVersion.StarWarsSquadrons || ShaderDb.Version == (int)ShaderDBVersion.PvZBattleForNeighborville || ShaderDb.Version == (int)ShaderDBVersion.NFSHeat)
                reader.ReadUShort();
            else
                unk = reader.ReadByte();

            surfaceType = reader.ReadByte();
            blendMode = reader.ReadByte();
            dispatchBlendMode = reader.ReadByte();
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
                    if (ShaderDb.Version == (int)ShaderDBVersion.NFSPayback_MECatalyst && ProfilesLibrary.DataVersion == (int)ProfileVersion.MirrorsEdgeCatalyst)
                        break;
                    pad = reader.ReadUInt();
                    break;
                case ShaderDBVersion.BattlefieldV:
                    break;
                case ShaderDBVersion.Anthem:
                case ShaderDBVersion.StarWarsSquadrons:
                case ShaderDBVersion.PvZBattleForNeighborville:
                case ShaderDBVersion.NFSHeat:
                    // these games have an extra 3 bytes here
                    reader.ReadBytes(3);
                    break;
                default:
                    break;
            }
            state = reader.ReadULong();
            vertexPermutationIndex = reader.ReadULong();
            pixelPermutationIndex = reader.ReadULong();
            geometryPermutationIndex = reader.ReadULong();
            hullPermutationIndex = reader.ReadULong();
            domainPermutationIndex = reader.ReadULong();
            if (ShaderDb.Version == (int)ShaderDBVersion.BattlefieldV)
            {
                // BFV has extra permutation indices for its new (unknown) permutation types
                reader.ReadULong();
                reader.ReadULong();
                reader.ReadULong();
            }
            vertexConstantsIndex = reader.ReadULong();
            pixelConstantsIndex = reader.ReadULong();
            if (ShaderDb.Version == (int)ShaderDBVersion.DragonAgeInquisition)
            {
                // DAI has 2 extra index refs compared to other FB2013 games, probably for hull and domain constants
                reader.ReadULong();
                reader.ReadULong();
            }
            if (ShaderDb.Version > (int)ShaderDBVersion.NFSRivals)
            {
                hullConstantsIndex = reader.ReadULong();
                domainConstantsIndex = reader.ReadULong();
                geometryConstantsIndex = reader.ReadULong();
                if (ShaderDb.Version == (int)ShaderDBVersion.BattlefieldV)
                {
                    // BFV has extra constants indices for its new (unknown) permutation types
                    reader.ReadULong();
                    reader.ReadULong();
                    reader.ReadULong();
                }
                switch ((ShaderDBVersion)ShaderDb.Version)
                {
                    case ShaderDBVersion.NFS2015_PvZGardenWarfare2:
                    case ShaderDBVersion.StarWarsBattlefront1:
                    case ShaderDBVersion.StarWarsBattlefront2Alpha:
                    case ShaderDBVersion.NFSPayback_MECatalyst:
                    case ShaderDBVersion.MassEffectAndromeda:
                    case ShaderDBVersion.Battlefield1:
                    case ShaderDBVersion.BattlefieldV:
                        if (ShaderDb.Version == (int)ShaderDBVersion.MassEffectAndromeda || ShaderDb.Version == (int)ShaderDBVersion.StarWarsBattlefront2Alpha
                        || (ShaderDb.Version == (int)ShaderDBVersion.NFSPayback_MECatalyst && ProfilesLibrary.DataVersion != (int)ProfileVersion.MirrorsEdgeCatalyst))
                            data = reader.ReadBytes(32);
                        else
                            data = reader.ReadBytes(24);
                        break;
                    case ShaderDBVersion.Anthem:
                        data = reader.ReadBytes(40);
                        break;
                    case ShaderDBVersion.StarWarsSquadrons:
                        data = reader.ReadBytes(32);
                        break;
                    case ShaderDBVersion.PvZBattleForNeighborville:
                    case ShaderDBVersion.NFSHeat:
                        data = reader.ReadBytes(7 * sizeof(ulong));
                        break;
                    default:
                        break;
                }

                genericShaderSolutionHash = reader.ReadGuid();
                if (ShaderDb.Version == (int)ShaderDBVersion.Anthem || ShaderDb.Version == (int)ShaderDBVersion.StarWarsSquadrons)
                    reader.ReadBytes(8);
            }
        }
    }
}
