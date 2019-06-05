using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;

namespace BrilliantSole
{
    public class DevicesController : ApiController
    {
        /// <summary>
        /// Http GET endpoint (api/devices) to get the current state of the connected devices.
        /// This endpoint can be called at anytime after the application starts and will always
        /// provide the ConnectionManager state which can be used to determine if haptic updates can be made.
        /// </summary>
        /// <returns></returns>
        public DevicesResponse Get()
        {
            DevicesResponse response = new DevicesResponse();
            response.state = ConnectionManager.Instance.GetState().ToString();

            ConnectionManager.BrilliantSoleDevice left = ConnectionManager.Instance.GetLeftDevice();
            if (left != null)
            {
                response.leftDeviceName = left.name;
                response.leftDeviceRssi = left.rssi;
            }
            ConnectionManager.BrilliantSoleDevice right = ConnectionManager.Instance.GetRightDevice();
            if (right != null)
            {
                response.rightDeviceName = right.name;
                response.rightDeviceRssi = right.rssi;
            }

            return response;
        }


        /// <summary>
        /// Http POST endpoint (api/devices) that can update connected devices haptic motors.
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public async Task<HapticsUpdateResult> Post([FromBody] DevicesArguments args)
        {
            Console.WriteLine("Haptic Update Request Received");
            var result = new HapticsUpdateResult();
            if (ConnectionManager.Instance.GetState() != ConnectionManager.State.Connected)
            {
                Console.WriteLine("Haptic Update Can't be Performed: Devices are not connected");
                result.status = "Disconnected";
                return result;
            }
            var update = new ConnectionManager.HapticUpdate();
            // ConnectionManager works in terms of ints for eventual variable intensity, API has been simplified since that has not been implemented in hardware.
            update.leftDeviceForwardHaptic = args.leftDeviceForwardHaptic ? ConnectionManager.ENABLED : ConnectionManager.DISABLED;
            update.leftDeviceReverseHaptic = args.leftDeviceReverseHaptic ? ConnectionManager.ENABLED : ConnectionManager.DISABLED;
            update.rightDeviceForwardHaptic = args.rightDeviceForwardHaptic ? ConnectionManager.ENABLED : ConnectionManager.DISABLED;
            update.rightDeviceReverseHaptic = args.rightDeviceReverseHaptic ? ConnectionManager.ENABLED : ConnectionManager.DISABLED;

            var newState = await ConnectionManager.Instance.PerformAction(args.action, update);
            result.status = "Sucess";
            result.leftDeviceForwardHaptic = newState.leftDeviceForwardHaptic == ConnectionManager.ENABLED;
            result.leftDeviceReverseHaptic = newState.leftDeviceForwardHaptic == ConnectionManager.ENABLED;
            result.rightDeviceForwardHaptic = newState.leftDeviceForwardHaptic == ConnectionManager.ENABLED;
            result.rightDeviceReverseHaptic = newState.leftDeviceForwardHaptic == ConnectionManager.ENABLED;

            return result;
        }

        /// <summary>
        /// The results from performing a haptic update.
        /// </summary>
        public class HapticsUpdateResult
        {
            public string status; // success / disconnected
            public bool leftDeviceForwardHaptic;
            public bool leftDeviceReverseHaptic;
            public bool rightDeviceForwardHaptic;
            public bool rightDeviceReverseHaptic;
        }

        public class DevicesArguments
        {
            /// <summary>
            /// Required action to be performed using the haptic flags.
            /// - update = Update the haptic motors using the haptic flags.
            /// - vibrate = Vibrate the specified motors using the haptic flags twice and turn them off.
            /// </summary>
            public string action = "update";
            /// <summary>
            /// Whether or not the forward haptic motor on the left device should be enabled.
            /// </summary>
            public bool leftDeviceForwardHaptic;
            /// <summary>
            /// Whether or not the reverse haptic motor on the left device should be enabled.
            /// </summary>
            public bool leftDeviceReverseHaptic;
            /// <summary>
            /// Whether or not the forward haptic motor on the right device should be enabled.
            /// </summary>
            public bool rightDeviceForwardHaptic;
            /// <summary>
            /// Whether or not the reverse haptic motor on the right device should be enabled.
            /// </summary>
            public bool rightDeviceReverseHaptic;
        }

        /// <summary>
        /// Information about the current state of the found devices.
        /// Note that in the event that the devices are not found, only the state will be returned.
        /// </summary>
        public class DevicesResponse
        {
            /// <summary>
            /// The state of the connected devices.
            /// - None: ConnectionManager was not started.
            /// - Scanning: Scanning for devices, both not found yet.
            /// - Connecting: Both devices found, attempting to connect.
            /// - Connected: Both devices connected, ready for haptic updates.
            /// </summary>
            public string state;
            public string leftDeviceName;
            public short leftDeviceRssi;

            public string rightDeviceName;
            public short rightDeviceRssi;
        }

    }
}
