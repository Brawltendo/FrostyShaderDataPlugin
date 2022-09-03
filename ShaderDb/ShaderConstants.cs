using FrostySdk;
using FrostySdk.Attributes;
using FrostySdk.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShaderDataPlugin
{
    public class TextureConstant
    {
        public byte index { get; set; }
        public byte textureType { get; set; }
        // 144 bytes long
        public string name { get; set; }
        public uint nameHash { get; set; }

        public TextureConstant(NativeReader reader)
        {
            ShaderDBVersion ver = (ShaderDBVersion)ShaderDb.Version;
            if (ver == ShaderDBVersion.StarWarsBattlefront2Alpha
            || (ver == ShaderDBVersion.NFSPayback_MECatalyst && ProfilesLibrary.DataVersion != (int)ProfileVersion.MirrorsEdgeCatalyst) // they use the same version, yet here we are
            || ver == ShaderDBVersion.StarWarsSquadrons
            || ver == ShaderDBVersion.PvZBattleForNeighborville
            || ver == ShaderDBVersion.NFSHeat)
            {
                nameHash = reader.ReadUInt();
                index = reader.ReadByte();
                textureType = reader.ReadByte();
                reader.ReadUShort();
                reader.ReadULong();
            }
            else
            {
                index = reader.ReadByte();
                textureType = reader.ReadByte();
                reader.Position += 6;
                // the name is actually a 144 byte string but we can just read it like this and advance the stream so it's easier to pass to the UI later
                long oldPos = reader.Position;
                name = reader.ReadNullTerminatedString();
                reader.Position = oldPos + 144;
            }
        }
    }

    public class ExternalValueConstant
    {
        // 32 bytes long
        public string name { get; set; }
        // param name hash
        public uint handle { get; set; }
        public ushort index { get; set; }
        public ushort arraySize { get; set; }
        public byte size { get; set; }
        public byte required { get; set; }
        public byte type { get; set; }
        // vec4 / array of 4 floats
        public dynamic defaultValue { get; set; }

        public ExternalValueConstant(NativeReader reader)
        {
            // the name is actually a 32 byte string but we can just read it like this and advance the stream so it's easier to pass to the UI later
            long oldPos = reader.Position;
            name = reader.ReadNullTerminatedString();
            reader.Position = oldPos + 32;

            handle = reader.ReadUInt();
            index = reader.ReadUShort();
            arraySize = reader.ReadUShort();
            size = reader.ReadByte();
            required = reader.ReadByte();
            type = reader.ReadByte();
            reader.Position += 1;
            defaultValue = TypeLibrary.CreateObject("Vec4");
            defaultValue.x = reader.ReadFloat();
            defaultValue.y = reader.ReadFloat();
            defaultValue.z = reader.ReadFloat();
            defaultValue.w = reader.ReadFloat();
        }
    }

    public class ExternalTextureConstant
    {
        // 32 bytes long
        public string name { get; set; }
        // param name hash
        public uint handle { get; set; }
        public ushort index { get; set; }
        public byte textureType { get; set; }
        public byte flags { get; set; }

        public ExternalTextureConstant(NativeReader reader)
        {
            // the name is actually a 32 byte string but we can just read it like this and advance the stream so it's easier to pass to the UI later
            long oldPos = reader.Position;
            name = reader.ReadNullTerminatedString();
            reader.Position = oldPos + 32;

            handle = reader.ReadUInt();
            index = reader.ReadUShort();
            if ((ShaderDBVersion)ShaderDb.Version == ShaderDBVersion.StarWarsSquadrons)
                reader.ReadUShort();
            textureType = reader.ReadByte();
            flags = reader.ReadByte();
            if ((ShaderDBVersion)ShaderDb.Version == ShaderDBVersion.StarWarsSquadrons)
                reader.ReadUShort();
        }
    }

    public class ExternalBufferConstant
    {
        // 32 bytes long
        public string name { get; set; }
        // name hash
        public uint handle { get; set; }
        public ushort index { get; set; }
        public byte valueType { get; set; }
        public bool required { get; set; }

        public ExternalBufferConstant(NativeReader reader)
        {
            // the name is actually a 32 byte string but we can just read it like this and advance the stream so it's easier to pass to the UI later
            long oldPos = reader.Position;
            name = reader.ReadNullTerminatedString();
            reader.Position = oldPos + 32;

            handle = reader.ReadUInt();
            index = reader.ReadUShort();
            valueType = reader.ReadByte();
            required = reader.ReadBoolean();
        }
    }

    public class GenericShaderConstants
    {
        public uint size;
        public uint pad;
        public ulong valueConstantsOffset;
        public ulong textureConstantsOffset;
        public ulong externalValueConstantsOffset;
        public ulong externalTextureConstantsOffset;
        public ulong externalBufferConstantsOffset;
        public ulong samplerStatesOffset;
        public ushort constantCount;
        public ushort valueConstantStart;
        public byte valueConstantCount;
        public byte textureConstantCount;
        public byte externalValueConstantCount;
        public byte externalTextureConstantCount;
        public byte externalBufferConstantCount;
        public byte samplerStateCount;
        // vec4
        public List<dynamic> valueConstants = new List<dynamic>();
        public List<TextureConstant> textureConstants = new List<TextureConstant>();
        public List<ExternalValueConstant> externalValueConstants = new List<ExternalValueConstant>();
        public List<ExternalTextureConstant> externalTextureConstants = new List<ExternalTextureConstant>();
        public List<ExternalBufferConstant> externalBufferConstants = new List<ExternalBufferConstant>();
        public List<SamplerState> samplerStates = new List<SamplerState>();

        public GenericShaderConstants(NativeReader reader)
        {
            ulong origPos = (ulong)reader.Position;
            size = reader.ReadUInt();
            pad = reader.ReadUInt();

            valueConstantsOffset = reader.ReadULong();
            textureConstantsOffset = reader.ReadULong();
            externalValueConstantsOffset = reader.ReadULong();
            externalTextureConstantsOffset = reader.ReadULong();
            if ((ShaderDBVersion)ShaderDb.Version > ShaderDBVersion.NFSRivals || (ShaderDBVersion)ShaderDb.Version == ShaderDBVersion.DragonAgeInquisition)
                externalBufferConstantsOffset = reader.ReadULong();
            samplerStatesOffset = reader.ReadULong();

            constantCount = reader.ReadUShort();
            valueConstantStart = reader.ReadUShort();
            valueConstantCount = reader.ReadByte();
            textureConstantCount = reader.ReadByte();
            externalValueConstantCount = reader.ReadByte();
            externalTextureConstantCount = reader.ReadByte();
            if ((ShaderDBVersion)ShaderDb.Version > ShaderDBVersion.NFSRivals || (ShaderDBVersion)ShaderDb.Version == ShaderDBVersion.DragonAgeInquisition)
                externalBufferConstantCount = reader.ReadByte();
            samplerStateCount = reader.ReadByte();

            // get constant values
            reader.Position = (long)(origPos + valueConstantsOffset);
            for (int i = 0; i < valueConstantCount; ++i)
            {
                dynamic val = TypeLibrary.CreateObject("Vec4");
                val.x = reader.ReadFloat();
                val.y = reader.ReadFloat();
                val.z = reader.ReadFloat();
                val.w = reader.ReadFloat();
                valueConstants.Add(val);
            }

            // get constant textures
            reader.Position = (long)(origPos + textureConstantsOffset);
            for (int i = 0; i < textureConstantCount; ++i)
            {
                TextureConstant tex = new TextureConstant(reader);
                textureConstants.Add(tex);
            }

            // get external values
            reader.Position = (long)(origPos + externalValueConstantsOffset);
            for (int i = 0; i < externalValueConstantCount; ++i)
            {
                ExternalValueConstant ext = new ExternalValueConstant(reader);
                externalValueConstants.Add(ext);
            }

            // get external textures
            reader.Position = (long)(origPos + externalTextureConstantsOffset);
            for (int i = 0; i < externalTextureConstantCount; ++i)
            {
                ExternalTextureConstant ext = new ExternalTextureConstant(reader);
                externalTextureConstants.Add(ext);
            }

            // get external buffers
            if (ShaderDb.Version > 198 || (ShaderDBVersion)ShaderDb.Version == ShaderDBVersion.DragonAgeInquisition)
            {
                reader.Position = (long)(origPos + externalBufferConstantsOffset);
                for (int i = 0; i < externalBufferConstantCount; ++i)
                {
                    ExternalBufferConstant ext = new ExternalBufferConstant(reader);
                    externalBufferConstants.Add(ext);
                }
            }

            // get sampler states
            reader.Position = (long)(origPos + samplerStatesOffset);
            for (int i = 0; i < samplerStateCount; ++i)
            {
                SamplerState state = new SamplerState(reader);
                samplerStates.Add(state);
            }

            reader.Position = (long)(origPos + size);
        }
    }
}
