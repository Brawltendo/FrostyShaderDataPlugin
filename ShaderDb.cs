using Frosty.Core;
using Frosty.Core.Controls;
using Frosty.Core.Windows;
using FrostySdk;
using FrostySdk.Attributes;
using FrostySdk.Ebx;
using FrostySdk.Interfaces;
using FrostySdk.IO;
using FrostySdk.Managers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;

namespace ShaderDataPlugin
{

    public enum ShaderType
    {
        PixelShader,
        VertexShader
    }

    public class ShaderDb
    {

        #region Properties
        public bool Loaded { get; private set; }
        private int numStreams
        {
            get
            {
                if (Version == (int)ShaderDBVersion.StarWarsBattlefront1)
                {
                    return 6;
                }
                else if (Version >= (int)ShaderDBVersion.StarWarsBattlefront2Alpha)
                {
                    if (Version == (int)ShaderDBVersion.MassEffectAndromeda
                    || Version == (int)ShaderDBVersion.Battlefield1
                    || Version == (int)ShaderDBVersion.BattlefieldV
                    || ProfilesLibrary.DataVersion == (int)ProfileVersion.MirrorsEdgeCatalyst)
                        return 8;
                    else
                        return 16;
                }
                else
                {
                    return 4;
                }
            }
        }
        #endregion

        #region Fields
        public static uint Version = 150;
        public static bool UseCache = false;

        private List<BundleEntry> entries;
        private AssetEntry assetEntry;
        private ILogger logger;

        // cached data so subsequent loads are faster
        private static Dictionary<uint, List<RenderPath>> shaderMap = new Dictionary<uint, List<RenderPath>>();
        private static Dictionary<uint, string> textureHashMap = new Dictionary<uint, string>();

        private uint dbSize = 0;
        static bool firstTimeLoad = true;
        #endregion

        public ShaderDb(List<BundleEntry> entriesToSearch, AssetEntry asset, ILogger inLogger)
        {
            entries = entriesToSearch;
            assetEntry = asset;
            logger = inLogger;
        }

        /// <summary>
        /// Loads ShaderDb data into memory on the initial load. Subsequent loads pull from the built cache
        /// </summary>
        /// <param name="targetData">The data to be displayed in the property grid</param>
        /// <param name="task"></param>
        public void Load(ShaderGraphData targetData, FrostyTaskWindow task)
        {
            targetData.Guid = ((EbxAssetEntry)assetEntry).Guid;
            targetData.Name = assetEntry.Name;

            // on the first load, build a dictionary with all the necessary shader info
            // also load the texture hash cache for the games that need it
            if (firstTimeLoad)
            {
                if (UseCache)
                {
                    string cachePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "ShaderDataPlugin");
                    cachePath = Path.Combine(cachePath, ((ProfileVersion)ProfilesLibrary.DataVersion).ToString());
                    cachePath = Path.Combine(cachePath, "textures.cache");

                    using (NativeReader cache = new NativeReader(File.Open(cachePath, FileMode.Open, FileAccess.Read, FileShare.Read)))
                    {
                        task.Update("Loading texture hash map");
                        int numTextures = cache.ReadInt();
                        for (int i = 0; i < numTextures; ++i)
                        {
                            task.Update(progress: (i / (double)numTextures) * 100.0);
                            uint hash = cache.ReadUInt();
                            Guid guid = cache.ReadGuid();

                            EbxAssetEntry asset = App.AssetManager.GetEbxEntry(guid);
                            textureHashMap.Add(hash, asset.Name);
                        }
                    }
                }

                IEnumerable<ResAssetEntry> shaderDbs = App.AssetManager.EnumerateRes((uint)Utils.HashString("IShaderDatabase", true));
                int dbCount = shaderDbs.Count();
                int progress = 0;
                foreach (ResAssetEntry resEntry in shaderDbs)
                {
                    task.Update($"Loading database: {resEntry.Name}", (progress++ / (double)dbCount) * 100.0);
                    Read(resEntry, targetData);
                    if (!Loaded)
                    {
                        logger.Log($"A shader database has failed to load");
                        break;
                    }
                }
                firstTimeLoad = false;
            }

