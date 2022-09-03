using Frosty.Core;
using Frosty.Core.Controls;
using Frosty.Core.Windows;
using FrostySdk;
using FrostySdk.Attributes;
using FrostySdk.Ebx;
using FrostySdk.Interfaces;
using FrostySdk.IO;
using FrostySdk.Managers;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;

namespace ShaderDataPlugin
{
    public class ShaderGraphOverride : BaseTypeOverride
    {
        public BaseFieldOverride MaxSubMaterialCount { get; set; }
        public BaseFieldOverride GammaCorrectionEnable { get; set; }
    }

    public class ShaderGraphEditor : FrostyAssetEditor
    {

        private FrostyPropertyGrid pgAsset;
        ShaderGraphData graphInfo = new ShaderGraphData();
        private ShaderDb db;

        static ShaderGraphEditor()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ShaderGraphEditor), new FrameworkPropertyMetadata(typeof(ShaderGraphEditor)));
        }

        public ShaderGraphEditor(ILogger inLogger)
            : base(inLogger)
        {
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            pgAsset = GetTemplateChild("PART_AssetPropertyGrid") as FrostyPropertyGrid;

            Loaded += ShaderGraphEditor_Loaded;

        }

        public override List<ToolbarItem> RegisterToolbarItems()
        {
            return new List<ToolbarItem>()
            {
                new ToolbarItem("Export Bytecode", "Exports all shader permutations", "Images/Export.png", new RelayCommand((object state) => { ExportButton_Click(this, new RoutedEventArgs()); }))
            };
        }

        void BuildTextureHashCache(FrostyTaskWindow task)
        {
            string cachePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "ShaderDataPlugin");
            cachePath = Path.Combine(cachePath, ((ProfileVersion)ProfilesLibrary.DataVersion).ToString());
            Directory.CreateDirectory(cachePath);

            cachePath = Path.Combine(cachePath, "textures.cache");
            if (!File.Exists(cachePath))
            {
                task.Update("Building texture hash cache");
                using (NativeWriter cache = new NativeWriter(File.Create(cachePath)))
                {
                    IEnumerable<EbxAssetEntry> textureAssets = App.AssetManager.EnumerateEbx("TextureAsset");
                    int numTextures = textureAssets.Count();
                    // write count for iteration when loading
                    cache.Write(numTextures);

                    int progress = 0;
                    foreach (EbxAssetEntry asset in textureAssets)
                    {
                        task.Update(progress: (progress++ / (double)numTextures) * 100.0);
                        // build map with the texture hash as the key and asset GUID as the value to keep this as compact as possible
                        cache.Write((uint)Utils.HashString(asset.Name, true));
                        cache.Write(asset.Guid);
                    }
                }
            }
        }

        private void ShaderGraphEditor_Loaded(object sender, RoutedEventArgs e)
        {
            List<BundleEntry> bundleEntries = new List<BundleEntry>();
            foreach (int i in AssetEntry.Bundles)
            {
                bundleEntries.Add(App.AssetManager.GetBundleEntry(i));
            }

            db = new ShaderDb(bundleEntries, AssetEntry, logger);

            // start loading shaderdb
            FrostyTaskWindow.Show("Loading shader databases", "", (task) =>
            {
                // newer games store constant texture references as hashes
                // SWBF2 alpha uses shaderdbs, and also needs a cache
                if (ProfilesLibrary.DataVersion == (int)ProfileVersion.NeedForSpeedPayback
                || ProfilesLibrary.DataVersion == (int)ProfileVersion.MassEffectAndromeda
                || ProfilesLibrary.DataVersion == (int)ProfileVersion.Anthem
                || ProfilesLibrary.DataVersion == (int)ProfileVersion.StarWarsSquadrons
                || ProfilesLibrary.DataVersion == (int)ProfileVersion.PlantsVsZombiesBattleforNeighborville
                || ProfilesLibrary.DataVersion == (int)ProfileVersion.NeedForSpeedHeat)
                {
                    ShaderDb.UseCache = true;
                    BuildTextureHashCache(task);
                }
                db.Load(graphInfo, task);
            });

            if (db.Loaded)
            {
                pgAsset.SetClass(graphInfo);
                logger.Log($"Loaded info for shader {AssetEntry.Filename}");
            }
            else
                logger.Log($"Shader database has failed loading");
        }

        private void ExportButton_Click(ShaderGraphEditor shaderGraphEditor, RoutedEventArgs routedEventArgs)
        {
            FrostyOpenFolderDialog ofd = new FrostyOpenFolderDialog("Export Bytecode", "Compiled Shader Object");

            if (ofd.ShowDialog())
            {
                string shaderName = AssetEntry.DisplayName;
                FrostyTaskWindow.Show("Exporting shader bytecode", "", (task) =>
                {
                    if (ProfilesLibrary.DataVersion == (int)ProfileVersion.PlantsVsZombiesBattleforNeighborville
                    ||  ProfilesLibrary.DataVersion == (int)ProfileVersion.NeedForSpeedHeat)
                    {
                        ExportExternalBytecode(ofd.FileName, shaderName, task);
                    }
                    else
                    {
                        ExportEmbeddedBytecode(ofd.FileName, shaderName, task);
                    }
                });
                logger.Log("Shaders successfully exported to " + Path.Combine(ofd.FileName, shaderName));
            }
        }

        // exporter for pre-FB2018 games with bytecode embedded in the ShaderDb
        private void ExportEmbeddedBytecode(string filename, string shaderName, FrostyTaskWindow task)
        {
            foreach (var path in graphInfo.RenderPaths)
            {
                int progress = 0;
                string dirName = Path.Combine(filename, shaderName, path.RenderPathName);

                //
                // since shaders can potentially be in multiple shaderdbs, separate them all so that each database will only need to be loaded once per render path
                //

                // use a tuple to store the pair index so users can easily find the shader info in the property grid using the permutation index
                Dictionary<string, List<(PermutationPair pair, int index)>> dbToPairMap = new Dictionary<string, List<(PermutationPair pair, int index)>>();
                for (int i = 0; i < path.PermutationPairs.Count; ++i)
                {
                    string dbPath = path.PermutationPairs[i].ps.shaderDataLookup.DbPath;
                    if (!dbToPairMap.ContainsKey(dbPath))
                    {
                        dbToPairMap[dbPath] = new List<(PermutationPair pair, int index)>();
                    }
                    dbToPairMap[dbPath].Add((path.PermutationPairs[i], i));
                }

                // loop through keys to avoid loading the entire database for every permutation
                foreach (var db in dbToPairMap.Keys)
                {
                    Stream resStream = App.AssetManager.GetRes(App.AssetManager.GetResEntry(db));
                    using (NativeReader reader = new NativeReader(resStream))
                    {
                        foreach (var pairData in dbToPairMap[db])
                        {
                            task.Update($"Exporting permutation pair {progress + 1}/{path.PermutationPairs.Count} ({path.RenderPathName})", progress / path.PermutationPairs.Count * 100.0);

                            exportBytecodeInternal(ShaderType.PixelShader);
                            exportBytecodeInternal(ShaderType.VertexShader);
                            ++progress;

                            void exportBytecodeInternal(ShaderType type)
                            {
                                bool isPs = type == ShaderType.PixelShader;

                                // seek to bytecode offset
                                ShaderPermutation lookupData = isPs ? pairData.pair.PixelShader.shaderDataLookup : pairData.pair.VertexShader.shaderDataLookup;
                                reader.Position = lookupData.DbOffset;

                                string outName = Path.Combine(dirName, type.ToString());
                                Directory.CreateDirectory(outName);

                                outName = Path.Combine(outName, $"{shaderName}_permutation{progress}_{(isPs ? "ps" : "vs")}.cso");
                                using (NativeWriter csoOut = new NativeWriter(File.Create(outName)))
                                {
                                    csoOut.Write(reader.ReadBytes((int)lookupData.ShaderSize));
                                }
                            }
                        }
                    }
                }
            }
        }

        // exporter for newer Frostbite games that store bytecode in external resources
        private void ExportExternalBytecode(string filename, string shaderName, FrostyTaskWindow task)
        {
            foreach (var path in graphInfo.RenderPaths)
            {
                int progress = 0;
                string dirName = Path.Combine(filename, shaderName, path.RenderPathName);

                foreach (var pairs in path.PermutationPairs)
                {
                    task.Update($"Exporting permutation pair {progress + 1}/{path.PermutationPairs.Count} ({path.RenderPathName})", progress / path.PermutationPairs.Count * 100.0);

                    exportBytecodeInternal(ShaderType.PixelShader);
                    exportBytecodeInternal(ShaderType.VertexShader);
                    ++progress;

                    void exportBytecodeInternal(ShaderType type)
                    {
                        bool isPs = type == ShaderType.PixelShader;

                        // get bytecode resource
                        ShaderPermutation lookupData = isPs ? pairs.PixelShader.shaderDataLookup : pairs.VertexShader.shaderDataLookup;
                        ResAssetEntry resEntry = App.AssetManager.GetResEntry($"shaders/bytecode/{lookupData.ShaderGuid}");

                        string outName = Path.Combine(dirName, type.ToString());
                        Directory.CreateDirectory(outName);
                        outName = Path.Combine(outName, $"{shaderName}_permutation{progress}_{(isPs ? "ps" : "vs")}.cso");
                        using (NativeWriter csoWriter = new NativeWriter(File.Create(outName)))
                        {
                            // don't know what the res meta represents for shader bytecode, but let's write it anyway
                            csoWriter.Write(resEntry.ResMeta);
                            using (NativeReader reader = new NativeReader(App.AssetManager.GetRes(resEntry)))
                                csoWriter.Write(reader.ReadToEnd());
                        }
                    }
                }
            }
        }
    }
}
