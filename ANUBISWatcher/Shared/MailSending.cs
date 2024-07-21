using ANUBISWatcher.Helpers;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Mail;
using System.Text;

namespace ANUBISWatcher.Shared
{
    public class SendMailOptions
    {
        public ILogger? Logging { get; init; }
        public string? File { get; init; }

#pragma warning disable CS8618
        public string From { get; init; }
        public string SmtpServer { get; init; }
#pragma warning restore CS8618
        public int? SmtpPort { get; init; }
        public bool UseSsl { get; init; }
        public string? SmtpUser { get; init; }
        public string? SmtpPassword { get; init; }
    }

    public static class MailSending
    {
        private static readonly char[] separator = [',', ';'];
        private const int c_Smtp_Ssl_Port = 587;
        private const int c_Smtp_NonSsl_Port = 25;

        public static void ReadAndSendMailConfig(SendMailOptions options, string? simulateTo, bool simulate)
        {
            using (options.Logging?.BeginScope("ReadAndSendMailConfig"))
            {
                try
                {
                    if (!string.IsNullOrWhiteSpace(options.File))
                    {
                        if (File.Exists(options.File))
                        {
                            options.Logging?.LogInformation(@"Sending mails according to configuration file ""{filepath}""", options.File);

                            FileStreamOptions fso = new() { Access = FileAccess.Read, Mode = FileMode.Open, Share = FileShare.Read };
                            using (StreamReader sr = new(options.File, Encoding.UTF8, true, fso))
                            {
                                string? strFirstLine = sr.ReadLine()?.Trim();
                                if (strFirstLine != null)
                                {
#pragma warning disable IDE0305 // Initialisierung der Sammlung vereinfachen
                                    List<string> lstRecipients = strFirstLine.Split(separator, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).ToList();
#pragma warning restore IDE0305 // Initialisierung der Sammlung vereinfachen

                                    if (lstRecipients.Count > 0)
                                    {
                                        List<MailAddress?> lstLegalRecipients = [];
                                        foreach (string singleRecipient in lstRecipients)
                                        {
                                            if (MailAddress.TryCreate(singleRecipient, out MailAddress? resultRecipient))
                                            {
                                                options.Logging?.LogTrace(@"Adding ""{recipient}"" as a legal mail address", singleRecipient);
                                                lstLegalRecipients.Add(resultRecipient);
                                            }
                                            else
                                            {
                                                options.Logging?.LogWarning(@"The email address ""{recipient}"" could not be parsed, not sending mail to this email", singleRecipient);
                                            }
                                        }
                                        if (lstLegalRecipients.Count > 0)
                                        {
                                            options.Logging?.LogDebug("Found the following legal recipients: " + string.Join(", ", lstLegalRecipients));
                                            if (simulate)
                                            {
                                                options.Logging?.LogDebug("Replacing legal recipients with simulated recipient");
                                                if (!string.IsNullOrWhiteSpace(simulateTo))
                                                {
                                                    if (MailAddress.TryCreate(simulateTo, out MailAddress? maSimulate))
                                                    {
                                                        lstLegalRecipients = [maSimulate];
                                                    }
                                                    else
                                                    {
                                                        throw new SendMailException($@"The email address ""{simulateTo}"" to send the simulated mails to is not valid");
                                                    }
                                                }
                                                else
                                                {
                                                    lstLegalRecipients = [];
                                                }
                                            }

                                            string? strSubject = sr.ReadLine()?.Trim();

                                            if (!string.IsNullOrEmpty(strSubject))
                                            {
                                                string? strBody = sr.ReadToEnd()?.Trim();

                                                if (!string.IsNullOrEmpty(strBody))
                                                {
                                                    SendMail(options, lstLegalRecipients, sr.CurrentEncoding, strSubject, strBody);
                                                }
                                                else
                                                {
                                                    throw new SendMailException("Body was empty");
                                                }
                                            }
                                            else
                                            {
                                                throw new SendMailException("Subject line (second line) was empty");
                                            }
                                        }
                                        else
                                        {
                                            throw new SendMailException("No recipients found in first line that would be a legal mail address");
                                        }
                                    }
                                    else
                                    {
                                        throw new SendMailException("No recipients defined in first line");
                                    }
                                }
                                else
                                {
                                    throw new SendMailException("Configuration file was empty");
                                }
                            }
                        }
                        else
                        {
                            options.Logging?.LogWarning(@"Mail configuration file ""{filepath}"" doesn't exist", options.File);
                        }
                    }
                    else
                    {
                        throw new SendMailException("The file path to the configuration file was null or empty");
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
                    options.Logging?.LogError("Error while trying to read mail configuration file {filepath}, will not try again to send this specific mail, error message was: {message}", options.File, ex.Message);
                }
            }
        }

        public static void SendMail(SendMailOptions options, List<MailAddress?> recipients, Encoding encoding, string subject, string body)
        {
            using (options.Logging?.BeginScope("SendMail"))
            {
                try
                {
                    if (recipients.Count == 0)
                    {
                        recipients = [null];
                    }
                    foreach (MailAddress? recipient in recipients)
                    {
                        if (recipient == null)
                            options.Logging?.LogInformation("Simulating sending mail with {subject} now...", subject);
                        else
                            options.Logging?.LogInformation("Sending mail with {subject} to recipient {recipient} now...", subject, recipient.Address);

                        if (!MailAddress.TryCreate(options.From, out MailAddress? maFrom))
                        {
                            throw new SendMailException($@"The email address ""{options.From}"" from which to send the mails from is not valid");
                        }

                        MailMessage message = new()
                        {
                            SubjectEncoding = encoding,
                            BodyEncoding = encoding,
                            Subject = subject,
                            Body = body,
                            Sender = maFrom,
                            From = maFrom,
                            IsBodyHtml = false,
                            Priority = MailPriority.High
                        };
                        if (recipient != null)
                            message.To.Add(recipient);

                        int port = options.SmtpPort ?? (options.UseSsl ? c_Smtp_Ssl_Port : c_Smtp_NonSsl_Port);

                        SmtpClient smtpClient = new(options.SmtpServer, port);
                        if (!string.IsNullOrEmpty(options.SmtpPassword))
                        {
                            smtpClient.Credentials = new NetworkCredential(options.SmtpUser, options.SmtpPassword);
                        }
                        smtpClient.EnableSsl = options.UseSsl;

                        if (recipient != null)
                        {
                            smtpClient.Send(message);
                            options.Logging?.LogInformation("Mail has been sent");
                        }
                        else
                        {
                            options.Logging?.LogInformation("Mail sending has been simulated");
                        }
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
                    options.Logging?.LogError("Error while trying to send an email with subject {subject}, to {recipientcount} recipients, error message was: {message}", subject, recipients.Count, ex.Message);
                }
            }
        }

    }
}
