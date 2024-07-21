using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Reflection;
using System.Text.RegularExpressions;

namespace ANUBISClewareAPI
{
    #region Enumerations

    public enum USBSwitchState
    {
        Unknown,
        On,
        Off,
        SwitchNotFound,
        NameNotFound,
        Error,
    }

    public enum ClewareAPICommand
    {
        GetUSBSwitchList,

        GetUSBSwitchState,

        SetUSBSwitchOn,
        SetUSBSwitchOff,
        SetUSBSwitchOnSecure,
        SetUSBSwitchOffSecure
    }

    #endregion

    #region Helper classes

    public class ClewareAPIException : Exception
    {
        public ClewareAPIException(string message) : base(message) { }

        public ClewareAPIException(string message, Exception? innerException) : base(message, innerException) { }
    }

    public class ClewareAPITimeoutException : ClewareAPIException
    {
        public ClewareAPITimeoutException(string? appendMessage = null) : this(null, appendMessage) { }
        public ClewareAPITimeoutException(Exception? innerException, string? appendMessage = null) : base($"The USBswitch command timed out{appendMessage}", innerException) { }
    }

    public class ClewareAPIOptions
    {
        /// <summary>
        /// A list of names for USB Switches with their respective number, so we can work with names
        /// </summary>
        public Dictionary<string, long> USBSwitchNameList { get; init; }

        /// <summary>
        /// The path to the USB switch command line tool to use
        /// </summary>
        public string USBSwitchCommand_Path { get; init; }

        /// <summary>
        /// The arguments to use for getting USB switch list
        /// </summary>
        public string USBSwitchCommand_Arguments_List { get; init; }

        /// <summary>
        /// The command to use for getting switch state.
        /// {switch} will be replace with switch ID.
        /// </summary>
        public string USBSwitchCommand_Arguments_Get { get; init; }

        /// <summary>
        /// The command to use for setting switch on.
        /// {switch} will be replace with switch ID.
        /// </summary>
        public string USBSwitchCommand_Arguments_SetOn { get; init; }

        /// <summary>
        /// The command to use for setting switch off.
        /// {switch} will be replace with switch ID.
        /// </summary>
        public string USBSwitchCommand_Arguments_SetOff { get; init; }

        /// <summary>
        /// The command to use for setting switch on securely.
        /// {switch} will be replace with switch ID.
        /// </summary>
        public string USBSwitchCommand_Arguments_SetOnSecure { get; init; }

        /// <summary>
        /// The command to use for setting switch off securely.
        /// {switch} will be replace with switch ID.
        /// </summary>
        public string USBSwitchCommand_Arguments_SetOffSecure { get; init; }

        /// <summary>
        /// What is the timeout for command calls in seconds?
        /// Default is 5 seconds.
        /// </summary>
        public ushort CommandTimeoutSeconds { get; init; }

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
        /// Default is 500 milliseconds
        /// </summary>
        public ushort AutoRetryMinWaitMilliseconds { get; init; }

        /// <summary>
        /// How long (in milliseconds) should the timespan be, starting from the AutoRetryMinWaitMilliseconds time after an error,
        /// in which a command could be resent? The actual waiting time before resending a command after an error will be randomly
        /// chosen between the AutoRetryMinWaitMilliseconds value and the AutoRetryMinWaitMilliseconds+AutoRetryWaitSpanMilliseconds value
        /// each time a command is resent.
        /// Default is 500 milliseconds.
        /// </summary>
        public ushort AutoRetryWaitSpanMilliseconds { get; init; }

        /// <summary>
        /// Logging object to implement logging.
        /// If no logger is set then nothing will be logged.
        /// </summary>
        public ILogger? Logger { get; init; }

        /// <summary>
        /// Returns the number of a switch according to its name, or null if no number to this name was found
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public long? GetUSBSwitchIdByName(string name)
        {
            return (USBSwitchNameList?.ContainsKey(name) ?? false) ? USBSwitchNameList[name] : null;
        }

