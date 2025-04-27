using System;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Net.Client;
using SmartHome.Services;

namespace SmartHomeClient
{
    class Program
    {
        private static readonly string Server1Address = "http://localhost:50051";
        private static readonly string Server2Address = "http://localhost:50052";
        private static ServerSelectionMode currentServerSelection = ServerSelectionMode.Auto;

        static async Task Main(string[] args)
        {
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

            Console.WriteLine("Smart Home Client");
            Console.WriteLine("=================");

            string userId = "user1";

            while (true)
            {
                Console.WriteLine("\nMain Menu:");
                Console.WriteLine("1. List all devices");
                Console.WriteLine("2. Get device information");
                Console.WriteLine("3. Control device");
                Console.WriteLine("4. Monitor device");
                Console.WriteLine("5. Change server selection");
                Console.WriteLine("6. Exit");
                Console.Write("\nEnter your choice (1-6): ");

                string choice = Console.ReadLine();

                try
                {
                    switch (choice)
                    {
                        case "1":
                            await ListDevicesAsync(userId);
                            break;
                        case "2":
                            await GetDeviceInfoAsync();
                            break;
                        case "3":
                            await ControlDeviceAsync();
                            break;
                        case "4":
                            await MonitorDeviceAsync();
                            break;
                        case "5":
                            ConfigureServerSelection();
                            break;
                        case "6":
                            Console.WriteLine("Exiting...");
                            return;
                        default:
                            Console.WriteLine("Invalid choice. Please try again.");
                            break;
                    }
                }
                catch (RpcException ex)
                {
                    Console.WriteLine($"RPC Error: {ex.Status.Detail}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }
            }
        }

        private enum ServerSelectionMode
        {
            Auto,
            Server1,
            Server2
        }

        private static void ConfigureServerSelection()
        {
            Console.WriteLine("\nServer Selection:");
            Console.WriteLine("1. Auto (try both servers)");
            Console.WriteLine("2. Server 1 only (localhost:50051)");
            Console.WriteLine("3. Server 2 only (localhost:50052)");
            Console.Write("Enter your choice (1-3): ");

            string choice = Console.ReadLine();
            switch (choice)
            {
                case "1":
                    currentServerSelection = ServerSelectionMode.Auto;
                    Console.WriteLine("Server selection set to Auto");
                    break;
                case "2":
                    currentServerSelection = ServerSelectionMode.Server1;
                    Console.WriteLine("Server selection set to Server 1");
                    break;
                case "3":
                    currentServerSelection = ServerSelectionMode.Server2;
                    Console.WriteLine("Server selection set to Server 2");
                    break;
                default:
                    Console.WriteLine("Invalid choice. Server selection unchanged.");
                    break;
            }
        }

        private static string[] GetTargetServers()
        {
            return currentServerSelection switch
            {
                ServerSelectionMode.Auto => new[] { Server1Address, Server2Address },
                ServerSelectionMode.Server1 => new[] { Server1Address },
                ServerSelectionMode.Server2 => new[] { Server2Address },
                _ => new[] { Server1Address, Server2Address }
            };
        }

        private static async Task ListDevicesAsync(string userId)
        {
            Console.WriteLine("\nListing all devices...");
            
            var servers = GetTargetServers();
            bool success = false;

            foreach (var serverAddress in servers)
            {
                Console.WriteLine($"Connecting to server {serverAddress}...");
                try
                {
                    using var channel = GrpcChannel.ForAddress(serverAddress);
                    var client = new SmartHomeService.SmartHomeServiceClient(channel);
                    
                    var request = new ListDevicesRequest { UserId = userId };
                    var response = await client.ListDevicesAsync(request);
                    
                    Console.WriteLine($"\nDevices from server {serverAddress}:");
                    Console.WriteLine("ID\tName\tType\tSubType\tOnline");
                    Console.WriteLine("--------------------------------------------------");

                    foreach (var device in response.Devices)
                    {
                        Console.WriteLine($"{device.Id}\t{device.Name}\t{GetDeviceTypeName(device.Type)}\t{device.SubType}\t{device.Online}");
                    }
                    
                    success = true;
                    if (currentServerSelection == ServerSelectionMode.Auto)
                    {
                        break;
                    }
                }
                catch (RpcException ex) when (ex.StatusCode == StatusCode.Unavailable)
                {
                    Console.WriteLine($"Server {serverAddress} is unavailable.");
                }
            }

            if (!success)
            {
                Console.WriteLine("Could not connect to any server.");
            }
        }

        private static async Task GetDeviceInfoAsync()
        {
            Console.Write("\nEnter device ID: ");
            string deviceId = Console.ReadLine();

            var servers = GetTargetServers();
            bool success = false;

            foreach (var serverAddress in servers)
            {
                try
                {
                    using var channel = GrpcChannel.ForAddress(serverAddress);
                    var client = new SmartHomeService.SmartHomeServiceClient(channel);

                    var request = new DeviceInfoRequest { DeviceId = deviceId };
                    var response = await client.GetDeviceInfoAsync(request);

                    Console.WriteLine($"\nDevice Information from server {serverAddress}:");
                    Console.WriteLine($"ID: {response.Device.Id}");
                    Console.WriteLine($"Name: {response.Device.Name}");
                    Console.WriteLine($"Type: {GetDeviceTypeName(response.Device.Type)}");
                    Console.WriteLine($"SubType: {response.Device.SubType}");
                    Console.WriteLine($"Online: {response.Device.Online}");

                    switch (response.DeviceSpecifiedInfoCase)
                    {
                        case DeviceInfoResponse.DeviceSpecifiedInfoOneofCase.SecurityCameraInfo:
                            var cameraInfo = response.SecurityCameraInfo;
                            Console.WriteLine("\nSecurity Camera Information:");
                            Console.WriteLine($"Location: {cameraInfo.Location}");
                            Console.WriteLine($"Recording: {cameraInfo.Recording}");
                            Console.WriteLine($"Position: Pan={cameraInfo.Position.Pan}, Tilt={cameraInfo.Position.Tilt}, Zoom={cameraInfo.Position.Zoom}");
                            Console.WriteLine($"Battery Level: {cameraInfo.BatteryLevel}%");
                            break;
                        case DeviceInfoResponse.DeviceSpecifiedInfoOneofCase.ThermostatInfo:
                            var thermostatInfo = response.ThermostatInfo;
                            Console.WriteLine("\nThermostat Information:");
                            Console.WriteLine($"Location: {thermostatInfo.Location}");
                            Console.WriteLine($"Temperature Unit: {thermostatInfo.TemperatureUnit}");
                            Console.WriteLine($"Current Temperature: {thermostatInfo.CurrentTemperature} {thermostatInfo.TemperatureUnit}");
                            Console.WriteLine($"Target Temperature: {thermostatInfo.TargetTemperature} {thermostatInfo.TemperatureUnit}");
                            Console.WriteLine($"Battery Level: {thermostatInfo.BatteryLevel}%");
                            break;
                        case DeviceInfoResponse.DeviceSpecifiedInfoOneofCase.RefrigeratorInfo:
                            var fridgeInfo = response.RefrigeratorInfo;
                            Console.WriteLine("\nRefrigerator Information:");
                            Console.WriteLine($"Mode: {GetRefrigeratorModeName(fridgeInfo.Mode)}");
                            Console.WriteLine($"Current Temperature: {fridgeInfo.CurrentTemperature}°C");
                            Console.WriteLine($"Door Open: {fridgeInfo.DoorOpen}");
                            Console.WriteLine("\nCompartments:");
                            foreach (var compartment in fridgeInfo.Compartments)
                            {
                                Console.WriteLine($"  {compartment.Name}: Current={compartment.CurrentTemperature}°C, Target={compartment.TargetTemperature}°C");
                            }
                            break;
                        default:
                            Console.WriteLine("No specific device information available.");
                            break;
                    }

                    success = true;
                    if (currentServerSelection == ServerSelectionMode.Auto)
                    {
                        break;
                    }
                }
                catch (RpcException ex) when (ex.StatusCode == StatusCode.Unavailable)
                {
                    Console.WriteLine($"Server {serverAddress} is unavailable.");
                }
                catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
                {
                    Console.WriteLine($"Device not found on server {serverAddress}.");
                }
            }

            if (!success)
            {
                Console.WriteLine($"Could not get device info from any server.");
            }
        }

        private static async Task ControlDeviceAsync()
        {
            Console.Write("\nEnter device ID: ");
            string deviceId = Console.ReadLine();

            DeviceType deviceType = DeviceType.Unknown;
            var servers = GetTargetServers();

            foreach (var serverAddress in servers)
            {
                try
                {
                    using var channel = GrpcChannel.ForAddress(serverAddress);
                    var client = new SmartHomeService.SmartHomeServiceClient(channel);

                    var infoRequest = new DeviceInfoRequest { DeviceId = deviceId };
                    var infoResponse = await client.GetDeviceInfoAsync(infoRequest);
                    
                    deviceType = infoResponse.Device.Type;
                    
                    ControlRequest controlRequest = new ControlRequest { DeviceId = deviceId };
                    
                    switch (deviceType)
                    {
                        case DeviceType.SecurityCamera:
                            Console.WriteLine("\nSecurity Camera Control Options:");
                            Console.WriteLine("1. Set Position");
                            Console.WriteLine("2. Set Recording");
                            Console.Write("\nEnter your choice (1-2): ");
                            
                            string cameraChoice = Console.ReadLine();
                            
                            switch (cameraChoice)
                            {
                                case "1":
                                    Console.Write("Enter Pan value: ");
                                    float pan = float.Parse(Console.ReadLine());
                                    Console.Write("Enter Tilt value: ");
                                    float tilt = float.Parse(Console.ReadLine());
                                    Console.Write("Enter Zoom value: ");
                                    float zoom = float.Parse(Console.ReadLine());
                                    controlRequest.SetPosition = new SetPosition { Position = new Position { Pan = pan, Tilt = tilt, Zoom = zoom } };
                                    break;
                                case "2":
                                    Console.Write("Start recording (true/false): ");
                                    bool recording = bool.Parse(Console.ReadLine());
                                    controlRequest.SetRecording = new SetRecording { Recording = recording };
                                    break;
                                default:
                                    Console.WriteLine("Invalid choice.");
                                    return;
                            }
                            break;
                        case DeviceType.Thermostat:
                            Console.WriteLine("\nThermostat Control Options:");
                            Console.WriteLine("1. Set Temperature");
                            Console.Write("\nEnter your choice (1): ");
                            
                            string thermostatChoice = Console.ReadLine();
                            
                            if (thermostatChoice == "1")
                            {
                                Console.Write("Enter target temperature: ");
                                float temperature = float.Parse(Console.ReadLine());
                                controlRequest.SetTemperature = new SetTemperature { Temperature = temperature };
                            }
                            else
                            {
                                Console.WriteLine("Invalid choice.");
                                return;
                            }
                            break;
                        case DeviceType.Refrigerator:
                            Console.WriteLine("\nRefrigerator Control Options:");
                            Console.WriteLine("1. Set Mode");
                            Console.Write("\nEnter your choice (1): ");
                            
                            string fridgeChoice = Console.ReadLine();
                            
                            if (fridgeChoice == "1")
                            {
                                Console.WriteLine("Available modes:");
                                Console.WriteLine("0 - Normal");
                                Console.WriteLine("1 - Eco");
                                Console.WriteLine("2 - Quick");
                                Console.Write("Enter mode (0-2): ");
                                int modeValue = int.Parse(Console.ReadLine());
                                RefrigeratorInfo.Types.Mode mode = (RefrigeratorInfo.Types.Mode)modeValue;
                                controlRequest.SetMode = new SetMode { Mode = mode };
                            }
                            else
                            {
                                Console.WriteLine("Invalid choice.");
                                return;
                            }
                            break;
                        default:
                            Console.WriteLine("Unknown device type. Cannot control.");
                            return;
                    }
                    
                    var controlResponse = await client.ControlDeviceAsync(controlRequest);
                    Console.WriteLine($"\nControl Response:");
                    Console.WriteLine($"Success: {controlResponse.Success}");
                    Console.WriteLine($"Message: {controlResponse.Message}");
                    return;
                }
                catch (RpcException ex) when (ex.StatusCode == StatusCode.Unavailable)
                {
                    Console.WriteLine($"Server {serverAddress} is unavailable.");
                }
                catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
                {
                    Console.WriteLine($"Device not found on server {serverAddress}.");
                }
            }

            Console.WriteLine($"Could not control device on any server.");
        }

        private static async Task MonitorDeviceAsync()
        {
            Console.Write("\nEnter device ID: ");
            string deviceId = Console.ReadLine();
            
            Console.Write("Enter monitoring interval in seconds: ");
            int interval = int.Parse(Console.ReadLine());
            
            Console.WriteLine($"\nMonitoring device {deviceId} with interval {interval}s...");
            Console.WriteLine("Press Ctrl+C to stop monitoring.");
            
            CancellationTokenSource cts = new CancellationTokenSource();
            Console.CancelKeyPress += (s, e) => {
                e.Cancel = true;
                cts.Cancel();
                Console.WriteLine("\nMonitoring stopped.");
            };
            
            var servers = GetTargetServers();

            foreach (var serverAddress in servers)
            {
                try
                {
                    using var channel = GrpcChannel.ForAddress(serverAddress);
                    var client = new SmartHomeService.SmartHomeServiceClient(channel);
                    
                    var request = new MonitorRequest { DeviceId = deviceId, Interval = interval };
                    using var call = client.MonitorDevice(request);
                    
                    try
                    {
                        await foreach (var status in call.ResponseStream.ReadAllAsync(cts.Token))
                        {
                            Console.WriteLine($"\n[{DateTime.Now}] Device Status Update:");
                            Console.WriteLine($"Device ID: {status.DeviceId}");
                            Console.WriteLine($"Device Type: {GetDeviceTypeName(status.DeviceType)}");
                            Console.WriteLine($"Online: {status.IsOnline}");
                            
                            switch (status.StatusInfoCase)
                            {
                                case DeviceStatus.StatusInfoOneofCase.SecurityCameraInfo:
                                    var cameraInfo = status.SecurityCameraInfo;
                                    Console.WriteLine("Security Camera Status:");
                                    Console.WriteLine($"  Location: {cameraInfo.Location}");
                                    Console.WriteLine($"  Recording: {cameraInfo.Recording}");
                                    Console.WriteLine($"  Position: Pan={cameraInfo.Position.Pan:F1}, Tilt={cameraInfo.Position.Tilt:F1}, Zoom={cameraInfo.Position.Zoom:F1}");
                                    Console.WriteLine($"  Battery Level: {cameraInfo.BatteryLevel:F1}%");
                                    break;
                                case DeviceStatus.StatusInfoOneofCase.ThermostatInfo:
                                    var thermostatInfo = status.ThermostatInfo;
                                    Console.WriteLine("Thermostat Status:");
                                    Console.WriteLine($"  Location: {thermostatInfo.Location}");
                                    Console.WriteLine($"  Current Temperature: {thermostatInfo.CurrentTemperature:F1} {thermostatInfo.TemperatureUnit}");
                                    Console.WriteLine($"  Target Temperature: {thermostatInfo.TargetTemperature:F1} {thermostatInfo.TemperatureUnit}");
                                    Console.WriteLine($"  Battery Level: {thermostatInfo.BatteryLevel:F1}%");
                                    break;
                                case DeviceStatus.StatusInfoOneofCase.RefrigeratorInfo:
                                    var fridgeInfo = status.RefrigeratorInfo;
                                    Console.WriteLine("Refrigerator Status:");
                                    Console.WriteLine($"  Mode: {GetRefrigeratorModeName(fridgeInfo.Mode)}");
                                    Console.WriteLine($"  Current Temperature: {fridgeInfo.CurrentTemperature:F1}°C");
                                    Console.WriteLine($"  Door Open: {fridgeInfo.DoorOpen}");
                                    Console.WriteLine("  Compartments:");
                                    foreach (var compartment in fridgeInfo.Compartments)
                                    {
                                        Console.WriteLine($"    {compartment.Name}: Current={compartment.CurrentTemperature:F1}°C, Target={compartment.TargetTemperature:F1}°C");
                                    }
                                    break;
                                default:
                                    Console.WriteLine("No specific status information available.");
                                    break;
                            }
                        }
                    }
                    catch (OperationCanceledException)
                    {
                    }
                    return;
                }
                catch (RpcException ex) when (ex.StatusCode == StatusCode.Unavailable)
                {
                    Console.WriteLine($"Server {serverAddress} is unavailable.");
                }
                catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
                {
                    Console.WriteLine($"Device not found on server {serverAddress}.");
                }
            }

            Console.WriteLine($"Could not monitor device on any server.");
        }

        private static string GetDeviceTypeName(DeviceType type)
        {
            return type switch
            {
                DeviceType.SecurityCamera => "Security Camera",
                DeviceType.Thermostat => "Thermostat",
                DeviceType.Refrigerator => "Refrigerator",
                _ => "Unknown"
            };
        }

        private static string GetRefrigeratorModeName(RefrigeratorInfo.Types.Mode mode)
        {
            return mode switch
            {
                RefrigeratorInfo.Types.Mode.Normal => "Normal",
                RefrigeratorInfo.Types.Mode.Eco => "Eco",
                RefrigeratorInfo.Types.Mode.Quick => "Quick",
                _ => "Unknown"
            };
        }
    }
}