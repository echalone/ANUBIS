using ANUBISWatcher.Configuration.ConfigFileData;
using ANUBISWatcher.Configuration.ConfigHelpers;
using ANUBISWatcher.Shared;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace ANUBISWatcher.Configuration.Serialization
{
    public static class ConfigFileManager
    {
        public static bool WriteConfig(string path)
        {
            if (SharedData.Config != null)
            {
                return WriteConfig(path, SharedData.Config);
            }
            else
            {
                SharedData.InterfaceLogging?.LogWarning("Cannot save configuration to file as no configuration was loaded");
                return false;
            }
        }

        public static bool WriteConfig(string path, ConfigFile configFile)
        {
            ILogger? logging = SharedData.ConfigLogging;
            using (logging?.BeginScope("WriteConfig"))
            {
                try
                {
                    logging?.LogTrace("Checking if configuration directory exists...");
                    string? pathDir = Path.GetDirectoryName(path);
                    if (!string.IsNullOrWhiteSpace(pathDir))
                    {
                        if (!Path.Exists(pathDir))
                        {
                            logging?.LogTrace("Directory doesn't exist yet, creating it now");
                            Directory.CreateDirectory(pathDir);
                            logging?.LogTrace("Directory for configuration file has been created");
                        }
                        else
                        {
                            logging?.LogTrace("Directory already exists, don't need to create it");
                        }
                    }
                    else
                    {
                        logging?.LogTrace("No directory information, will just try to write to file");
                    }

#pragma warning disable CA1869 // JsonSerializerOptions-Instanzen zwischenspeichern und wiederverwenden
                    JsonSerializerOptions opt = new()
                    {
                        WriteIndented = true,
                    };
#pragma warning restore CA1869 // JsonSerializerOptions-Instanzen zwischenspeichern und wiederverwenden
                    string strSerialized = JsonSerializer.Serialize(configFile, opt);
                    logging?.LogTrace("Serialized configuration to the following object: {serialized}", strSerialized.Replace("\r", "").Replace("\n", ""));

                    using (FileStream fs = new(path, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        using (StreamWriter sw = new(fs))
                        {
                            sw.Write(strSerialized);
                            logging?.LogTrace(@"Serialized configuration has been written to file ""{path}""", path);
                        }
                    }
                    return true;
                }
                catch (Exception ex)
                {
                    logging?.LogError(ex, @"While writing configuration to ""{path}"": {message}", path, ex.Message);
                    return false;
                }
            }
        }

        public static (ConfigFile?, DiscoveryInfo?) ReadAndLoadConfig(string path, bool discoverDevices, bool tryTurnOnSwitchBots)
        {
            ConfigFile? configFile = ReadConfig(path);

            return (configFile, LoadConfig(configFile, discoverDevices, tryTurnOnSwitchBots));
        }

        public static DiscoveryInfo? ReloadConfig(bool discoverDevices, bool tryTurnOnSwitchBots)
        {
            if (SharedData.Config != null)
            {
                return LoadConfig(discoverDevices, tryTurnOnSwitchBots);
            }
            else
            {
                return null;
            }
        }

        public static DiscoveryInfo? LoadConfig(ConfigFile? configFile, bool discoverDevices, bool tryTurnOnSwitchBots)
        {
            if (configFile != null)
            {
                configFile.pollersAndTriggers.pollers.Where(itm => itm is CountdownPollerData).ToList().ForEach(itm => ((CountdownPollerData)itm).options.CalculateCountdownT0());

                SharedData.RemoveConfig();
                SharedData.SetConfig(configFile);

                return LoadConfig(discoverDevices, tryTurnOnSwitchBots);
            }
            else
            {
                return null;
            }
        }

        private static DiscoveryInfo? LoadConfig(bool discoverDevices, bool tryTurnOnSwitchBots)
        {
            using (SharedData.InterfaceLogging?.BeginScope("LoadConfig"))
            {
                try
                {
                    DiscoveryInfo? infoDiscovery = null;

                    if (discoverDevices)
                    {
                        // Initialize our write-to files here 
                        var lfpd = (LocalFilePollerData?)SharedData.Config?.pollersAndTriggers.pollers.FirstOrDefault(itm => itm is LocalFilePollerData && itm.enabled);
                        if (lfpd != null)
                        {
                            foreach (var file in lfpd.files)
                            {
                                if (file.enabled && !string.IsNullOrWhiteSpace(file.path))
                                {
                                    try
                                    {
                                        Entities.WatcherPollerFile.WriteToFile(file.path, Entities.WatcherFileState.Stopped, DateTime.UtcNow);
                                    }
                                    catch (Exception ex)
                                    {
                                        SharedData.InterfaceLogging?.LogError(ex, @"Could not initialize local write file ""{name}"" at location ""{path}""", file.name, file.path);
                                    }
                                }
                            }
                        }
                        infoDiscovery = Discoverer.DiscoverDevices(tryTurnOnSwitchBots);
                        Discoverer.SetDiscoveryInConfiguration(infoDiscovery);
                        Discoverer.SetEnabledAccordingToDiscovered();
                    }

                    return infoDiscovery;
                }
                catch (Exception ex)
                {
                    SharedData.InterfaceLogging?.LogError(ex, "While trying to load configuration");

                    return null;
                }
            }
        }

        public static ConfigFile? ReadConfig(string path)
        {
            ILogger? logging = SharedData.ConfigLogging;
            ConfigFile? configFile = null;

            using (logging?.BeginScope("ReadConfig"))
            {
                try
                {
                    if (Path.Exists(path))
                    {
                        using (FileStream fs = new(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                        {
                            using (StreamReader sw = new(fs))
                            {
                                string strContent = sw.ReadToEnd();
                                logging?.LogTrace(@"Read in the following configuration from configuration file ""{path}"": {serialized}", path, strContent.Replace("\r", "").Replace("\n", ""));

#pragma warning disable CA1869 // JsonSerializerOptions-Instanzen zwischenspeichern und wiederverwenden
                                JsonSerializerOptions opt = new()
                                {
                                    AllowTrailingCommas = true,
                                };
#pragma warning restore CA1869 // JsonSerializerOptions-Instanzen zwischenspeichern und wiederverwenden
                                ConfigFile? objConfig = JsonSerializer.Deserialize<ConfigFile>(strContent, opt);

                                logging?.LogTrace(@"Configuration has been deserialized");

                                return objConfig;
                            }
                        }
                    }
                    else
                    {
                        throw new Exception(@$"Configuration file ""{path}"" doesn't exist");
                    }
                }
                catch (Exception ex)
                {
                    logging?.LogError(ex, @"While reading in configuration from ""{path}"": {message}", path, ex.Message);
                }
            }

            return configFile;
        }
    }
}
