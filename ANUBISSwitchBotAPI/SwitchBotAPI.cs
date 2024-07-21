using Microsoft.Extensions.Logging;
using System.Data;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ANUBISSwitchBotAPI
{
    #region Enumerations

    public enum SwitchBotResponseStatus : int
    {
        Unknown = 0,
        Success = 100,
        DeviceTypeError = 151,
        DeviceNotFound = 152,
        CommandNotSupported = 160,
        DeviceOffline = 161,
        HubOffline = 171,
        InternalError = 190,
    }

    public enum SwitchBotPowerState
    {
        [JsonEnumNullValue]
        [JsonEnumErrorValue]
        Unknown = 0,
        [JsonEnumName(true, "off")]
        Off,
        [JsonEnumName(true, "on")]
        On,
    }

    public enum SwitchBotDeviceMode
    {
        [JsonEnumNullValue]
        [JsonEnumErrorValue]
        Unknown = 0,
        [JsonEnumName(true, "pressMode")]
        PressMode,
        [JsonEnumName(true, "switchMode")]
        SwitchMode,
        [JsonEnumName(true, "customizeMode")]
        CustomizeMode,
    }

    public enum SwitchBotDeviceType
    {
        [JsonEnumNullValue]
        [JsonEnumErrorValue]
        Unknown = 0,
        Bot,
        Hub,
        [JsonEnumName(true, "Hub Plus")]
        HubPlus,
        [JsonEnumName(true, "Hub Mini")]
        HubMini,
        [JsonEnumName(true, "Hub 2")]
        Hub2,
        Curtain,
        Meter,
        MeterPlus,
        WoIOSensor,
        [JsonEnumName(true, "Smart Lock")]
        SmartLock,
        Keypad,
        [JsonEnumName(true, "Keypad Touch")]
        KeypadTouch,
        Remote,
        [JsonEnumName(true, "Motion Sensor")]
        MotionSensor,
        [JsonEnumName(true, "Contact Sensor")]
        ContactSensor,
        [JsonEnumName(true, "Ceiling Light")]
        CeilingLight,
        [JsonEnumName(true, "Ceiling Light Pro")]
        CeilingLightPro,
        [JsonEnumName(true, "Plug Mini (US)")]
        PlugMiniUS,
        [JsonEnumName(true, "Plug Mini (JP)")]
        PlugMiniJP,
        Plug,
        [JsonEnumName(true, "Strip Light")]
        StripLight,
        [JsonEnumName(true, "Color Bulb")]
        ColorBulb,
        [JsonEnumName(true, "Robot Vacuum Cleaner S1")]
        RobotVacuumCleanerS1,
        [JsonEnumName(true, "Robot Vacuum Cleaner S1 Plus")]
        RobotVacuumCleanerS1Plus,
        Humidifier,
        [JsonEnumName(true, "Indoor Cam")]
        IndoorCam,
        [JsonEnumName(true, "Pan/Tilt Cam")]
        PanTiltCam,
        [JsonEnumName(true, "Blind Tilt")]
        BlindTilt,
    }

    public enum SwitchBotAPICommand
    {
        GetSwitchBotList,

        GetSwitchBotState,

        SetSwitchBotOn,
        SetSwitchBotOff,
        PressSwitchBot,
    }

    #endregion

    #region Response classes

    public class SwitchBotDefaultResponse : SwitchBotResponse<SwitchBotResponseContent>
    {
        [JsonPropertyName("body")]
        public override SwitchBotResponseContent? Content { get; set; }

        public new static SwitchBotResponseContent? GetResponse(string response)
        {
            return GetResponse<SwitchBotResponseContent>(response);
        }
    }

    public class SwitchBotStateResponse : SwitchBotResponse<SwitchBotStateContent>
    {
        [JsonPropertyName("body")]
        public override SwitchBotStateContent? Content { get; set; }

        public new static SwitchBotStateResponse? GetResponse(string response)
        {
            return GetResponse<SwitchBotStateResponse>(response);
        }
    }

    public class SwitchBotDevicesResponse : SwitchBotResponse<SwitchBotDevicesContent>
    {
        [JsonPropertyName("body")]
        public override SwitchBotDevicesContent? Content { get; set; }

        public new static SwitchBotDevicesResponse? GetResponse(string response)
        {
            return GetResponse<SwitchBotDevicesResponse>(response);
        }

        public override void CheckResponse(ILogger? logger, bool allowEmptyContent = false)
        {
            using (logger?.BeginScope("SwitchBotDevicesResponse.CheckResponse"))
            {
                base.CheckResponse(logger, allowEmptyContent);
                if (Content?.Devices == null)
                    throw new SwitchBotAPIException($"Returned SwitchBot list was null");
            }
        }
    }

    public class SwitchBotCommandResponse : SwitchBotResponse<SwitchBotCommandContent>
    {
        [JsonPropertyName("body")]
        public override SwitchBotCommandContent? Content { get; set; }

        public new static SwitchBotCommandResponse? GetResponse(string response)
        {
            return GetResponse<SwitchBotCommandResponse>(response);
        }

        public override void CheckResponse(ILogger? logger, bool allowEmptyContent = false)
        {
            using (logger?.BeginScope("SwitchBotCommandResponse.CheckResponse"))
            {
                base.CheckResponse(logger, allowEmptyContent);
                if (Content?.DeviceResponses == null)
                    throw new SwitchBotAPIException($"Returned device response array was null");
                if (Content.DeviceResponses.Length <= 0)
                    throw new SwitchBotAPIException($"Returned device response array was empty");
                Content.DeviceResponses.ToList().ForEach(itm => itm.CheckResponse(logger));
            }
        }
    }

    public class SwitchBotResponse<T> where T : SwitchBotResponseContent
    {
        [JsonPropertyName("statusCode")]
        public virtual int StatusCode { get; set; }

        [JsonIgnore]
        public SwitchBotResponseStatus Status { get { return Enum.IsDefined(typeof(SwitchBotResponseStatus), StatusCode) ? (SwitchBotResponseStatus)StatusCode : SwitchBotResponseStatus.Unknown; } }


        [JsonPropertyName("message")]
        public virtual string? Message { get; set; }

        [JsonPropertyName("body")]
        public virtual T? Content { get; set; }

        public static SwitchBotResponse<T>? GetResponse(string response)
        {
            return GetResponse<SwitchBotResponse<T>>(response);
        }

#pragma warning disable CS0693
        protected static T? GetResponse<T>(string response, bool allowEmpty = false)
#pragma warning restore CS0693
        {
            if (string.IsNullOrWhiteSpace(response))
            {
                if (allowEmpty)
                    return default;
                else
                    throw new SwitchBotAPIException("Unexpected empty response");
            }

            T? retVal = JsonSerializer.Deserialize<T>(response);

            if (retVal == null)
                throw new SwitchBotAPIException("Unexpected empty response object");
            else
                return retVal;
        }

        public virtual void CheckResponse(ILogger? logger, bool allowEmptyContent = false)
        {
            CheckResponse(logger, "", allowEmptyContent);
        }

        public virtual void CheckResponse(ILogger? logger, string appendMessage, bool allowEmptyContent = false)
        {
            using (logger?.BeginScope("SwitchBotResponse.CheckResponse"))
            {
                if (Status == SwitchBotResponseStatus.Success && (Message == "success" || (Message?.StartsWith("queue,wait to action") ?? false)))
                {
                    if (!allowEmptyContent && Content == null)
                        throw new SwitchBotAPIException($"Response body was null{appendMessage}");
                }
                else
                {
                    throw new SwitchBotAPIResponseException($"{Message}{appendMessage}", StatusCode);
                }
            }
        }
    }

    public class SwitchBotResponseContent
    {
    }

    public class SwitchBotStateContent : SwitchBotResponseContent
    {
        [JsonPropertyName("deviceId")]
        public string? DeviceId { get; set; }

        [JsonPropertyName("deviceType")]
        [JsonConverter(typeof(JsonEnumConverter<SwitchBotDeviceType>))]
        public SwitchBotDeviceType DeviceType { get; set; }

        [JsonPropertyName("power")]
        [JsonConverter(typeof(JsonEnumConverter<SwitchBotPowerState>))]
        public SwitchBotPowerState Power { get; set; }

        [JsonPropertyName("battery")]
        public short? Battery { get; set; }

        [JsonPropertyName("version")]
        public string? Version { get; set; }

        [JsonPropertyName("deviceMode")]
        [JsonConverter(typeof(JsonEnumConverter<SwitchBotDeviceMode>))]
        public SwitchBotDeviceMode Mode { get; set; }

        [JsonPropertyName("hubDeviceId")]
        public string? HubId { get; set; }
    }

    public class SwitchBotDevicesContent : SwitchBotResponseContent
    {
        [JsonPropertyName("deviceList")]
        public SwitchBotDevice[]? Devices { get; set; }
    }

    public class SwitchBotDevice
    {
        [JsonPropertyName("deviceId")]
        public string? Id { get; set; }

        [JsonPropertyName("deviceName")]
        public string? Name { get; set; }

        [JsonPropertyName("deviceType")]
        [JsonConverter(typeof(JsonEnumConverter<SwitchBotDeviceType>))]
        public SwitchBotDeviceType DeviceType { get; set; }

        [JsonPropertyName("enableCloudService")]
        public bool CloudServiceEnabled { get; set; }

        [JsonPropertyName("hubDeviceId")]
        public string? HubId { get; set; }
    }

    public class SwitchBotCommandContent : SwitchBotResponseContent
    {
        [JsonPropertyName("items")]
        public SwitchBotDeviceCommandResponse[]? DeviceResponses { get; set; }
    }

    public class SwitchBotDeviceCommandResponse : SwitchBotResponse<SwitchBotDeviceCommandStatusContent>
    {
        [JsonPropertyName("code")]
        public override int StatusCode { get; set; }

        [JsonPropertyName("status")]
        public override SwitchBotDeviceCommandStatusContent? Content { get; set; }

        [JsonPropertyName("deviceID")]
        public string? DeviceId { get; set; }

        public override void CheckResponse(ILogger? logger, bool allowEmptyContent = false)
        {
            using (logger?.BeginScope("SwitchBotDeviceCommandResponse.CheckResponse"))
            {
                if (string.IsNullOrWhiteSpace(DeviceId))
                    throw new SwitchBotAPIException($"Returned device id was empty");
                base.CheckResponse(logger, $" for device with id {DeviceId}", allowEmptyContent);
            }
        }
    }

    public class SwitchBotDeviceCommandStatusContent : SwitchBotResponseContent
    {
        [JsonPropertyName("power")]
        [JsonConverter(typeof(JsonEnumConverter<SwitchBotPowerState>))]
        public SwitchBotPowerState Power { get; set; }

        [JsonPropertyName("battery")]
        public int? Battery { get; set; }

        [JsonPropertyName("connect")]
        public bool? ConnectedToHub { get; set; }

        [JsonPropertyName("code")]
        public long? Code { get; set; }
    }

    #endregion

    #region Helper classes

    #region Enum serialization

    public class JsonEnumNullValueAttribute : JsonAttribute
    {
    }

    public class JsonEnumErrorValueAttribute : JsonAttribute
    {
    }

    public class JsonEnumNameAttribute : JsonAttribute
    {
        public List<string> Names { get; init; }

        public bool IgnoreCase { get; init; }

        public string? WriteName { get { return Names?.FirstOrDefault(); } }

        public JsonEnumNameAttribute(params string[] names)
            : this(false, names)
        {
        }

        public JsonEnumNameAttribute(bool ignoreCase, params string[] names)
        {
            IgnoreCase = ignoreCase;
            Names = new List<string>(names ?? []);
        }

        public bool InNames(string value)
        {
            return Names.Any(itm => string.Equals(itm, value,
                                                    IgnoreCase ?
                                                        StringComparison.InvariantCultureIgnoreCase :
                                                        StringComparison.InvariantCulture));
        }
    }

#pragma warning disable CS8625, CS8600, CS8603
    public class JsonEnumConverter<T> : JsonConverter<T>
    {
        private readonly JsonConverter<T>? _converter;
        private readonly Type _underlyingType;

        public JsonEnumConverter() : this(null) { }

        public JsonEnumConverter(JsonSerializerOptions options)
        {
            // for performance, use the existing converter if available
            if (options != null)
                _converter = (JsonConverter<T>)options.GetConverter(typeof(T));

            // cache the underlying type
            _underlyingType = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);
        }

        public override bool CanConvert(Type typeToConvert)
        {
            return typeof(T).IsAssignableFrom(typeToConvert);
        }

        public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (_converter != null)
            {
                return _converter.Read(ref reader, _underlyingType, options) ?? default;
            }

            string? value = reader.GetString();

            if (string.IsNullOrWhiteSpace(value))
            {
                var nullName = JsonEnumConverter<T>.GetMemberAttributePairs<JsonEnumNullValueAttribute>(_underlyingType).FirstOrDefault().Name;

                if (!string.IsNullOrWhiteSpace(nullName))
                    return Enum.TryParse(_underlyingType, nullName, out var parsedResult) ? (T)parsedResult : default;
                else
                    return default;
            }

            var enumName = JsonEnumConverter<T>.GetMemberAttributePairs<JsonEnumNameAttribute>(_underlyingType)
                                            .FirstOrDefault(item => item.Attribute.InNames(value)).Name;


            // for performance, parse with ignoreCase:false first.
            if ((enumName == null || !Enum.TryParse(_underlyingType, enumName, ignoreCase: false, out object result))
                    && !Enum.TryParse(_underlyingType, value, ignoreCase: false, out result)
                    && !Enum.TryParse(_underlyingType, value, ignoreCase: true, out result))
            {
                var errorName = JsonEnumConverter<T>.GetMemberAttributePairs<JsonEnumErrorValueAttribute>(_underlyingType).FirstOrDefault().Name;

                if (!string.IsNullOrWhiteSpace(errorName))
                    return Enum.TryParse(_underlyingType, errorName, out var parsedResult) ? (T)parsedResult : default;
                else
                    throw new JsonException($"Unable to convert \"{value}\" to enum \"{_underlyingType}\".");
            }

            return (T)result;
        }

        public override void Write(Utf8JsonWriter writer,
            T value, JsonSerializerOptions options)
        {
            var strValue = value?.ToString();

            if (!string.IsNullOrWhiteSpace(strValue))
            {
                var enumValue = JsonEnumConverter<T>.GetMemberAttributePairs<JsonEnumNameAttribute>(_underlyingType)
                                        .FirstOrDefault(item => item.Name == strValue).Attribute?.WriteName;

                if (enumValue != null)
                    strValue = enumValue;
            }

            writer.WriteStringValue(strValue);
        }

