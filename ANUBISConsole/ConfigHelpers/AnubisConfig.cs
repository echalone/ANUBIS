using ANUBISWatcher.Shared;
using Microsoft.Extensions.Logging;

namespace ANUBISConsole.ConfigHelpers
{
    public class AnubisConfig
    {
        public const string CNST_None = "<none>";
        public const string CNST_New = "<new>";
        private const string EXT_ANUBISConfig = "json";

        public static string? LoadedConfig { get; set; } = null;

        public string FullPath { get; set; }
        public string FileName { get; set; }

        public AnubisConfig()
        {
            FullPath = CNST_None;
            FileName = "<none>";
        }

        public AnubisConfig(string path)
        {
            FullPath = path;
            FileName = Path.GetFileNameWithoutExtension(path);
        }

        public override string ToString()
        {
            return FileName;
        }

        public static string GetFullPathByName(string name)
        {
            return Path.Join(AnubisOptions.Options.configDirectory, name + "." + EXT_ANUBISConfig);
        }

        public static AnubisConfig GetNone()
        {
            return new AnubisConfig();
        }

        public static AnubisConfig GetNew()
        {
            return new AnubisConfig()
            {
                FullPath = CNST_New,
                FileName = "<new>",
            };
        }

        public static List<AnubisConfig> DiscoverConfigs()
        {
            List<AnubisConfig> lstRetVal = [];

            foreach (var cfg in Directory.GetFiles(AnubisOptions.Options.configDirectory, "*." + EXT_ANUBISConfig, SearchOption.TopDirectoryOnly))
            {
                try
                {
                    var acf = new AnubisConfig(cfg);
                    lstRetVal.Add(acf);
                }
                catch (Exception ex)
                {
                    SharedData.InterfaceLogging?.LogError(ex, "While trying to add config file \"{filepath}\" to choice list: {message}", cfg, ex.Message);
                }
            };

#pragma warning disable IDE0305 // Initialisierung der Sammlung vereinfachen
            return lstRetVal.OrderBy(itm => itm.FileName).ToList();
#pragma warning restore IDE0305 // Initialisierung der Sammlung vereinfachen
        }
    }
}