        public ClewareAPIOptions()
        {
            USBSwitchNameList = [];
            USBSwitchCommand_Path = "USBswitchCmd";
            USBSwitchCommand_Arguments_List = "-l";
            USBSwitchCommand_Arguments_Get = "-n {switch} -R";
            USBSwitchCommand_Arguments_SetOn = "-n {switch} 1";
            USBSwitchCommand_Arguments_SetOnSecure = "-n {switch} 1 -s";
            USBSwitchCommand_Arguments_SetOff = "-n {switch} 0";
            USBSwitchCommand_Arguments_SetOffSecure = "-n {switch} 0 -s";
            CommandTimeoutSeconds = 5;
            AutoRetryOnErrors = true;
            AutoRetryCount = 2;
            AutoRetryMinWaitMilliseconds = 500;
            AutoRetryWaitSpanMilliseconds = 500;
            Logger = null;
        }
    }

    public class ClewareAPICommandResponse
    {
        public ClewareAPICommand Type { get; init; }
        public List<string> ResponseLines { get; init; }
        public int ExitCode { get; init; }
        public string? FirstResponseLine { get { return ResponseLines.FirstOrDefault(); } }

        public ClewareAPICommandResponse()
        {
            ResponseLines = [];
        }
    }

    public class ClewareDeviceMappingInfo
    {
        public string? Name { get; set; }
        public long? Id { get; set; }
        public bool FoundId { get; set; }
        public bool FoundName { get; set; }
    }

    #endregion

    public partial class ClewareAPI
    {
        #region Fields

        #region Constants

        private readonly Regex rx_SwitchInfo = RxSwitchInfo();

        #endregion

        #region Unchangable fields

        #endregion

        #region Changable fields

        private CancellationToken? _cancellationToken = null;

        #endregion

        #endregion

        #region Properties

        public ClewareAPIOptions Options { get; init; }

        protected ILogger? Logging { get { return Options?.Logger; } }

        #endregion

        #region Constructors

        public ClewareAPI(ClewareAPIOptions options)
        {
            using (Logging?.BeginScope("ClewareAPI.Constructor"))
            {
                Options = options;

                Logging?.LogTrace("Created ClewareAPI object with Options: {@Options}", options);
            }
        }

        public ClewareAPI()
            : this(new ClewareAPIOptions())
        {
        }

        #endregion

        #region General helper methods