#pragma warning disable CS0693, CS8619
        private static (string Name, T Attribute)[] GetMemberAttributePairs<T>(Type type) where T : JsonAttribute
        {
            return type.GetMembers()
                    .Select(member => (member.Name, Attribute: (T)member.GetCustomAttributes(typeof(T), false).FirstOrDefault()))
                    .Where(p => p.Attribute != null).ToArray();
        }
#pragma warning restore CS0693, CS8619
    }
#pragma warning restore CS8625, CS8600, CS8603

    #endregion

    #region Exceptions

    internal static class ExceptionExtension
    {
        public static Exception ThrowSpecificError(this HttpRequestException hre, string? appendMessage = null)
        {
            if (hre?.InnerException is System.Net.Sockets.SocketException se)
            {
                switch (se.SocketErrorCode)
                {
                    case System.Net.Sockets.SocketError.HostUnreachable:
                    case System.Net.Sockets.SocketError.HostNotFound:
                    case System.Net.Sockets.SocketError.HostDown:
                        return new SwitchBotAPINetworkException($"Network error {se.SocketErrorCode} occured{appendMessage}: {hre?.Message}; {se.Message}", hre, se.SocketErrorCode);
                    case System.Net.Sockets.SocketError.TimedOut:
                        return new SwitchBotAPITimeoutException(hre, $" according to socket error {se.SocketErrorCode}: {hre?.Message}; {se.Message}");
                    default:
                        return new SwitchBotAPIHttpException($"Socket error {se.SocketErrorCode} occured{appendMessage}: {hre?.Message}; {se.Message}", hre, hre?.StatusCode);
                }
            }
            else
                throw new SwitchBotAPIHttpException($"Generic http request exception occured ({hre?.StatusCode}): {hre?.Message}", hre, hre?.StatusCode);
        }
    }

    public class SwitchBotAPIException : Exception
    {
        public SwitchBotAPIException() : base() { }

        public SwitchBotAPIException(string message) : base(message) { }

        public SwitchBotAPIException(string message, Exception? innerException) : base(message, innerException) { }
    }

    public class SwitchBotAPINameNotFoundException : SwitchBotAPIException
    {
        public SwitchBotAPINameNotFoundException(string message) : base(message) { }
    }

    public class SwitchBotAPIHttpException : HttpRequestException
    {
        public SwitchBotAPIHttpException(string message) : base(message) { }
        public SwitchBotAPIHttpException(string message, HttpStatusCode? statusCode) : this(message, null, statusCode) { }
        public SwitchBotAPIHttpException(string message, Exception? innerException, HttpStatusCode? statusCode) : base(message, innerException, statusCode) { }
    }

    public class SwitchBotAPINetworkException : SwitchBotAPIException
    {
        public System.Net.Sockets.SocketError? StatusCode { get; init; }

        public SwitchBotAPINetworkException(string message) : base(message) { }
        public SwitchBotAPINetworkException(string message, System.Net.Sockets.SocketError? statusCode) : base(message)
        {
            StatusCode = statusCode;
        }
        public SwitchBotAPINetworkException(string message, Exception? innerException, System.Net.Sockets.SocketError? statusCode) : base(message, innerException)
        {
            StatusCode = statusCode;
        }
    }

    public class SwitchBotAPIResponseException : SwitchBotAPIException
    {
        private readonly string _message;

        public override string Message { get { return _message; } }
        public int? StatusCode { get; set; }
        public SwitchBotResponseStatus Status { get { return StatusCode.HasValue && Enum.IsDefined(typeof(SwitchBotResponseStatus), StatusCode) ? (SwitchBotResponseStatus)StatusCode : SwitchBotResponseStatus.Unknown; } }

        public SwitchBotAPIResponseException(string? message) : this(message, null) { }
        public SwitchBotAPIResponseException(string? message, int? statusCode)
        {
            if (statusCode.HasValue)
            {
                StatusCode = statusCode;
                _message = $"SwitchBotAPI response {Status} did not indicate success (code {statusCode.Value}); ";
                switch (Status)
                {
                    case SwitchBotResponseStatus.DeviceTypeError:
                        _message += "device type error";
                        break;
                    case SwitchBotResponseStatus.DeviceNotFound:
                        _message += "device not found";
                        break;
                    case SwitchBotResponseStatus.CommandNotSupported:
                        _message += "command is not supported";
                        break;
                    case SwitchBotResponseStatus.DeviceOffline:
                        _message += "device offline";
                        break;
                    case SwitchBotResponseStatus.HubOffline:
                        _message += "hub device is offline";
                        break;
                    case SwitchBotResponseStatus.InternalError:
                        _message += "device states not synchronized/command format invalid";
                        break;
                    default:
                        _message += "unknown status code";
                        break;
                }
                if (!string.IsNullOrWhiteSpace(message))
                {
                    _message += $": {message}";
                }
            }
            else
            {
                if (string.IsNullOrWhiteSpace(message))
                    _message = "Unkown SwitchBotAPI response exception (message and status unknown)";
                else
                    _message = $"SwitchBotAPI response exception for status {Status}: {message}";
            }
        }
    }

    public class SwitchBotAPITimeoutException : SwitchBotAPIException
    {
        public SwitchBotAPITimeoutException(Exception? innerException, string? appendMessage = null) : base($"The http request timed out{appendMessage}", innerException) { }
    }

    #endregion

    internal class HttpClientContainer : IDisposable
    {
        internal bool IsInternalClient { get; init; }
        internal HttpClient HttpClient { get; init; }

        internal HttpClientContainer(HttpClient httpClient)
        {
            HttpClient = httpClient;
        }

        public void Dispose()
        {
            if (IsInternalClient)
            {
                HttpClient.Dispose();
            }
        }
    }

    public class SwitchBotAPIOptions
    {
        /// <summary>
        /// Base URL of SwitchBot API.
        /// Default is https://api.switch-bot.com/
        /// </summary>
        public string BaseUrl { get; init; }

        /// <summary>
        /// Token to use for SwitchBot login.
        /// To provide this property is mandatory.
        /// </summary>
        public string? Token { get; init; }

        /// <summary>
        /// Secret to use for SwitchBot login.
        /// To provide this property is mandatory.
        /// </summary>
        public string? Secret { get; init; }

        /// <summary>
        /// Should the api reload all device names if a device was not found by the name you provided during a command?
        /// Default is flase (do not reload all device names if device name was not found).
        /// </summary>
        public bool ReloadNamesIfNotFound { get; init; }

        /// <summary>
        /// What is the timeout for command calls in seconds?
        /// Default is 20 seconds.
        /// </summary>
        public ushort CommandTimeoutSeconds { get; init; }

        /// <summary>
        /// Should any SSL validation error be ignored?
        /// Default is false.
        /// </summary>
        public bool IgnoreSSLError { get; init; }

        /// <summary>
        /// Should commands be resent on errors according to AutoRetry settings?
        /// Default is true.
        /// </summary>
        public bool AutoRetryOnErrors { get; init; }

        /// <summary>
        /// How often should commands be resent on errors if AutoRetryOnErrors is set to true?
        /// Default is 3.
        /// </summary>
        public byte AutoRetryCount { get; init; }

        /// <summary>
        /// How long (in milliseconds) should the api wait at least before resending a command after an error if AutoRetryOnErrors is set to true?
        /// Default is 1500 milliseconds
        /// </summary>
        public ushort AutoRetryMinWaitMilliseconds { get; init; }

        /// <summary>
        /// How long (in milliseconds) should the timespan be, starting from the AutoRetryMinWaitMilliseconds time after an error,
        /// in which a command could be resent? The actual waiting time before resending a command after an error will be randomly
        /// chosen between the AutoRetryMinWaitMilliseconds value and the AutoRetryMinWaitMilliseconds+AutoRetryWaitSpanMilliseconds value
        /// each time a command is resent.
        /// Default is 3000 milliseconds.
        /// </summary>
        public ushort AutoRetryWaitSpanMilliseconds { get; init; }

        /// <summary>
        /// Logging object to implement logging.
        /// If no logger is set then nothing will be logged.
        /// </summary>
        public ILogger? Logger { get; init; }

        public SwitchBotAPIOptions()
        {
            BaseUrl = "https://api.switch-bot.com/v1.1/";
            ReloadNamesIfNotFound = false;
            CommandTimeoutSeconds = 20;
            IgnoreSSLError = false;
            AutoRetryOnErrors = true;
            AutoRetryCount = 3;
            AutoRetryMinWaitMilliseconds = 1500;
            AutoRetryWaitSpanMilliseconds = 3000;
            Logger = null;
        }
    }

    #endregion

    public class SwitchBotAPI
    {
        #region Fields

        #region Constants

        private const string c_relativeUrl_Version = "/v1.1";
        private const string c_relativeUrl_Devices = "/devices";
        private const string c_relativeUrl_Status = "/status";
        private const string c_relativeUrl_Command = "/commands";
        private const short c_sleepMilSec_BetweenCommandAndState = 1500;

        #endregion

        #region Unchangable fields

        private readonly HttpClient? _globalHttpClient;
        private bool _globalHttpClientInitialized;

        #endregion

        #region Changable fields

        private Dictionary<string, string> _dicSwitchBotIdsByName = [];
        private CancellationToken? _cancellationToken = null;

        #endregion

        #endregion

        #region Properties

        public SwitchBotAPIOptions Options { get; init; }

        protected ILogger? Logging { get { return Options?.Logger; } }

        #endregion

        #region Constructors

        public SwitchBotAPI(SwitchBotAPIOptions options)
            : this(options, null)
        {
        }

        public SwitchBotAPI(SwitchBotAPIOptions options, HttpClient? httpClient = null)
        {
            using (Logging?.BeginScope("SwitchBotAPI.Constructor"))
            {
                Options = options;
                _globalHttpClient = httpClient;
                _globalHttpClientInitialized = false;

                Logging?.LogTrace("Created SwitchBotAPI object with Options: {@Options}", options);
                if (httpClient != null)
                {
                    Logging?.LogTrace("External HttpClient injected");
                }
            }
        }

        public SwitchBotAPI(string token, string secret)
            : this(new SwitchBotAPIOptions()
            {
                Token = token,
                Secret = secret,
            })
        {
        }

        public SwitchBotAPI(string baseUrl, string token, string secret)
            : this(new SwitchBotAPIOptions()
            {
                BaseUrl = baseUrl,
                Token = token,
                Secret = secret,
            })
        {
        }

        #endregion

        #region General helper methods

        private void CheckThreadCancellation()
        {
            _cancellationToken?.ThrowIfCancellationRequested();
        }

        private HttpClientContainer GetHttpClient()
        {
            using (Logging?.BeginScope("GetHttpClient"))
            {
                CheckThreadCancellation();

                HttpClientContainer container;
                bool doInitializing = true;
                TimeSpan timeoutTimespan = TimeSpan.FromMilliseconds(Options.CommandTimeoutSeconds * 1000);

                Logging?.LogTrace("Initializing HttpClient object " +
                                  "with timeout of {timeoutTimespan}, " +
                                  $"{(Options.IgnoreSSLError ? "ignoring" : "respecting")} SSL errors, " +
                                  "for base URI {baseUrl}", timeoutTimespan, Options.BaseUrl);

                if (_globalHttpClient == null)
                {
                    HttpClient client = Options.IgnoreSSLError ?
                                            new HttpClient(new HttpClientHandler()
                                            {
                                                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                                            }) :
                                            new HttpClient();

                    container = new HttpClientContainer(client);
                }
                else
                {
                    container = new HttpClientContainer(_globalHttpClient);
                    if (_globalHttpClientInitialized)
                    {
                        doInitializing = false;
                    }
                    else
                    {
                        _globalHttpClientInitialized = true;
                    }
                }

                if (doInitializing)
                {
                    if (string.IsNullOrWhiteSpace(Options.Token))
                        throw new SwitchBotAPIException("Token was null or empty, you need to provide a token for SwitchBotAPI access");

                    if (string.IsNullOrWhiteSpace(Options.Secret))
                        throw new SwitchBotAPIException("Secret was null or empty, you need to provide a secret for SwitchBotAPI access");

                    string strTimestamp = (DateTime.UtcNow - DateTime.UnixEpoch).TotalMilliseconds.ToString("0");
                    string strGuid = Guid.NewGuid().ToString();
                    string strData = Options.Token + strTimestamp + strGuid;
                    HMACSHA256 hmac = new(Encoding.UTF8.GetBytes(Options.Secret));
                    string strSignature = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(strData)));

                    container.HttpClient.DefaultRequestHeaders.TryAddWithoutValidation(@"Authorization", Options.Token);
                    container.HttpClient.DefaultRequestHeaders.TryAddWithoutValidation(@"sign", strSignature);
                    container.HttpClient.DefaultRequestHeaders.TryAddWithoutValidation(@"nonce", strGuid);
                    container.HttpClient.DefaultRequestHeaders.TryAddWithoutValidation(@"t", strTimestamp);
                    container.HttpClient.Timeout = timeoutTimespan;
                    container.HttpClient.BaseAddress = new Uri(Options.BaseUrl);
                }

                return container;
            }
        }

        private int GetWaitingTimeBeforeRetry()
        {
            CheckThreadCancellation();

            return Random.Shared.Next(Options.AutoRetryMinWaitMilliseconds,
                                        Options.AutoRetryMinWaitMilliseconds + Options.AutoRetryWaitSpanMilliseconds);
        }

        /// <summary>
        /// Waits a random waiting time on errors.
        /// Returns true if it waited and command should be retried.
        /// Returns false if it didn't wait and command should be retried.
        /// </summary>
        /// <param name="retry">Retry count for current command</param>
        private bool WaitBeforeRetry(ref byte retry)
        {
            using (Logging?.BeginScope("WaitBeforeRetry"))
            {
                CheckThreadCancellation();

                if (Options.AutoRetryOnErrors && retry < Options.AutoRetryCount)
                {
                    // waiting before retrying command
                    retry++;
                    int retryInMilliseconds = GetWaitingTimeBeforeRetry();
                    Logging?.LogWarning("Http request failed or returned unexpected data, retrying in {retryInMilliseconds} milliseconds (retry {currentRetry}/{maxRetries})", retryInMilliseconds, retry, Options.AutoRetryCount);

                    if (_cancellationToken.HasValue)
                    {
                        if (_cancellationToken.Value.WaitHandle.WaitOne(retryInMilliseconds))
                            Logging?.LogTrace("SwitchBot api recieved thread cancellation request during http-retry sleep");
                    }
                    else
                        Thread.Sleep(retryInMilliseconds);

                    Logging?.LogTrace("Done waiting before retrying http request");

                    CheckThreadCancellation();

                    return true;
                }
                else
                {
                    if (retry > 0)
                    {
                        Logging?.LogWarning("Http request failed, all {maxRetries} retries exhausted", retry);
                    }
                    else
                    {
                        Logging?.LogWarning($"Http request failed, no retrying configured");
                    }
                    return false;
                }
            }
        }

        #endregion

        #region Device commands

        #region General commands

        public List<SwitchBotDevice> GetSwitchBotList(bool onlyCloudServiceEnabled = true)
        {
            using (Logging?.BeginScope("GetSwitchBotList"))
            {
                CheckThreadCancellation();

                Logging?.LogDebug("Request to retrieve switch bot list" + (onlyCloudServiceEnabled ? " (of cloud service activated bots only)" : ""));

                Exception? exCaught = null;
                byte retry = 0;
                List<SwitchBotDevice> lstRetVal = [];

                do
                {
                    exCaught = null;

                    try
                    {
                        string response = SendCommand(SwitchBotAPICommand.GetSwitchBotList);

                        CheckThreadCancellation();

                        SwitchBotDevicesResponse? switchBotDevicesResponse = SwitchBotDevicesResponse.GetResponse(response);

                        switchBotDevicesResponse?.CheckResponse(Options?.Logger);

                        lstRetVal = switchBotDevicesResponse?.Content?.Devices
                                        ?.Where(itm => itm.DeviceType == SwitchBotDeviceType.Bot
                                                        && (!onlyCloudServiceEnabled || itm.CloudServiceEnabled))
                                        ?.ToList() ?? [];
                    }
                    catch (SwitchBotAPITimeoutException ex)
                    {
                        Logging?.LogWarning("Recieved http timeout");
                        exCaught = ex;
                    }
                    catch (OperationCanceledException)
                    {
                        Logging?.LogTrace("SwitchBot API thread was canceled");
                        throw;
                    }
                    catch (ThreadInterruptedException)
                    {
                        Logging?.LogTrace("SwitchBot API thread was interrupted");
                        throw;
                    }
                    catch (Exception ex)
                    {
                        exCaught = ex;
                        Logging?.LogWarning(ex, "Error \"{errortype}\" during SwitchBot command, error message was: {message}", exCaught.GetType().Name, exCaught.Message);
                    }
                }
                while (exCaught != null && WaitBeforeRetry(ref retry));
                // retry if exception was caught or if response was empty

                CheckForException(exCaught, "Failed to get switch bot list");

                return lstRetVal;
            }
        }

        public Dictionary<string, string>? GetCachedSwitchBotNames()
        {
            using (Logging?.BeginScope("GetCachedSwitchBotNames"))
            {
                try
                {
                    CheckThreadCancellation();

                    Dictionary<string, string> retVal = [];

                    foreach (KeyValuePair<string, string> kvp in _dicSwitchBotIdsByName)
                    {
                        retVal.Add(kvp.Key, kvp.Value);
                    }

                    CheckThreadCancellation();

                    return retVal;
                }
                catch (OperationCanceledException)
                {
                    Logging?.LogTrace("SwitchBot API thread was canceled");
                    throw;
                }
                catch (ThreadInterruptedException)
                {
                    Logging?.LogTrace("SwitchBot API thread was interrupted");
                    throw;
                }
                catch (Exception ex)
                {
                    Logging?.LogError(ex, "While trying to copy cached switch bot name dictionary: {message}", ex.Message);

                    return null;
                }
            }
        }

        #endregion

        #region Commands by id

        public SwitchBotDeviceCommandStatusContent TurnSwitchBotOn(string id, bool checkStateAfterwards = false)
        {
            using (Logging?.BeginScope("TurnSwitchBotOn"))
            {
                Logging?.LogDebug("Request to turn on switch bot with id {switchBotId}", id);

                CheckThreadCancellation();

                Exception? exCaught = null;
                byte retry = 0;
                SwitchBotDeviceCommandStatusContent? switchBotStatus;
                SwitchBotPowerState checkedState;

                if (string.IsNullOrWhiteSpace(id))
                    throw new SwitchBotAPIException("TurnSwitchBotOn: Switch bot id must not be empty");

                do
                {
                    exCaught = null;
                    switchBotStatus = null;
                    checkedState = SwitchBotPowerState.On;

                    try
                    {
                        string responseCommand = SendCommand(SwitchBotAPICommand.SetSwitchBotOn, id);

                        CheckThreadCancellation();

                        SwitchBotCommandResponse? switchBotCommandResponse = SwitchBotCommandResponse.GetResponse(responseCommand);

                        switchBotCommandResponse?.CheckResponse(Options?.Logger);

                        switchBotStatus = switchBotCommandResponse?.Content?.DeviceResponses?.FirstOrDefault(itm => itm.DeviceId == id)?.Content;

                        if (switchBotStatus == null)
                            throw new SwitchBotAPIException($"Activated device not found in response");

                        if (checkStateAfterwards && switchBotStatus.Power == SwitchBotPowerState.Off)
                        {
                            CheckThreadCancellation();

                            if (_cancellationToken.HasValue)
                            {
                                if (_cancellationToken.Value.WaitHandle.WaitOne(c_sleepMilSec_BetweenCommandAndState))
                                    Logging?.LogTrace("SwitchBot api recieved thread cancellation request during wait for check after switching");
                            }
                            else
                            {
                                Thread.Sleep(c_sleepMilSec_BetweenCommandAndState);
                            }

                            CheckThreadCancellation();

                            string responseState = SendCommand(SwitchBotAPICommand.GetSwitchBotState, id);

                            CheckThreadCancellation();

                            SwitchBotStateResponse? switchBotStateResponse = SwitchBotStateResponse.GetResponse(responseState);

                            switchBotStateResponse?.CheckResponse(Options?.Logger, " when checking state after command action");

                            checkedState = switchBotStateResponse?.Content?.Power ?? SwitchBotPowerState.Unknown;

                            CheckThreadCancellation();
                        }

                        if (switchBotStatus?.Power != SwitchBotPowerState.On)
                        {
                            Logging?.LogWarning("Unexpected SwitchBot switch power state {state} ater turning switch on, expected On", switchBotStatus?.Power);
                        }
                        if (checkedState != SwitchBotPowerState.On)
                        {
                            Logging?.LogWarning("Unexpected checked SwitchBot switch power state {state} ater checking switch after switch turn-on, expected On", checkedState);
                        }
                    }
                    catch (SwitchBotAPITimeoutException ex)
                    {
                        Logging?.LogWarning("Recieved http timeout");
                        exCaught = ex;
                    }
                    catch (OperationCanceledException)
                    {
                        Logging?.LogTrace("SwitchBot API thread was canceled");
                        throw;
                    }
                    catch (ThreadInterruptedException)
                    {
                        Logging?.LogTrace("SwitchBot API thread was interrupted");
                        throw;
                    }
                    catch (Exception ex)
                    {
                        exCaught = ex;
                        Logging?.LogWarning(ex, "Error \"{errortype}\" during SwitchBot command, error message was: {message}", exCaught.GetType().Name, exCaught.Message);
                    }
                }
                while ((exCaught != null ||
                            switchBotStatus?.Power != SwitchBotPowerState.On ||
                            checkedState != SwitchBotPowerState.On) &&
                                WaitBeforeRetry(ref retry));
                // retry if exception was caught or if response was empty

                CheckForException(exCaught, "Failed to turn switch bot on");

                if (switchBotStatus?.Power != SwitchBotPowerState.On)
                    Logging?.LogWarning("Failed to turn on switch bot with id {switchBotId}", id);
                else if (checkedState != SwitchBotPowerState.On)
                    Logging?.LogWarning("Failed to turn on switch bot with id {switchBotId} according to state check after command action", id);
                else
                    Logging?.LogDebug("Successfully turned on switch bot with id {switchBotId}", id);

#pragma warning disable CS8603
                return switchBotStatus;
#pragma warning restore CS8603
            }
        }

        public SwitchBotDeviceCommandStatusContent TurnSwitchBotOff(string id, bool checkStateAfterwards = false)
        {
            using (Logging?.BeginScope("TurnSwitchBotOff"))
            {
                Logging?.LogDebug("Request to turn off switch bot with id {switchBotId}", id);

                CheckThreadCancellation();

                Exception? exCaught = null;
                byte retry = 0;
                SwitchBotDeviceCommandStatusContent? switchBotStatus;
                SwitchBotPowerState checkedState;

                if (string.IsNullOrWhiteSpace(id))
                    throw new SwitchBotAPIException("TurnSwitchBotOff: Switch bot id must not be empty");

                do
                {
                    exCaught = null;
                    switchBotStatus = null;
                    checkedState = SwitchBotPowerState.Off;

                    try
                    {
                        string responseCommand = SendCommand(SwitchBotAPICommand.SetSwitchBotOff, id);

                        CheckThreadCancellation();

                        SwitchBotCommandResponse? switchBotCommandResponse = SwitchBotCommandResponse.GetResponse(responseCommand);

                        switchBotCommandResponse?.CheckResponse(Options?.Logger);

                        switchBotStatus = switchBotCommandResponse?.Content?.DeviceResponses?.FirstOrDefault(itm => itm.DeviceId == id)?.Content;

                        if (switchBotStatus == null)
                            throw new SwitchBotAPIException($"Activated device not found in response");

                        if (checkStateAfterwards && switchBotStatus.Power == SwitchBotPowerState.Off)
                        {
                            CheckThreadCancellation();

                            if (_cancellationToken.HasValue)
                            {
                                if (_cancellationToken.Value.WaitHandle.WaitOne(c_sleepMilSec_BetweenCommandAndState))
                                    Logging?.LogTrace("SwitchBot api recieved thread cancellation request during wait for check after switching");
                            }
                            else
                            {
                                Thread.Sleep(c_sleepMilSec_BetweenCommandAndState);
                            }

                            CheckThreadCancellation();

                            string responseState = SendCommand(SwitchBotAPICommand.GetSwitchBotState, id);

                            CheckThreadCancellation();

                            SwitchBotStateResponse? switchBotStateResponse = SwitchBotStateResponse.GetResponse(responseState);

                            switchBotStateResponse?.CheckResponse(Options?.Logger, " when checking state after command action");

                            checkedState = switchBotStateResponse?.Content?.Power ?? SwitchBotPowerState.Unknown;

                            CheckThreadCancellation();
                        }

                        if (switchBotStatus?.Power != SwitchBotPowerState.Off)
                        {
                            Logging?.LogWarning("Unexpected SwitchBot switch power state {state} ater turning switch off, expected Off", switchBotStatus?.Power);
                        }
                        if (checkedState != SwitchBotPowerState.Off)
                        {
                            Logging?.LogWarning("Unexpected checked SwitchBot switch power state {state} ater checking switch after switch turn-off, expected Off", checkedState);
                        }
                    }
                    catch (SwitchBotAPITimeoutException ex)
                    {
                        Logging?.LogWarning("Recieved http timeout");
                        exCaught = ex;
                    }
                    catch (OperationCanceledException)
                    {
                        Logging?.LogTrace("SwitchBot API thread was canceled");
                        throw;
                    }
                    catch (ThreadInterruptedException)
                    {
                        Logging?.LogTrace("SwitchBot API thread was interrupted");
                        throw;
                    }
                    catch (Exception ex)
                    {
                        exCaught = ex;
                        Logging?.LogWarning(ex, "Error \"{errortype}\" during SwitchBot command, error message was: {message}", exCaught.GetType().Name, exCaught.Message);
                    }
                }
                while ((exCaught != null ||
                            switchBotStatus?.Power != SwitchBotPowerState.Off ||
                            checkedState != SwitchBotPowerState.Off) &&
                                WaitBeforeRetry(ref retry));
                // retry if exception was caught or if response was empty

                CheckForException(exCaught, "Failed to turn switch bot off");

                if (switchBotStatus?.Power != SwitchBotPowerState.Off)
                    Logging?.LogWarning("Failed to turn off switch bot with id {switchBotId}", id);
                else if (checkedState != SwitchBotPowerState.Off)
                    Logging?.LogWarning("Failed to turn off switch bot with id {switchBotId} according to state check after command action", id);
                else
                    Logging?.LogDebug("Successfully turned off switch bot with id {switchBotId}", id);

#pragma warning disable CS8603
                return switchBotStatus;
#pragma warning restore CS8603
            }
        }

        public SwitchBotDeviceCommandStatusContent PressSwitchBot(string id)
        {
            using (Logging?.BeginScope("PressSwitchBot"))
            {
                Logging?.LogDebug("Request to press switch bot with id {switchBotId}", id);

                CheckThreadCancellation();

                Exception? exCaught = null;
                byte retry = 0;
                SwitchBotDeviceCommandStatusContent? switchBotStatus;

                if (string.IsNullOrWhiteSpace(id))
                    throw new SwitchBotAPIException("PressSwitchBot: Switch bot id must not be empty");

                do
                {
                    exCaught = null;
                    switchBotStatus = null;

                    try
                    {
                        string responseCommand = SendCommand(SwitchBotAPICommand.PressSwitchBot, id);

                        CheckThreadCancellation();

                        SwitchBotCommandResponse? switchBotCommandResponse = SwitchBotCommandResponse.GetResponse(responseCommand);

                        switchBotCommandResponse?.CheckResponse(Options?.Logger);

                        switchBotStatus = switchBotCommandResponse?.Content?.DeviceResponses?.FirstOrDefault(itm => itm.DeviceId == id)?.Content;

                        if (switchBotStatus == null)
                            throw new SwitchBotAPIException($"Activated device not found in response");
                    }
                    catch (SwitchBotAPITimeoutException ex)
                    {
                        Logging?.LogWarning("Recieved http timeout");
                        exCaught = ex;
                    }
                    catch (OperationCanceledException)
                    {
                        Logging?.LogTrace("SwitchBot API thread was canceled");
                        throw;
                    }
                    catch (ThreadInterruptedException)
                    {
                        Logging?.LogTrace("SwitchBot API thread was interrupted");
                        throw;
                    }
                    catch (Exception ex)
                    {
                        exCaught = ex;
                        Logging?.LogWarning(ex, "Error \"{errortype}\" during SwitchBot command, error message was: {message}", exCaught.GetType().Name, exCaught.Message);
                    }
                }
                while (exCaught != null && WaitBeforeRetry(ref retry));
                // retry if exception was caught or if response was empty

                CheckForException(exCaught, "Failed to press switch bot");

                Logging?.LogDebug("Successfully pressed switch bot with id {switchBotId}", id);

#pragma warning disable CS8603
                return switchBotStatus;
#pragma warning restore CS8603
            }
        }

        public SwitchBotStateContent GetSwitchBotState(string id)
        {
            using (Logging?.BeginScope("GetSwitchBotState"))
            {
                Logging?.LogDebug("Request to get current state of switch bot with id {switchBotId}", id);

                CheckThreadCancellation();

                Exception? exCaught = null;
                byte retry = 0;
                SwitchBotStateResponse? switchBotStateResponse = null;

                if (string.IsNullOrWhiteSpace(id))
                    throw new SwitchBotAPIException("GetSwitchBotState: Switch bot id must not be empty");

                do
                {
                    exCaught = null;
                    switchBotStateResponse = null;

                    try
                    {
                        string responseState = SendCommand(SwitchBotAPICommand.GetSwitchBotState, id);

                        CheckThreadCancellation();

                        switchBotStateResponse = SwitchBotStateResponse.GetResponse(responseState);

                        switchBotStateResponse?.CheckResponse(Options?.Logger);
                    }
                    catch (SwitchBotAPITimeoutException ex)
                    {
                        Logging?.LogWarning("Recieved http timeout");
                        exCaught = ex;
                    }
                    catch (OperationCanceledException)
                    {
                        Logging?.LogTrace("SwitchBot API thread was canceled");
                        throw;
                    }
                    catch (ThreadInterruptedException)
                    {
                        Logging?.LogTrace("SwitchBot API thread was interrupted");
                        throw;
                    }
                    catch (Exception ex)
                    {
                        exCaught = ex;
                        Logging?.LogWarning(ex, "Error \"{errortype}\" during SwitchBot command, error message was: {message}", exCaught.GetType().Name, exCaught.Message);
                    }
                }
                while (exCaught != null && WaitBeforeRetry(ref retry));
                // retry if exception was caught or if response was empty

                CheckForException(exCaught, "Failed to retrieve switch bot state");

#pragma warning disable CS8602, CS8603
                return switchBotStateResponse.Content;
#pragma warning restore CS8602, CS8603
            }
        }

        #endregion

        #region Commands by name

        public SwitchBotDeviceCommandStatusContent TurnSwitchBotOnByName(string name, bool checkStateAfterwards = false, bool? reloadNamesIfNotFound = null)
        {
            using (Logging?.BeginScope("TurnSwitchBotOnByName"))
            {
                Logging?.LogDebug("Request to turn on switch bot with name \"{name}\"", name);

                string? id = GetSwitchBotIdByName(name, reloadNamesIfNotFound ?? Options.ReloadNamesIfNotFound);

                if (id != null)
                    return TurnSwitchBotOn(id, checkStateAfterwards);
                else
                    throw new SwitchBotAPINameNotFoundException($@"Switch bot with name ""{name}"" not found");
            }
        }

        public SwitchBotDeviceCommandStatusContent TurnSwitchBotOffByName(string name, bool checkStateAfterwards = false, bool? reloadNamesIfNotFound = null)
        {
            using (Logging?.BeginScope("TurnSwitchBotOffByName"))
            {
                Logging?.LogDebug("Request to turn off switch bot with name \"{name}\"", name);

                string? id = GetSwitchBotIdByName(name, reloadNamesIfNotFound ?? Options.ReloadNamesIfNotFound);

                if (id != null)
                    return TurnSwitchBotOff(id, checkStateAfterwards);
                else
                    throw new SwitchBotAPINameNotFoundException($@"Switch bot with name ""{name}"" not found");
            }
        }

        public SwitchBotDeviceCommandStatusContent PressSwitchBotByName(string name, bool? reloadNamesIfNotFound = null)
        {
            using (Logging?.BeginScope("PressSwitchBotByName"))
            {
                Logging?.LogDebug("Request to press switch bot with name \"{name}\"", name);

                string? id = GetSwitchBotIdByName(name, reloadNamesIfNotFound ?? Options.ReloadNamesIfNotFound);

                if (id != null)
                    return PressSwitchBot(id);
                else
                    throw new SwitchBotAPINameNotFoundException($@"Switch bot with name ""{name}"" not found");
            }
        }

        public SwitchBotStateContent GetSwitchBotStateByName(string name, bool? reloadNamesIfNotFound = null)
        {
            using (Logging?.BeginScope("GetSwitchBotStateByName"))
            {
                Logging?.LogDebug("Request to get current state of switch bot with name \"{name}\"", name);

                string? id = GetSwitchBotIdByName(name, reloadNamesIfNotFound ?? Options.ReloadNamesIfNotFound);

                if (id != null)
                    return GetSwitchBotState(id);
                else
                    throw new SwitchBotAPINameNotFoundException($@"Switch bot with name ""{name}"" not found");
            }
        }

        #endregion

        #endregion

        #region Device command helper methods

        private void CheckForException(Exception? exCaught, string? message = null)
        {
            CheckThreadCancellation();

            if (exCaught != null)
            {
                Logging?.LogError(exCaught, message + $": {exCaught.Message} ({exCaught.GetType().Name})");
                throw exCaught;
            }
        }

        private string SendCommand(SwitchBotAPICommand command, string? id = null)
        {
            using (Logging?.BeginScope("SendCommand"))
            {
                using (var client = GetHttpClient())
                {
                    Logging?.LogTrace("Requesting command {command} for user token {token}" + (id != null ? " and device {deviceId}" : ""), command, Options.Token, id);

                    HttpMethod method = HttpMethod.Get;
                    Dictionary<string, string>? parameters = null;

                    string strRelativeUrl = $"{c_relativeUrl_Version}{c_relativeUrl_Devices}";
                    if (id != null)
                    {
                        strRelativeUrl += $"/{id}";
                    }

                    switch (command)
                    {
                        case SwitchBotAPICommand.GetSwitchBotList:
                            break;
                        case SwitchBotAPICommand.GetSwitchBotState:
                            strRelativeUrl += c_relativeUrl_Status;
                            break;
                        case SwitchBotAPICommand.SetSwitchBotOn:
                        case SwitchBotAPICommand.SetSwitchBotOff:
                        case SwitchBotAPICommand.PressSwitchBot:
                            strRelativeUrl += c_relativeUrl_Command;

                            method = HttpMethod.Post;

                            parameters = new Dictionary<string, string>()
                                                {
                                                    { "parameter", "default" },
                                                    { "commandType", "command" },
                                                };

                            if (command == SwitchBotAPICommand.SetSwitchBotOn)
                                parameters.Add("command", "turnOn");
                            else if (command == SwitchBotAPICommand.SetSwitchBotOff)
                                parameters.Add("command", "turnOff");
                            else
                                parameters.Add("command", "press");

                            break;
                        default:
                            throw new SwitchBotAPIException($"Unknown command: {command}");
                    }

                    try
                    {
                        var request = new HttpRequestMessage(method, strRelativeUrl);

                        if (parameters != null)
                            request.Content = new StringContent(JsonSerializer.Serialize(parameters), Encoding.UTF8, "application/json");

                        Logging?.LogTrace("Sending command request: {@Request}, with parameters: {@Parameters}", request, parameters);
                        HttpResponseMessage response;
                        try
                        {
                            if (_cancellationToken.HasValue)
                                response = client.HttpClient.Send(request, _cancellationToken.Value);
                            else
                                response = client.HttpClient.Send(request);
                        }
                        catch (HttpRequestException ex) { throw ex.ThrowSpecificError(); }

                        if (response.IsSuccessStatusCode)
                        {
                            using (StreamReader sr = new(response.Content.ReadAsStream()))
                            {
                                string strResponse = sr.ReadToEnd();

                                Logging?.LogTrace("Command response was: {response}", strResponse);

                                return strResponse;
                            }
                        }
                        else
                        {
                            throw new SwitchBotAPIHttpException($"Http response ({response.StatusCode}) did not indicate success", response.StatusCode);
                        }
                    }
                    catch (TaskCanceledException tce) when (tce.Source == "System.Net.Http" && (!_cancellationToken.HasValue || !_cancellationToken.Value.IsCancellationRequested))
                    {
                        throw new SwitchBotAPITimeoutException(tce);
                    }
                    catch (OperationCanceledException)
                    {
                        Logging?.LogTrace("SwitchBot API thread was canceled");
                        throw;
                    }
                    catch (ThreadInterruptedException)
                    {
                        Logging?.LogTrace("SwitchBot API thread was interrupted");
                        throw;
                    }
                }
            }
        }

        #endregion

        #region Device name helpers

        public void LoadSwitchBotNames()
        {
            using (Logging?.BeginScope("LoadSwitchBotNames"))
            {
                Logging?.LogDebug("Retrieving all switch bot names (of cloud service activated bots)");

                CheckThreadCancellation();

                _dicSwitchBotIdsByName = [];
                List<SwitchBotDevice> lstSwitchBots = GetSwitchBotList();

                foreach (SwitchBotDevice singleSwitchBot in lstSwitchBots)
                {
                    if (!string.IsNullOrWhiteSpace(singleSwitchBot.Id) &&
                        !string.IsNullOrWhiteSpace(singleSwitchBot.Name))
                    {
                        _dicSwitchBotIdsByName[singleSwitchBot.Name] = singleSwitchBot.Id;
                    }
                }

                CheckThreadCancellation();
            }
        }

        public string? GetSwitchBotIdByName(string name, bool reloadIfNotFound)
        {
            using (Logging?.BeginScope("GetSwitchBotIdByName"))
            {
                CheckThreadCancellation();

                Logging?.LogTrace("Looking up id for switch bot with name \"{name}\"", name);
                string? id = _dicSwitchBotIdsByName.GetValueOrDefault(name);

                CheckThreadCancellation();

                if (id == null && reloadIfNotFound)
                {
                    Logging?.LogDebug("Found no id for a switch bot with name \"{name}\", reloading all names", name);
                    LoadSwitchBotNames();
                    id = _dicSwitchBotIdsByName.GetValueOrDefault(name);
                }

                if (id != null)
                {
                    Logging?.LogDebug("Looked up id {id} for switch bot with name \"{name}\"", id, name);
                }
                else
                {
                    Logging?.LogError("Id for a switch bot with name \"{name}\" was not found", name);
                }

                return id;
            }
        }

        #endregion

        #region Control methods

        public void SetCancellationToken(CancellationToken cancellationToken)
        {
            _cancellationToken = cancellationToken;
        }

        public void RemoveCancellationToken()
        {
            _cancellationToken = null;
        }

        #endregion
    }
}