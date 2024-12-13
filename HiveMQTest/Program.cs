using HiveMQtt.Client;
using HiveMQtt.Client.Options;
using HiveMQtt.MQTT5.ReasonCodes;
using HiveMQtt.MQTT5.Types;
using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Diagnostics;

class HiveMQTTDisplay
{
    static async Task Main(string[] args)
    {
        // Launch a new console window for displaying telemetry data
        // Method at the bottom of code
        LaunchNewConsoleWindow();

        // Step 1: Load the configuration from a JSON file
        var options = LoadConfig("ClientOptions.json");

        if (options == null)
        {
            Console.WriteLine("Failed to load configuration.");
            return;
        }

        // Step 2: Create the client with the loaded options
        var client = new HiveMQClient(options);

        Console.WriteLine($"Connecting to {options.Host} on port {options.Port} ...");

        // Step 3: Connect to the MQTT broker
        HiveMQtt.Client.Results.ConnectResult connectResult;
        try
        {
            connectResult = await client.ConnectAsync().ConfigureAwait(false);
            if (connectResult.ReasonCode == ConnAckReasonCode.Success)
            {
                Console.WriteLine($"Connect successful: {connectResult}");
            }
            else
            {
                Console.WriteLine($"Connect failed: {connectResult}");
                Environment.Exit(-1);
            }
        }
        catch (System.Net.Sockets.SocketException e)
        {
            Console.WriteLine($"Error connecting to the MQTT Broker with the following socket error: {e.Message}");
            Environment.Exit(-1);
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error connecting to the MQTT Broker with the following message: {e.Message}");
            Environment.Exit(-1);
        }

        //Create a new process to display telemetry in another console window
        //var telemetryProcess = new Process
        //{
        //    StartInfo = new ProcessStartInfo
        //    {
        //        FileName = "dotnet", // This can be the executable of another .NET application or a script
        //        Arguments = "TelemetryDisplay.dll", // A separate .NET console app to show telemetry data
        //        RedirectStandardOutput = true,
        //        UseShellExecute = false,
        //        CreateNoWindow = false // Keep the new window open
        //    }
        //};
        //telemetryProcess.Start();


        // Message Handler
        client.OnMessageReceived += (sender, args) =>
        {
            string received_message = args.PublishMessage.PayloadAsString;
            Console.WriteLine(received_message);
        };

        // Subscribe
        await client.SubscribeAsync("hivemqdemo/commands").ConfigureAwait(false);

        // Initializing telemetry values
        double temperature = 25.1;
        double humidity = 77.5;
        var rand = new Random();

        Console.WriteLine("Publishing message...");

        while (true)
        {
            double currentTemperature = temperature + rand.NextDouble();
            double currentHumidity = humidity + rand.NextDouble();
            var msg = JsonSerializer.Serialize(new
            {
                temperature = currentTemperature,
                humidity = currentHumidity,
            });

            // Publish MQTT messages
            var result = await client.PublishAsync("hivemqdemo/telemetry", msg, QualityOfService.AtLeastOnceDelivery).ConfigureAwait(false);

            //// Display the published message in the new console window
            WriteToNewConsole($"Published: {msg}");
        }
    }

    // Step 4: Method to load configuration from a JSON file
    static HiveMQClientOptions LoadConfig(string filePath)
    {
        try
        {
            // Read the JSON content from the file
            string json = File.ReadAllText(filePath);

            // Deserialize the JSON content into the HiveMQClientOptions object
            var options = JsonSerializer.Deserialize<HiveMQClientOptions>(json);

            return options;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error reading configuration file: {ex.Message}");
            return null;
        }
    }

    // Function to launch a new console window
    static void LaunchNewConsoleWindow()
    {
        // Launch a new console application process
        Process.Start(new ProcessStartInfo("cmd.exe", "/K") { CreateNoWindow = false });
    }

    // Write to the new console window (This assumes that the second console is listening for output)
    static void WriteToNewConsole(string message)
    { Console.WriteLine(message); }
}