        private void CheckThreadCancellation()
        {
            _cancellationToken?.ThrowIfCancellationRequested();
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
                    Logging?.LogWarning("USBSwitch command failed or returned unexpected data, retrying in {retryInMilliseconds} milliseconds (retry {currentRetry}/{maxRetries})", retryInMilliseconds, retry, Options.AutoRetryCount);

                    if (_cancellationToken.HasValue)
                    {
                        if (_cancellationToken.Value.WaitHandle.WaitOne(retryInMilliseconds))
                            Logging?.LogTrace("Cleware api recieved thread cancellation request during command-retry sleep");
                    }
                    else
                        Thread.Sleep(retryInMilliseconds);

                    Logging?.LogTrace("Done waiting before retrying USBSwitch command");

                    CheckThreadCancellation();

                    return true;
                }
                else
                {
                    if (retry > 0)
                    {
                        Logging?.LogWarning("USBSwitch command failed, all {maxRetries} retries exhausted", retry);
                    }
                    else
                    {
                        Logging?.LogWarning($"USBSwitch command failed, no retrying configured");
                    }
                    return false;
                }
            }
        }

        #endregion

        #region Device commands

        #region General commands

        public List<long> GetUSBSwitchIds()
        {
            using (Logging?.BeginScope("GetUSBSwitchIds"))
            {
                CheckThreadCancellation();

                Logging?.LogDebug("Request to retrieve usb switch ids");

                Exception? exCaught = null;
                byte retry = 0;
                List<long> lstRetVal = [];

                do
                {
                    exCaught = null;

                    try
                    {
                        ClewareAPICommandResponse response = SendCommand(ClewareAPICommand.GetUSBSwitchList);

                        CheckThreadCancellation();

                        if (response.ResponseLines.Count > 0)
                        {
                            foreach (var line in response.ResponseLines)
                            {
                                var mtLine = rx_SwitchInfo.Match(line);

                                if (mtLine.Success &&
                                        mtLine.Groups.ContainsKey("deviceId") &&
                                        mtLine.Groups["deviceId"].Success)
                                {
                                    if (long.TryParse(mtLine.Groups["deviceId"].Value, out long idFound))
                                    {
                                        lstRetVal.Add(idFound);
                                    }
                                    else
                                    {
                                        Logging?.LogError("GetUSBSwitchIds: Could not parse device id: {deviceId}", mtLine.Groups["deviceId"].Value);
                                    }
                                }
                                else
                                {
                                    Logging?.LogError("GetUSBSwitchIds: Could not parse command response line: {responseLine}", line);
                                }
                            }
                        }
                    }
                    catch (ClewareAPITimeoutException ex)
                    {
                        Logging?.LogWarning("Recieved command timeout");
                        exCaught = ex;
                    }
                    catch (OperationCanceledException)
                    {
                        Logging?.LogTrace("Cleware API thread was canceled");
                        throw;
                    }
                    catch (ThreadInterruptedException)
                    {
                        Logging?.LogTrace("Cleware API thread was interrupted");
                        throw;
                    }
                    catch (Exception ex)
                    {
                        exCaught = ex;
                        Logging?.LogWarning(ex, "Error \"{errortype}\" during Cleware USB command, error message was: {message}", exCaught.GetType().Name, exCaught.Message);
                    }
                }
                while (exCaught != null && WaitBeforeRetry(ref retry));
                // retry if exception was caught or if response was empty

                CheckForException(exCaught, "Failed to get usb switch id list");

                return lstRetVal;
            }
        }

        public Dictionary<string, long> GetUSBSwitchNames()
        {
            using (Logging?.BeginScope("GetUSBSwitchNames"))
            {
                try
                {
                    List<long> lstFound = GetUSBSwitchIds();

                    CheckThreadCancellation();

                    var dicRetVal = Options?.USBSwitchNameList
                                            ?.Where(itm => lstFound.Contains(itm.Value))
                                            ?.ToDictionary(itm => itm.Key, itm => itm.Value)
                                                ?? [];

                    lstFound.Where(itm => !dicRetVal.ContainsValue(itm))
                            .ToList().ForEach(itm => Logging?.LogWarning("GetUSBSwitchNames: Found usb switch with id {deviceId} that has no name configured", itm));

                    Options?.USBSwitchNameList?.Where(itm => !dicRetVal.ContainsKey(itm.Key))
                            .ToList().ForEach(itm => Logging?.LogDebug("GetUSBSwitchNames: Found no usb switch with id {deviceId} for name {deviceName}", itm.Value, itm.Key));

                    return dicRetVal;
                }
                catch (OperationCanceledException)
                {
                    Logging?.LogTrace("Cleware API thread was canceled");
                    throw;
                }
                catch (ThreadInterruptedException)
                {
                    Logging?.LogTrace("Cleware API thread was interrupted");
                    throw;
                }
                catch (Exception ex)
                {
                    Logging?.LogError(ex, "While trying to get usb switch name list: {message}", ex.Message);
                    throw;
                }
            }
        }

        public List<ClewareDeviceMappingInfo> GetUSBSwitchesNameIdMapping()
        {
            using (Logging?.BeginScope("GetUSBSwitchesNameIdMapping"))
            {
                try
                {
                    List<ClewareDeviceMappingInfo> lstMappings = [];
                    List<long> lstFound = GetUSBSwitchIds();

                    CheckThreadCancellation();

                    var lstRetVal = Options?.USBSwitchNameList
                                            ?.Select(itm => new ClewareDeviceMappingInfo()
                                            {
                                                Id = itm.Value,
                                                Name = itm.Key,
                                                FoundId = lstFound.Contains(itm.Value),
                                                FoundName = true,
                                            })
                                            ?.ToList() ?? [];

                    lstFound.Where(itm => !lstRetVal.Any(itmSub => itmSub.Id == itm))
                            .ToList().ForEach(itm => lstRetVal.Add(new ClewareDeviceMappingInfo()
                            {
                                FoundId = true,
                                FoundName = false,
                                Id = itm,
                                Name = null,
                            }));

                    lstRetVal.Where(itm => !itm.FoundName)
                            .ToList().ForEach(itm => Logging?.LogWarning("GetUSBSwitchesNameIdMapping: Found usb switch with id {deviceId} that has no name configured", itm.Id));

                    lstRetVal.Where(itm => !itm.FoundId)
                            .ToList().ForEach(itm => Logging?.LogDebug("GetUSBSwitchesNameIdMapping: Found no usb switch with id {deviceId} for name {deviceName}", itm.Id, itm.Name));

                    return lstRetVal;
                }
                catch (OperationCanceledException)
                {
                    Logging?.LogTrace("Cleware API thread was canceled");
                    throw;
                }
                catch (ThreadInterruptedException)
                {
                    Logging?.LogTrace("Cleware API thread was interrupted");
                    throw;
                }
                catch (Exception ex)
                {
                    Logging?.LogError(ex, "While trying to get usb switch name list: {message}", ex.Message);
                    throw;
                }
            }
        }

        #endregion

        #region Commands by id

        public USBSwitchState TurnUSBSwitchOn(long id, bool secure = true)
        {
            using (Logging?.BeginScope("TurnUSBSwitchOn"))
            {
                Logging?.LogDebug("Request to turn on usb switch with id {switchId}", id);

                CheckThreadCancellation();

                Exception? exCaught = null;
                byte retry = 0;
                USBSwitchState response = USBSwitchState.Unknown;

                if (id <= 0)
                    throw new ClewareAPIException("TurnUSBSwitchOn: USB Switch id must not be lower or equal to 0");

                do
                {
                    exCaught = null;

                    try
                    {
                        response = GetUSBSwitchStateByResponse(SendCommand(secure ? ClewareAPICommand.SetUSBSwitchOnSecure : ClewareAPICommand.SetUSBSwitchOn, id));
                        if (response != USBSwitchState.On)
                        {
                            Logging?.LogWarning("Unexpected Cleware USB switch state {state} after turning switch on, expected On", response);
                        }
                    }
                    catch (ClewareAPITimeoutException ex)
                    {
                        Logging?.LogWarning("Recieved command timeout");
                        exCaught = ex;
                    }
                    catch (OperationCanceledException)
                    {
                        Logging?.LogTrace("Cleware API thread was canceled");
                        throw;
                    }
                    catch (ThreadInterruptedException)
                    {
                        Logging?.LogTrace("Cleware API thread was interrupted");
                        throw;
                    }
                    catch (Exception ex)
                    {
                        exCaught = ex;
                        Logging?.LogWarning(ex, "Error \"{errortype}\" during Cleware USB command, error message was: {message}", exCaught.GetType().Name, exCaught.Message);
                    }
                }
                while ((exCaught != null || response != USBSwitchState.On) && WaitBeforeRetry(ref retry));
                // retry if exception was caught or if response was empty

                CheckForException(exCaught, "Failed to turn usb switch on");

                if (response != USBSwitchState.On)
                    Logging?.LogWarning("Failed to turn on usb switch with id {switchId}, response was: {response}", id, response);
                else
                    Logging?.LogDebug("Successfully turned on usb switch with id {switchId}", id);

                return response;
            }
        }

        public USBSwitchState TurnUSBSwitchOff(long id, bool secure = true)
        {
            using (Logging?.BeginScope("TurnUSBSwitchOff"))
            {
                Logging?.LogDebug("Request to turn off usb switch with id {switchId}", id);

                CheckThreadCancellation();

                Exception? exCaught = null;
                byte retry = 0;
                USBSwitchState response = USBSwitchState.Unknown;

                if (id <= 0)
                    throw new ClewareAPIException("TurnUSBSwitchOff: USB Switch id must not be lower or equal to 0");

                do
                {
                    exCaught = null;

                    try
                    {
                        response = GetUSBSwitchStateByResponse(SendCommand(secure ? ClewareAPICommand.SetUSBSwitchOffSecure : ClewareAPICommand.SetUSBSwitchOff, id));
                        if (response != USBSwitchState.Off)
                        {
                            Logging?.LogWarning("Unexpected Cleware USB switch state {state} after turning switch off, expected Off", response);
                        }
                    }
                    catch (ClewareAPITimeoutException ex)
                    {
                        Logging?.LogWarning("Recieved command timeout");
                        exCaught = ex;
                    }
                    catch (OperationCanceledException)
                    {
                        Logging?.LogTrace("Cleware API thread was canceled");
                        throw;
                    }
                    catch (ThreadInterruptedException)
                    {
                        Logging?.LogTrace("Cleware API thread was interrupted");
                        throw;
                    }
                    catch (Exception ex)
                    {
                        exCaught = ex;
                        Logging?.LogWarning(ex, "Error \"{errortype}\" during Cleware USB command, error message was: {message}", exCaught.GetType().Name, exCaught.Message);
                    }
                }
                while ((exCaught != null || response != USBSwitchState.Off) && WaitBeforeRetry(ref retry));
                // retry if exception was caught or if response was empty

                CheckForException(exCaught, "Failed to turn usb switch off");

                if (response != USBSwitchState.Off)
                    Logging?.LogWarning("Failed to turn off usb switch with id {switchId}, response was: {response}", id, response);
                else
                    Logging?.LogDebug("Successfully turned off usb switch with id {switchId}", id);

                return response;
            }
        }

        public USBSwitchState GetUSBSwitchState(long id)
        {
            using (Logging?.BeginScope("GetUSBSwitchState"))
            {
                Logging?.LogDebug("Request to get current state of usb switch with id {switchId}", id);

                CheckThreadCancellation();

                Exception? exCaught = null;
                byte retry = 0;
                USBSwitchState response = USBSwitchState.Unknown;

                if (id <= 0)
                    throw new ClewareAPIException("GetUSBSwitchState: Switch id must not be lower or equal to 0");

                do
                {
                    exCaught = null;

                    try
                    {
                        response = GetUSBSwitchStateByResponse(SendCommand(ClewareAPICommand.GetUSBSwitchState, id));
                        if (response != USBSwitchState.On && response != USBSwitchState.Off)
                        {
                            Logging?.LogWarning("Unexpected Cleware USB switch state {state}, expected On or Off", response);
                        }
                    }
                    catch (ClewareAPITimeoutException ex)
                    {
                        Logging?.LogWarning("Recieved command timeout");
                        exCaught = ex;
                    }
                    catch (OperationCanceledException)
                    {
                        Logging?.LogTrace("Cleware API thread was canceled");
                        throw;
                    }
                    catch (ThreadInterruptedException)
                    {
                        Logging?.LogTrace("Cleware API thread was interrupted");
                        throw;
                    }
                    catch (Exception ex)
                    {
                        exCaught = ex;
                        Logging?.LogWarning(ex, "Error \"{errortype}\" during Cleware USB command, error message was: {message}", exCaught.GetType().Name, exCaught.Message);
                    }
                }
                while ((exCaught != null || (response != USBSwitchState.On && response != USBSwitchState.Off)) && WaitBeforeRetry(ref retry));
                // retry if exception was caught or if response was empty

                CheckForException(exCaught, "Failed to get usb switch state");

                if (response != USBSwitchState.On && response != USBSwitchState.Off)
                    Logging?.LogWarning("Failed to get current state for usb switch with id {switchId}, response was: {response}", id, response);
                else
                    Logging?.LogDebug("Successfully recieved current state for usb switch with id {switchId}, switch state is reported to be {response}", id, response);

                return response;
            }
        }

        #endregion

        #region Commands by name

        public USBSwitchState TurnUSBSwitchOnByName(string name, bool secure = true)
        {
            using (Logging?.BeginScope("TurnUSBSwitchOnByName"))
            {
                Logging?.LogDebug("Request to turn on usb switch with name \"{name}\"", name);

                long? id = Options.GetUSBSwitchIdByName(name);

                if (id.HasValue)
                    return TurnUSBSwitchOn(id.Value, secure);
                else
                    return USBSwitchState.NameNotFound;
            }
        }

        public USBSwitchState TurnUSBSwitchOffByName(string name, bool secure = true)
        {
            using (Logging?.BeginScope("TurnUSBSwitchOffByName"))
            {
                Logging?.LogDebug("Request to turn off usb switch with name \"{name}\"", name);

                long? id = Options.GetUSBSwitchIdByName(name);

                if (id != null)
                    return TurnUSBSwitchOff(id.Value, secure);
                else
                    return USBSwitchState.NameNotFound;
            }
        }

        public USBSwitchState GetUSBSwitchStateByName(string name)
        {
            using (Logging?.BeginScope("GetUSBSwitchStateByName"))
            {
                Logging?.LogDebug("Request to get current state of usb switch with name \"{name}\"", name);

                long? id = Options.GetUSBSwitchIdByName(name);

                if (id != null)
                    return GetUSBSwitchState(id.Value);
                else
                    return USBSwitchState.NameNotFound;
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

        private void KillProcess(Process prc)
        {
            if (!(prc?.HasExited ?? true))
            {
                try
                {
                    prc.Kill(true);
                }
                catch (Exception ex1)
                {
                    Logging?.LogError(ex1, "While trying to kill Cleware USBswitchCmd process tree: {message}", ex1.Message);

                    try
                    {
                        prc.Kill();
                    }
                    catch (Exception ex2)
                    {
                        Logging?.LogError(ex2, "While trying to kill Cleware USBswitchCmd process: {message}", ex2.Message);
                    }
                }
            }
        }

        private ClewareAPICommandResponse SendCommand(ClewareAPICommand command, long? id = null)
        {
            using (Logging?.BeginScope("SendCommand"))
            {
                try
                {
                    CheckThreadCancellation();

                    Logging?.LogTrace("Requesting cleware usb switch command {command}" + (id != null ? " for device {deviceId}" : ""), command, id);
                    string? strCommand = Options?.USBSwitchCommand_Path;
                    string? strArguments = null;

                    switch (command)
                    {
                        case ClewareAPICommand.GetUSBSwitchList:
                            strArguments = Options?.USBSwitchCommand_Arguments_List;
                            break;
                        case ClewareAPICommand.GetUSBSwitchState:
                            strArguments = Options?.USBSwitchCommand_Arguments_Get?.Replace("{switch}", id?.ToString());
                            break;
                        case ClewareAPICommand.SetUSBSwitchOn:
                            strArguments = (Options?.USBSwitchCommand_Arguments_SetOn ?? Options?.USBSwitchCommand_Arguments_SetOnSecure)?.Replace("{switch}", id?.ToString());
                            break;
                        case ClewareAPICommand.SetUSBSwitchOnSecure:
                            strArguments = (Options?.USBSwitchCommand_Arguments_SetOnSecure ?? Options?.USBSwitchCommand_Arguments_SetOn)?.Replace("{switch}", id?.ToString());
                            break;
                        case ClewareAPICommand.SetUSBSwitchOff:
                            strArguments = (Options?.USBSwitchCommand_Arguments_SetOff ?? Options?.USBSwitchCommand_Arguments_SetOffSecure)?.Replace("{switch}", id?.ToString());
                            break;
                        case ClewareAPICommand.SetUSBSwitchOffSecure:
                            strArguments = (Options?.USBSwitchCommand_Arguments_SetOffSecure ?? Options?.USBSwitchCommand_Arguments_SetOff)?.Replace("{switch}", id?.ToString());
                            break;
                        default:
                            throw new ClewareAPIException($"Unknown cleware command: {Enum.GetName(command)}");
                    }

                    if (string.IsNullOrWhiteSpace(strCommand))
                        throw new ClewareAPIException($"Cleware command call was empty for command {Enum.GetName(command)}");

                    string strWorkingDirectory = "";
                    if (!Path.IsPathRooted(Options?.USBSwitchCommand_Path))
                    {
                        string? strAssemblyLocation = Assembly.GetEntryAssembly()?.Location;
                        if (!string.IsNullOrWhiteSpace(strAssemblyLocation))
                        {
                            string? strAssemblyDirectory = Path.GetDirectoryName(strAssemblyLocation);
                            if (!string.IsNullOrWhiteSpace(strAssemblyDirectory))
                            {
                                strWorkingDirectory = strAssemblyDirectory;
                            }
                        }
                    }

                    Logging?.LogTrace("Starting command \"{command}\" in working directory \"{workingdirectory}\" with arguments: {arguments}", strCommand, strWorkingDirectory, strArguments);

                    ProcessStartInfo psi =
                        new(strCommand, strArguments ?? "")
                        {
                            UseShellExecute = false,
                            WorkingDirectory = strWorkingDirectory,
                            WindowStyle = ProcessWindowStyle.Hidden,
                            CreateNoWindow = false,
                            RedirectStandardOutput = true,
                        };

                    CheckThreadCancellation();
                    Process? prc = Process.Start(psi);
                    CheckThreadCancellation();

                    if (prc != null)
                    {
                        bool hasExited = true;
                        CheckThreadCancellation();
                        if (_cancellationToken.HasValue)
                        {
                            try
                            {
                                Task tsk = prc.WaitForExitAsync();
                                CheckThreadCancellation();
                                if (tsk != null)
                                {
                                    hasExited = tsk.Wait((Options?.CommandTimeoutSeconds ?? 10) * 1000, _cancellationToken.Value) && prc.HasExited;
                                    CheckThreadCancellation();
                                    KillProcess(prc);
                                }
                                else
                                {
                                    if ((Options?.CommandTimeoutSeconds ?? 0) > 0)
                                    {
                                        DateTime dtKillAfter = DateTime.UtcNow.AddSeconds(Options?.CommandTimeoutSeconds ?? 10);
                                        while (!prc.HasExited)
                                        {
                                            CheckThreadCancellation();
                                            if (DateTime.UtcNow > dtKillAfter)
                                                break;
                                            Thread.Sleep(10);
                                        }
                                        hasExited = prc.HasExited;
                                        KillProcess(prc);
                                    }
                                    else
                                    {
                                        while (!prc.HasExited)
                                            CheckThreadCancellation();
                                    }
                                    CheckThreadCancellation();
                                }
                            }
                            catch (OperationCanceledException)
                            {
                                hasExited = false;
                                Logging?.LogTrace("Cleware API thread was canceled during command call");
                                if (!prc.HasExited)
                                    KillProcess(prc);
                                throw;
                            }
                            catch (ThreadInterruptedException)
                            {
                                hasExited = false;
                                Logging?.LogTrace("Cleware API thread was interrupted during command call");
                                if (!prc.HasExited)
                                    KillProcess(prc);
                                throw;
                            }
                        }
                        else
                        {
                            if ((Options?.CommandTimeoutSeconds ?? 0) > 0)
                            {
                                hasExited = prc.WaitForExit((Options?.CommandTimeoutSeconds ?? 10) * 1000) && prc.HasExited;
                                KillProcess(prc);
                            }
                            else
                            {
                                prc.WaitForExit();
                            }
                        }

                        if (!hasExited)
                            throw new ClewareAPITimeoutException($" for command {Enum.GetName(command)}");
                    }
                    else
                    {
                        throw new ClewareAPIException($"Could not create process for command {Enum.GetName(command)}");
                    }

                    CheckThreadCancellation();

                    List<string> lstOutput = [];
                    while (!prc.StandardOutput.EndOfStream)
                    {
                        var line = prc.StandardOutput.ReadLine();
                        if (!string.IsNullOrWhiteSpace(line))
                            lstOutput.Add(line);
                    }

                    return new ClewareAPICommandResponse()
                    {
                        Type = command,
                        ResponseLines = lstOutput,
                        ExitCode = prc.ExitCode,
                    };
                }
                catch (OperationCanceledException)
                {
                    Logging?.LogTrace("Cleware API thread was canceled");
                    throw;
                }
                catch (ThreadInterruptedException)
                {
                    Logging?.LogTrace("Cleware API thread was interrupted");
                    throw;
                }
                catch (Exception ex)
                {
                    Logging?.LogError(ex, "Error {errortype} while trying to execute cleware usb switch command {command}, message: {message}", ex.GetType().FullName, Enum.GetName(command), ex.Message);
                    throw;
                }
            }
        }

        private USBSwitchState GetUSBSwitchStateByResponse(ClewareAPICommandResponse response)
        {
            using (Logging?.BeginScope("GetUSBSwitchStateByResponse"))
            {
                CheckThreadCancellation();

                if (response.ExitCode == 0)
                {
                    return USBSwitchState.Off;
                }
                else if (response.ExitCode == 1)
                {
                    return USBSwitchState.On;
                }
                else if (response.ExitCode == -1 || response.ExitCode == 255)
                {
                    return USBSwitchState.SwitchNotFound;
                }
                else
                {
                    return USBSwitchState.Error;
                }
            }
        }

        #endregion

        #region Device name helpers

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

        [GeneratedRegex(@"^Device\s*(?<deviceNr>\d*)\s*:\s*Type\s*=\s*(?<deviceType>\d*)\s*,\s*Version\s*=\s*(?<deviceVer>\d*)\s*,\s*SerNum\s*=\s*(?<deviceId>\d+)\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
        private static partial Regex RxSwitchInfo();

        #endregion
    }
}