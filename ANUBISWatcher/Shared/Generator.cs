using ANUBISClewareAPI;
using ANUBISFritzAPI;
using ANUBISSwitchBotAPI;
using ANUBISWatcher.Configuration.ConfigFileData;
using ANUBISWatcher.Configuration.ConfigHelpers;
using ANUBISWatcher.Controlling;
using ANUBISWatcher.Entities;
using ANUBISWatcher.Helpers;
using ANUBISWatcher.Options;
using ANUBISWatcher.Pollers;
using ANUBISWatcher.Triggering;
using ANUBISWatcher.Triggering.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Extensions.Logging;
using System.Reflection;

namespace ANUBISWatcher.Shared
{
    public class GeneratorInfoCache
    {
        public Serilog.Core.Logger? SeriLogger { get; set; }
        public Microsoft.Extensions.Logging.ILogger? GeneratorLogger { get; set; }
        public CancellationTokenSource? GlobalCancellationSource { get; set; }
        public CancellationToken? GlobalCancellationToken { get; set; }
    }

    public class VersionCollection
    {
        public string? AnubisInterfaceName { get; set; }
        public Version? AnubisInterface { get; set; }
        public string? AnubisInterface_Copyright { get; set; }
        public Version? AnubisWatcher { get; init; }
        public string? AnubisWatcher_Copyright { get; set; }
        public Version? ApiClewareUsb { get; init; }
        public string? ApiClewareUsb_Copyright { get; set; }
        public Version? ApiSwitchBot { get; init; }
        public string? ApiSwitchBot_Copyright { get; set; }
        public Version? ApiFritz { get; init; }
        public string? ApiFritz_Copyright { get; set; }
        public string? LicenseText { get; set; }

        public VersionCollection(Assembly? assInterface, Assembly? assWatcher, Assembly? assClewareUsb, Assembly? assSwitchBot, Assembly? assFritz)
        {
            AnubisInterfaceName = assInterface?.GetName()?.Name;
            AnubisInterface = assInterface?.GetName()?.Version;
            AnubisWatcher = assWatcher?.GetName().Version;
            ApiClewareUsb = assClewareUsb?.GetName().Version;
            ApiSwitchBot = assSwitchBot?.GetName().Version;
            ApiFritz = assFritz?.GetName().Version;

            AnubisInterface_Copyright = GetCopyright(assInterface);
            AnubisWatcher_Copyright = GetCopyright(assWatcher);
            ApiClewareUsb_Copyright = GetCopyright(assClewareUsb);
            ApiSwitchBot_Copyright = GetCopyright(assSwitchBot);
            ApiFritz_Copyright = GetCopyright(assFritz);

            string? strLocation = Assembly.GetEntryAssembly()?.Location;
            if (!string.IsNullOrWhiteSpace(strLocation) )
            {
                string? strContainingDirectory = Path.GetDirectoryName(strLocation);
                if (!string.IsNullOrWhiteSpace(strContainingDirectory))
                {
                    string strLicenseFile = Path.Combine(strContainingDirectory, "license.txt");
                    if (File.Exists(strLicenseFile))
                    {
                        LicenseText = File.ReadAllText(strLicenseFile).Trim();
                    }
                }
            }
        }

        public static string? GetCopyright(Assembly? ass)
        {
            return (ass?.GetCustomAttributes(typeof(AssemblyCopyrightAttribute), true)?.FirstOrDefault() as AssemblyCopyrightAttribute)?.Copyright;
        }
    }

    public class Generator
    {
        #region Constants

        private const uint c_TriggerTimeInSeconds_Startup = 10;
        private const uint c_TriggerTimeInSeconds_ClewareUSB = 8;
        private const uint c_TriggerTimeInSeconds_Fritz = 15;
        private const uint c_TriggerTimeInSeconds_SwitchBot = 12;
        private const uint c_TriggerTimeInSeconds_OSShutdown = 8;
        private static readonly GeneratorInfoCache generatorInfoCache = new();

        #endregion

        #region Fields

        private static readonly GeneratorInfoCache _info = generatorInfoCache;

        #endregion

        #region Properties

        private static Microsoft.Extensions.Logging.ILogger? Logging { get; set; }

        #endregion


        #region Private methods

        private static Serilog.Core.Logger? GetSerilogLogger(string configurationFileName = "logsettings")
        {
            var logSeriConfig = new ConfigurationBuilder()
                        .SetBasePath(Directory.GetCurrentDirectory())
                        .AddJsonFile(configurationFileName + ".json")
                        .AddJsonFile(configurationFileName + $".{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", true)
                        .Build();

            if (logSeriConfig != null)
            {
                return new LoggerConfiguration()
                            .ReadFrom.Configuration(logSeriConfig)
                            .CreateLogger();
            }
            else
            {
                return null;
            }
        }

        #endregion

        #region Public methods

        #region Versions

        public static VersionCollection GetVersions()
        {
            return new VersionCollection(Assembly.GetEntryAssembly(), 
                                            typeof(Generator).Assembly, typeof(ClewareAPI).Assembly,
                                            typeof(SwitchBotAPI).Assembly, typeof(FritzAPI).Assembly);
        }

        #endregion

        #region Initialization

        public static void Init(string configurationFileName = "logsettings")
        {
            InitLogging(configurationFileName);
            InitCancellationToken();
        }

        public static void InitLogging(string configurationFileName = "logsettings")
        {
            _info.SeriLogger = GetSerilogLogger(configurationFileName);
            _info.GeneratorLogger = GetLogger("Configuration.Generator");
            Logging = GetLogger("Shared.Generator");
            SharedData.Logging = GetLogger("Shared.Data");
            SharedData.ConfigLogging = GetLogger("Shared.Config");
            SharedData.InterfaceLogging = GetLogger("Shared.Interface");
        }

        public static void InitCancellationToken()
        {
            _info.GlobalCancellationSource?.Dispose();
            _info.GlobalCancellationSource = new CancellationTokenSource();
            _info.GlobalCancellationToken = _info.GlobalCancellationSource.Token;
        }

        #endregion

        #region Cancellation

        public static void CancelGlobalCancellationToken()
        {
            _info?.GlobalCancellationSource?.Cancel();
        }

        public static void CheckThreadCancellation()
        {
            _info?.GlobalCancellationToken?.ThrowIfCancellationRequested();
        }

        public static bool IsThreadCancelled()
        {
            return _info?.GlobalCancellationToken?.IsCancellationRequested ?? false;
        }

        public static bool WaitMilliseconds(int? milliseconds)
        {
            return Helpers.CancellationUtils.WaitMilliseconds(_info?.GlobalCancellationToken, milliseconds);
        }

        public static bool WaitMilliseconds(uint? milliseconds)
        {
            return Helpers.CancellationUtils.WaitMilliseconds(_info?.GlobalCancellationToken, milliseconds);
        }

        public static bool WaitMilliseconds(long? milliseconds)
        {
            return Helpers.CancellationUtils.WaitMilliseconds(_info?.GlobalCancellationToken, milliseconds);
        }

        public static bool WaitMilliseconds(ulong? milliseconds)
        {
            return Helpers.CancellationUtils.WaitMilliseconds(_info?.GlobalCancellationToken, milliseconds);
        }

        public static bool WaitMilliseconds(double? milliseconds)
        {
            return Helpers.CancellationUtils.WaitMilliseconds(_info?.GlobalCancellationToken, milliseconds);
        }

        public static bool WaitSeconds(int? seconds)
        {
            return Helpers.CancellationUtils.WaitSeconds(_info?.GlobalCancellationToken, seconds);
        }

        public static bool WaitSeconds(uint? seconds)
        {
            return Helpers.CancellationUtils.WaitSeconds(_info?.GlobalCancellationToken, seconds);
        }

        public static bool WaitSeconds(long? seconds)
        {
            return Helpers.CancellationUtils.WaitSeconds(_info?.GlobalCancellationToken, seconds);
        }

        public static bool WaitSeconds(ulong? seconds)
        {
            return Helpers.CancellationUtils.WaitSeconds(_info?.GlobalCancellationToken, seconds);
        }

        public static bool WaitSeconds(double? seconds)
        {
            return Helpers.CancellationUtils.WaitSeconds(_info?.GlobalCancellationToken, seconds);
        }

        public static bool WaitMinutes(int? minutes)
        {
            return Helpers.CancellationUtils.WaitMinutes(_info?.GlobalCancellationToken, minutes);
        }

        public static bool WaitMinutes(uint? minutes)
        {
            return Helpers.CancellationUtils.WaitMinutes(_info?.GlobalCancellationToken, minutes);
        }

        public static bool WaitMinutes(long? minutes)
        {
            return Helpers.CancellationUtils.WaitMinutes(_info?.GlobalCancellationToken, minutes);
        }

        public static bool WaitMinutes(ulong? minutes)
        {
            return Helpers.CancellationUtils.WaitMinutes(_info?.GlobalCancellationToken, minutes);
        }

        public static bool WaitMinutes(double? minutes)
        {
            return Helpers.CancellationUtils.WaitMinutes(_info?.GlobalCancellationToken, minutes);
        }

        public static bool WaitLocal(DateTime? timestamp)
        {
            return Helpers.CancellationUtils.WaitLocal(_info?.GlobalCancellationToken, timestamp);
        }

        public static bool WaitUtc(DateTime? timestamp)
        {
            return Helpers.CancellationUtils.WaitUtc(_info?.GlobalCancellationToken, timestamp);
        }

        public static bool Wait(TimeSpan? timespan)
        {
            return Helpers.CancellationUtils.Wait(_info?.GlobalCancellationToken, timespan);
        }

        public static CancellationTokenSource? GetLinkedCancellationTokenSource()
        {
            if (_info?.GlobalCancellationToken != null)
            {
                return CancellationTokenSource.CreateLinkedTokenSource(_info.GlobalCancellationToken.Value);
            }
            else
            {
                return null;
            }
        }

        #endregion

        #region Logging

        public static Microsoft.Extensions.Logging.ILogger? GetLogger(string name)
        {
            if (_info.SeriLogger != null)
                return new SerilogLoggerFactory(_info.SeriLogger).CreateLogger(name);
            else
                return null;
        }

        #endregion

        #region APIs

