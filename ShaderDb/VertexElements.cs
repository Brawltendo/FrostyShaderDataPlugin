using FrostySdk;
using FrostySdk.Attributes;
using FrostySdk.Ebx;
using FrostySdk.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShaderDataPlugin
{
    /// <summary>
    /// Base class to allow the property grid to diverge per game
    /// </summary>
    public class VertexElementBase
    {
    }

    [EbxClassMeta(EbxFieldType.Struct)]
    public class VertexElement_Old : VertexElementBase
    {
        [EbxClassMeta(EbxFieldType.Struct)]
        public class VertexStream
        {
            [IsReadOnly]
            [EbxFieldMeta(EbxFieldType.UInt32)]
            public uint Stride => stride;

            [IsReadOnly]
            [EbxFieldMeta(EbxFieldType.CString)]
            public CString Classification => TypeLibrary.GetType("VertexElementClassification").GetEnumName(classification);

            public byte stride;
            // maps to VertexElementClassification
            public byte classification;

            public VertexStream()
            {
            }
        }

        [IsReadOnly]
        [EbxFieldMeta(EbxFieldType.CString)]
        public CString Usage => TypeLibrary.GetType("VertexElementUsage").GetEnumName(usage);

        [IsReadOnly]
        [EbxFieldMeta(EbxFieldType.CString)]
        public CString Format => TypeLibrary.GetType("VertexElementFormat").GetEnumName(format);

        [IsReadOnly]
        public byte Offset => offset;

        [IsReadOnly]
        public byte StreamIndex => streamIndex;

        [IsReadOnly]
        [EbxFieldMeta(EbxFieldType.Struct)]
        public VertexStream Stream { get; set; } = new VertexStream();

        // maps to VertexElementUsage
        public byte usage;
        // maps to VertexElementFormat
        public byte format;
        public byte offset;
        public byte streamIndex;

        public VertexElement_Old()
        {
        }

    }

    [EbxClassMeta(EbxFieldType.Struct)]
    public class VertexElement_New : VertexElementBase
    {

        [IsReadOnly]
        [EbxFieldMeta(EbxFieldType.CString)]
        public CString Format => TypeLibrary.GetType("VertexElementFormat").GetEnumName(format);

        [IsReadOnly]
        [EbxFieldMeta(EbxFieldType.CString)]
        public CString Classification => TypeLibrary.GetType("VertexElementClassification").GetEnumName(classification);

        [IsReadOnly]
        public byte Offset => offset;

        [IsReadOnly]
        public byte StreamIndex => streamIndex;

        // maps to VertexElementFormat
        public byte format;
        // maps to VertexElementClassification
        public byte classification;
        public byte offset;
        public byte streamIndex;

        public VertexElement_New()
        {
        }

    }
}
