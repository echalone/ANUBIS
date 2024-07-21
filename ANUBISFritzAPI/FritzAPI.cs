using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.RegularExpressions;
using System.Xml;

namespace ANUBISFritzAPI
{
    #region Enumerations

    public enum SwitchState
    {
        On,
        Off,
        Unknown,
        NameNotFound,
        Error,
    }

    public enum SwitchPresence
    {
        Present,
        Missing,
        NameNotFound,
        Error,
    }

    public enum FritzAPICommand
    {
        GetSwitchList,

        GetSwitchName,
        GetSwitchPresent,
        GetSwitchState,
        GetSwitchPower,

        SetSwitchOn,
        SetSwitchOff,
        SetSwitchToggle,
    }

    #endregion

    #region Helper classes

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
                        return new FritzAPINetworkException($"Network error {se.SocketErrorCode} occured{appendMessage}: {hre?.Message}; {se.Message}", hre, se.SocketErrorCode);
                    case System.Net.Sockets.SocketError.TimedOut:
                        return new FritzAPITimeoutException(hre, $" according to socket error {se.SocketErrorCode}: {hre?.Message}; {se.Message}");
                    default:
                        return new FritzAPIHttpException($"Socket error {se.SocketErrorCode} occured{appendMessage}: {hre?.Message}; {se.Message}", hre, hre?.StatusCode);
                }
            }
            else
                return new FritzAPIHttpException($"Generic http request exception occured{appendMessage} ({hre?.StatusCode}): {hre?.Message}", hre, hre?.StatusCode);
        }
    }

    public class FritzAPIException : Exception
    {
        public FritzAPIException(string message) : base(message) { }

        public FritzAPIException(string message, Exception? innerException) : base(message, innerException) { }
    }

    public class FritzAPIHttpException : HttpRequestException
    {
        public FritzAPIHttpException(string message) : base(message) { }
        public FritzAPIHttpException(string message, HttpStatusCode? statusCode) : this(message, null, statusCode) { }
        public FritzAPIHttpException(string message, Exception? innerException, HttpStatusCode? statusCode) : base(message, innerException, statusCode) { }
    }

    public class FritzAPINetworkException : FritzAPIException
    {
        public System.Net.Sockets.SocketError? StatusCode { get; init; }

        public FritzAPINetworkException(string message) : base(message) { }
        public FritzAPINetworkException(string message, System.Net.Sockets.SocketError? statusCode) : base(message)
        {
            StatusCode = statusCode;
        }
        public FritzAPINetworkException(string message, Exception? innerException, System.Net.Sockets.SocketError? statusCode) : base(message, innerException)
        {
            StatusCode = statusCode;
        }
    }

    public class FritzAPITimeoutException : FritzAPIException
    {
        public FritzAPITimeoutException(Exception? innerException, string? appendMessage = null) : base($"The http request timed out{appendMessage}", innerException) { }
    }

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

    public class FritzAPIOptions
    {
        /// <summary>
        /// Base URL of FritzBox.
        /// Default is http://fritz.box
        /// </summary>
        public string BaseUrl { get; init; }

        /// <summary>
        /// User to use for FritzBox login.
        /// To provide this property is mandatory.
        /// </summary>
        public string? User { get; init; }

        /// <summary>
        /// Password to use for FritzBox login.
        /// To provide this property is mandatory.
        /// </summary>
        public string? Password { get; init; }

        /// <summary>
        /// Should the api auto-login in case you're currently not logged in?
        /// If this and the CheckLoginBeforeCommands options are activated the api will automatically
        /// log you in before a command if you're logged out (even due to inactity).
        /// Default is true (will perform login if you're currently not logged in).
        /// </summary>
        public bool AutoLogin { get; init; }

        /// <summary>
        /// Should the api check your login status against the server before each command?
        /// If this and the AutoLogin options are activated the api will automatically
        /// log you in before a command if you're logged out (even due to inactity).
        /// Default is true (will check if you're logged in before a command).
        /// </summary>
        public bool CheckLoginBeforeCommands { get; init; }

        /// <summary>
        /// Should the api reload all device names if a device was not found by the name you provided during a command?
        /// Default is flase (do not reload all device names if device name was not found).
        /// </summary>
        public bool ReloadNamesIfNotFound { get; init; }

        /// <summary>
        /// What is the timeout for login calls in seconds?
        /// Default is 8 seconds.
        /// </summary>
        public ushort LoginTimeoutSeconds { get; init; }

        /// <summary>
        /// What is the timeout for command calls in seconds?
        /// Default is 5 seconds.
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
        /// Default is 2.
        /// </summary>
        public byte AutoRetryCount { get; init; }

        /// <summary>
        /// How long (in milliseconds) should the api wait at least before resending a command after an error if AutoRetryOnErrors is set to true?
        /// Default is 700 milliseconds
        /// </summary>
        public ushort AutoRetryMinWaitMilliseconds { get; init; }

        /// <summary>
        /// How long (in milliseconds) should the timespan be, starting from the AutoRetryMinWaitMilliseconds time after an error,
        /// in which a command could be resent? The actual waiting time before resending a command after an error will be randomly
        /// chosen between the AutoRetryMinWaitMilliseconds value and the AutoRetryMinWaitMilliseconds+AutoRetryWaitSpanMilliseconds value
        /// each time a command is resent.
        /// Default is 800 milliseconds.
        /// </summary>
        public ushort AutoRetryWaitSpanMilliseconds { get; init; }

        /// <summary>
        /// Logging object to implement logging.
        /// If no logger is set then nothing will be logged.
        /// </summary>
        public ILogger? Logger { get; init; }

        public FritzAPIOptions()
        {
            BaseUrl = "http://fritz.box";
            AutoLogin = true;
            CheckLoginBeforeCommands = true;
            ReloadNamesIfNotFound = false;
            LoginTimeoutSeconds = 8;
            CommandTimeoutSeconds = 5;
            IgnoreSSLError = false;
            AutoRetryOnErrors = true;
            AutoRetryCount = 2;
            AutoRetryMinWaitMilliseconds = 700;
            AutoRetryWaitSpanMilliseconds = 800;
            Logger = null;
        }
    }

    internal class LoginState
    {
        public string Challenge { get; set; }
        public int BlockTime { get; set; }
        public bool IsPbkdf2 { get; set; }
        private const int c_maxBlockTime = 60;

        public LoginState(string challenge, int blockTime)
        {
            Challenge = challenge;
            if (blockTime <= c_maxBlockTime)
            {
                if (blockTime >= 0)
                {
                    BlockTime = blockTime;
                }
                else
                {
                    BlockTime = 0;
                }
            }
            else
            {
                BlockTime = c_maxBlockTime;
            }
            IsPbkdf2 = Challenge.StartsWith("2$");
        }
    }

    #endregion

    public partial class FritzAPI
    {
        #region Fields

        #region Constants

        private const string c_relativeUrl_Login = "/login_sid.lua";
        private const string c_relativeUrl_Command = "/webservices/homeautoswitch.lua";
        private readonly Regex rx_BoolTrue = RxBoolTrue();
        private readonly Regex rx_BoolFalse = RxBoolFalse();
        private readonly Regex rx_Invalid = RxInvalid();

        #endregion

        #region Unchangable fields

        private readonly HttpClient? _globalHttpClient;
        private bool _globalHttpClientInitialized;

        #endregion

        #region Changable fields

        private string? _sessionId;
        private bool _isLoggedIn;
        private Dictionary<string, string> _dicSwitchIdsByName = [];
        private CancellationToken? _cancellationToken = null;

        #endregion

        #endregion

        #region Properties

        public string? SessionId { get { return _sessionId; } }

        public FritzAPIOptions Options { get; init; }

        protected ILogger? Logging { get { return Options?.Logger; } }

        private static readonly string[] separator = [",", ";", "\r", "\n"];

        #endregion

        #region Constructors

        public FritzAPI(FritzAPIOptions options)
            : this(options, null)
        {
        }

        public FritzAPI(FritzAPIOptions options, HttpClient? httpClient = null)
        {
            using (Logging?.BeginScope("FritzAPI.Constructor"))
            {
                Options = options;
                _globalHttpClient = httpClient;
                _globalHttpClientInitialized = false;

                Logging?.LogTrace("Created FritzAPI object with Options: {@Options}", options);
                if (httpClient != null)
                {
                    Logging?.LogTrace("External HttpClient injected");
                }
            }
        }

        public FritzAPI(string user, string password)
            : this(new FritzAPIOptions()
            {
                User = user,
                Password = password,
            })
        {
        }

        public FritzAPI(string baseUrl, string user, string password)
            : this(new FritzAPIOptions()
            {
                BaseUrl = baseUrl,
                User = user,
                Password = password,
            })
        {
        }

        #endregion

        #region General helper methods

        private void CheckThreadCancellation()
        {
            _cancellationToken?.ThrowIfCancellationRequested();
        }

        private HttpClientContainer GetHttpClient(bool isLoginClient = false)
        {
            using (Logging?.BeginScope("GetHttpClient"))
            {
                CheckThreadCancellation();

                HttpClientContainer container;
                bool doInitializing = true;
                TimeSpan timeoutTimespan = TimeSpan.FromMilliseconds((isLoginClient ? Options.LoginTimeoutSeconds : Options.CommandTimeoutSeconds) * 1000);

                Logging?.LogTrace("Initializing HttpClient object for {clientType} " +
                                  "with timeout of {timeoutTimespan}, " +
                                  $"{(Options.IgnoreSSLError ? "ignoring" : "respecting")} SSL errors, " +
                                  "for base URI {baseUrl}", (isLoginClient ? "login" : "command"), timeoutTimespan, Options.BaseUrl);

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
                            Logging?.LogTrace("Fritz api recieved thread cancellation request during http-retry sleep");
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

        #region Login commands

        public void Login(bool loadSwitchNames = true)
        {
            using (Logging?.BeginScope("Login"))
            {
                try
                {
                    CheckThreadCancellation();

                    Logging?.LogTrace("Request to login user \"{user}\"", Options?.User);
                    if (!_isLoggedIn || !CheckSID())
                    {
                        Logging?.LogTrace("Logging in user \"{user}\"", Options?.User);
                        GetNewSID();
                        Logging?.LogDebug("User \"{user}\" successfully logged in with session {sessionId}", Options?.User, _sessionId);
                        _isLoggedIn = true;

                        if (loadSwitchNames)
                            LoadSwitchNames(true);

                        CheckThreadCancellation();
                    }
                    else
                    {
                        Logging?.LogTrace("User \"{user}\" already logged in with session {sessionId}", Options?.User, _sessionId);
                    }
                }
                catch (OperationCanceledException)
                {
                    Logging?.LogTrace("Fritz API thread was canceled");
                    throw;
                }
                catch (ThreadInterruptedException)
                {
                    Logging?.LogTrace("Fritz API thread was interrupted");
                    throw;
                }
                catch (Exception ex)
                {
                    Logging?.LogError(ex, "While trying to log in: {message}", ex.Message);
                    Logout();

                    throw;
                }
            }
        }

        public bool IsLoggedIn()
        {
            using (Logging?.BeginScope("IsLoggedIn"))
            {
                CheckThreadCancellation();

                Logging?.LogTrace("Checking if user is logged in, currently assumption is: {loginState}", (_isLoggedIn ? "logged in" : "logged out"));
                return _isLoggedIn && CheckSID();
            }
        }

        public void Logout()
        {
            using (Logging?.BeginScope("Logout"))
            {
                Logging?.LogTrace("Logging out user \"{user}\" with sessionId {sessionId}", Options?.User, _sessionId);
                _isLoggedIn = false;
                try
                {
                    CheckThreadCancellation();

                    if (_sessionId != null)
                    {
                        SendLoginCommand(new Dictionary<string, string>()
                                        {
                                            { "logout" , "" },
                                            { "sid" , _sessionId },
                                        });
                    }
                }
                catch (OperationCanceledException)
                {
                    Logging?.LogTrace("Fritz API thread was canceled");
                    throw;
                }
                catch (ThreadInterruptedException)
                {
                    Logging?.LogTrace("Fritz API thread was interrupted");
                    throw;
                }
                catch (Exception ex)
                {
                    Logging?.LogError(ex, "While trying to log out, will pretent I logged out: {message}", ex.Message);
                }
            }
        }

        #endregion

        #region Login helper methods

        private LoginState GetLoginState()
        {
            using (Logging?.BeginScope("GetLoginState"))
            {
                CheckThreadCancellation();

                Exception? exCaught = null;
                byte retry = 0;
                string? responseText = null;
                LoginState? loginState = null;

                do
                {
                    exCaught = null;
                    try
                    {
                        using (var client = GetHttpClient(true))
                        {
                            try
                            {
                                var request = new HttpRequestMessage(HttpMethod.Get, c_relativeUrl_Login);
                                Logging?.LogTrace("Sending login request: {@Request}", request);

                                HttpResponseMessage response;
                                try
                                {
                                    if (_cancellationToken.HasValue)
                                        response = client.HttpClient.Send(request, _cancellationToken.Value);
                                    else
                                        response = client.HttpClient.Send(request);
                                }
                                catch (HttpRequestException ex) { throw ex.ThrowSpecificError(" during login request"); }

                                if (response.IsSuccessStatusCode)
                                {
                                    CheckThreadCancellation();

                                    using (StreamReader sr = new(response.Content.ReadAsStream()))
                                    {
                                        responseText = sr.ReadToEnd();
                                        Logging?.LogTrace("Retrieved challenge document for login: {responseText}", responseText?.Trim());

                                        if (!string.IsNullOrEmpty(responseText))
                                        {
                                            XmlDocument xdoc = new();
                                            xdoc.LoadXml(responseText);

                                            CheckThreadCancellation();

                                            var strChallenge = xdoc.SelectSingleNode("//Challenge")?.InnerText;
                                            var strBlockTime = xdoc.SelectSingleNode("//BlockTime")?.InnerText;
                                            int iBlockTime = 0;

                                            if (string.IsNullOrWhiteSpace(strChallenge))
                                                throw new FritzAPIException("GetLoginState: Challenge was null or empty");

                                            if (!string.IsNullOrWhiteSpace(strBlockTime))
                                            {
                                                if (!int.TryParse(strBlockTime, out iBlockTime))
                                                {
                                                    iBlockTime = 0;
                                                }
                                            }

                                            loginState = new LoginState(strChallenge, iBlockTime);

                                            CheckThreadCancellation();

                                            Logging?.LogTrace("Retrieved challenge information for login: {@LoginState}", loginState);

                                            if (loginState == null)
                                            {
                                                Logging?.LogWarning("Unexpected empty Fritz login state response");
                                            }
                                        }
                                        else
                                        {
                                            Logging?.LogWarning("Unexpected empty Fritz command response");
                                        }
                                    }
                                }
                                else
                                {
                                    throw new FritzAPIHttpException($"Http response ({response.StatusCode}) did not indicate success during login request", response.StatusCode);
                                }
                            }
                            catch (TaskCanceledException tce) when (tce.Source == "System.Net.Http" && (!_cancellationToken.HasValue || !_cancellationToken.Value.IsCancellationRequested))
                            {
                                Logging?.LogError(tce, "Http request timed out during login request");
                                throw new FritzAPITimeoutException(tce, " during login request");
                            }
                            catch (OperationCanceledException)
                            {
                                Logging?.LogTrace("Fritz API thread was canceled");
                                throw;
                            }
                            catch (ThreadInterruptedException)
                            {
                                Logging?.LogTrace("Fritz API thread was interrupted");
                                throw;
                            }
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        Logging?.LogTrace("Fritz API thread was canceled");
                        throw;
                    }
                    catch (ThreadInterruptedException)
                    {
                        Logging?.LogTrace("Fritz API thread was interrupted");
                        throw;
                    }
                    catch (Exception ex)
                    {
                        exCaught = ex;
                        Logging?.LogWarning(ex, "Error \"{errortype}\" while trying to get login challenge for Fritz, error message was: {message}", exCaught.GetType().Name, exCaught.Message);
                    }
                }
                while ((exCaught != null || string.IsNullOrWhiteSpace(responseText) || loginState == null) && WaitBeforeRetry(ref retry));
                // retry if exception was caught or if response was empty

                CheckForException(exCaught, "Failed to get login challenge");

                if (string.IsNullOrWhiteSpace(responseText))
                    throw new FritzAPIException("GetLoginState: Response was null or empty");

                if (loginState == null)
                    throw new FritzAPIException("GetLoginState: Weren't able to generate LoginState object");

                return loginState;

            }
        }

        private string SendLoginCommand(Dictionary<string, string> parameters)
        {
            using (Logging?.BeginScope("SendLoginCommand"))
            {
                CheckThreadCancellation();

                Exception? exCaught = null;
                byte retry = 0;
                string? responseText = null;

                do
                {
                    exCaught = null;
                    try
                    {
                        using (var client = GetHttpClient(true))
                        {
                            var request = new HttpRequestMessage(HttpMethod.Post, c_relativeUrl_Login)
                            {
                                Content = new FormUrlEncodedContent(parameters)
                            };

                            try
                            {
                                Logging?.LogTrace("Sending login command: {@Request}", request);



                                HttpResponseMessage response;
                                try
                                {
                                    if (_cancellationToken.HasValue)
                                        response = client.HttpClient.Send(request, _cancellationToken.Value);
                                    else
                                        response = client.HttpClient.Send(request);

                                }
                                catch (HttpRequestException ex) { throw ex.ThrowSpecificError(" during login command"); }

                                CheckThreadCancellation();

                                if (response.IsSuccessStatusCode)
                                {
                                    using (StreamReader sr = new(response.Content.ReadAsStream()))
                                    {
                                        responseText = sr.ReadToEnd();
                                        Logging?.LogTrace("Retrieved login response: {responseText}", responseText?.Trim());

                                        CheckThreadCancellation();

                                        if (string.IsNullOrWhiteSpace(responseText))
                                        {
                                            throw new FritzAPIException("SendLoginCommand: Response was null or empty");
                                        }
                                    }
                                }
                                else
                                {
                                    throw new FritzAPIHttpException($"Http response ({response.StatusCode}) did not indicate success during login command", response.StatusCode);
                                }
                            }
                            catch (TaskCanceledException tce) when (tce.Source == "System.Net.Http" && (!_cancellationToken.HasValue || !_cancellationToken.Value.IsCancellationRequested))
                            {
                                Logging?.LogError(tce, "Http request timed out during login command");
                                throw new FritzAPITimeoutException(tce, " during login command");
                            }
                            catch (OperationCanceledException)
                            {
                                Logging?.LogTrace("Fritz API thread was canceled");
                                throw;
                            }
                            catch (ThreadInterruptedException)
                            {
                                Logging?.LogTrace("Fritz API thread was interrupted");
                                throw;
                            }
                        }
                    }
                    catch (FritzAPITimeoutException ex)
                    {
                        Logging?.LogWarning("Recieved http timeout");
                        exCaught = ex;
                    }
                    catch (OperationCanceledException)
                    {
                        Logging?.LogTrace("Fritz API thread was canceled");
                        throw;
                    }
                    catch (ThreadInterruptedException)
                    {
                        Logging?.LogTrace("Fritz API thread was interrupted");
                        throw;
                    }
                    catch (Exception ex)
                    {
                        exCaught = ex;
                        Logging?.LogWarning(ex, "Error \"{errortype}\" while sending Fritz login command, error message was: {message}", exCaught.GetType().Name, exCaught.Message);
                    }
                }
                while ((exCaught != null || string.IsNullOrWhiteSpace(responseText)) && WaitBeforeRetry(ref retry));
                // retry if exception was caught or if response was empty

                CheckForException(exCaught, "Failed to send login command");

                return responseText ?? "";
            }
        }

        private string? GetSID(string responseText)
        {
            using (Logging?.BeginScope("GetSID"))
            {
                CheckThreadCancellation();

                Logging?.LogTrace("Looking for session id in login response");
                XmlDocument xdoc = new();
                xdoc.LoadXml(responseText);
                string? sid = xdoc.SelectSingleNode("//SID")?.InnerText;
                Logging?.LogTrace("Found session id {sessionId} in login response", sid);

                if (string.IsNullOrWhiteSpace(sid))
                    throw new FritzAPIException("GetSID: SID was null or empty");

                CheckThreadCancellation();

                if (RxZeroSID().IsMatch(sid))
                    return null;
                else
                    return sid.Trim();
            }
        }

        private string? GetLoginResponse(string challengeResponse)
        {
            using (Logging?.BeginScope("GetLoginResponse"))
            {
                CheckThreadCancellation();

                if (Options.User == null)
                    throw new FritzAPIException("GetLoginResponse: Username was null");

                string responseText = SendLoginCommand(new Dictionary<string, string>()
                                                        {
                                                            { "username" , Options.User },
                                                            { "response" , challengeResponse },
                                                        });

                return GetSID(responseText);
            }
        }

        private void GetNewSID()
        {
            using (Logging?.BeginScope("GetNewSID"))
            {
                CheckThreadCancellation();

                _isLoggedIn = false;
                var loginState = GetLoginState();

                var challenge_response = GetMD5Response(loginState.Challenge);

                Logging?.LogTrace("Calculated MD5 response is: {md5Response}", challenge_response);

                if (loginState.BlockTime > 0)
                {
                    int waitMilliseconds = loginState.BlockTime * 1000;
                    Logging?.LogTrace("Waiting {blockTime} seconds before trying to login user (blocktime)", loginState.BlockTime);

                    if (_cancellationToken.HasValue)
                    {
                        if (_cancellationToken.Value.WaitHandle.WaitOne(waitMilliseconds))
                            Logging?.LogTrace("Fritz api recieved thread cancellation request block time sleep");
                    }
                    else
                        Thread.Sleep(waitMilliseconds);

                    Logging?.LogTrace("Done waiting before trying to login");
                }

                CheckThreadCancellation();

                _sessionId = GetLoginResponse(challenge_response);

                if (_sessionId == null)
                    throw new FritzAPIException("GetNewSID: Wrong username, wrong password, or wrong network (must be inside FritzBox network)");
            }
        }

        private string GetMD5Response(string challenge)
        {
            using (Logging?.BeginScope("GetMD5Response"))
            {
                CheckThreadCancellation();

                if (Options.Password == null)
                    throw new FritzAPIException("GetMD5Response: Password was null");

                var response = $"{challenge}-{Options.Password}";
                var responseBytes = System.Text.Encoding.Unicode.GetBytes(response);
                var md5 = System.Security.Cryptography.MD5.Create();
#pragma warning disable CA1850 // Statische Methode „HashData“ gegenüber „ComputeHash“ bevorzugen.
                var md5Bytes = md5.ComputeHash(responseBytes);
#pragma warning restore CA1850 // Statische Methode „HashData“ gegenüber „ComputeHash“ bevorzugen.
                var md5String = string.Join(null, md5Bytes.ToList().Select(itm => itm.ToString("x2")));
                //var md5String = System.Text.Encoding.UTF8.GetString(md5Bytes);
                var md5Response = $"{challenge}-{md5String}";

                CheckThreadCancellation();

                return md5Response;
            }
        }

        private bool CheckSID()
        {
            using (Logging?.BeginScope("CheckSID"))
            {
                CheckThreadCancellation();

                if (_sessionId != null)
                {
                    Logging?.LogTrace("Sending check session id request");
                    string responseText = SendLoginCommand(new Dictionary<string, string>()
                                                            {
                                                                { "sid" , _sessionId },
                                                            });

                    string? checkSessionId = GetSID(responseText);

                    Logging?.LogTrace("Checking current session id {currentSessionId} against retrieved session id {checkSessionId}", _sessionId, checkSessionId);

                    return _sessionId == checkSessionId;
                }
                else
                {
                    throw new FritzAPIException("CheckSID: Current SessionID is null");
                }
            }
        }

        #endregion

        #region Device commands

        #region General commands

        public List<string> GetSwitchList(bool skipAutoLogin = false)
        {
            using (Logging?.BeginScope("GetSwitchList"))
            {
                CheckThreadCancellation();

                Logging?.LogDebug("Request to retrieve switch list");

                Exception? exCaught = null;
                byte retry = 0;
                List<string> lstRetVal = [];

                CheckLoginOrAutoLogin(skipAutoLogin);

                do
                {
                    exCaught = null;

                    try
                    {
                        string response = SendCommand(FritzAPICommand.GetSwitchList);

                        CheckThreadCancellation();

                        if (!string.IsNullOrWhiteSpace(response))
                        {
#pragma warning disable IDE0305 // Initialisierung der Sammlung vereinfachen
                            lstRetVal = response.Split(separator, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).ToList();
#pragma warning restore IDE0305 // Initialisierung der Sammlung vereinfachen
                        }
                    }
                    catch (FritzAPITimeoutException ex)
                    {
                        Logging?.LogWarning("Recieved http timeout");
                        exCaught = ex;
                    }
                    catch (OperationCanceledException)
                    {
                        Logging?.LogTrace("Fritz API thread was canceled");
                        throw;
                    }
                    catch (ThreadInterruptedException)
                    {
                        Logging?.LogTrace("Fritz API thread was interrupted");
                        throw;
                    }
                    catch (Exception ex)
                    {
                        exCaught = ex;
                        Logging?.LogWarning(ex, "Error \"{errortype}\" during Fritz command, error message was: {message}", exCaught.GetType().Name, exCaught.Message);
                    }
                }
                while (exCaught != null && WaitBeforeRetry(ref retry));
                // retry if exception was caught or if response was empty

                CheckForException(exCaught, "Failed to get switch list");

                return lstRetVal;
            }
        }

        public Dictionary<string, string>? GetCachedSwitchNames()
        {
            using (Logging?.BeginScope("GetCachedSwitchNames"))
            {
                try
                {
                    CheckThreadCancellation();

                    Dictionary<string, string> retVal = [];

                    foreach (KeyValuePair<string, string> kvp in _dicSwitchIdsByName)
                    {
                        retVal.Add(kvp.Key, kvp.Value);
                    }

                    CheckThreadCancellation();

                    return retVal;
                }
                catch (OperationCanceledException)
                {
                    Logging?.LogTrace("Fritz API thread was canceled");
                    throw;
                }
                catch (ThreadInterruptedException)
                {
                    Logging?.LogTrace("Fritz API thread was interrupted");
                    throw;
                }
                catch (Exception ex)
                {
                    Logging?.LogError(ex, "While trying to copy cached switch name dictionary: {message}", ex.Message);

                    return null;
                }
            }
        }

        #endregion

        #region Commands by id

        public SwitchState TurnSwitchOn(string id, bool skipAutoLogin = false)
        {
            using (Logging?.BeginScope("TurnSwitchOn"))
            {
                Logging?.LogDebug("Request to turn on switch with id {switchId}", id);

                CheckThreadCancellation();

                Exception? exCaught = null;
                byte retry = 0;
                SwitchState response = SwitchState.Unknown;

                if (string.IsNullOrWhiteSpace(id))
                    throw new FritzAPIException("TurnSwitchOn: Switch id must not be empty");

                CheckLoginOrAutoLogin(skipAutoLogin);

                do
                {
                    exCaught = null;

                    try
                    {
                        response = GetSwitchStateByResponse(SendCommand(FritzAPICommand.SetSwitchOn, id));

                        if (response != SwitchState.On)
                        {
                            Logging?.LogWarning("Unexpected Fritz switch state {state} after turning switch on, expected On", response);
                        }
                    }
                    catch (FritzAPITimeoutException ex)
                    {
                        Logging?.LogWarning("Recieved http timeout");
                        exCaught = ex;
                    }
                    catch (OperationCanceledException)
                    {
                        Logging?.LogTrace("Fritz API thread was canceled");
                        throw;
                    }
                    catch (ThreadInterruptedException)
                    {
                        Logging?.LogTrace("Fritz API thread was interrupted");
                        throw;
                    }
                    catch (Exception ex)
                    {
                        exCaught = ex;
                        Logging?.LogWarning(ex, "Error \"{errortype}\" during Fritz command, error message was: {message}", exCaught.GetType().Name, exCaught.Message);
                    }
                }
                while ((exCaught != null || response != SwitchState.On) && WaitBeforeRetry(ref retry));
                // retry if exception was caught or if response was empty

                CheckForException(exCaught, "Failed to turn switch on");

                if (response != SwitchState.On)
                    Logging?.LogWarning("Failed to turn on switch with id {switchId}", id);
                else
                    Logging?.LogDebug("Successfully turned on switch with id {switchId}", id);

                return response;
            }
        }

        public SwitchState TurnSwitchOff(string id, bool skipAutoLogin = false)
        {
            using (Logging?.BeginScope("TurnSwitchOff"))
            {
                Logging?.LogDebug("Request to turn off switch with id {switchId}", id);

                CheckThreadCancellation();

                Exception? exCaught = null;
                byte retry = 0;
                SwitchState response = SwitchState.Unknown;

                if (string.IsNullOrWhiteSpace(id))
                    throw new FritzAPIException("TurnSwitchOff: Switch id must not be empty");

                CheckLoginOrAutoLogin(skipAutoLogin);

                do
                {
                    exCaught = null;

                    try
                    {
                        response = GetSwitchStateByResponse(SendCommand(FritzAPICommand.SetSwitchOff, id));

                        if (response != SwitchState.Off)
                        {
                            Logging?.LogWarning("Unexpected Fritz switch state {state} after turning switch off, expected Off", response);
                        }
                    }
                    catch (FritzAPITimeoutException ex)
                    {
                        Logging?.LogWarning("Recieved http timeout");
                        exCaught = ex;
                    }
                    catch (OperationCanceledException)
                    {
                        Logging?.LogTrace("Fritz API thread was canceled");
                        throw;
                    }
                    catch (ThreadInterruptedException)
                    {
                        Logging?.LogTrace("Fritz API thread was interrupted");
                        throw;
                    }
                    catch (Exception ex)
                    {
                        exCaught = ex;
                        Logging?.LogWarning(ex, "Error \"{errortype}\" during Fritz command, error message was: {message}", exCaught.GetType().Name, exCaught.Message);
                    }
                }
                while ((exCaught != null || response != SwitchState.Off) && WaitBeforeRetry(ref retry));
                // retry if exception was caught or if response was empty

                CheckForException(exCaught, "Failed to turn switch off");

                if (response != SwitchState.Off)
                    Logging?.LogWarning("Failed to turn off switch with id {switchId}", id);
                else
                    Logging?.LogDebug("Successfully turned off switch with id {switchId}", id);

                return response;
            }
        }

        public SwitchState ToggleSwitchState(string id, bool skipAutoLogin = false)
        {
            using (Logging?.BeginScope("ToggleSwitchState"))
            {
                Logging?.LogDebug("Request to toggle switch with id {switchId}", id);

                CheckThreadCancellation();

                Exception? exCaught = null;
                byte retry = 0;
                SwitchState response = SwitchState.Unknown;

                if (string.IsNullOrWhiteSpace(id))
                    throw new FritzAPIException("ToggleSwitchState: Switch id must not be empty");

                CheckLoginOrAutoLogin(skipAutoLogin);

                do
                {
                    exCaught = null;

                    try
                    {
                        response = GetSwitchStateByResponse(SendCommand(FritzAPICommand.SetSwitchToggle, id));

                        if (response != SwitchState.On && response != SwitchState.Off)
                        {
                            Logging?.LogWarning("Unexpected Fritz switch state {state} after toggeling switch, expected On or Off", response);
                        }
                    }
                    catch (FritzAPITimeoutException ex)
                    {
                        Logging?.LogWarning("Recieved http timeout");
                        exCaught = ex;
                    }
                    catch (OperationCanceledException)
                    {
                        Logging?.LogTrace("Fritz API thread was canceled");
                        throw;
                    }
                    catch (ThreadInterruptedException)
                    {
                        Logging?.LogTrace("Fritz API thread was interrupted");
                        throw;
                    }
                    catch (Exception ex)
                    {
                        exCaught = ex;
                        Logging?.LogWarning(ex, "Error \"{errortype}\" during Fritz command, error message was: {message}", exCaught.GetType().Name, exCaught.Message);
                    }
                }
                while ((exCaught != null || (response != SwitchState.On && response != SwitchState.Off)) && WaitBeforeRetry(ref retry));
                // retry if exception was caught or if response was empty

                CheckForException(exCaught, "Failed to toggle switch");

                if (response != SwitchState.On && response != SwitchState.Off)
                    Logging?.LogWarning("Failed to toggle switch with id {switchId}", id);
                else
                    Logging?.LogDebug("Successfully sent toggle command to switch with id {switchId}, switch state is now reported to be {response}", id, response);

                return response;
            }
        }

        public SwitchState GetSwitchState(string id, bool skipAutoLogin = false)
        {
            using (Logging?.BeginScope("GetSwitchState"))
            {
                Logging?.LogDebug("Request to get current state of switch with id {switchId}", id);

                CheckThreadCancellation();

                Exception? exCaught = null;
                byte retry = 0;
                SwitchState response = SwitchState.Unknown;

                if (string.IsNullOrWhiteSpace(id))
                    throw new FritzAPIException("GetSwitchState: Switch id must not be empty");

                CheckLoginOrAutoLogin(skipAutoLogin);

                do
                {
                    exCaught = null;

                    try
                    {
                        response = GetSwitchStateByResponse(SendCommand(FritzAPICommand.GetSwitchState, id));

                        if (response != SwitchState.On && response != SwitchState.Off)
                        {
                            Logging?.LogWarning("Unexpected Fritz switch state {state}, expected On or Off", response);
                        }
                    }
                    catch (FritzAPITimeoutException ex)
                    {
                        Logging?.LogWarning("Recieved http timeout");
                        exCaught = ex;
                    }
                    catch (OperationCanceledException)
                    {
                        Logging?.LogTrace("Fritz API thread was canceled");
                        throw;
                    }
                    catch (ThreadInterruptedException)
                    {
                        Logging?.LogTrace("Fritz API thread was interrupted");
                        throw;
                    }
                    catch (Exception ex)
                    {
                        exCaught = ex;
                        Logging?.LogWarning(ex, "Error \"{errortype}\" during Fritz command, error message was: {message}", exCaught.GetType().Name, exCaught.Message);
                    }
                }
                while ((exCaught != null || (response != SwitchState.On && response != SwitchState.Off)) && WaitBeforeRetry(ref retry));
                // retry if exception was caught or if response was empty

                CheckForException(exCaught, "Failed to get switch state");

                if (response != SwitchState.On && response != SwitchState.Off)
                    Logging?.LogWarning("Failed to get current state for switch with id {switchId}", id);
                else
                    Logging?.LogDebug("Successfully recieved current state for switch with id {switchId}, switch state is reported to be {response}", id, response);

                return response;
            }
        }

        public long? GetSwitchPower(string id, bool skipAutoLogin = false)
        {
            using (Logging?.BeginScope("GetSwitchPower"))
            {
                Logging?.LogDebug("Request to get current power usage for switch with id {switchId}", id);

                CheckThreadCancellation();

                Exception? exCaught = null;
                byte retry = 0;
                long? response = null;

                if (string.IsNullOrWhiteSpace(id))
                    throw new FritzAPIException("GetSwitchPower: Switch id must not be empty");

                CheckLoginOrAutoLogin(skipAutoLogin);

                do
                {
                    exCaught = null;

                    try
                    {
                        response = GetNumeric(SendCommand(FritzAPICommand.GetSwitchPower, id));

                        if (!response.HasValue)
                        {
                            Logging?.LogWarning("Unexpected empty Fritz switch power, expected greater or equal to 0");
                        }
                        else if (response.Value < 0)
                        {
                            Logging?.LogWarning("Unexpected negative Fritz switch power {power}, expected greater or equal to 0", response);
                        }
                    }
                    catch (FritzAPITimeoutException ex)
                    {
                        Logging?.LogWarning("Recieved http timeout");
                        exCaught = ex;
                    }
                    catch (OperationCanceledException)
                    {
                        Logging?.LogTrace("Fritz API thread was canceled");
                        throw;
                    }
                    catch (ThreadInterruptedException)
                    {
                        Logging?.LogTrace("Fritz API thread was interrupted");
                        throw;
                    }
                    catch (Exception ex)
                    {
                        exCaught = ex;
                        Logging?.LogWarning(ex, "Error \"{errortype}\" during Fritz command, error message was: {message}", exCaught.GetType().Name, exCaught.Message);
                    }
                }
                while ((exCaught != null || !response.HasValue || response.Value < 0) && WaitBeforeRetry(ref retry));
                // retry if exception was caught or if response was empty

                CheckForException(exCaught, "Failed to get switch power");

                if (!response.HasValue || response.Value < 0)
                    Logging?.LogWarning("Failed to get current power usage for switch with id {switchId}", id);
                else
                    Logging?.LogDebug("Successfully recieved power usage for switch with id {switchId}, power usage is reported to be {response} mW", id, response.Value);

                return response;
            }
        }

        public SwitchPresence GetSwitchPresence(string id, bool skipAutoLogin = false)
        {
            using (Logging?.BeginScope("GetSwitchPresence"))
            {
                Logging?.LogDebug("Request to get presence for switch with id {switchId}", id);

                CheckThreadCancellation();

                Exception? exCaught = null;
                byte retry = 0;
                SwitchPresence response = SwitchPresence.Error;

                if (string.IsNullOrWhiteSpace(id))
                    throw new FritzAPIException("GetSwitchPresence: Switch id must not be empty");

                CheckLoginOrAutoLogin(skipAutoLogin);

                do
                {
                    exCaught = null;

                    try
                    {
                        response = GetSwitchPresenceByResponse(SendCommand(FritzAPICommand.GetSwitchPresent, id));

                        if (response != SwitchPresence.Missing && response != SwitchPresence.Present)
                        {
                            Logging?.LogWarning("Unexpected Fritz switch presence {presence}, expected Missing or Present", response);
                        }
                    }
                    catch (FritzAPITimeoutException ex)
                    {
                        Logging?.LogWarning("Recieved http timeout");
                        exCaught = ex;
                    }
                    catch (OperationCanceledException)
                    {
                        Logging?.LogTrace("Fritz API thread was canceled");
                        throw;
                    }
                    catch (ThreadInterruptedException)
                    {
                        Logging?.LogTrace("Fritz API thread was interrupted");
                        throw;
                    }
                    catch (Exception ex)
                    {
                        exCaught = ex;
                        Logging?.LogWarning(ex, "Error \"{errortype}\" during Fritz command, error message was: {message}", exCaught.GetType().Name, exCaught.Message);
                    }
                }
                while ((exCaught != null || (response != SwitchPresence.Missing && response != SwitchPresence.Present)) && WaitBeforeRetry(ref retry));
                // retry if exception was caught or if response was empty

                CheckForException(exCaught, "Failed to get switch presence");

                if (response != SwitchPresence.Missing && response != SwitchPresence.Present)
                    Logging?.LogWarning("Failed to get presence for switch with id {switchId}", id);
                else
                    Logging?.LogDebug("Successfully recieved presence for switch with id {switchId}, switch presence is reported to be {response}", id, response);

                return response;
            }
        }

        public string GetSwitchName(string id, bool skipAutoLogin = false)
        {
            using (Logging?.BeginScope("GetSwitchName"))
            {
                Logging?.LogDebug("Request to get name for switch with id {switchId}", id);

                CheckThreadCancellation();

                Exception? exCaught = null;
                byte retry = 0;
                string? responseText = null;

                if (string.IsNullOrWhiteSpace(id))
                    throw new FritzAPIException("GetSwitchName: Switch id must not be empty");

                CheckLoginOrAutoLogin(skipAutoLogin);

                do
                {
                    exCaught = null;

                    try
                    {
                        responseText = SendCommand(FritzAPICommand.GetSwitchName, id).Trim();

                        if (string.IsNullOrWhiteSpace(responseText))
                        {
                            Logging?.LogWarning("Unexpected empty Fritz command response");
                        }
                    }
                    catch (FritzAPITimeoutException ex)
                    {
                        Logging?.LogWarning("Recieved http timeout");
                        exCaught = ex;
                    }
                    catch (OperationCanceledException)
                    {
                        Logging?.LogTrace("Fritz API thread was canceled");
                        throw;
                    }
                    catch (ThreadInterruptedException)
                    {
                        Logging?.LogTrace("Fritz API thread was interrupted");
                        throw;
                    }
                    catch (Exception ex)
                    {
                        exCaught = ex;
                        Logging?.LogWarning(ex, "Error \"{errortype}\" during Fritz command, error message was: {message}", exCaught.GetType().Name, exCaught.Message);
                    }
                }
                while ((exCaught != null || string.IsNullOrWhiteSpace(responseText)) && WaitBeforeRetry(ref retry));
                // retry if exception was caught or if response was empty

                CheckForException(exCaught, "Failed to get switch name");

                if (string.IsNullOrWhiteSpace(responseText))
                    Logging?.LogWarning("Failed to get name for switch with id {switchId}", id);
                else
                    Logging?.LogDebug("Successfully recieved name for switch with id {switchId}, name is reported to be \"{responseText}\"", id, responseText);

                return responseText ?? "";
            }
        }

        #endregion

        #region Commands by name

        public SwitchState TurnSwitchOnByName(string name, bool? reloadNamesIfNotFound = null, bool skipAutoLogin = false)
        {
            using (Logging?.BeginScope("TurnSwitchOnByName"))
            {
                Logging?.LogDebug("Request to turn on switch with name \"{name}\"", name);

                CheckLoginOrAutoLogin(skipAutoLogin);

                string? id = GetSwitchIdByName(name, reloadNamesIfNotFound ?? Options.ReloadNamesIfNotFound, true);

                if (id != null)
                    return TurnSwitchOn(id, true);
                else
                    return SwitchState.NameNotFound;
            }
        }

        public SwitchState TurnSwitchOffByName(string name, bool? reloadNamesIfNotFound = null, bool skipAutoLogin = false)
        {
            using (Logging?.BeginScope("TurnSwitchOffByName"))
            {
                Logging?.LogDebug("Request to turn off switch with name \"{name}\"", name);

                CheckLoginOrAutoLogin(skipAutoLogin);

                string? id = GetSwitchIdByName(name, reloadNamesIfNotFound ?? Options.ReloadNamesIfNotFound, true);

                if (id != null)
                    return TurnSwitchOff(id, true);
                else
                    return SwitchState.NameNotFound;
            }
        }

        public SwitchState ToggleSwitchStateByName(string name, bool? reloadNamesIfNotFound = null, bool skipAutoLogin = false)
        {
            using (Logging?.BeginScope("ToggleSwitchStateByName"))
            {
                Logging?.LogDebug("Request to toggle switch with name \"{name}\"", name);

                CheckLoginOrAutoLogin(skipAutoLogin);

                string? id = GetSwitchIdByName(name, reloadNamesIfNotFound ?? Options.ReloadNamesIfNotFound, true);

                if (id != null)
                    return ToggleSwitchState(id, true);
                else
                    return SwitchState.NameNotFound;
            }
        }

        public SwitchState GetSwitchStateByName(string name, bool? reloadNamesIfNotFound = null, bool skipAutoLogin = false)
        {
            using (Logging?.BeginScope("GetSwitchStateByName"))
            {
                Logging?.LogDebug("Request to get current state of switch with name \"{name}\"", name);

                CheckLoginOrAutoLogin(skipAutoLogin);

                string? id = GetSwitchIdByName(name, reloadNamesIfNotFound ?? Options.ReloadNamesIfNotFound, true);

                if (id != null)
                    return GetSwitchState(id, true);
                else
                    return SwitchState.NameNotFound;
            }
        }

        public long? GetSwitchPowerByName(string name, bool? reloadNamesIfNotFound = null, bool skipAutoLogin = false)
        {
            using (Logging?.BeginScope("GetSwitchPowerByName"))
            {
                Logging?.LogDebug("Request to get current power usage for switch with name \"{name}\"", name);

                CheckLoginOrAutoLogin(skipAutoLogin);

                string? id = GetSwitchIdByName(name, reloadNamesIfNotFound ?? Options.ReloadNamesIfNotFound, true);

                if (id != null)
                    return GetSwitchPower(id, true);
                else
                    return null;
            }
        }

        public SwitchPresence GetSwitchPresenceByName(string name, bool? reloadNamesIfNotFound = null, bool skipAutoLogin = false)
        {
            using (Logging?.BeginScope("GetSwitchPresenceByName"))
            {
                Logging?.LogDebug("Request to get presence for switch with name \"{name}\"", name);

                CheckLoginOrAutoLogin(skipAutoLogin);

                string? id = GetSwitchIdByName(name, reloadNamesIfNotFound ?? Options.ReloadNamesIfNotFound, true);

                if (id != null)
                    return GetSwitchPresence(id, true);
                else
                    return SwitchPresence.NameNotFound;
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

        private void CheckLoginOrAutoLogin(bool skipAutoLogin)
        {
            using (Logging?.BeginScope("CheckLoginOrAutoLogin"))
            {
                CheckThreadCancellation();

                if (!_isLoggedIn)
                {
                    if (skipAutoLogin)
                    {
                        throw new FritzAPIException($"CheckLoginOrAutoLogin: User \"{Options?.User}\" was not logged in and already tried to auto log in (skipping auto login)");
                    }
                    else
                    {
                        if (Options.AutoLogin)
                        {
                            Logging?.LogWarning("User \"{user}\" was not logged in, logging in user now as AutoLogin was set to true", Options?.User);
                            Login();
                        }
                        else
                        {
                            throw new FritzAPIException($"CheckLoginOrAutoLogin: Not logged in and autologin not activated");
                        }
                    }
                }
                else if (Options.CheckLoginBeforeCommands)
                {
                    if (!skipAutoLogin)
                    {
                        if (!CheckSID())
                        {
                            if (Options.AutoLogin)
                            {
                                Logging?.LogDebug("User \"{user}\" was found to be logged out, logging in user now as AutoLogin was set to true", Options?.User);
                                Login();
                            }
                            else
                            {
                                _isLoggedIn = false;
                                throw new FritzAPIException($"CheckLoginOrAutoLogin: SessionID is invalid and autologin not activated");
                            }
                        }
                    }
                }
            }
        }

        private string SendCommand(FritzAPICommand command, string? id = null)
        {
            using (Logging?.BeginScope("SendCommand"))
            {
                using (var client = GetHttpClient())
                {
                    Logging?.LogTrace("Requesting command {command} for session {sessionId}" + (id != null ? " and device {deviceId}" : ""), command, _sessionId, id);
                    string strRelativeUrlWithQP = c_relativeUrl_Command + $"?sid={_sessionId}&switchcmd={command.ToString().ToLower()}";
                    if (id != null)
                    {
                        strRelativeUrlWithQP += $"&ain={id}";
                    }

                    try
                    {
                        var request = new HttpRequestMessage(HttpMethod.Get, strRelativeUrlWithQP);
                        Logging?.LogTrace("Sending command request: {@Request}", request);

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
                                return sr.ReadToEnd();
                            }
                        }
                        else
                        {
                            throw new FritzAPIHttpException($"Http response ({response.StatusCode}) did not indicate success", response.StatusCode);
                        }
                    }
                    catch (TaskCanceledException tce) when (tce.Source == "System.Net.Http" && (!_cancellationToken.HasValue || !_cancellationToken.Value.IsCancellationRequested))
                    {
                        throw new FritzAPITimeoutException(tce);
                    }
                    catch (OperationCanceledException)
                    {
                        Logging?.LogTrace("Fritz API thread was canceled");
                        throw;
                    }
                    catch (ThreadInterruptedException)
                    {
                        Logging?.LogTrace("Fritz API thread was interrupted");
                        throw;
                    }
                }
            }
        }

        private SwitchState GetSwitchStateByResponse(string response)
        {
            using (Logging?.BeginScope("GetSwitchStateByResponse"))
            {
                CheckThreadCancellation();

                if (rx_Invalid.IsMatch(response))
                {
                    return SwitchState.Unknown;
                }
                else if (rx_BoolTrue.IsMatch(response))
                {
                    return SwitchState.On;
                }
                else if (rx_BoolFalse.IsMatch(response))
                {
                    return SwitchState.Off;
                }
                else
                {
                    return SwitchState.Error;
                }
            }
        }

        private SwitchPresence GetSwitchPresenceByResponse(string response)
        {
            using (Logging?.BeginScope("GetSwitchPresenceByResponse"))
            {
                CheckThreadCancellation();

                if (rx_BoolTrue.IsMatch(response))
                {
                    return SwitchPresence.Present;
                }
                else if (rx_BoolFalse.IsMatch(response))
                {
                    return SwitchPresence.Missing;
                }
                else
                {
                    return SwitchPresence.Error;
                }
            }
        }

        private long? GetNumeric(string response)
        {
            using (Logging?.BeginScope("GetNumeric"))
            {
                CheckThreadCancellation();

                if (long.TryParse(response, out long retVal))
                {
                    return retVal;
                }
                else
                {
                    return null;
                }
            }
        }

        #endregion

        #region Device name helpers

        public void LoadSwitchNames(bool skipAutoLogin = false)
        {
            using (Logging?.BeginScope("LoadSwitchNames"))
            {
                Logging?.LogDebug("Retrieving all switch names");

                CheckThreadCancellation();

                _dicSwitchIdsByName = [];
                List<string> lstSwitchIds = GetSwitchList(skipAutoLogin);

                foreach (string singleSwitchId in lstSwitchIds)
                {
                    string switchName = GetSwitchName(singleSwitchId, true);
                    if (!string.IsNullOrWhiteSpace(switchName))
                    {
                        _dicSwitchIdsByName[switchName] = singleSwitchId;
                    }
                }

                CheckThreadCancellation();
            }
        }

        public string? GetSwitchIdByName(string name, bool reloadIfNotFound, bool skipAutoLogin = false)
        {
            using (Logging?.BeginScope("GetSwitchIdByName"))
            {
                CheckThreadCancellation();

                Logging?.LogTrace("Looking up id for switch with name \"{name}\"", name);
                string? id = _dicSwitchIdsByName.GetValueOrDefault(name);

                CheckThreadCancellation();

                if (id == null && reloadIfNotFound)
                {
                    Logging?.LogDebug("Found no id for a switch with name \"{name}\", reloading all names", name);
                    LoadSwitchNames(skipAutoLogin);
                    id = _dicSwitchIdsByName.GetValueOrDefault(name);
                }

                if (id != null)
                {
                    Logging?.LogDebug("Looked up id {id} for switch with name \"{name}\"", id, name);
                }
                else
                {
                    Logging?.LogError("Id for a switch with name \"{name}\" was not found", name);
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

        [GeneratedRegex(@"^\s*1\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
        private static partial Regex RxBoolTrue();

        [GeneratedRegex(@"^\s*0\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
        private static partial Regex RxBoolFalse();

        [GeneratedRegex(@"^\s*inval\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
        private static partial Regex RxInvalid();
        [GeneratedRegex(@"^\s*0+\s*$")]
        private static partial Regex RxZeroSID();

        #endregion
    }
}