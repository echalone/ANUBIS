using ANUBISConsole.ConfigHelpers;
using ANUBISWatcher.Shared;
using Microsoft.Extensions.Logging;
using Spectre.Console;

namespace ANUBISConsole
{
    internal class Program
    {
        private static ConsoleColor? clr_originalBackground = null;
        private static ConsoleColor? clr_originalText = null;

#pragma warning disable IDE0060 // Nicht verwendete Parameter entfernen
        static void Main(string[] args)
#pragma warning restore IDE0060 // Nicht verwendete Parameter entfernen
        {
            // this should happen as fast as possible, no time for options
            Console.InputEncoding = Console.OutputEncoding = System.Text.Encoding.UTF8;
            AnsiConsole.Profile.Encoding = System.Text.Encoding.UTF8;
            AnsiConsole.Profile.Capabilities.Unicode = true;

            try
            {
                //save all example configs for comparison to test if examples are equal to saved json configs:
                //ANUBISWatcher.Configuration.ConfigFileData.Examples.SaveAllExamples(@"C:\tmp");

                AnsiConsole.Clear();
                AnsiConsole.Status()
                            .AutoRefresh(true)
                            .Spinner(Spinner.Known.Dots)
                            .SpinnerStyle(Style.Parse("lime"))
                            .Start("Initializing...", ctx =>
                                    {
                                        Init();
                                    });

                if (AnubisOptions.Options.exitAlternateBufferOnStartup)
                {

                    Console.Write("\x1b[?1049l");
                }

                if (AnubisOptions.Options.forceInteractiveConsole)
                {
                    AnsiConsole.Profile.Capabilities.Interactive = true;
                }

                if (AnubisOptions.Options.forceDefaultColors)
                {
                    clr_originalBackground = Console.BackgroundColor;
                    clr_originalText = Console.ForegroundColor;
                    Console.BackgroundColor = AnubisOptions.Options.backgroundColor;
                    Console.ForegroundColor = AnubisOptions.Options.textColor;
                }

                AnsiConsole.MarkupLine($"ANUBIS Watcher [{AnubisOptions.Options.defaultColor_Ok}]initialized[/]");
                Thread.Sleep(1000);

                UI.InitConfig initConfig = new();
                if (initConfig.MainConfiguration())
                {
                    UI.Controlling.StartUpController(AnubisConfig.LoadedConfig ?? "<unknown config>");
                }
            }
            catch (Exception ex)
            {
                ILogger? logging = SharedData.InterfaceLogging;
                if (logging != null)
                {
                    using (logging.BeginScope("Interface.MainError"))
                    {
                        logging.LogCritical(ex, "Uncaught error of type {type} in main interface, message was: {message}", ex.GetType().Name, ex.Message);
                    }
                }
            }
            finally
            {
                Stop();
            }
        }

        private static void Init()
        {
            Generator.Init();

            var logging = SharedData.InterfaceLogging;

            using (logging?.BeginScope("Interface.Init"))
            {
                logging?.LogInformation("\r\n\r\n\r\n==================================================\r\n");
                logging?.LogInformation("---------- ANUBIS Watcher started ----------");

                AnubisOptions.LoadOptions();
            }
        }

        private static void Stop()
        {
            var logging = SharedData.InterfaceLogging;

            using (logging?.BeginScope("Interface.Stop"))
            {
                if (SharedData.Controller != null)
                {
                    logging?.LogInformation("Stopping controller...");
                    try
                    {
                        Generator.StopController();
                        logging?.LogInformation("Controller stopped gracefully in time");
                    }
                    catch (Exception ex)
                    {
                        logging?.LogError(ex, "Error trying to stop controller: {message}", ex.Message);
                    }
                }

                if (clr_originalBackground.HasValue)
                {
                    Console.BackgroundColor = clr_originalBackground.Value;
                }

                if (clr_originalText.HasValue)
                {
                    Console.ForegroundColor = clr_originalText.Value;
                }

                logging?.LogInformation("---------- ANUBIS Watcher stopped ----------");
            }

            System.Environment.Exit(0);
        }
    }
}
