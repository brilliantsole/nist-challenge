using System;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Storage.Streams;

namespace BrilliantSole
{
    /// <summary>
    /// The ConnectionManager is a singleton that is responsible for wrapping
    /// the UWP Bluetooth API to interface with the BrilliantSole hardware.
    /// </summary>
    class ConnectionManager
    {
        /// <summary>
        /// The watcher is the equivalent of a Bluetooth Adapter in most BLE frameworks.
        /// </summary>
        private BluetoothLEAdvertisementWatcher watcher;
        /// <summary>
        /// A simple object that represents a collection of information about the left device.
        /// </summary>
        private BrilliantSoleDevice left;
        /// <summary>
        /// A simple object that represents a collection of information about the right device.
        /// </summary>
        private BrilliantSoleDevice right;
        /// <summary>
        /// The current state of the manager.
        /// </summary>
        private State state = State.None;

        public ConnectionManager()
        {
            this.watcher = new BluetoothLEAdvertisementWatcher();
            this.watcher.ScanningMode = BluetoothLEScanningMode.Active;
            // Only activate the watcher when we're recieving values >= -80
            this.watcher.SignalStrengthFilter.InRangeThresholdInDBm = -127;
            // Stop watching if the value drops below -90 (user walked away)
            this.watcher.SignalStrengthFilter.OutOfRangeThresholdInDBm = -127;
            // Wait 5 seconds to make sure the device is really out of range
            this.watcher.SignalStrengthFilter.OutOfRangeTimeout = TimeSpan.FromMilliseconds(5000);
            this.watcher.SignalStrengthFilter.SamplingInterval = TimeSpan.FromMilliseconds(2000);
            // Bind an internal event handler for device advertisements
            this.watcher.Received += this.InternalAdvertisementHandler;
        }

        /// <summary>
        /// Get the current state of the connection manager.
        /// </summary>
        /// <returns></returns>
        public State GetState()
        {
            return this.state;
        }

        /// <summary>
        /// Get a reference to the left device.
        /// </summary>
        /// <returns></returns>
        public BrilliantSoleDevice GetLeftDevice()
        {
            return this.left;
        }

        /// <summary>
        /// Get a reference to the right device.
        /// </summary>
        /// <returns></returns>
        public BrilliantSoleDevice GetRightDevice()
        {
            return this.right;
        }

        /// <summary>
        /// Attempts to start the scanning process. If the adapter is unable to start immediately, it will return
        /// the aborted status.
        /// </summary>
        /// <returns></returns>
        public BluetoothLEAdvertisementWatcherStatus Start()
        {
            Console.WriteLine("Starting bluetooth scanning");
            this.watcher.Start();
            this.state = State.Scanning;
            return this.watcher.Status;
        }

        /// <summary>
        /// Update the left device's haptic values.
        /// </summary>
        /// <param name="forward">The forward value to set.</param>
        /// <param name="reverse">The reverse value to set.</param>
        /// <returns></returns>
        public async Task<BrilliantSoleDevice> UpdateLeftDeviceHaptics(UInt16 forward, UInt16 reverse)
        {
            if (this.state != State.Connected) return this.left;

            await this.UpdateDeviceHaptics(this.left, forward, reverse);
            return this.left;
        }

        /// <summary>
        /// Update the left device's haptic values.
        /// </summary>
        /// <param name="forward">The forward value to set.</param>
        /// <param name="reverse">The reverse value to set.</param>
        /// <returns></returns>
        public async Task<BrilliantSoleDevice> UpdateRightDeviceHaptics(UInt16 forward, UInt16 reverse)
        {
            if (this.state != State.Connected) return this.right;
            await this.UpdateDeviceHaptics(this.right, forward, reverse);
            return this.right;
        }

        /// <summary>
        /// Update a device's haptic values.
        /// </summary>
        /// <param name="device">The device to update.</param>
        /// <param name="forward">The forward value to set.</param>
        /// <param name="reverse">The reverse value to set.</param>
        /// <returns></returns>
        private async Task<BrilliantSoleDevice> UpdateDeviceHaptics(BrilliantSoleDevice device, UInt16 forward, UInt16 reverse)
        {
            if (this.state != State.Connected) return null;
            Console.WriteLine("Attempting to update device {0} with forward value of {1} and reverse value of {2}", device.name, forward, reverse);

            try
            {
                var forwardWriter = new DataWriter();
                forwardWriter.WriteUInt16(forward);

                var reverseWriter = new DataWriter();
                reverseWriter.WriteUInt16(reverse);

                var forwardResult = await device.forward.WriteValueWithResultAsync(forwardWriter.DetachBuffer());
                Console.WriteLine(String.Format("Wrote value {0} to {1} forward haptic with status {2}", forward, device.name, forwardResult.Status));

                var reverseResult = await device.reverse.WriteValueWithResultAsync(reverseWriter.DetachBuffer());
                Console.WriteLine(String.Format("Wrote value {0} to {1} reverse haptic with status {2}", reverse, device.name, reverseResult.Status));

            } catch(Exception err)
            {
                Console.WriteLine("Invalid value exception, unable to write values.");
                Console.WriteLine(err);
            }
            return device;
        }