        public static ClewareAPI? GetClewareAPI(bool catchErrors = true)
        {
            using (Logging?.BeginScope("Shared.Generator.GetClewareAPI"))
            {
                try
                {
                    var config = SharedData.Config;
                    if (config != null)
                    {
                        Logging?.LogTrace("Generating Cleware USB Switch API");
                        ClewareAPIConfigSettings? opt = config?.clewareApiSettings;

                        if (opt != null)
                        {
                            var api = new ClewareAPI(new ClewareAPIOptions()
                            {
                                USBSwitchNameList = GetClewareUSBMapping()
                                                        ?.ToDictionary(itm => itm.name ?? "", itm => itm.id ?? 0) ?? [],
                                AutoRetryCount = opt.autoRetryCount,
                                AutoRetryMinWaitMilliseconds = opt.autoRetryMinWaitMilliseconds,
                                AutoRetryOnErrors = opt.autoRetryOnErrors,
                                AutoRetryWaitSpanMilliseconds = opt.autoRetryWaitSpanMilliseconds,
                                CommandTimeoutSeconds = opt.commandTimeoutSeconds,
                                USBSwitchCommand_Arguments_Get = opt.usbSwitchCommand_Arguments_Get,
                                USBSwitchCommand_Arguments_List = opt.usbSwitchCommand_Arguments_List,
                                USBSwitchCommand_Arguments_SetOff = opt.usbSwitchCommand_Arguments_SetOff,
                                USBSwitchCommand_Arguments_SetOffSecure = opt.usbSwitchCommand_Arguments_SetOffSecure,
                                USBSwitchCommand_Arguments_SetOn = opt.usbSwitchCommand_Arguments_SetOn,
                                USBSwitchCommand_Arguments_SetOnSecure = opt.usbSwitchCommand_Arguments_SetOnSecure,
                                USBSwitchCommand_Path = opt.usbSwitchCommand_Path,
                                Logger = GetLogger("Cleware.Api"),
                            });

                            if (_info.GlobalCancellationToken.HasValue)
                                api.SetCancellationToken(_info.GlobalCancellationToken.Value);

                            return api;
                        }
                        else
                        {
                            Logging?.LogTrace("No cleware api configuration found");
                            return null;
                        }
                    }
                    else
                    {
                        throw new ConfigException("Found no loaded configuration");
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (ThreadInterruptedException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    Logging?.LogError(ex, "While trying to generate Cleware USB Switch API: {message}", ex.Message);
                    if (catchErrors)
                    {
                        return null;
                    }
                    else
                    {
                        throw;
                    }
                }
            }
        }

        public static SwitchBotAPI? GetSwitchBotAPI(bool loadNames = true, bool catchErrors = true)
        {
            using (Logging?.BeginScope("Shared.Generator.GetSwitchBotAPI"))
            {
                try
                {
                    var config = SharedData.Config;
                    if (config != null)
                    {
                        Logging?.LogTrace("Generating SwitchBot API");
                        SwitchBotAPIConfigSettings? opt = config?.switchApiSettings;

                        if (opt != null)
                        {
                            var api = new SwitchBotAPI(new SwitchBotAPIOptions()
                            {
                                Token = opt.token,
                                Secret = opt.secret,
                                AutoRetryCount = opt.autoRetryCount,
                                AutoRetryMinWaitMilliseconds = opt.autoRetryMinWaitMilliseconds,
                                AutoRetryOnErrors = opt.autoRetryOnErrors,
                                AutoRetryWaitSpanMilliseconds = opt.autoRetryWaitSpanMilliseconds,
                                BaseUrl = opt.baseUrl,
                                CommandTimeoutSeconds = opt.commandTimeoutSeconds,
                                IgnoreSSLError = opt.ignoreSSLError,
                                ReloadNamesIfNotFound = opt.reloadNamesIfNotFound,
                                Logger = GetLogger("SwitchBot.Api"),
                            });

                            if (_info.GlobalCancellationToken.HasValue)
                                api.SetCancellationToken(_info.GlobalCancellationToken.Value);

                            if (loadNames)
                            {
                                Logging?.LogTrace("Loading SwitchBot names");

                                api.LoadSwitchBotNames();
                            }
                            else
                            {
                                Logging?.LogTrace("Skipping loading SwitchBot names");
                            }

                            return api;
                        }
                        else
                        {
                            Logging?.LogTrace("No switchbot api configuration found");
                            return null;
                        }
                    }
                    else
                    {
                        throw new ConfigException("Found no loaded configuration");
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (ThreadInterruptedException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    Logging?.LogError(ex, "While trying to generate SwitchBot API: {message}", ex.Message);
                    if (catchErrors)
                    {
                        return null;
                    }
                    else
                    {
                        throw;
                    }
                }
            }
        }

        public static FritzAPI? GetFritzAPI(bool login = true, bool loadNames = true, bool catchErrors = true)
        {
            using (Logging?.BeginScope("Shared.Generator.GetFritzAPI"))
            {
                try
                {
                    var config = SharedData.Config;
                    if (config != null)
                    {
                        Logging?.LogTrace("Generating Fritz API");
                        FritzAPIConfigSettings? opt = config?.fritzApiSettings;

                        if (opt != null)
                        {
                            var api = new FritzAPI(new FritzAPIOptions()
                            {
                                AutoLogin = opt.autoLogin,
                                AutoRetryCount = opt.autoRetryCount,
                                AutoRetryMinWaitMilliseconds = opt.autoRetryMinWaitMilliseconds,
                                AutoRetryOnErrors = opt.autoRetryOnErrors,
                                AutoRetryWaitSpanMilliseconds = opt.autoRetryWaitSpanMilliseconds,
                                BaseUrl = opt.baseUrl,
                                CheckLoginBeforeCommands = opt.checkLoginBeforeCommands,
                                CommandTimeoutSeconds = opt.commandTimeoutSeconds,
                                IgnoreSSLError = opt.ignoreSSLError,
                                LoginTimeoutSeconds = opt.loginTimeoutSeconds,
                                Password = opt.password,
                                ReloadNamesIfNotFound = opt.reloadNamesIfNotFound,
                                User = opt.user,
                                Logger = GetLogger("Fritz.Api"),
                            });

                            if (_info.GlobalCancellationToken.HasValue)
                                api.SetCancellationToken(_info.GlobalCancellationToken.Value);

                            if (login)
                            {
                                if (loadNames)
                                    Logging?.LogTrace("Logging in to FritzAPI and loading names");
                                else
                                    Logging?.LogTrace("Logging in to FritzAPI without loading names");

                                api.Login(loadNames);
                            }

                            return api;
                        }
                        else
                        {
                            Logging?.LogTrace("No fritz api configuration found");
                            return null;
                        }
                    }
                    else
                    {
                        throw new ConfigException("Found no loaded configuration");
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (ThreadInterruptedException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    Logging?.LogError(ex, "While trying to generate Fritz API: {message}", ex.Message);
                    if (catchErrors)
                    {
                        return null;
                    }
                    else
                    {
                        throw;
                    }
                }
            }
        }

        #endregion

        #region Panic reason translation

        public static UniversalPanicReason[]? GetUniversalPanicReasons(WatcherFilePanicReason[]? panic, bool warnIfNoPanic = true)
        {
            return panic?.Select(itm => GetUniversalPanicReason(itm, warnIfNoPanic)).ToArray();
        }

        public static UniversalPanicReason GetUniversalPanicReason(WatcherFilePanicReason? panic, bool warnIfNoPanic = true)
        {
            switch (panic ?? WatcherFilePanicReason.NoPanic)
            {
                case WatcherFilePanicReason.All:
                    return UniversalPanicReason.All;
                case WatcherFilePanicReason.Panic:
                    return UniversalPanicReason.Panic;
                case WatcherFilePanicReason.InvalidState:
                    return UniversalPanicReason.InvalidState;
                case WatcherFilePanicReason.InvalidTimestamp:
                    return UniversalPanicReason.InvalidTimestamp;
                case WatcherFilePanicReason.Error:
                    return UniversalPanicReason.Error;
                case WatcherFilePanicReason.DeSynced:
                    return UniversalPanicReason.DeSynced;
                case WatcherFilePanicReason.NoResponse:
                    return UniversalPanicReason.NoResponse;
                case WatcherFilePanicReason.Unreachable:
                    return UniversalPanicReason.Unreachable;
                case WatcherFilePanicReason.NoPanic:
                    if (warnIfNoPanic)
                        Logging?.LogWarning("Tried to translate WatcherFilePanicReason but value indicated no panic");
                    return UniversalPanicReason.NoPanic;
                default:
                    Logging?.LogError("Tried to translate WatcherFilePanicReason but value {reason} is unknown", panic);
                    return UniversalPanicReason.Unknown;
            }
        }

        public static UniversalPanicReason[]? GetUniversalPanicReasons(ClewareUSBPanicReason[]? panic, bool warnIfNoPanic = true)
        {
            return panic?.Select(itm => GetUniversalPanicReason(itm, warnIfNoPanic)).ToArray();
        }

        public static UniversalPanicReason GetUniversalPanicReason(ClewareUSBPanicReason? panic, bool warnIfNoPanic = true)
        {
            switch (panic ?? ClewareUSBPanicReason.NoPanic)
            {
                case ClewareUSBPanicReason.All:
                    return UniversalPanicReason.All;
                case ClewareUSBPanicReason.SafeModeTurnOn:
                    return UniversalPanicReason.SafeModeTurnOn;
                case ClewareUSBPanicReason.NameNotFound:
                    return UniversalPanicReason.NameNotFound;
                case ClewareUSBPanicReason.InvalidState:
                    return UniversalPanicReason.InvalidState;
                case ClewareUSBPanicReason.SwitchNotFound:
                    return UniversalPanicReason.SwitchNotFound;
                case ClewareUSBPanicReason.SwitchOff:
                    return UniversalPanicReason.SwitchOff;
                case ClewareUSBPanicReason.ErrorState:
                    return UniversalPanicReason.ErrorState;
                case ClewareUSBPanicReason.GeneralError:
                    return UniversalPanicReason.GeneralError;
                case ClewareUSBPanicReason.UnknownState:
                    return UniversalPanicReason.UnknownState;
                case ClewareUSBPanicReason.CommandTimeout:
                    return UniversalPanicReason.CommandTimeout;
                case ClewareUSBPanicReason.CommandError:
                    return UniversalPanicReason.CommandError;
                case ClewareUSBPanicReason.NoPanic:
                    if (warnIfNoPanic)
                        Logging?.LogWarning("Tried to translate ClewarePanicReason but value indicated no panic");
                    return UniversalPanicReason.NoPanic;
                default:
                    Logging?.LogError("Tried to translate ClewarePanicReason but value {reason} is unknown", panic);
                    return UniversalPanicReason.Unknown;
            }
        }

        public static UniversalPanicReason[]? GetUniversalPanicReasons(FritzPanicReason[]? panic, bool warnIfNoPanic = true)
        {
            return panic?.Select(itm => GetUniversalPanicReason(itm, warnIfNoPanic)).ToArray();
        }

        public static UniversalPanicReason GetUniversalPanicReason(FritzPanicReason? panic, bool warnIfNoPanic = true)
        {
            switch (panic ?? FritzPanicReason.NoPanic)
            {
                case FritzPanicReason.All:
                    return UniversalPanicReason.All;
                case FritzPanicReason.SafeModePowerUp:
                    return UniversalPanicReason.SafeModePowerUp;
                case FritzPanicReason.NameNotFound:
                    return UniversalPanicReason.NameNotFound;
                case FritzPanicReason.InvalidPower:
                    return UniversalPanicReason.InvalidPower;
                case FritzPanicReason.SwitchNotFound:
                    return UniversalPanicReason.SwitchNotFound;
                case FritzPanicReason.SwitchOff:
                    return UniversalPanicReason.SwitchOff;
                case FritzPanicReason.PowerTooLow:
                    return UniversalPanicReason.PowerTooLow;
                case FritzPanicReason.PowerTooHigh:
                    return UniversalPanicReason.PowerTooHigh;
                case FritzPanicReason.InvalidState:
                    return UniversalPanicReason.InvalidState;
                case FritzPanicReason.ErrorPresence:
                    return UniversalPanicReason.ErrorPresence;
                case FritzPanicReason.ErrorState:
                    return UniversalPanicReason.ErrorState;
                case FritzPanicReason.ErrorPower:
                    return UniversalPanicReason.ErrorPower;
                case FritzPanicReason.GeneralError:
                    return UniversalPanicReason.GeneralError;
                case FritzPanicReason.UnknownPresence:
                    return UniversalPanicReason.UnknownPresence;
                case FritzPanicReason.UnknownState:
                    return UniversalPanicReason.UnknownState;
                case FritzPanicReason.UnknownPower:
                    return UniversalPanicReason.UnknownPower;
                case FritzPanicReason.HttpTimeout:
                    return UniversalPanicReason.HttpTimeout;
                case FritzPanicReason.NetworkError:
                    return UniversalPanicReason.NetworkError;
                case FritzPanicReason.NoPanic:
                    if (warnIfNoPanic)
                        Logging?.LogWarning("Tried to translate FritzPanicReason but value indicated no panic");
                    return UniversalPanicReason.NoPanic;
                default:
                    Logging?.LogError("Tried to translate FritzPanicReason but value {reason} is unknown", panic);
                    return UniversalPanicReason.Unknown;
            }
        }

        public static UniversalPanicReason[]? GetUniversalPanicReasons(SwitchBotPanicReason[]? panic, bool warnIfNoPanic = true)
        {
            return panic?.Select(itm => GetUniversalPanicReason(itm, warnIfNoPanic)).ToArray();
        }

        public static UniversalPanicReason GetUniversalPanicReason(SwitchBotPanicReason? panic, bool warnIfNoPanic = true)
        {
            switch (panic ?? SwitchBotPanicReason.NoPanic)
            {
                case SwitchBotPanicReason.All:
                    return UniversalPanicReason.All;
                case SwitchBotPanicReason.NameNotFound:
                    return UniversalPanicReason.NameNotFound;
                case SwitchBotPanicReason.SwitchNotFound:
                    return UniversalPanicReason.SwitchNotFound;
                case SwitchBotPanicReason.SwitchOff:
                    return UniversalPanicReason.SwitchOff;
                case SwitchBotPanicReason.BatteryTooLow:
                    return UniversalPanicReason.BatteryTooLow;
                case SwitchBotPanicReason.ErrorResponse:
                    return UniversalPanicReason.ErrorResponse;
                case SwitchBotPanicReason.GeneralError:
                    return UniversalPanicReason.GeneralError;
                case SwitchBotPanicReason.InvalidState:
                    return UniversalPanicReason.InvalidState;
                case SwitchBotPanicReason.InvalidBattery:
                    return UniversalPanicReason.InvalidBattery;
                case SwitchBotPanicReason.UnknownBattery:
                    return UniversalPanicReason.UnknownBattery;
                case SwitchBotPanicReason.UnknownState:
                    return UniversalPanicReason.UnknownState;
                case SwitchBotPanicReason.HttpTimeout:
                    return UniversalPanicReason.HttpTimeout;
                case SwitchBotPanicReason.NetworkError:
                    return UniversalPanicReason.NetworkError;
                case SwitchBotPanicReason.NoPanic:
                    if (warnIfNoPanic)
                        Logging?.LogWarning("Tried to translate SwitchBotPanicReason but value indicated no panic");
                    return UniversalPanicReason.NoPanic;
                default:
                    Logging?.LogError("Tried to translate SwitchBotPanicReason but value {reason} is unknown", panic);
                    return UniversalPanicReason.Unknown;
            }
        }

        #endregion

        #region Triggers

        public static void TriggerShutDown(UniversalPanicType type, string? id, UniversalPanicReason reason, bool triggerOnError = true)
        {
            using (Logging?.BeginScope("TriggerShutDown"))
            {
                bool canBeTriggered = false;
                ControllerStatus statusController = SharedData.CurrentControllerStatus;
                try
                {
                    canBeTriggered = statusController == ControllerStatus.Armed || statusController == ControllerStatus.SafeMode ||
                                        statusController == ControllerStatus.ShutDown || statusController == ControllerStatus.Triggered;
                    if (canBeTriggered)
                    {
                        List<string> lstTriggeredConfigs = [];
                        string[] strTriggerConfigNames = Generator.GetTriggerConfigurationNames(type, id, reason);

                        if (strTriggerConfigNames.Length > 0)
                        {
                            foreach (string singleTriggerConfigName in strTriggerConfigNames)
                            {
                                TriggerConfiguration? config = Generator.GetTriggerConfiguration(singleTriggerConfigName);
                                if (config != null)
                                {
                                    if (TriggerController.StartTriggerThread(config, reason != UniversalPanicReason.NoPanic))
                                    {
                                        Logging?.LogInformation("Triggered configuration \"{config}\" for switch type {switchtype} with name/id \"{id}\" for reason {reason}", config.Name, type, id, reason);
                                        lstTriggeredConfigs.Add(config.Name);
                                    }
                                    else
                                    {
                                        Logging?.LogDebug("Skipped configuration \"{config}\" for switch type {switchtype} with name/id \"{id}\" for reason {reason}", config.Name, type, id, reason);
                                    }
                                }
                                else
                                {
                                    Logging?.LogWarning("Found no enabled trigger configuration for trigger config name \"{name}\"", singleTriggerConfigName);
                                }
                            }
                            Logging?.LogInformation("Triggered system shutdown for switch type {switchtype} with name/id \"{id}\" for reason {reason}", type, id, reason);

                            if (type != UniversalPanicType.Countdown && reason == UniversalPanicReason.NoPanic)
                            {
                                Logging?.LogWarning("Triggered system shutdown for NoPanic reason for something other than Countdown poller, this was unexpected");
                            }
                            else if (reason == UniversalPanicReason.Unknown)
                            {
                                Logging?.LogWarning("Triggered system shutdown for Unknown reason, this was unexpected");
                            }
                            if (lstTriggeredConfigs.Count > 0)
                            {
                                if (type != UniversalPanicType.Countdown || reason != UniversalPanicReason.NoPanic)
                                {
                                    SharedData.AddControllerStatusHistory(ControllerStatus.Triggered);
                                }
                                else
                                {
                                    SharedData.AddControllerStatusHistory(ControllerStatus.Triggered, triggerIsCountdownT0: true);
                                }
                            }
                        }
                        else
                        {
                            Logging?.LogDebug("Not triggering system shutdown for switch type {switchtype} with name/id {id} for reason {reason} because no trigger was defined or all defined triggers have been repeated as often as they are allowed to", type, id, reason);
                        }

                        if (type != UniversalPanicType.Countdown || reason != UniversalPanicReason.NoPanic)
                        {
                            // Add panic to history since we didn't trigger the system shutdown via countdown
                            SharedData.AddPanicHistory(type, id, reason, lstTriggeredConfigs);
                        }
                    }
                    else
                    {
                        Logging?.LogCritical("ANUBIS Watcher wanted to trigger system shutdown in wrong mode {mode} for switch type {switchtype} with name/id {id} for reason {reason}; NOT triggering system shutdown", statusController, type, id, reason);
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (ThreadInterruptedException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    if (triggerOnError)
                    {
                        Logging?.LogCritical(ex, "While trying to trigger system shutdown, will trigger general panic because of this error: {message}", ex.Message);
                        Generator.TriggerShutDown(UniversalPanicType.General, ConstantTriggerIDs.ID_TriggerShutDown, UniversalPanicReason.GeneralError, false);
                    }
                    else
                    {
                        Logging?.LogCritical(ex, "While trying to trigger system shutdown, will not trigger panic because of this error: {message}", ex.Message);
                    }
                }
            }
        }

        /// <summary>
        /// Will search first for matching entry with same id and same panic, then with same id and any panic,
        /// then with any id and same panic, and then with any id and any panic. If none are found will search in fallback triggers.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="id"></param>
        /// <param name="reason"></param>
        /// <returns></returns>
        public static string[] GetTriggerConfigurationNames(UniversalPanicType type, string? id, UniversalPanicReason reason, bool increaseRepeatCount = true)
        {
            BasePollerData[]? bpd = SharedData.Config?.pollersAndTriggers?.pollers;
            GeneralData? dataGeneral = (GeneralData?)bpd?.FirstOrDefault(itm => itm is GeneralData && itm.enabled);
            List<PollerTriggerConfigBase> lstFound = [];

            if (type == UniversalPanicType.General)
            {
                if (dataGeneral != null)
                {
                    lstFound.AddRange(dataGeneral.generalTriggers.Where(itm => Discoverer.ContainsSameId(id, itm.id, true))
                                                               .Where(itm => Discoverer.ContainsSamePanic(reason, itm.panic))
                                                               .Where(itm => itm.enabled));

                    if (lstFound.Count == 0)
                    {
                        lstFound.AddRange(dataGeneral.generalTriggers.Where(itm => Discoverer.ContainsSameId(id, itm.id, true))
                                                                    .Where(itm => Discoverer.ContainsSameOrAnyPanic(reason, itm.panic))
                                                                    .Where(itm => itm.enabled));
                    }

                    if (lstFound.Count == 0)
                    {
                        lstFound.AddRange(dataGeneral.generalTriggers.Where(itm => Discoverer.ContainsSameOrAnyId(id, itm.id, true))
                                                                    .Where(itm => Discoverer.ContainsSamePanic(reason, itm.panic))
                                                                    .Where(itm => itm.enabled));
                    }

                    if (lstFound.Count == 0)
                    {
                        lstFound.AddRange(dataGeneral.generalTriggers.Where(itm => Discoverer.ContainsSameOrAnyId(id, itm.id, true))
                                                                   .Where(itm => Discoverer.ContainsSameOrAnyPanic(reason, itm.panic))
                                                                   .Where(itm => itm.enabled));
                    }
                }
            }
            if (type == UniversalPanicType.Controller)
            {
                ControllerData? data = (ControllerData?)bpd?.FirstOrDefault(itm => itm is ControllerData && itm.enabled);
                if (data != null)
                {
                    lstFound.AddRange(data.generalTriggers.Where(itm => Discoverer.ContainsSamePanic(reason, itm.panic))
                                                        .Where(itm => itm.enabled));

                    if (lstFound.Count == 0)
                    {
                        lstFound.AddRange(data.generalTriggers.Where(itm => Discoverer.ContainsSameOrAnyPanic(reason, itm.panic))
                                                                .Where(itm => itm.enabled));
                    }
                }
            }
            if (type == UniversalPanicType.ControllerPollers)
            {
                ControllerData? data = (ControllerData?)bpd?.FirstOrDefault(itm => itm is ControllerData && itm.enabled);
                if (data != null)
                {
                    lstFound.AddRange(data.pollerTriggers.Where(itm => Discoverer.ContainsSameId(id, itm.id, true))
                                                        .Where(itm => Discoverer.ContainsSamePanic(reason, itm.panic))
                                                        .Where(itm => itm.enabled));

                    if (lstFound.Count == 0)
                    {
                        lstFound.AddRange(data.pollerTriggers.Where(itm => Discoverer.ContainsSameId(id, itm.id, true))
                                                            .Where(itm => Discoverer.ContainsSameOrAnyPanic(reason, itm.panic))
                                                            .Where(itm => itm.enabled));
                    }

                    if (lstFound.Count == 0)
                    {
                        lstFound.AddRange(data.pollerTriggers.Where(itm => Discoverer.ContainsSameOrAnyId(id, itm.id, true))
                                                            .Where(itm => Discoverer.ContainsSamePanic(reason, itm.panic))
                                                            .Where(itm => itm.enabled));
                    }

                    if (lstFound.Count == 0)
                    {
                        lstFound.AddRange(data.pollerTriggers.Where(itm => Discoverer.ContainsSameOrAnyId(id, itm.id, true))
                                                        .Where(itm => Discoverer.ContainsSameOrAnyPanic(reason, itm.panic))
                                                        .Where(itm => itm.enabled));
                    }
                }
            }
            else if (type == UniversalPanicType.CountdownPoller)
            {
                CountdownPollerData? data = (CountdownPollerData?)bpd?.FirstOrDefault(itm => itm is CountdownPollerData && itm.enabled);
                if (data != null)
                {
                    lstFound.AddRange(data.generalTriggers.Where(itm => Discoverer.ContainsSamePanic(reason, itm.panic))
                                                        .Where(itm => itm.enabled));

                    if (lstFound.Count == 0)
                    {
                        lstFound.AddRange(data.generalTriggers.Where(itm => Discoverer.ContainsSameOrAnyPanic(reason, itm.panic))
                                                        .Where(itm => itm.enabled));
                    }
                }
            }
            else if (type == UniversalPanicType.Countdown)
            {
                CountdownPollerData? data = (CountdownPollerData?)bpd?.FirstOrDefault(itm => itm is CountdownPollerData && itm.enabled);
                if (data != null)
                {
                    lstFound.AddRange(data.countdownTriggers.Where(itm => Discoverer.ContainsSameId(id, itm.id, true))
                                                               .Where(itm => Discoverer.ContainsSamePanic(reason, itm.panic))
                                                               .Where(itm => itm.enabled));

                    if (lstFound.Count == 0)
                    {
                        lstFound.AddRange(data.countdownTriggers.Where(itm => Discoverer.ContainsSameId(id, itm.id, true))
                                                            .Where(itm => Discoverer.ContainsSameOrAnyPanic(reason, itm.panic))
                                                            .Where(itm => itm.enabled));
                    }

                    if (lstFound.Count == 0)
                    {
                        lstFound.AddRange(data.countdownTriggers.Where(itm => Discoverer.ContainsSameOrAnyId(id, itm.id, true))
                                                            .Where(itm => Discoverer.ContainsSamePanic(reason, itm.panic))
                                                            .Where(itm => itm.enabled));
                    }

                    if (lstFound.Count == 0)
                    {
                        lstFound.AddRange(data.countdownTriggers.Where(itm => Discoverer.ContainsSameOrAnyId(id, itm.id, true))
                                                            .Where(itm => Discoverer.ContainsSameOrAnyPanic(reason, itm.panic))
                                                            .Where(itm => itm.enabled));
                    }
                }
            }
            else if (type == UniversalPanicType.SwitchBotPoller)
            {
                SwitchBotPollerData? data = (SwitchBotPollerData?)bpd?.FirstOrDefault(itm => itm is SwitchBotPollerData && itm.enabled);
                if (data != null)
                {
                    lstFound.AddRange(data.generalTriggers.Where(itm => Discoverer.ContainsSamePanic(reason, itm.panic))
                                                        .Where(itm => itm.enabled));

                    if (lstFound.Count == 0)
                    {
                        lstFound.AddRange(data.generalTriggers.Where(itm => Discoverer.ContainsSameOrAnyPanic(reason, itm.panic))
                                                        .Where(itm => itm.enabled));
                    }
                }
            }
            else if (type == UniversalPanicType.SwitchBotSwitch)
            {
                SwitchBotPollerData? data = (SwitchBotPollerData?)bpd?.FirstOrDefault(itm => itm is SwitchBotPollerData && itm.enabled);
                if (data != null)
                {
                    lstFound.AddRange(data.switchTriggers.Where(itm => Discoverer.ContainsSameId(id, itm.id, false))
                                                               .Where(itm => Discoverer.ContainsSamePanic(reason, itm.panic))
                                                               .Where(itm => itm.enabled));

                    if (lstFound.Count == 0)
                    {
                        lstFound.AddRange(data.switchTriggers.Where(itm => Discoverer.ContainsSameId(id, itm.id, true))
                                                            .Where(itm => Discoverer.ContainsSameOrAnyPanic(reason, itm.panic))
                                                            .Where(itm => itm.enabled));
                    }

                    if (lstFound.Count == 0)
                    {
                        lstFound.AddRange(data.switchTriggers.Where(itm => Discoverer.ContainsSameOrAnyId(id, itm.id, true))
                                                            .Where(itm => Discoverer.ContainsSamePanic(reason, itm.panic))
                                                            .Where(itm => itm.enabled));
                    }

                    if (lstFound.Count == 0)
                    {
                        lstFound.AddRange(data.switchTriggers.Where(itm => Discoverer.ContainsSameOrAnyId(id, itm.id, false))
                                                            .Where(itm => Discoverer.ContainsSameOrAnyPanic(reason, itm.panic))
                                                            .Where(itm => itm.enabled));
                    }
                }
            }
            else if (type == UniversalPanicType.FritzPoller)
            {
                FritzPollerData? data = (FritzPollerData?)bpd?.FirstOrDefault(itm => itm is FritzPollerData && itm.enabled);
                if (data != null)
                {
                    lstFound.AddRange(data.generalTriggers.Where(itm => Discoverer.ContainsSamePanic(reason, itm.panic))
                                                        .Where(itm => itm.enabled));

                    if (lstFound.Count == 0)
                    {
                        lstFound.AddRange(data.generalTriggers.Where(itm => Discoverer.ContainsSameOrAnyPanic(reason, itm.panic))
                                                        .Where(itm => itm.enabled));
                    }
                }
            }
            else if (type == UniversalPanicType.FritzSwitch)
            {
                FritzPollerData? data = (FritzPollerData?)bpd?.FirstOrDefault(itm => itm is FritzPollerData && itm.enabled);
                if (data != null)
                {
                    lstFound.AddRange(data.switchTriggers.Where(itm => Discoverer.ContainsSameId(id, itm.id, false))
                                                               .Where(itm => Discoverer.ContainsSamePanic(reason, itm.panic))
                                                               .Where(itm => itm.enabled));

                    if (lstFound.Count == 0)
                    {
                        lstFound.AddRange(data.switchTriggers.Where(itm => Discoverer.ContainsSameId(id, itm.id, true))
                                                            .Where(itm => Discoverer.ContainsSameOrAnyPanic(reason, itm.panic))
                                                            .Where(itm => itm.enabled));
                    }

                    if (lstFound.Count == 0)
                    {
                        lstFound.AddRange(data.switchTriggers.Where(itm => Discoverer.ContainsSameOrAnyId(id, itm.id, true))
                                                            .Where(itm => Discoverer.ContainsSamePanic(reason, itm.panic))
                                                            .Where(itm => itm.enabled));
                    }

                    if (lstFound.Count == 0)
                    {
                        lstFound.AddRange(data.switchTriggers.Where(itm => Discoverer.ContainsSameOrAnyId(id, itm.id, false))
                                                            .Where(itm => Discoverer.ContainsSameOrAnyPanic(reason, itm.panic))
                                                            .Where(itm => itm.enabled));
                    }
                }
            }
            else if (type == UniversalPanicType.ClewareUSBPoller)
            {
                ClewareUSBPollerData? data = (ClewareUSBPollerData?)bpd?.FirstOrDefault(itm => itm is ClewareUSBPollerData && itm.enabled);
                if (data != null)
                {
                    lstFound.AddRange(data.generalTriggers.Where(itm => Discoverer.ContainsSamePanic(reason, itm.panic))
                                                        .Where(itm => itm.enabled));

                    if (lstFound.Count == 0)
                    {
                        lstFound.AddRange(data.generalTriggers.Where(itm => Discoverer.ContainsSameOrAnyPanic(reason, itm.panic))
                                                        .Where(itm => itm.enabled));
                    }
                }
            }
            else if (type == UniversalPanicType.ClewareUSBSwitch)
            {
                ClewareUSBPollerData? data = (ClewareUSBPollerData?)bpd?.FirstOrDefault(itm => itm is ClewareUSBPollerData && itm.enabled);
                if (data != null)
                {
                    lstFound.AddRange(data.switchTriggers.Where(itm => Discoverer.ContainsSameId(id, itm.id, false))
                                                               .Where(itm => Discoverer.ContainsSamePanic(reason, itm.panic))
                                                               .Where(itm => itm.enabled));

                    if (lstFound.Count == 0)
                    {
                        lstFound.AddRange(data.switchTriggers.Where(itm => Discoverer.ContainsSameId(id, itm.id, true))
                                                            .Where(itm => Discoverer.ContainsSameOrAnyPanic(reason, itm.panic))
                                                            .Where(itm => itm.enabled));
                    }

                    if (lstFound.Count == 0)
                    {
                        lstFound.AddRange(data.switchTriggers.Where(itm => Discoverer.ContainsSameOrAnyId(id, itm.id, true))
                                                            .Where(itm => Discoverer.ContainsSamePanic(reason, itm.panic))
                                                            .Where(itm => itm.enabled));
                    }

                    if (lstFound.Count == 0)
                    {
                        lstFound.AddRange(data.switchTriggers.Where(itm => Discoverer.ContainsSameOrAnyId(id, itm.id, false))
                                                            .Where(itm => Discoverer.ContainsSameOrAnyPanic(reason, itm.panic))
                                                            .Where(itm => itm.enabled));
                    }
                }
            }
            else if (type == UniversalPanicType.ReaderFilePoller)
            {
                RemoteFilePollerData? data = (RemoteFilePollerData?)bpd?.FirstOrDefault(itm => itm is RemoteFilePollerData && itm.enabled);
                if (data != null)
                {
                    lstFound.AddRange(data.generalTriggers.Where(itm => Discoverer.ContainsSamePanic(reason, itm.panic))
                                                        .Where(itm => itm.enabled));

                    if (lstFound.Count == 0)
                    {
                        lstFound.AddRange(data.generalTriggers.Where(itm => Discoverer.ContainsSameOrAnyPanic(reason, itm.panic))
                                                        .Where(itm => itm.enabled));
                    }
                }
            }
            else if (type == UniversalPanicType.ReaderFile)
            {
                RemoteFilePollerData? data = (RemoteFilePollerData?)bpd?.FirstOrDefault(itm => itm is RemoteFilePollerData && itm.enabled);
                if (data != null)
                {
                    lstFound.AddRange(data.fileTriggers.Where(itm => Discoverer.ContainsSameId(id, itm.id, false))
                                                    .Where(itm => Discoverer.ContainsSamePanic(reason, itm.panic))
                                                    .Where(itm => itm.enabled));

                    if (lstFound.Count == 0)
                    {
                        lstFound.AddRange(data.fileTriggers.Where(itm => Discoverer.ContainsSameId(id, itm.id, true))
                                                            .Where(itm => Discoverer.ContainsSameOrAnyPanic(reason, itm.panic))
                                                            .Where(itm => itm.enabled));
                    }

                    if (lstFound.Count == 0)
                    {
                        lstFound.AddRange(data.fileTriggers.Where(itm => Discoverer.ContainsSameOrAnyId(id, itm.id, true))
                                                            .Where(itm => Discoverer.ContainsSamePanic(reason, itm.panic))
                                                            .Where(itm => itm.enabled));
                    }

                    if (lstFound.Count == 0)
                    {
                        lstFound.AddRange(data.fileTriggers.Where(itm => Discoverer.ContainsSameOrAnyId(id, itm.id, false))
                                                            .Where(itm => Discoverer.ContainsSameOrAnyPanic(reason, itm.panic))
                                                            .Where(itm => itm.enabled));
                    }
                }
            }
            else if (type == UniversalPanicType.WriterFilePoller)
            {
                LocalFilePollerData? data = (LocalFilePollerData?)bpd?.FirstOrDefault(itm => itm is LocalFilePollerData && itm.enabled);
                if (data != null)
                {
                    lstFound.AddRange(data.generalTriggers.Where(itm => Discoverer.ContainsSamePanic(reason, itm.panic))
                                                        .Where(itm => itm.enabled));

                    if (lstFound.Count == 0)
                    {
                        lstFound.AddRange(data.generalTriggers.Where(itm => Discoverer.ContainsSameOrAnyPanic(reason, itm.panic))
                                                        .Where(itm => itm.enabled));

                    }
                }
            }
            else if (type == UniversalPanicType.WriterFile)
            {
                LocalFilePollerData? data = (LocalFilePollerData?)bpd?.FirstOrDefault(itm => itm is LocalFilePollerData && itm.enabled);
                if (data != null)
                {
                    lstFound.AddRange(data.fileTriggers.Where(itm => Discoverer.ContainsSameId(id, itm.id, false))
                                           .Where(itm => Discoverer.ContainsSamePanic(reason, itm.panic))
                                           .Where(itm => itm.enabled));

                    if (lstFound.Count == 0)
                    {
                        lstFound.AddRange(data.fileTriggers.Where(itm => Discoverer.ContainsSameId(id, itm.id, true))
                                                            .Where(itm => Discoverer.ContainsSameOrAnyPanic(reason, itm.panic))
                                                            .Where(itm => itm.enabled));
                    }

                    if (lstFound.Count == 0)
                    {
                        lstFound.AddRange(data.fileTriggers.Where(itm => Discoverer.ContainsSameOrAnyId(id, itm.id, true))
                                                            .Where(itm => Discoverer.ContainsSamePanic(reason, itm.panic))
                                                            .Where(itm => itm.enabled));
                    }

                    if (lstFound.Count == 0)
                    {
                        lstFound.AddRange(data.fileTriggers.Where(itm => Discoverer.ContainsSameOrAnyId(id, itm.id, false))
                                                            .Where(itm => Discoverer.ContainsSameOrAnyPanic(reason, itm.panic))
                                                            .Where(itm => itm.enabled));
                    }
                }
            }

            if (lstFound.Count == 0)
            {
                if (dataGeneral != null)
                {
                    lstFound.AddRange(dataGeneral.fallbackTriggers.Where(itm => Discoverer.ContainsSameId(id, itm.id, false))
                                                                .Where(itm => Discoverer.ContainsSamePanic(reason, itm.panic))
                                                                .Where(itm => itm.enabled));

                    if (lstFound.Count == 0)
                    {
                        lstFound.AddRange(dataGeneral.fallbackTriggers.Where(itm => Discoverer.ContainsSameId(id, itm.id, true))
                                                                    .Where(itm => Discoverer.ContainsSameOrAnyPanic(reason, itm.panic))
                                                                    .Where(itm => itm.enabled));
                    }

                    if (lstFound.Count == 0)
                    {
                        lstFound.AddRange(dataGeneral.fallbackTriggers.Where(itm => Discoverer.ContainsSameOrAnyId(id, itm.id, true))
                                                                    .Where(itm => Discoverer.ContainsSamePanic(reason, itm.panic))
                                                                    .Where(itm => itm.enabled));
                    }

                    if (lstFound.Count == 0)
                    {
                        lstFound.AddRange(dataGeneral.fallbackTriggers.Where(itm => Discoverer.ContainsSameOrAnyId(id, itm.id, false))
                                                                    .Where(itm => Discoverer.ContainsSameOrAnyPanic(reason, itm.panic))
                                                                    .Where(itm => itm.enabled));
                    }
                }
            }

            if (lstFound.Count > 0)
            {
                List<string> lstRetVal = [];

                foreach (var triggerConfig in lstFound)
                {
                    if (triggerConfig.AskForPermission(increaseRepeatCount))
                    {
                        if (!string.IsNullOrWhiteSpace(triggerConfig.config))
                        {
                            lstRetVal.Add(triggerConfig.config);
                        }
                    }
                    else
                    {
                        Logging?.LogInformation("Skipping trigger configuration {configname} for switch type {switchtype} with name/id {id} for reason {reason} because it has already been executed {maxrepeatecount} times", triggerConfig.config, type, id, reason, triggerConfig.maxRepeats);
                    }
                }

                if (lstRetVal.Count == 0)
                {
                    Logging?.LogInformation("All trigger configurations for switch type {switchtype} with name/id {id} for reason {reason} have been skipped because they have already been repeated their maximum amount of times or there was deliberately an empty configuration defined for this id/panic combination", type, id, reason);
                }

#pragma warning disable IDE0305 // Initialisierung der Sammlung vereinfachen
                return lstRetVal.ToArray();
#pragma warning restore IDE0305 // Initialisierung der Sammlung vereinfachen
            }
            else
            {
                Logging?.LogWarning("Found no enabled trigger defined for switch type {switchtype} with name/id {id} for reason {reason}", type, id, reason);

                return [];
            }
        }

        public static TriggerConfiguration? GetTriggerConfiguration(string name)
        {
            using (Logging?.BeginScope("GetTriggerConfiguration"))
            {
                if (!string.IsNullOrWhiteSpace(name))
                {
                    Logging?.LogTrace("Getting trigger configuration \"{name}\"", name);

                    TriggerConfig? config = SharedData.Config?.pollersAndTriggers?.triggerConfigs?.FirstOrDefault(itm => itm.id == name && itm.enabled);

                    if (config == null)
                    {
                        Logging?.LogWarning("Found no trigger configuration with name \"{name}\", looking for fallback configuration", name);
                        config = SharedData.Config?.pollersAndTriggers?.triggerConfigs?.FirstOrDefault(itm => itm.isFallback && itm.enabled);
                        if (config == null)
                        {
                            Logging?.LogWarning("Found no trigger fallback configuration");
                        }
                        else
                        {
                            Logging?.LogInformation("Found trigger fallback configuration with name \"{fallbackname}\", will use this instead of not found configuration \"{originalname}\"", config.id, name);
                        }
                    }

                    if (config != null)
                    {
                        List<TriggerEntity> lstTriggers = [];

                        if (config.triggers != null)
                        {
                            foreach (var trg in config.triggers)
                            {
                                if (trg is SingleDeviceTrigger trgdvc)
                                {
                                    if (trg.enabled)
                                    {
                                        if (!string.IsNullOrWhiteSpace(trgdvc.id))
                                        {
                                            if (trgdvc.deviceType == TriggerType.ClewareUSB)
                                            {
                                                lstTriggers.Add(new ClewareTrigger(trgdvc.id, !trgdvc.repeatable, _info?.GlobalCancellationToken));
                                            }
                                            else if (trgdvc.deviceType == TriggerType.Fritz)
                                            {
                                                lstTriggers.Add(new FritzTrigger(trgdvc.id, !trgdvc.repeatable, _info?.GlobalCancellationToken));
                                            }
                                            else if (trgdvc.deviceType == TriggerType.SwitchBot)
                                            {
                                                lstTriggers.Add(new SwitchBotTrigger(trgdvc.id, !trgdvc.repeatable, _info?.GlobalCancellationToken));
                                            }
                                            else if (trgdvc.deviceType == TriggerType.OSShutdown)
                                            {
                                                lstTriggers.Add(new OSShutdownTrigger(trgdvc.id, !trgdvc.repeatable, _info?.GlobalCancellationToken));
                                            }
                                            else
                                            {
                                                Logging?.LogError("Unknown device type {type} for trigger device", trgdvc.deviceType);
                                            }
                                        }
                                        else
                                        {
                                            Logging?.LogError("Empty device name for {type} in trigger device", trgdvc.deviceType);
                                        }
                                    }
                                    else
                                    {
                                        Logging?.LogDebug("Skipping trigger \"{triggername}\" in configuration \"{configname}\" as it was disabled", trgdvc.id, name);
                                    }
                                }
                                else
                                {
                                    if (trg.enabled)
                                    {
                                        if (trg is SingleDelayTrigger trgdel)
                                        {
                                            lstTriggers.Add(new DelayTrigger(trgdel.milliseconds, _info?.GlobalCancellationToken));
                                        }
                                        else
                                        {
                                            Logging?.LogError("Trigger device configuration was of unknown type");
                                        }
                                    }
                                    else
                                    {
                                        Logging?.LogDebug("Skipping a delay trigger in configuration \"{configname}\" as it was disabled", name);
                                    }
                                }
                            }
                        }

                        uint triggerDelay = c_TriggerTimeInSeconds_Startup +
                                                (uint)(lstTriggers.Count(itm => itm is ClewareTrigger) * c_TriggerTimeInSeconds_ClewareUSB) +
                                                (uint)(lstTriggers.Count(itm => itm is FritzTrigger) * c_TriggerTimeInSeconds_Fritz) +
                                                (uint)(lstTriggers.Count(itm => itm is SwitchBotTrigger) * c_TriggerTimeInSeconds_SwitchBot) +
                                                (uint)(lstTriggers.Count(itm => itm is OSShutdownTrigger) * c_TriggerTimeInSeconds_OSShutdown) +
                                                (uint)(lstTriggers.Where(itm => itm is DelayTrigger).Sum(itm => ((DelayTrigger)itm).DelayInSeconds));

                        if (lstTriggers.Any(itm => itm is not DelayTrigger))
                        {
                            Logging?.LogDebug("Prepared trigger configuration with name \"{name}\" with {triggercount} triggers and a max duration of {maxduration} seconds", name, lstTriggers.Count, triggerDelay);
#pragma warning disable CS8604 // Mögliches Nullverweisargument.
                            return new TriggerConfiguration(config.id, config.repeatable, lstTriggers, triggerDelay);
#pragma warning restore CS8604 // Mögliches Nullverweisargument.
                        }
                        else
                        {
                            Logging?.LogWarning("Trigger configuration with name \"{name}\" would have been empty (aside from delay triggers), not gonna execute this trigger configuration", name);

                            return null;
                        }
                    }
                    else
                    {
                        Logging?.LogWarning("Found no enabled trigger configuration with name \"{name}\"", name);

                        return null;
                    }
                }
                else
                {
                    Logging?.LogWarning("Cannot look for a trigger configuration with empty name");

                    return null;
                }
            }
        }

        #endregion

        #region Pollers

        public static List<ClewareUSBConfigMapping>? GetClewareUSBMapping()
        {
            return GetClewareUSBMapping(SharedData.Config);
        }

        public static List<ClewareUSBConfigMapping>? GetClewareUSBMapping(ConfigFile? configFile)
        {
            Microsoft.Extensions.Logging.ILogger? logging = SharedData.ConfigLogging;
            using (logging?.BeginScope("GetClewareUSBMapping"))
            {
                if (configFile != null && configFile.clewareUSBMappings != null)
                {
                    List<ClewareUSBConfigMapping> lstMappings = [];

                    foreach (var map in configFile.clewareUSBMappings)
                    {
                        if (!string.IsNullOrWhiteSpace(map.name) && map.id.HasValue)
                        {
                            if (!lstMappings.Any(itm => itm.id == map.id))
                            {
                                if (!lstMappings.Any(itm => itm.name == map.name))
                                {
                                    lstMappings.Add(map);
                                }
                                else
                                {
                                    logging?.LogWarning("The usb name {name} has already been mapped", map.name);
                                }
                            }
                            else
                            {
                                logging?.LogWarning("The usb id {id} has already been mapped", map.id);
                            }
                        }
                        else
                        {
                            logging?.LogWarning(@"Name or id are null or empty for usb mapping with name ""{name}"" and id ""{id}"" has already been mapped", map.name, map.id);
                        }
                    }

                    return lstMappings;
                }
                else
                {
                    return null;
                }
            }
        }

        public static List<FileConfigOptions>? GetRemoteReadFileOptions()
        {
            return GetRemoteReadFileOptions(SharedData.Config);
        }

        public static List<FileConfigOptions>? GetRemoteReadFileOptions(ConfigFile? configFile)
        {
            Microsoft.Extensions.Logging.ILogger? logging = SharedData.ConfigLogging;
            using (logging?.BeginScope("GetRemoteReadFiles"))
            {
                if (configFile != null)
                {
                    List<FileConfigOptions[]> lstCnfg = configFile.pollersAndTriggers?.pollers
                                                                        ?.Where(itm => itm is RemoteFilePollerData)
                                                                        ?.Cast<RemoteFilePollerData>()
                                                                        ?.Select(itm => itm.files)
                                                                        ?.ToList() ?? [];

                    List<FileConfigOptions> lstMappings = [];
                    foreach (var cnfg in lstCnfg)
                    {
                        foreach (var map in cnfg)
                        {
                            if (!string.IsNullOrWhiteSpace(map.name) && !string.IsNullOrWhiteSpace(map.path))
                            {
                                if (!lstMappings.Any(itm => itm.name != map.name))
                                {
                                    if (!lstMappings.Any(itm => itm.path != map.path))
                                    {
                                        lstMappings.Add(map);
                                    }
                                    else
                                    {
                                        logging?.LogWarning(@"The remote file path ""{path}"" has already been mapped", map.path);
                                    }
                                }
                                else
                                {
                                    logging?.LogWarning(@"The remote file name ""{name}"" has already been mapped", map.name);
                                }
                            }
                            else
                            {
                                logging?.LogWarning(@"Name or path are null or empty for remote file mapping with name ""{name}"" and path ""{path}"" has already been mapped", map.name, map.path);
                            }
                        }
                    }

                    return lstMappings;
                }
                else
                {
                    return null;
                }
            }
        }

        public static List<FileConfigOptions>? GetLocalWriteFileOptions()
        {
            return GetLocalWriteFileOptions(SharedData.Config);
        }

        public static List<FileConfigOptions>? GetLocalWriteFileOptions(ConfigFile? configFile)
        {
            Microsoft.Extensions.Logging.ILogger? logging = SharedData.ConfigLogging;
            using (logging?.BeginScope("GetLocalWriteFiles"))
            {
                if (configFile != null)
                {
                    List<FileConfigOptions[]> lstCnfg = configFile.pollersAndTriggers?.pollers
                                                                        ?.Where(itm => itm is LocalFilePollerData)
                                                                        ?.Cast<LocalFilePollerData>()
                                                                        ?.Select(itm => itm.files)
                                                                        ?.ToList() ?? [];

                    List<FileConfigOptions> lstMappings = [];
                    foreach (var cnfg in lstCnfg)
                    {
                        foreach (var map in cnfg)
                        {
                            if (!string.IsNullOrWhiteSpace(map.name) && !string.IsNullOrWhiteSpace(map.path))
                            {
                                if (!lstMappings.Any(itm => itm.name != map.name))
                                {
                                    if (!lstMappings.Any(itm => itm.path != map.path))
                                    {
                                        lstMappings.Add(map);
                                    }
                                    else
                                    {
                                        logging?.LogWarning(@"The local file path ""{path}"" has already been mapped", map.path);
                                    }
                                }
                                else
                                {
                                    logging?.LogWarning(@"The local file name ""{name}"" has already been mapped", map.name);
                                }
                            }
                            else
                            {
                                logging?.LogWarning(@"Name or path are null or empty for local file mapping with name ""{name}"" and path ""{path}"" has already been mapped", map.name, map.path);
                            }
                        }
                    }

                    return lstMappings;
                }
                else
                {
                    return null;
                }
            }
        }

        private static List<FritzPollerSwitch> GetFritzPollerSwitches(FritzPollerData opt, Microsoft.Extensions.Logging.ILogger? logger)
        {
            using (logger?.BeginScope("GetFritzPollerSwitches"))
            {
                List<FritzPollerSwitch> lstRetVal = [];
                FritzAPI? api = GetFritzAPI();

                if (api != null)
                {
                    foreach (var swt in opt.switches)
                    {
                        if (!string.IsNullOrWhiteSpace(swt.switchName))
                        {
                            if (swt.enabled)
                            {
                                lstRetVal.Add(
                                    new FritzPollerSwitch(
                                        new FritzPollerSwitchOptions()
                                        {
                                            Api = api,
                                            ArmPanicMode = swt.armPanicMode,
                                            EnterSafeMode = swt.enterSafeMode,
                                            LockTimeoutInMilliseconds = swt.lockTimeoutInMilliseconds,
                                            Logger = logger,
                                            MarkShutDownIfOff = swt.markShutDownIfOff,
                                            LowPowerCutOff = swt.lowPowerCutOff,
                                            TurnOffOnLowPower = swt.turnOffOnLowPower,
                                            SafeModeSensitive = swt.safeModeSensitive,
                                            MaxPowerWarn = swt.maxPowerWarn,
                                            MinPowerWarn = swt.minPowerWarn,
                                            MaxPower = swt.maxPower,
                                            MinPower = swt.minPower,
                                            SafeModePowerUpAlarm = swt.safeModePowerUpAlarm,
                                            SwitchName = swt.switchName,
                                            TurnOffOnPanic = swt.turnOffOnPanic,
                                        }
                                    ));
                            }
                            else
                            {
                                logger?.LogDebug("Fritz switch \"{name}\" was disabled, will ignore this switch", swt.switchName);
                            }
                        }
                        else
                        {
                            logger?.LogWarning("Fritz switch was defined with empty name, will ignore this switch");
                        }
                    }
                }

                return lstRetVal;
            }
        }

        public static FritzPoller? GetFritzPoller()
        {
            Microsoft.Extensions.Logging.ILogger? logger = GetLogger("Fritz.Poller");
            using (logger?.BeginScope("GetFritzPoller"))
            {
                var config = SharedData.Config;
                if (config != null)
                {
                    Logging?.LogTrace("Generating Fritz Poller");
                    FritzPollerData? opt = (FritzPollerData?)config?.pollersAndTriggers?.pollers?.FirstOrDefault(itm => itm is FritzPollerData && itm.enabled);

                    if (opt != null)
                    {
                        var switches = GetFritzPollerSwitches(opt, logger);
                        if (switches.Count > 0)
                        {
                            FritzPoller poller = new(new FritzPollerOptions()
                            {
                                Switches = switches,
                                AlertTimeInMilliseconds = opt.options.alertTimeInMilliseconds,
                                AutoSafeMode = opt.options.autoSafeMode,
                                MinPollerCountToArm = opt.options.minPollerCountToArm,
                                LockTimeoutInMilliseconds = opt.options.lockTimeoutInMilliseconds,
                                SleepTimeInMilliseconds = opt.options.sleepTimeInMilliseconds,
                                Logger = logger,
                            });

                            Logging?.LogInformation("Created new Fritz poller with {count} switches", switches.Count);

                            return poller;
                        }
                        else
                        {
                            Logging?.LogInformation("Not creating Fritz poller due to no enabled switches present");
                            return null;
                        }
                    }
                    else
                    {
                        return null;
                    }
                }
                else
                {
                    return null;
                }
            }
        }

        private static List<WatcherPollerFile> GetWriteFilePollerFiles(LocalFilePollerData opt, Microsoft.Extensions.Logging.ILogger? logger)
        {
            using (logger?.BeginScope("GetWriteFilePollerFiles"))
            {
                List<WatcherPollerFile> lstRetVal = [];

                foreach (var swt in opt.files)
                {
                    if (!string.IsNullOrWhiteSpace(swt.name))
                    {
                        if (!string.IsNullOrWhiteSpace(swt.path))
                        {
                            if (swt.enabled)
                            {
                                lstRetVal.Add(
                                new WatcherPollerFile(
                                    new WatcherPollerFileOptions()
                                    {
                                        LockTimeoutInMilliseconds = swt.lockTimeoutInMilliseconds,
                                        FileAccessRetryCountMax = swt.fileAccessRetryCountMax,
                                        FileAccessRetryMillisecondsMax = swt.fileAccessRetryMillisecondsMax,
                                        FileAccessRetryMillisecondsMin = swt.fileAccessRetryMillisecondsMin,
                                        FilePath = swt.path,
                                        MailPriority = swt.mailPriority,
                                        MaxUpdateAgeInNegativeSeconds = swt.maxUpdateAgeInNegativeSeconds,
                                        MaxUpdateAgeInSeconds = swt.maxUpdateAgeInSeconds,
                                        StateCheck = swt.stateCheck,
                                        StateTimestampCheck = swt.stateTimestampCheck,
                                        WatcherFileName = swt.name,
                                        WriteStateOnPanic = swt.writeStateOnPanic,
                                        SafeModeSensitive = swt.safeModeSensitive,
                                        Logger = logger,
                                    }
                                ));
                            }
                            else
                            {
                                logger?.LogDebug("Local write file \"{name}\" was disabled, will ignore this file", swt.name);
                            }
                        }
                        else
                        {
                            logger?.LogWarning(@"Local write file ""{name}"" was defined with empty path, will ignore this file", swt.name);
                        }
                    }
                    else
                    {
                        logger?.LogWarning(@"Local write file ""{}"" was defined with empty name, will ignore this file", swt.path);
                    }
                }

                return lstRetVal;
            }
        }

        public static WatcherFilePoller? GetWriteFilePoller()
        {
            Microsoft.Extensions.Logging.ILogger? logger = GetLogger("LocalWriteFile.Poller");
            using (logger?.BeginScope("GetWriteFilePoller"))
            {
                var config = SharedData.Config;
                if (config != null)
                {
                    Logging?.LogTrace("Generating LocalWriteFile Poller");
                    LocalFilePollerData? opt = (LocalFilePollerData?)config?.pollersAndTriggers?.pollers?.FirstOrDefault(itm => itm is LocalFilePollerData && itm.enabled);

                    if (opt != null)
                    {
                        var files = GetWriteFilePollerFiles(opt, logger);
                        if (files.Count > 0)
                        {
                            WatcherFilePoller poller = new(new WatcherFilePollerOptions()
                            {
                                Files = files,
                                AlertTimeInMilliseconds = opt.options.alertTimeInMilliseconds,
                                AutoSafeMode = opt.options.autoSafeMode,
                                MinPollerCountToArm = opt.options.minPollerCountToArm,
                                LockTimeoutInMilliseconds = opt.options.lockTimeoutInMilliseconds,
                                SleepTimeInMilliseconds = opt.options.sleepTimeInMilliseconds,
                                WriteTo = true,
                                Logger = logger,
                            });

                            Logging?.LogInformation("Created new local write file poller with {count} files", files.Count);

                            return poller;
                        }
                        else
                        {
                            Logging?.LogInformation("Not creating local write file poller due to no enabled files present");
                            return null;
                        }
                    }
                    else
                    {
                        return null;
                    }
                }
                else
                {
                    return null;
                }
            }
        }

        private static List<WatcherPollerFile> GetReadFilePollerFiles(RemoteFilePollerData opt, Microsoft.Extensions.Logging.ILogger? logger)
        {
            using (logger?.BeginScope("GetReadFilePollerFiles"))
            {
                List<WatcherPollerFile> lstRetVal = [];

                foreach (var swt in opt.files)
                {
                    if (!string.IsNullOrWhiteSpace(swt.name))
                    {
                        if (!string.IsNullOrWhiteSpace(swt.path))
                        {
                            if (swt.enabled)
                            {
                                lstRetVal.Add(
                                new WatcherPollerFile(
                                    new WatcherPollerFileOptions()
                                    {
                                        LockTimeoutInMilliseconds = swt.lockTimeoutInMilliseconds,
                                        FileAccessRetryCountMax = swt.fileAccessRetryCountMax,
                                        FileAccessRetryMillisecondsMax = swt.fileAccessRetryMillisecondsMax,
                                        FileAccessRetryMillisecondsMin = swt.fileAccessRetryMillisecondsMin,
                                        FilePath = swt.path,
                                        MailPriority = swt.mailPriority,
                                        MaxUpdateAgeInNegativeSeconds = swt.maxUpdateAgeInNegativeSeconds,
                                        MaxUpdateAgeInSeconds = swt.maxUpdateAgeInSeconds,
                                        StateCheck = swt.stateCheck,
                                        StateTimestampCheck = swt.stateTimestampCheck,
                                        WatcherFileName = swt.name,
                                        WriteStateOnPanic = swt.writeStateOnPanic,
                                        SafeModeSensitive = swt.safeModeSensitive,
                                        Logger = logger,
                                    }
                                ));
                            }
                            else
                            {
                                logger?.LogDebug("Local write file \"{name}\" was disabled, will ignore this file", swt.name);
                            }
                        }
                        else
                        {
                            logger?.LogWarning(@"Remote read file ""{name}"" was defined with empty path, will ignore this file", swt.name);
                        }
                    }
                    else
                    {
                        logger?.LogWarning(@"Remote read file ""{}"" was defined with empty name, will ignore this file", swt.path);
                    }
                }

                return lstRetVal;
            }
        }

        public static WatcherFilePoller? GetReadFilePoller()
        {
            Microsoft.Extensions.Logging.ILogger? logger = GetLogger("RemoteReadFile.Poller");
            using (logger?.BeginScope("GetReadFilePoller"))
            {
                var config = SharedData.Config;
                if (config != null)
                {
                    Logging?.LogTrace("Generating RemoteReadFile Poller");
                    RemoteFilePollerData? opt = (RemoteFilePollerData?)config?.pollersAndTriggers?.pollers?.FirstOrDefault(itm => itm is RemoteFilePollerData && itm.enabled);

                    if (opt != null)
                    {
                        var files = GetReadFilePollerFiles(opt, logger);
                        if (files.Count > 0)
                        {
                            WatcherFilePoller poller = new(new WatcherFilePollerOptions()
                            {
                                Files = files,
                                AlertTimeInMilliseconds = opt.options.alertTimeInMilliseconds,
                                AutoSafeMode = opt.options.autoSafeMode,
                                MinPollerCountToArm = opt.options.minPollerCountToArm,
                                LockTimeoutInMilliseconds = opt.options.lockTimeoutInMilliseconds,
                                SleepTimeInMilliseconds = opt.options.sleepTimeInMilliseconds,
                                WriteTo = false,
                                Logger = logger,
                            });

                            Logging?.LogInformation("Created new remote read file poller with {count} files", files.Count);

                            return poller;
                        }
                        else
                        {
                            Logging?.LogInformation("Not creating remote read file poller due to no enabled files present");
                            return null;
                        }
                    }
                    else
                    {
                        return null;
                    }
                }
                else
                {
                    return null;
                }
            }
        }

        public static CountdownPoller? GetCountdownPoller()
        {
            Microsoft.Extensions.Logging.ILogger? logger = GetLogger("Countdown.Poller");
            using (logger?.BeginScope("GetCountdownPoller"))
            {
                var config = SharedData.Config;
                if (config != null)
                {
                    Logging?.LogTrace("Generating Countdown Poller");
                    CountdownPollerData? opt = (CountdownPollerData?)config?.pollersAndTriggers?.pollers?.FirstOrDefault(itm => itm is CountdownPollerData && itm.enabled);

                    if (opt != null)
                    {
                        if (!opt.options.countdownT0TimestampUTC.HasValue)
                        {
                            opt.options.CalculateCountdownT0();
                        }

                        CountdownPoller poller = new(new CountdownPollerOptions()
                        {
                            AlertTimeInMilliseconds = opt.options.alertTimeInMilliseconds,
                            AutoSafeMode = opt.options.autoSafeMode,
                            MinPollerCountToArm = opt.options.minPollerCountToArm,
                            LockTimeoutInMilliseconds = opt.options.lockTimeoutInMilliseconds,
                            SleepTimeInMilliseconds = opt.options.sleepTimeInMilliseconds,
                            CheckShutDownAfterMinutes = opt.options.checkShutDownAfterMinutes,
                            CountdownAutoSafeModeMinutes = opt.options.countdownAutoSafeModeMinutes,
                            CountdownT0UTC = opt.options.countdownT0TimestampUTC ?? DateTime.UtcNow.AddHours(6),
                            ShutDownOnT0 = opt.options.shutDownOnT0,
                            MailSettings = new MailingOptions()
                            {
                                CheckForShutDown = opt.mailingOptions.checkForShutDown,
                                CheckForShutDownVerified = opt.mailingOptions.checkForShutDownVerified,
                                CountdownSendMailMinutes = opt.mailingOptions.countdownSendMailMinutes,
                                MailAddress_Simulate = opt.mailingOptions.mailAddress_Simulate,
                                MailConfig_Emergency = opt.mailingOptions.mailConfig_Emergency != null &&
                                                            opt.mailingOptions.mailConfig_Emergency.Length > 0 ?
                                                                new List<string>(opt.mailingOptions.mailConfig_Emergency) : [],
                                MailConfig_Info = opt.mailingOptions.mailConfig_Info != null &&
                                                            opt.mailingOptions.mailConfig_Info.Length > 0 ?
                                                                new List<string>(opt.mailingOptions.mailConfig_Info) : [],
                                MailSettings_FromAddress = opt.mailingOptions.mailSettings_FromAddress,
                                MailSettings_Password = opt.mailingOptions.mailSettings_Password,
                                MailSettings_Port = opt.mailingOptions.mailSettings_Port,
                                MailSettings_SmtpServer = opt.mailingOptions.mailSettings_SmtpServer,
                                MailSettings_User = opt.mailingOptions.mailSettings_User,
                                MailSettings_UseSsl = opt.mailingOptions.mailSettings_UseSsl,
                                SendEmergencyMails = opt.mailingOptions.enabled && opt.mailingOptions.sendEmergencyMails,
                                SendInfoMails = opt.mailingOptions.enabled && opt.mailingOptions.sendInfoMails,
                                SimulateMails = opt.mailingOptions.simulateMails,
                            },
                            Logger = logger,
                        });

                        for (int i = 0; i < poller.Options.MailSettings.MailConfig_Emergency.Count; i++)
                        {
                            string strFilePath = poller.Options.MailSettings.MailConfig_Emergency[i];
                            try
                            {
                                strFilePath = Path.GetFullPath(strFilePath);
                                poller.Options.MailSettings.MailConfig_Emergency[i] = strFilePath;

                                if (!File.Exists(strFilePath))
                                {
                                    Logging?.LogWarning(@"While trying to add emergency mail configuration file, file ""{path}"" doesn't exist", strFilePath);
                                }
                            }
                            catch (Exception ex)
                            {
                                Logging?.LogError(ex, @"While trying to fully qualify emergency mail configuration file path ""{path}"": {message}", strFilePath, ex.Message);
                            }
                        }

                        for (int i = 0; i < poller.Options.MailSettings.MailConfig_Info.Count; i++)
                        {
                            string strFilePath = poller.Options.MailSettings.MailConfig_Info[i];
                            try
                            {
                                strFilePath = Path.GetFullPath(strFilePath);
                                poller.Options.MailSettings.MailConfig_Info[i] = strFilePath;

                                if (!File.Exists(strFilePath))
                                {
                                    Logging?.LogWarning(@"While trying to add info mail configuration file, file ""{path}"" doesn't exist", strFilePath);
                                }
                            }
                            catch (Exception ex)
                            {
                                Logging?.LogError(ex, @"While trying to fully qualify info mail configuration file path ""{path}"": {message}", strFilePath, ex.Message);
                            }
                        }

                        Logging?.LogInformation("Created new Countdown poller with T0 {timestamp} UTC", poller.Options.CountdownT0UTC);

                        return poller;
                    }
                    else
                    {
                        return null;
                    }
                }
                else
                {
                    return null;
                }
            }
        }

        private static List<ClewarePollerSwitch> GetClewareUSBPollerSwitches(ClewareUSBPollerData opt, Microsoft.Extensions.Logging.ILogger? logger)
        {
            using (logger?.BeginScope("GetClewareUSBPollerSwitches"))
            {
                List<ClewarePollerSwitch> lstRetVal = [];
                ClewareAPI? api = GetClewareAPI();

                if (api != null)
                {
                    foreach (var swt in opt.switches)
                    {
                        if (!string.IsNullOrWhiteSpace(swt.usbSwitchName))
                        {
                            if (swt.enabled)
                            {
                                lstRetVal.Add(
                                    new ClewarePollerSwitch(
                                        new ClewarePollerSwitchOptions()
                                        {
                                            Api = api,
                                            ArmPanicMode = swt.armPanicMode,
                                            EnterSafeMode = swt.enterSafeMode,
                                            LockTimeoutInMilliseconds = swt.lockTimeoutInMilliseconds,
                                            Logger = logger,
                                            MarkShutDownIfOff = swt.markShutDownIfOff,
                                            SafeModeSensitive = swt.safeModeSensitive,
                                            SafeModeTurnOnAlarm = swt.safeModeTurnOnAlarm,
                                            TurnOffOnPanic = swt.turnOffOnPanic,
                                            USBSwitchName = swt.usbSwitchName,
                                        }
                                    ));
                            }
                            else
                            {
                                logger?.LogDebug("ClewareUSB switch \"{name}\" was disabled, will ignore this switch", swt.usbSwitchName);
                            }
                        }
                        else
                        {
                            logger?.LogWarning("ClewareUSB switch was defined with empty name, will ignore this switch");
                        }
                    }
                }

                return lstRetVal;
            }
        }

        public static ClewarePoller? GetClewarePoller()
        {
            Microsoft.Extensions.Logging.ILogger? logger = GetLogger("ClewareUSB.Poller");
            using (logger?.BeginScope("GetClewarePoller"))
            {
                var config = SharedData.Config;
                if (config != null)
                {
                    Logging?.LogTrace("Generating ClewareUSB Poller");
                    ClewareUSBPollerData? opt = (ClewareUSBPollerData?)config?.pollersAndTriggers?.pollers?.FirstOrDefault(itm => itm is ClewareUSBPollerData && itm.enabled);

                    if (opt != null)
                    {
                        var switches = GetClewareUSBPollerSwitches(opt, logger);
                        if (switches.Count > 0)
                        {
                            ClewarePoller poller = new(new ClewarePollerOptions()
                            {
                                Switches = switches,
                                AlertTimeInMilliseconds = opt.options.alertTimeInMilliseconds,
                                AutoSafeMode = opt.options.autoSafeMode,
                                MinPollerCountToArm = opt.options.minPollerCountToArm,
                                LockTimeoutInMilliseconds = opt.options.lockTimeoutInMilliseconds,
                                SleepTimeInMilliseconds = opt.options.sleepTimeInMilliseconds,
                                Logger = logger,
                            });

                            Logging?.LogInformation("Created new ClewareUSB poller with {count} switches", switches.Count);

                            return poller;
                        }
                        else
                        {
                            Logging?.LogInformation("Not creating ClewareUSB poller due to no enabled switches present");
                            return null;
                        }
                    }
                    else
                    {
                        return null;
                    }
                }
                else
                {
                    return null;
                }
            }
        }

        private static List<SwitchBotPollerSwitch> GetSwitchBotPollerSwitches(SwitchBotPollerData opt, Microsoft.Extensions.Logging.ILogger? logger)
        {
            using (logger?.BeginScope("GetSwitchBotPollerSwitches"))
            {
                List<SwitchBotPollerSwitch> lstRetVal = [];
                SwitchBotAPI? api = GetSwitchBotAPI();

                if (api != null)
                {
                    foreach (var swt in opt.switches)
                    {
                        if (!string.IsNullOrWhiteSpace(swt.switchName))
                        {
                            if (swt.enabled)
                            {
                                lstRetVal.Add(
                                    new SwitchBotPollerSwitch(
                                        new SwitchBotPollerSwitchOptions()
                                        {
                                            Api = api,
                                            ArmPanicMode = swt.armPanicMode,
                                            EnterSafeMode = swt.enterSafeMode,
                                            LockTimeoutInMilliseconds = swt.lockTimeoutInMilliseconds,
                                            Logger = logger,
                                            MarkShutDownIfOff = swt.markShutDownIfOff,
                                            SafeModeSensitive = swt.safeModeSensitive,
                                            MinBattery = swt.minBattery,
                                            StateCheck = swt.stateCheck,
                                            StrictBatteryCheck = swt.strictBatteryCheck,
                                            StrictStateCheck = swt.strictStateCheck,
                                            SwitchBotName = swt.switchName,
                                            TurnOffOnPanic = swt.turnOffOnPanic,
                                            ZeroBatteryIsValidPanic = swt.zeroBatteryIsValidPanic,
                                        }
                                    ));
                            }
                            else
                            {
                                logger?.LogDebug("SwitchBot switch \"{name}\" was disabled, will ignore this switch", swt.switchName);
                            }
                        }
                        else
                        {
                            logger?.LogWarning("SwitchBot switch was defined with empty name, will ignore this switch");
                        }
                    }
                }

                return lstRetVal;
            }
        }

        public static SwitchBotPoller? GetSwitchBotPoller()
        {
            Microsoft.Extensions.Logging.ILogger? logger = GetLogger("SwichBot.Poller");
            using (logger?.BeginScope("GetSwitchBotPoller"))
            {
                var config = SharedData.Config;
                if (config != null)
                {
                    Logging?.LogTrace("Generating SwitchBot Poller");
                    SwitchBotPollerData? opt = (SwitchBotPollerData?)config?.pollersAndTriggers?.pollers?.FirstOrDefault(itm => itm is SwitchBotPollerData && itm.enabled);

                    if (opt != null)
                    {
                        var switches = GetSwitchBotPollerSwitches(opt, logger);
                        if (switches.Count > 0)
                        {
                            SwitchBotPoller poller = new(new SwitchBotPollerOptions()
                            {
                                Switches = switches,
                                AlertTimeInMilliseconds = opt.options.alertTimeInMilliseconds,
                                AutoSafeMode = opt.options.autoSafeMode,
                                MinPollerCountToArm = opt.options.minPollerCountToArm,
                                LockTimeoutInMilliseconds = opt.options.lockTimeoutInMilliseconds,
                                SleepTimeInMilliseconds = opt.options.sleepTimeInMilliseconds,
                                Logger = logger,
                            });

                            Logging?.LogInformation("Created new SwitchBot poller with {count} switches", switches.Count);

                            return poller;
                        }
                        else
                        {
                            Logging?.LogInformation("Not creating SwitchBot poller due to no enabled switches present");
                            return null;
                        }
                    }
                    else
                    {
                        return null;
                    }
                }
                else
                {
                    return null;
                }
            }
        }

        #endregion

        #region Turn on devices

        public static long GetTurnOnTimeInSeconds()
        {
            ConfigFile? configFile = SharedData.Config;

            if (configFile != null)
            {
                return GetTurnOnTimeInSeconds(configFile);
            }
            else
            {
                return 0;
            }
        }

        public static bool TurnOnDevices()
        {
            ConfigFile? configFile = SharedData.Config;

            if (configFile != null)
            {
                return TurnOnDevices(configFile);
            }
            else
            {
                Logging?.LogError("Cannot turn on devices because no configuration was loaded");

                return false;
            }
        }

        public static bool TurnOnDevices(ConfigFile configFile)
        {
            using (Logging?.BeginScope("TurnOnDevices"))
            {
                ANUBISFritzAPI.FritzAPI? apiFritz = null;
                ANUBISClewareAPI.ClewareAPI? apiCleware = null;
                ANUBISSwitchBotAPI.SwitchBotAPI? apiSwitchBot = null;
                long? waitSecondsNext = null;
                bool allTurnedOn = true;

                foreach (var entton in configFile.turnOn.Where(itm => itm.enabled))
                {
                    if (waitSecondsNext.HasValue)
                    {
                        Logging?.LogInformation("Waiting {seconds} seconds before moving on to next step...", waitSecondsNext.Value);
                        Generator.WaitSeconds(waitSecondsNext);
                        Logging?.LogInformation("done waiting {seconds} seconds, moving on to next step", waitSecondsNext.Value);
                        waitSecondsNext = null;
                    }

                    if (entton is DeviceTurnOn tondvc)
                    {
                        if (!string.IsNullOrWhiteSpace(tondvc.id))
                        {
                            Logging?.LogInformation("Turning on {type} device with name/id \"{id}\"", tondvc.deviceType, tondvc.id);
                            switch (tondvc.deviceType)
                            {
                                case TriggerType.SwitchBot:
                                    apiSwitchBot ??= Generator.GetSwitchBotAPI();

                                    if (apiSwitchBot != null)
                                    {
                                        try
                                        {
                                            var result = apiSwitchBot.TurnSwitchBotOnByName(tondvc.id);

                                            if (result != null)
                                            {
                                                if (result.Power == SwitchBotPowerState.On)
                                                {
                                                    Logging?.LogInformation("Successfully turned on SwitchBot switch with name/id \"{id}\"", tondvc.id);
                                                    waitSecondsNext = tondvc.waitSecondsAfterTurnOn;
                                                }
                                                else
                                                {
                                                    Logging?.LogWarning("Could not turn on SwitchBot switch with name/id \"{id}\", result was {result}", tondvc.id, result.Power);
                                                    allTurnedOn = false;
                                                }
                                            }
                                            else
                                            {
                                                Logging?.LogWarning("Could not turn on SwitchBot switch with name/id \"{id}\", result was null", tondvc.id);
                                                allTurnedOn = false;
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            Logging?.LogWarning(ex, "Could not turn on SwitchBot switch with name/id \"{id}\", because of the following error: {message}", tondvc.id, ex.Message);
                                            allTurnedOn = false;
                                        }
                                        Logging?.LogWarning("VISUALLY CONFIRM SWITCH CONTROLLED BY SwitchBot WITH NAME/ID \"{id}\" IS IN TURNED ON POSITION, might have actually turned off now by this automated action (turn it on manually if so)", tondvc.id);
                                    }
                                    else
                                    {
                                        Logging?.LogWarning("Could not generate SwitchBot API for device with name/id \"{id}\", cannot turn it on", tondvc.id);
                                        allTurnedOn = false;
                                    }
                                    break;
                                case TriggerType.Fritz:
                                    apiFritz ??= Generator.GetFritzAPI();

                                    if (apiFritz != null)
                                    {
                                        try
                                        {
                                            var resultPresent = apiFritz.GetSwitchPresenceByName(tondvc.id);

                                            if (resultPresent == SwitchPresence.Present)
                                            {
                                                var resultTurnOn = apiFritz.TurnSwitchOnByName(tondvc.id);

                                                if (resultTurnOn == SwitchState.On)
                                                {
                                                    Logging?.LogInformation("Successfully turned on Fritz switch with name/id \"{id}\"", tondvc.id);
                                                    waitSecondsNext = tondvc.waitSecondsAfterTurnOn;
                                                }
                                                else
                                                {
                                                    Logging?.LogWarning("Could not turn on Fritz switch with name/id \"{id}\", result was {result}", tondvc.id, resultTurnOn);
                                                    allTurnedOn = false;
                                                }
                                            }
                                            else
                                            {
                                                Logging?.LogWarning("Could not turn on Fritz switch with name/id \"{id}\" because it was not found, look up result was {result}", tondvc.id, resultPresent);
                                                allTurnedOn = false;
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            Logging?.LogWarning(ex, "Could not turn on Fritz switch with name/id \"{id}\", because of the following error: {message}", tondvc.id, ex.Message);
                                            allTurnedOn = false;
                                        }
                                    }
                                    else
                                    {
                                        Logging?.LogWarning("Could not generate Fritz API for device with name/id \"{id}\", cannot turn it on", tondvc.id);
                                        allTurnedOn = false;
                                    }
                                    break;
                                case TriggerType.ClewareUSB:
                                    apiCleware ??= Generator.GetClewareAPI();

                                    if (apiCleware != null)
                                    {
                                        try
                                        {
                                            var result = apiCleware.TurnUSBSwitchOnByName(tondvc.id);

                                            if (result == USBSwitchState.On)
                                            {
                                                Logging?.LogInformation("Successfully turned on Cleware USB switch with name/id \"{id}\"", tondvc.id);
                                                waitSecondsNext = tondvc.waitSecondsAfterTurnOn;
                                            }
                                            else
                                            {
                                                Logging?.LogWarning("Could not turn on Cleware USB switch with name/id \"{id}\", result was {result}", tondvc.id, result);
                                                allTurnedOn = false;
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            Logging?.LogWarning(ex, "Could not turn on Cleware USB switch with name/id \"{id}\", because of the following error: {message}", tondvc.id, ex.Message);
                                            allTurnedOn = false;
                                        }
                                    }
                                    else
                                    {
                                        Logging?.LogWarning("Could not generate Cleware USB API for device with name/id \"{id}\", cannot turn it on", tondvc.id);
                                        allTurnedOn = false;
                                    }
                                    break;
                                case TriggerType.OSShutdown:
                                    Logging?.LogWarning("OSShutdown type is not allowed for device with name/id \"{id}\", cannot turn it on", tondvc.id);
                                    break;
                                default:
                                    Logging?.LogWarning("Unknown device type {type} for device with name/id \"{id}\", cannot turn it on", tondvc.deviceType, tondvc.id);
                                    break;
                            }
                        }
                        else
                        {
                            Logging?.LogWarning("Could not turn on {type} device with empty name/id", tondvc.id);
                        }
                    }
                    else if (entton is MessageTurnOn mto)
                    {
                        Logging?.LogWarning($"EXECUTE MANUAL STEP NOW (complete this step in {mto.waitSecondsAfterTurnOn} seconds): {mto.message}");
                        waitSecondsNext = mto.waitSecondsAfterTurnOn;
                    }
                    else
                    {
                        Logging?.LogError("Unknown turn on type {type} for id {id}", entton.GetType().FullName, entton.id);
                    }
                }

                if (allTurnedOn)
                {
                    Logging?.LogInformation("Successfully turned on all devices");
                }
                else
                {
                    Logging?.LogWarning("Could not turn on all devices");
                }

                return allTurnedOn;
            }
        }

        public static long GetTurnOnTimeInSeconds(ConfigFile configFile)
        {
            using (Logging?.BeginScope("GetTurnOnTimeInSeconds"))
            {
                long retVal = 0;
                long? waitSecondsNext = null;

                foreach (var entton in configFile.turnOn.Where(itm => itm.enabled))
                {
                    if (!string.IsNullOrWhiteSpace(entton.id))
                    {
                        if (waitSecondsNext.HasValue)
                        {
                            retVal += waitSecondsNext.Value;
                            waitSecondsNext = null;
                        }

                        waitSecondsNext = entton.waitSecondsAfterTurnOn;

                        if (entton is DeviceTurnOn tondvc)
                        {
                            switch (tondvc.deviceType)
                            {
                                case TriggerType.SwitchBot:
                                    retVal += 5;
                                    break;
                                case TriggerType.Fritz:
                                    retVal += 3;
                                    break;
                                case TriggerType.ClewareUSB:
                                    retVal += 3;
                                    break;
                            }
                        }
                    }
                }
                return retVal;
            }
        }

        public static long GetTurnOnDeviceCount()
        {
            return GetTurnOnDeviceCount(SharedData.Config);
        }

        public static long GetTurnOnDeviceCount(ConfigFile? configFile)
        {
            using (Logging?.BeginScope("GetTurnOnDeviceCount"))
            {
                return configFile?.turnOn?.Count(itm => itm.enabled) ?? 0;
            }
        }

        #endregion

        #region Controller

        public static bool StartController()
        {
            Microsoft.Extensions.Logging.ILogger? logger = GetLogger("Controller");
            using (logger?.BeginScope("StartController"))
            {
                var config = SharedData.Config;
                if (config != null)
                {
                    Logging?.LogDebug("Starting up main controller");
                    ControllerData? opt = (ControllerData?)config?.pollersAndTriggers?.pollers?.FirstOrDefault(itm => itm is ControllerData);

                    if (opt != null)
                    {
                        var controller = new MainController(
                                            new MainControllerOptions()
                                            {
                                                AlertTimeInMilliseconds = opt.options.alertTimeInMilliseconds,
                                                LockTimeoutInMilliseconds = opt.options.lockTimeoutInMilliseconds,
                                                SendMailEarliestAfterMinutes = opt.options.sendMailEarliestAfterMinutes,
                                                SleepTimeInMilliseconds = opt.options.sleepTimeInMilliseconds,
                                                Logger = logger,
                                            });

                        SharedData.SetMainController(controller);
                        return controller.StartControllerThread();
                    }
                    else
                    {
                        Logging?.LogError("Cannot start controller because no controller configuration was found");

                        return false;
                    }
                }
                else
                {
                    Logging?.LogError("Cannot start controller because no config was loaded");

                    return false;
                }
            }
        }

        public static bool StopController()
        {
            bool hasStopped = SharedData.Controller?.StopControllerThread() ?? false;
            if (hasStopped)
            {
                SharedData.RemoveMainController();
            }
            return hasStopped;
        }

        #endregion

        #endregion
    }
}