            uint shaderHash = (uint)Utils.HashString(assetEntry.Name, true);
            if (shaderMap.ContainsKey(shaderHash))
            {
                Loaded = true;
                targetData.RenderPaths = shaderMap[shaderHash];
            }
            else
            {
                Loaded = false;
                logger.Log($"Shader {assetEntry.Filename} not found in any database");
            }
        }

        private void Read(ResAssetEntry db, ShaderGraphData targetData)
        {
            Stream resStream = App.AssetManager.GetRes(db);
            using (NativeReader reader = new NativeReader(resStream))
            {
                uint numRenderPaths;

                // Anthem uses the res meta as the database header
                // and this is just the beginning of many Anthem exclusive changes...
                if (ProfilesLibrary.DataVersion == (int)ProfileVersion.Anthem)
                {
                    var pathBytes = db.ResMeta.Take(4).ToArray();
                    numRenderPaths = BitConverter.ToUInt32(pathBytes, 0);
                    // db version is the last int in the res meta
                    Version = BitConverter.ToUInt32(db.ResMeta, 12);
                }
                else
                {
                    numRenderPaths = reader.ReadUInt();
                }

                for (int pathIt = 0; pathIt < numRenderPaths; ++pathIt)
                {
                    // get render path header data
                    uint renderPath = reader.ReadUInt();
                    if (ProfilesLibrary.DataVersion == (int)ProfileVersion.Anthem)
                    {
                        // quality level
                        reader.ReadUInt();
                        dbSize = reader.ReadUInt();
                    }
                    else
                    {
                        dbSize = reader.ReadUInt();
                        Version = reader.ReadUInt();
                        // path
                        reader.ReadUInt();
                        // quality level
                        reader.ReadUInt();
                        // BFV has an extra byte in the render path header
                        if ((ShaderDBVersion)Version == ShaderDBVersion.BattlefieldV)
                            reader.ReadByte();
                    }

                    switch ((ShaderDBVersion)Version)
                    {
                        case ShaderDBVersion.Battlefield4:
                        case ShaderDBVersion.DragonAgeInquisition:
                        case ShaderDBVersion.PvZGardenWarfare1:
                        case ShaderDBVersion.NFSRivals:
                        case ShaderDBVersion.NFS2015_PvZGardenWarfare2:
                        case ShaderDBVersion.StarWarsBattlefront1:
                        case ShaderDBVersion.StarWarsBattlefront2Alpha:
                        case ShaderDBVersion.NFSPayback_MECatalyst:
                        case ShaderDBVersion.MassEffectAndromeda:
                        case ShaderDBVersion.Battlefield1:
                        case ShaderDBVersion.BattlefieldV:
                        case ShaderDBVersion.Anthem:
                        case ShaderDBVersion.PvZBattleForNeighborville:
                        case ShaderDBVersion.NFSHeat:
                            break;
                        default:
                            Loaded = false;
                            logger.Log($"Shader database version {Version} is not supported at this time");
                            return;
                    }

                    // get constants for this database
                    uint shaderConstantsCount = reader.ReadUInt();
                    List<GenericShaderConstants> shaderConstants = new List<GenericShaderConstants>();
                    for (int i = 0; i < shaderConstantsCount; ++i)
                        shaderConstants.Add(new GenericShaderConstants(reader));

                    if (Version == (int)ShaderDBVersion.Anthem)
                        reader.ReadUInt();

                    uint constantFunctionBlocksCount = reader.ReadUInt();
                    List<ConstantFunctionBlock> constantFunctionBlocks = new List<ConstantFunctionBlock>();
                    // get constant function blocks
                    for (int i = 0; i < constantFunctionBlocksCount; ++i)
                        constantFunctionBlocks.Add(new ConstantFunctionBlock(reader));

                    if (Version == (int)ShaderDBVersion.Anthem)
                        reader.ReadUInt();

                    uint textureFunctionBlocksCount = reader.ReadUInt();
                    List<TextureFunctionBlock> textureFunctionBlocks = new List<TextureFunctionBlock>();
                    // get texture function blocks
                    for (int i = 0; i < textureFunctionBlocksCount; ++i)
                        textureFunctionBlocks.Add(new TextureFunctionBlock(reader));

                    List<BufferFunctionBlock> bufferFunctionBlocks = new List<BufferFunctionBlock>();
                    // FB2013 games (BF4, PvZ GW1, NFS Rivals, etc.) don't have buffer function blocks
                    if (Version > (int)ShaderDBVersion.NFSRivals)
                    {
                        if (Version == (int)ShaderDBVersion.Anthem)
                            reader.ReadUInt();

                        uint bufferFunctionBlocksCount = reader.ReadUInt();
                        // get buffer function blocks
                        for (int i = 0; i < bufferFunctionBlocksCount; ++i)
                            bufferFunctionBlocks.Add(new BufferFunctionBlock(reader));
                    }

                    //
                    // read all shader permutations
                    // Anthem has an extra array of structs after each permutation array
                    //

                    // get vertex shader permutations
                    uint vertexShaderPermutationCount = reader.ReadUInt();
                    List<ShaderPermutation> vsPermutations = new List<ShaderPermutation>();
                    for (int i = 0; i < vertexShaderPermutationCount; ++i)
                        vsPermutations.Add(new VertexShaderPermutation(reader, db.Name));

                    if (Version == (int)ShaderDBVersion.Anthem)
                    {
                        uint count = reader.ReadUInt();
                        for (int i = 0; i < count; ++i)
                            new AnthemUnkStruct(reader);
                    }

                    // get pixel shader permutations
                    uint pixelShaderPermutationCount = reader.ReadUInt();
                    List<ShaderPermutation> psPermutations = new List<ShaderPermutation>();
                    for (int i = 0; i < pixelShaderPermutationCount; ++i)
                        psPermutations.Add(new PixelShaderPermutation(reader, db.Name));

                    if (Version == (int)ShaderDBVersion.Anthem)
                    {
                        uint count = reader.ReadUInt();
                        for (int i = 0; i < count; ++i)
                            new AnthemUnkStruct(reader);
                    }

                    // read geometry shader permutations
                    uint geometryShaderPermutationCount = reader.ReadUInt();
                    for (int i = 0; i < geometryShaderPermutationCount; ++i)
                        new GeometryShaderPermutation(reader);

                    if (Version == (int)ShaderDBVersion.Anthem)
                    {
                        uint count = reader.ReadUInt();
                        for (int i = 0; i < count; ++i)
                            new AnthemUnkStruct(reader);
                    }

                    // read hull shader permutations
                    uint hullShaderPermutationCount = reader.ReadUInt();
                    for (int i = 0; i < hullShaderPermutationCount; ++i)
                        new HullShaderPermutation(reader, db.Name);

                    if (Version == (int)ShaderDBVersion.Anthem)
                    {
                        uint count = reader.ReadUInt();
                        for (int i = 0; i < count; ++i)
                            new AnthemUnkStruct(reader);
                    }

                    // get domain shader permutations
                    uint domainShaderPermutationCount = reader.ReadUInt();
                    for (int i = 0; i < domainShaderPermutationCount; ++i)
                        new DomainShaderPermutation(reader, db.Name);

                    if (Version == (int)ShaderDBVersion.Anthem)
                    {
                        uint count = reader.ReadUInt();
                        for (int i = 0; i < count; ++i)
                            new AnthemUnkStruct(reader);

                        count = reader.ReadUInt();
                        for (int i = 0; i < count; ++i)
                            new AnthemUnkStruct1(reader);
                    }

                    // BFV has 3 new arrays of shader permutations, not sure what they're for
                    if (Version == (int)ShaderDBVersion.BattlefieldV)
                    {
                        uint count = reader.ReadUInt();
                        for (int i = 0; i < count; ++i)
                            new BFVUnkShaderPermutation(reader, db.Name);

                        // these next 2 permutation arrays contain DXIL data
                        count = reader.ReadUInt();
                        for (int i = 0; i < count; ++i)
                            new BFVUnkShaderPermutation1(reader, db.Name);

                        count = reader.ReadUInt();
                        for (int i = 0; i < count; ++i)
                            new BFVUnkShaderPermutation1(reader, db.Name);
                    }

                    // get shader solutions
                    uint solutionCount = reader.ReadUInt();
                    List<ShaderSolution> solutions = new List<ShaderSolution>();
                    for (int i = 0; i < solutionCount; ++i)
                        solutions.Add(new ShaderSolution(reader));

                    // get all geometry declaration hashes from the shader solution states
                    // this is the only data needed from them so that the geometry declarations can be looked up later
                    uint stateCount = reader.ReadUInt();
                    List<uint> geomDeclLookupList = new List<uint>();
                    for (int i = 0; i < stateCount; ++i)
                    {
                        // surfaceShaderNameHash
                        reader.ReadUInt();
                        // vertexShaderFragmentNameHash
                        reader.ReadUInt();
                        // tessellationShaderFragmentNameHash
                        reader.ReadUInt();

                        // geometryDeclarationHash
                        // since newer games don't look up vertex elements with this hash, we can skip adding them to the list
                        if (Version >= (int)ShaderDBVersion.Anthem)
                            reader.ReadUInt();
                        else
                            geomDeclLookupList.Add(reader.ReadUInt());

                        // renderStateFlags
                        reader.ReadUInt();
                        // unknown data
                        reader.ReadBytes(12);
                    }

                    uint geometryDeclarationsCount = reader.ReadUInt();
                    Dictionary<uint, List<VertexElementBase>> geomDecls = new Dictionary<uint, List<VertexElementBase>>();
                    // get geometry declarations
                    for (int i = 0; i < geometryDeclarationsCount; ++i)
                    {
                        List<VertexElementBase> elements = new List<VertexElementBase>();

                        if (Version < (int)ShaderDBVersion.Anthem || Version == (int)ShaderDBVersion.StarWarsSquadrons)
                        {
                            uint elementsHash = reader.ReadUInt();

                            // skip over the elements and streams and store the offset before both so we can grab their counts and return to them later
                            long elementsOffset = reader.Position;
                            reader.ReadBytes(64); // struct is 4 bytes, and the array has 16 elements
                            long streamsOffset = reader.Position;
                            reader.ReadBytes(2 * numStreams); // 2 byte struct

                            byte elementCount = reader.ReadByte();
                            byte streamCount = reader.ReadByte();
                            // unknown
                            reader.ReadUShort();
                            long endOffset = reader.Position;

                            // read streams first so we can look them up in a list when reading the elements
                            reader.Position = streamsOffset;
                            List<VertexElement_Old.VertexStream> streams = new List<VertexElement_Old.VertexStream>();
                            // streamCount is unreliable because for some reason it doesn't always give the true number of streams
                            // so instead we can fall back to NumStreams and discard any null streams (when stride and classification are zero)
                            for (int streamIt = 0; streamIt < numStreams; ++streamIt)
                            {
                                VertexElement_Old.VertexStream stream = new VertexElement_Old.VertexStream();
                                stream.stride = reader.ReadByte();
                                stream.classification = reader.ReadByte();
                                // the vertex stride can never be zero or else the vertex stream wouldn't be able to advance, so we can use this to discard null streams
                                if (stream.stride != 0)
                                    streams.Add(stream);
                            }

                            reader.Position = elementsOffset;
                            for (int elemIt = 0; elemIt < elementCount; ++elemIt)
                            {
                                VertexElement_Old element = new VertexElement_Old();
                                element.usage = reader.ReadByte();
                                element.format = reader.ReadByte();
                                element.offset = reader.ReadByte();
                                element.streamIndex = reader.ReadByte();
                                element.Stream = streams[element.streamIndex];
                                elements.Add(element);
                            }

                            // return to the end position
                            reader.Position = endOffset;
                            geomDecls.Add(elementsHash, elements);
                        }
                        else
                        {
                            // newer games don't have a separate streams array and the vertex element struct is slightly different
                            // there also isn't an element hash so the vertex element lookup is handled differently

                            uint elementCount = reader.ReadUInt();
                            for (int elemIt = 0; elemIt < elementCount; ++elemIt)
                            {
                                VertexElement_New element = new VertexElement_New();
                                element.format = reader.ReadByte();
                                element.classification = reader.ReadByte();
                                element.offset = reader.ReadByte();
                                element.streamIndex = reader.ReadByte();
                                elements.Add(element);
                            }

                            // skip the rest of the elements since they're empty and don't need to be stored
                            reader.ReadBytes((int)(64 - (elementCount * 4)));
                            // just do it like this so we don't have to use a separate list
                            geomDecls.Add((uint)i, elements);
                        }

                    }

                    // database versions >=249 all have a list of indices that map to the geometry declaration that a shader uses
                    if (Version > (int)ShaderDBVersion.BattlefieldV)
                    {
                        uint count = reader.ReadUInt();
                        for (int i = 0; i < count; ++i)
                            geomDeclLookupList.Add(reader.ReadUInt());
                    }

                    uint shaderCount = reader.ReadUInt();
                    Dictionary<uint, List<uint>> surfaceShaderSolutionMap = new Dictionary<uint, List<uint>>();
                    // get shader map
                    for (int i = 0; i < shaderCount; ++i)
                    {
                        uint key = reader.ReadUInt();

                        // shaderType, maps to SurfaceShaderType
                        if (Version != (int)ShaderDBVersion.MassEffectAndromeda && Version != (int)ShaderDBVersion.BattlefieldV)
                            reader.ReadUInt();
                        else
                            reader.ReadByte();
                        // unknown data
                        switch ((ShaderDBVersion)Version)
                        {
                            case ShaderDBVersion.Battlefield4:
                            case ShaderDBVersion.DragonAgeInquisition:
                            case ShaderDBVersion.PvZGardenWarfare1:
                            case ShaderDBVersion.NFSRivals:
                            case ShaderDBVersion.NFS2015_PvZGardenWarfare2:
                            case ShaderDBVersion.StarWarsBattlefront1:
                            case ShaderDBVersion.NFSPayback_MECatalyst:
                            case ShaderDBVersion.Battlefield1:
                                // Certain games have 4 more bytes in whatever this data is
                                if (ProfilesLibrary.DataVersion == (int)ProfileVersion.NeedForSpeed || Version == (int)ShaderDBVersion.StarWarsBattlefront1)
                                    reader.ReadBytes(40);
                                else
                                    reader.ReadBytes(36);
                                break;
                            case ShaderDBVersion.BattlefieldV:
                                reader.ReadBytes(39);
                                break;
                            case ShaderDBVersion.StarWarsBattlefront2Alpha:
                            case ShaderDBVersion.MassEffectAndromeda:
                            case ShaderDBVersion.Anthem:
                            case ShaderDBVersion.StarWarsSquadrons:
                            case ShaderDBVersion.PvZBattleForNeighborville:
                            case ShaderDBVersion.NFSHeat:
                                reader.ReadBytes(104);
                                break;
                            default:
                                break;
                        }


                        int count = reader.ReadInt();
                        int size = (Version == (int)ShaderDBVersion.DragonAgeInquisition || Version == (int)ShaderDBVersion.MassEffectAndromeda) ? 17 : 16;
                        // streamableTextures:
                        // - (4 bytes) nameHash, uint
                        // - (4 bytes) coordType, maps to ShaderTextureCoordType
                        // - (4 bytes) vertexUsage, maps to VertexElementUsage
                        // - (4 bytes) textureTilesPerCoord, float
                        // - (1 byte)  unknown, both DAI and MEA have it
                        reader.ReadBytes(count * size);

                        count = reader.ReadInt();
                        size = (Version == (int)ShaderDBVersion.DragonAgeInquisition || Version == (int)ShaderDBVersion.MassEffectAndromeda) ? 21 : 20;
                        // streamableExternalTextures:
                        // - (4 bytes) nameHash, uint
                        // - (4 bytes) textureHandle, uint
                        // - (4 bytes) coordType, maps to ShaderTextureCoordType
                        // - (4 bytes) vertexUsage, maps to VertexElementUsage
                        // - (4 bytes) textureTilesPerCoord, float
                        // - (1 byte)  unknown, both DAI and MEA have it
                        reader.ReadBytes(count * size);

                        count = reader.ReadInt();
                        List<uint> solutionIndices = new List<uint>();
                        for (int j = 0; j < count; ++j)
                        {
                            // FB2013 games store these indices as uint16 values
                            if (Version <= (int)ShaderDBVersion.NFSRivals)
                                solutionIndices.Add(reader.ReadUShort());
                            else
                            {
                                try
                                {
                                    solutionIndices.Add(reader.ReadUInt());
                                }
                                catch
                                {
                                    throw new Exception($"Encountered error in database: {db.Name}");
                                }
                            }
                        }
                        surfaceShaderSolutionMap.Add(key, solutionIndices);
                    }

                    try
                    {
                        foreach (uint key in surfaceShaderSolutionMap.Keys)
                        {
                            List<uint> indices = surfaceShaderSolutionMap[key];

                            if (!shaderMap.ContainsKey(key))
                            {
                                List<RenderPath> paths = new List<RenderPath>();
                                shaderMap.Add(key, paths);
                            }
                            string pathName = TypeLibrary.GetType("ShaderRenderPath").GetEnumName(renderPath);


                            List<PermutationPair> permutationPairs;
                            try
                            {
                                permutationPairs = shaderMap[key].Find(x => x.RenderPathName == pathName).PermutationPairs;
                            }
                            catch
                            {
                                RenderPath path = new RenderPath() { RenderPathName = pathName };
                                shaderMap[key].Add(path);
                                permutationPairs = path.PermutationPairs;
                            }

                            // fill in shader data
                            for (int i = 0; i < indices.Count; ++i)
                            {
                                int solutionIndex = (int)indices[i];
                                PermutationPair pair = new PermutationPair();
                                pair.ps = new ShaderGraphPermutation();
                                pair.vs = new ShaderGraphPermutation();
                                List<VertexElementBase> elems;
                                if (Version > (int)ShaderDBVersion.BattlefieldV)
                                    elems = geomDecls[geomDeclLookupList[(int)solutions[solutionIndex].vertexPermutationIndex]];
                                else
                                    elems = geomDecls[geomDeclLookupList[solutionIndex]];

                                pair.PixelShader.VertexElements = elems;
                                pair.ps.shaderDataLookup = psPermutations[(int)solutions[solutionIndex].pixelPermutationIndex];
                                GetShaderPermutation(shaderConstants[(int)solutions[solutionIndex].pixelConstantsIndex],
                                    constantFunctionBlocks.Count > 0 ? constantFunctionBlocks[(int)pair.ps.shaderDataLookup.ConstantFunctionBlocksIndex] : null,
                                    textureFunctionBlocks.Count > 0 ? textureFunctionBlocks[(int)pair.ps.shaderDataLookup.TextureFunctionBlocksIndex] : null,
                                    bufferFunctionBlocks.Count > 0 ? bufferFunctionBlocks[(int)pair.ps.shaderDataLookup.BufferFunctionBlocksIndex] : null,
                                    ref pair.ps);

                                pair.VertexShader.VertexElements = elems;
                                pair.vs.shaderDataLookup = vsPermutations[(int)solutions[solutionIndex].vertexPermutationIndex];
                                GetShaderPermutation(shaderConstants[(int)solutions[solutionIndex].vertexConstantsIndex],
                                    constantFunctionBlocks.Count > 0 ? constantFunctionBlocks[(int)pair.vs.shaderDataLookup.ConstantFunctionBlocksIndex] : null,
                                    textureFunctionBlocks.Count > 0 ? textureFunctionBlocks[(int)pair.vs.shaderDataLookup.TextureFunctionBlocksIndex] : null,
                                    bufferFunctionBlocks.Count > 0 ? bufferFunctionBlocks[(int)pair.vs.shaderDataLookup.BufferFunctionBlocksIndex] : null,
                                    ref pair.vs);

                                permutationPairs.Add(pair);
                            }
                        }

                        Loaded = true;
                    }
                    catch (Exception ex)
                    {
                        logger.Log($"Encountered error building shader map in database: {db.Name}" +
                            $"\nMessage ---\n{ex.Message}" +
                            $"\nStackTrace ---\n{ex.StackTrace}" +
                            $"\nTargetSite ---\n{ex.TargetSite}");
                    }
                }
            }
        }

