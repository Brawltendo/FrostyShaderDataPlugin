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
    [EbxClassMeta(EbxFieldType.Enum)]
    public enum D3D11_FILTER
    {
        D3D11_FILTER_MIN_MAG_MIP_POINT = 0,
        D3D11_FILTER_MIN_MAG_POINT_MIP_LINEAR = 0x1,
        D3D11_FILTER_MIN_POINT_MAG_LINEAR_MIP_POINT = 0x4,
        D3D11_FILTER_MIN_POINT_MAG_MIP_LINEAR = 0x5,
        D3D11_FILTER_MIN_LINEAR_MAG_MIP_POINT = 0x10,
        D3D11_FILTER_MIN_LINEAR_MAG_POINT_MIP_LINEAR = 0x11,
        D3D11_FILTER_MIN_MAG_LINEAR_MIP_POINT = 0x14,
        D3D11_FILTER_MIN_MAG_MIP_LINEAR = 0x15,
        D3D11_FILTER_ANISOTROPIC = 0x55,
        D3D11_FILTER_COMPARISON_MIN_MAG_MIP_POINT = 0x80,
        D3D11_FILTER_COMPARISON_MIN_MAG_POINT_MIP_LINEAR = 0x81,
        D3D11_FILTER_COMPARISON_MIN_POINT_MAG_LINEAR_MIP_POINT = 0x84,
        D3D11_FILTER_COMPARISON_MIN_POINT_MAG_MIP_LINEAR = 0x85,
        D3D11_FILTER_COMPARISON_MIN_LINEAR_MAG_MIP_POINT = 0x90,
        D3D11_FILTER_COMPARISON_MIN_LINEAR_MAG_POINT_MIP_LINEAR = 0x91,
        D3D11_FILTER_COMPARISON_MIN_MAG_LINEAR_MIP_POINT = 0x94,
        D3D11_FILTER_COMPARISON_MIN_MAG_MIP_LINEAR = 0x95,
        D3D11_FILTER_COMPARISON_ANISOTROPIC = 0xd5
    }

    [EbxClassMeta(EbxFieldType.Enum)]
    public enum D3D11_TEXTURE_ADDRESS_MODE
    {
        D3D11_TEXTURE_ADDRESS_WRAP = 1,
        D3D11_TEXTURE_ADDRESS_MIRROR = 2,
        D3D11_TEXTURE_ADDRESS_CLAMP = 3,
        D3D11_TEXTURE_ADDRESS_BORDER = 4,
        D3D11_TEXTURE_ADDRESS_MIRROR_ONCE = 5
    }

    [EbxClassMeta(EbxFieldType.Enum)]
    public enum D3D11_COMPARISON_FUNC
    {
        D3D11_COMPARISON_NEVER = 1,
        D3D11_COMPARISON_LESS = 2,
        D3D11_COMPARISON_EQUAL = 3,
        D3D11_COMPARISON_LESS_EQUAL = 4,
        D3D11_COMPARISON_GREATER = 5,
        D3D11_COMPARISON_NOT_EQUAL = 6,
        D3D11_COMPARISON_GREATER_EQUAL = 7,
        D3D11_COMPARISON_ALWAYS = 8
    }

    [EbxClassMeta(EbxFieldType.Struct)]
    public class D3D11_SAMPLER_DESC
    {
        [EbxFieldMeta(EbxFieldType.Enum)]
        public D3D11_FILTER Filter { get; set; }
        [EbxFieldMeta(EbxFieldType.Enum)]
        public D3D11_TEXTURE_ADDRESS_MODE AddressU { get; set; }
        [EbxFieldMeta(EbxFieldType.Enum)]
        public D3D11_TEXTURE_ADDRESS_MODE AddressV { get; set; }
        [EbxFieldMeta(EbxFieldType.Enum)]
        public D3D11_TEXTURE_ADDRESS_MODE AddressW { get; set; }
        public float MipLODBias { get; set; }
        public uint MaxAnisotropy { get; set; }
        [EbxFieldMeta(EbxFieldType.Enum)]
        public D3D11_COMPARISON_FUNC ComparisonFunc { get; set; }
        public List<float> BorderColor { get; set; } = new List<float>();
        public float MinLOD { get; set; }
        public float MaxLOD { get; set; }

        public D3D11_SAMPLER_DESC(NativeReader reader)
        {
            Filter = (D3D11_FILTER)reader.ReadInt();
            AddressU = (D3D11_TEXTURE_ADDRESS_MODE)reader.ReadInt();
            AddressV = (D3D11_TEXTURE_ADDRESS_MODE)reader.ReadInt();
            AddressW = (D3D11_TEXTURE_ADDRESS_MODE)reader.ReadInt();
            MipLODBias = reader.ReadFloat();
            MaxAnisotropy = reader.ReadUInt();
            ComparisonFunc = (D3D11_COMPARISON_FUNC)reader.ReadInt();
            BorderColor = new List<float> { reader.ReadFloat(), reader.ReadFloat(), reader.ReadFloat(), reader.ReadFloat() };
            MinLOD = reader.ReadFloat();
            MaxLOD = reader.ReadFloat();
        }
        public D3D11_SAMPLER_DESC()
        {

        }
    }

    [EbxClassMeta(EbxFieldType.Struct)]
    public class SamplerState
    {
        [DisplayName("Index")]
        public uint index { get; set; }
        [DisplayName("Sampler Description")]
        [EbxFieldMeta(EbxFieldType.Struct)]
        public D3D11_SAMPLER_DESC desc { get; set; } = new D3D11_SAMPLER_DESC();
        [DisplayName("State")]
        public ulong state { get; set; }

        public SamplerState(NativeReader reader)
        {
            index = reader.ReadUInt();
            // unknown
            if (ShaderDb.Version == (int)ShaderDBVersion.NFSPayback_MECatalyst && ProfilesLibrary.DataVersion == (int)ProfileVersion.MirrorsEdgeCatalyst)
                reader.ReadUInt();
            desc = new D3D11_SAMPLER_DESC(reader);
            state = reader.ReadULong();
            // unknown
            if (ShaderDb.Version == (int)ShaderDBVersion.NFSPayback_MECatalyst && ProfilesLibrary.DataVersion == (int)ProfileVersion.MirrorsEdgeCatalyst)
                reader.ReadUInt();
        }
    }
}