        /// <summary>
        /// Turn of all haptic motors.
        /// </summary>
        /// <returns></returns>
        public async Task<ConnectionManager> TurnOffHaptics()
        {
            await this.UpdateLeftDeviceHaptics(DISABLED, DISABLED);
            await this.UpdateRightDeviceHaptics(DISABLED, DISABLED);
            return this;
        }

        /// <summary>
        /// Turn on all haptic motors.
        /// </summary>
        /// <returns></returns>
        public async Task<ConnectionManager> TurnOnHaptics()
        {
            Console.WriteLine("Attempting to turn on both haptics");
            await this.UpdateLeftDeviceHaptics(ENABLED, ENABLED);
            await this.UpdateRightDeviceHaptics(ENABLED, ENABLED);
            return this;
        }

        /// <summary>
        /// Disconnect from both footwear devices and dispose of their resources
        /// so that they can be reconnected to.
        /// </summary>
        /// <returns></returns>
        public async Task<ConnectionManager> Disconnect()
        {
            Console.WriteLine("Attempting to disconnect from devices.");
            if (this.state == State.Connected)
            {
                Console.WriteLine("Disabling Haptics");
                await this.TurnOffHaptics();
            }
            Console.WriteLine("Disconnecting from Left Device");
            this.left?.haptics?.Dispose();
            this.left?.device?.Dispose();
            Console.WriteLine("Disconnecting from Right Device");
            this.right?.haptics?.Dispose();
            this.right?.device?.Dispose();
            return this;
        }

        /// <summary>
        /// Attempt to connect to both footwear devices.
        /// </summary>
        /// <returns></returns>
        private async Task<ConnectionManager> ConnectAsync()
        {
            this.state = State.Connecting;
            Console.WriteLine("Attempting to connect to both devices.");
            await this.ConnectToDeviceAsync(this.left);
            await this.ConnectToDeviceAsync(this.right);
            Console.WriteLine("Succesfully connected to both devices.");

            Console.WriteLine("Attempting to retrieve services from both devices.");
            await this.RetrieveServicesAsync(this.left);
            await this.RetrieveServicesAsync(this.right);
            Console.WriteLine("Succesfully retrieved services from both devices.");

            Console.WriteLine("Attempting to retrieve characterstics from both devices.");
            await this.RetrieveCharacteristicsAsync(this.left);
            await this.RetrieveCharacteristicsAsync(this.right);
            Console.WriteLine("Succesfully retrieved characteristics from both devices.");

            Console.WriteLine("Successfully Established Connection to Both Devices. Testing Haptic Motor Vibration...");
            this.state = State.Connected;
            HapticUpdate update = new HapticUpdate();
            update.leftDeviceForwardHaptic = ENABLED;
            update.leftDeviceReverseHaptic = ENABLED;
            update.rightDeviceForwardHaptic = ENABLED;
            update.rightDeviceReverseHaptic = ENABLED;
            await this.PerformAction("vibrate", update);
            Console.WriteLine("Sucessfully Tested Haptic Motors, Devices are Ready!");
            return this;
        }

        /// <summary>
        /// Perform a haptic action.
        /// </summary>
        /// <param name="action"></param>
        /// <param name="update"></param>
        /// <returns></returns>
        public async Task<HapticUpdate> PerformAction(string action, HapticUpdate update)
        {
            HapticUpdate result = new HapticUpdate();
            switch (action.ToLower())
            {
                case "vibrate":
                    await this.UpdateLeftDeviceHaptics(update.leftDeviceForwardHaptic, update.leftDeviceReverseHaptic);
                    await this.UpdateRightDeviceHaptics(update.rightDeviceForwardHaptic, update.rightDeviceForwardHaptic);
                    await Task.Delay(250);
                    await this.TurnOffHaptics();
                    await Task.Delay(100);
                    await this.UpdateLeftDeviceHaptics(update.leftDeviceForwardHaptic, update.leftDeviceReverseHaptic);
                    await this.UpdateRightDeviceHaptics(update.rightDeviceForwardHaptic, update.rightDeviceForwardHaptic);
                    await Task.Delay(250);
                    await this.TurnOffHaptics();
                    return result;
                case "update":
                default:
                    await this.UpdateLeftDeviceHaptics(update.leftDeviceForwardHaptic, update.leftDeviceReverseHaptic);
                    await this.UpdateRightDeviceHaptics(update.rightDeviceForwardHaptic, update.rightDeviceForwardHaptic);
                    return update;
            }
        }

        private async Task<BluetoothLEDevice> ConnectToDeviceAsync(BrilliantSoleDevice sole)
        {
            Console.WriteLine("Attempting to connect to " + sole.name);
            sole.device = await BluetoothLEDevice.FromBluetoothAddressAsync(sole.address);
            if (sole.device == null)
            {
                Console.WriteLine("Failed to connect to " + sole.name);
                throw new Exception("Failed to connect."); // TODO - Recover
            }
            Console.WriteLine("Successfully connected to " + sole.name);
            return sole.device;
        }