        private void GetShaderPermutation(GenericShaderConstants constants,
            ConstantFunctionBlock constantFunctionBlock,
            TextureFunctionBlock textureFunctionBlock,
            BufferFunctionBlock bufferFunctionBlock,
            ref ShaderGraphPermutation permutation)
        {
            if (constantFunctionBlock != null)
            {
                foreach (ConstantFunctionBlock.Constant cfunc in constantFunctionBlock.Constants)
                {
                    permutation.ConstantFunctions.Add
                    (
                        new ConstantFunction
                        {
                            funcType = cfunc.constFunction,
                            CBufferIndex = cfunc.index,
                            ArraySize = cfunc.arraySize,
                            MatrixDims = cfunc.matrixDims
                        }
                    );
                }
            }

            if (textureFunctionBlock != null)
            {
                foreach (TextureFunctionBlock.Texture tfunc in textureFunctionBlock.Textures)
                {
                    permutation.TextureFunctions.Add
                    (
                        new TextureFunction
                        {
                            funcType = tfunc.constFunction,
                            texType = tfunc.valueType,
                            Index = tfunc.index
                        }
                    );
                }
            }

            if (bufferFunctionBlock != null)
            {
                foreach (BufferFunctionBlock.Buffer bfunc in bufferFunctionBlock.Buffers)
                {
                    permutation.BufferFunctions.Add
                    (
                        new BufferFunction
                        {
                            funcType = bfunc.constFunction,
                            bufType = bfunc.valueType,
                            Index = bfunc.index
                        }
                    );
                }
            }

            permutation.ValueConstants = constants.valueConstants;
            foreach (TextureConstant tex in constants.textureConstants)
            {
                permutation.TextureConstants.Add(UseCache ? textureHashMap[tex.nameHash] : tex.name);
            }
            foreach (ExternalValueConstant val in constants.externalValueConstants)
            {
                Type extType = TypeLibrary.GetType("ExternalValueConstantType");
                permutation.ExternalValueConstants.Add
                (
                    new ExternalValue
                    {
                        Name = val.name,
                        Type = extType == null ? "ExternalValueConstantType_Vec" : extType.GetEnumName(val.type),
                        Required = Convert.ToBoolean(val.required),
                        DefaultValue = val.defaultValue
                    }
                );
            }
            foreach (ExternalTextureConstant tex in constants.externalTextureConstants)
            {
                permutation.ExternalTextureConstants.Add(tex.name);
            }
            foreach (ExternalBufferConstant buf in constants.externalBufferConstants)
            {
                Type extType = TypeLibrary.GetType("ShaderValueType");
                permutation.ExternalBufferConstants.Add
                (
                    new ExternalBuffer
                    {
                        Name = buf.name,
                        Required = Convert.ToBoolean(buf.required),
                        Type = extType.GetEnumName(buf.valueType)
                    }
                );
            }
            permutation.SamplerStates = constants.samplerStates;
        }

    }
}
