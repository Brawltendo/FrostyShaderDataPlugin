using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Media;
using Frosty.Core;
using Frosty.Core.Controls;
using FrostySdk.Interfaces;
using FrostySdk.IO;
using FrostySdk.Managers;

namespace ShaderDataPlugin
{
    public class ShaderGraphAssetDefinition : AssetDefinition
    {
        protected static ImageSource shaderGraphImageSource = new ImageSourceConverter().ConvertFromString("pack://application:,,,/FrostyCore;Component/Images/Assets/ShaderFileType.png") as ImageSource;

        public override void GetSupportedExportTypes(List<AssetExportType> exportTypes)
        {
            exportTypes.Add(new AssetExportType("cso", "Compiled Shader Object"));
            base.GetSupportedExportTypes(exportTypes);
        }

        public override FrostyAssetEditor GetEditor(ILogger logger)
        {
            return new ShaderGraphEditor(logger);
        }

        public override ImageSource GetIcon()
        {
            return shaderGraphImageSource;
        }

    }
}