        private async Task<GattDeviceServicesResult> RetrieveServicesAsync(BrilliantSoleDevice sole)
        {
            Console.WriteLine("Attempting to retrieve haptic service from " + sole.name);
            var result = await sole.device.GetGattServicesForUuidAsync(HAPTIC_SERVICE);

            if (result.Services.Count == 0)
            {
                Console.WriteLine("Failed to retrieve services for " + sole.name);
                Console.WriteLine("Status Code: " + result.Status);
                throw new Exception("Failed to retrieve services."); // TODO - Recover
            }

            sole.haptics = result.Services[0];

            Console.WriteLine("Successfully to retrieved haptic service from " + sole.name);
            return result;
        }

        private async Task<BrilliantSoleDevice> RetrieveCharacteristicsAsync(BrilliantSoleDevice sole)
        {
            Console.WriteLine("Attempting to retrieve forward haptic characteristic for " + sole.name);
            var forward = await sole.haptics.GetCharacteristicsForUuidAsync(FORWARD_HAPTIC_CHARACTERISTIC);
            if (forward.Characteristics.Count == 0)
            {
                Console.WriteLine("Failed to retrieve characteristics for " + sole.name);
                Console.WriteLine("Status Code: " + forward.Status);
                throw new Exception("Failed to retrieve characteristics."); // TODO - Recover
            }
            sole.forward = forward.Characteristics[0];

            Console.WriteLine("Attempting to retrieve reverse haptic characteristic for " + sole.name);
            var reverse = await sole.haptics.GetCharacteristicsForUuidAsync(REVERSE_HAPTIC_CHARACTERISTIC);
            if (reverse.Characteristics.Count == 0)
            {
                Console.WriteLine("Failed to retrieve characteristics for " + sole.name);
                Console.WriteLine("Status Code: " + forward.Status);
                throw new Exception("Failed to retrieve characteristics."); // TODO - Recover
            }
            sole.reverse = reverse.Characteristics[0];

            Console.WriteLine("Successfully to retrieved haptic characteristics from " + sole.name);
            return sole;
        }

        private void InternalAdvertisementHandler(BluetoothLEAdvertisementWatcher sender, BluetoothLEAdvertisementReceivedEventArgs args)
        {
            Console.WriteLine("Advertisement Received For: " + args.Advertisement.LocalName);
            if (args.Advertisement.LocalName.ToLower().StartsWith("brsolesleft"))
            {
                Console.WriteLine(String.Format("Found {0} with RSSI {1}", args.Advertisement.LocalName, args.RawSignalStrengthInDBm));
                this.left = new BrilliantSoleDevice(args.Advertisement.LocalName, args.BluetoothAddress, args.RawSignalStrengthInDBm);
            }
            if (args.Advertisement.LocalName.ToLower().StartsWith("brsolesright"))
            {
                Console.WriteLine(String.Format("Found {0} with RSSI {1}", args.Advertisement.LocalName, args.RawSignalStrengthInDBm));
                this.right = new BrilliantSoleDevice(args.Advertisement.LocalName, args.BluetoothAddress, args.RawSignalStrengthInDBm);
            }
            // If we have found both devices, and are not already connecting.
            if (this.left != null && this.right != null && this.state != State.Connecting)
            {
                Console.WriteLine(
                    String.Format(
                        "Left Device ({0}) and Right Device ({1}) have been found, stopping scanning and attempting to connect.",
                        this.left.name,
                        this.right.name
                    )
                );
                this.watcher.Stop();
                this.ConnectAsync();
            }
        }

        public enum State
        {
            None,
            Scanning,
            Connecting,
            Connected
        }

        public class HapticUpdate
        {
            public UInt16 leftDeviceForwardHaptic;
            public UInt16 leftDeviceReverseHaptic;
            public UInt16 rightDeviceForwardHaptic;
            public UInt16 rightDeviceReverseHaptic;
        }

        public class BrilliantSoleDevice
        {
            public string name;
            public ulong address;
            public short rssi;
            public BluetoothLEDevice device;
            public GattDeviceService haptics;
            public GattCharacteristic forward;
            public GattCharacteristic reverse;

            public BrilliantSoleDevice(string name, ulong address, short rssi)
            {
                this.name = name;
                this.address = address;
                this.rssi = rssi;
            }
        }

        private static readonly Guid HAPTIC_SERVICE = new Guid("00001101-0000-1000-8000-00805F9B34FD");
        private static readonly Guid FORWARD_HAPTIC_CHARACTERISTIC = new Guid("6FE511C7-5C39-4390-871D-A6136E71AB12");
        private static readonly Guid REVERSE_HAPTIC_CHARACTERISTIC = new Guid("6FE511C7-5C39-4390-871D-A6136E71AB13");

        private static readonly Lazy<ConnectionManager> instance = new Lazy<ConnectionManager>(() => new ConnectionManager());
        public static ConnectionManager Instance { get { return instance.Value; } }
        public static readonly UInt16 ENABLED = 0xFFFF;
        public static readonly UInt16 DISABLED = 0x0000;
    }
}
