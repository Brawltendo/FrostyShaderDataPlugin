using Frosty.Core.Attributes;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows;
using ShaderDataPlugin;
using FrostySdk;
//using ShaderDataPlugin.Extensions;

// Setting ComVisible to false makes the types in this assembly not visible
// to COM components.  If you need to access a type in this assembly from
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

[assembly: ThemeInfo(
    ResourceDictionaryLocation.None, //where theme specific resource dictionaries are located
                                     //(used if a resource is not found in the page, 
                                     // or application resource dictionaries)
    ResourceDictionaryLocation.SourceAssembly //where the generic resource dictionary is located
                                              //(used if a resource is not found in the page, 
                                              // app, or any theme specific resource dictionaries)
)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("29189dbc-1d9c-4760-960e-805b60211401")]

[assembly: PluginDisplayName("ShaderGraph Data Viewer")]
[assembly: PluginAuthor("Brawltendo")]
[assembly: PluginVersion("1.0.0.0")]

// Battlefront II doesn't use shader databases
[assembly: PluginNotValidForProfile((int)ProfileVersion.StarWarsBattlefrontII)]
//[assembly: RegisterMenuExtension(typeof(ShaderDbTestMenuExtension))]
[assembly: RegisterAssetDefinition("ShaderGraph", typeof(ShaderGraphAssetDefinition))]