# nist-challenge

## Requirements

- Visual Studio +2017
- Windows 10 + Creators Edition
- .NET Framework 4.7.1
- Bluetooth Capabilities

## Setup

* Open the BrilliantSole solution in VisualStudio
    * Start the solution
    * This will start a console application with a web api running at port 9000.
* Power on the footwear.
    * Observe the console and see that it should see the left and right and attempt to connect.
* Once connected, the haptic motors will vibrate and then you will know you are ready to perform updates.
* When finished, click the exit icon of the console application and wait for it to cleanly exit so that it can leave the devices in a good state.

## Documentation

This is a WIP API created specifically for this challenge.  It is just meant to quickly facilitate interaction between Unreal and the BrilliantSole Footwear.  For this competition we will be using the UWP Bluetooth APIs sitting behind a Web API running on the local machine.

Host: localhost
Port: 9000

* Endpoint: api/devices (http://localhost:9000/api/devices)
    * Method: GET
    * Headers: None
    * Body: None
    * Response: Connection state information (DevicesResponse)

Example Response
```json
{
    "state": "Connected",
    "leftDeviceName": "BRSolesLeft-WM8",
    "leftDeviceRssi": -82,
    "rightDeviceName": "BRSolesRight-WM8",
    "rightDeviceRssi": -68
}
```

* Endpoint: api/devices (http://localhost:9000/api/devices)
    * Method: POST
    * Headers: `Content-Type: application/json`
    * Body: Haptic motor update flags and actions (DevicesArguments)
    * Response: Haptic update results (HapticsUpdateResult)

Example Body of Request to Enable All Motors
```json
{
    "action": "update",
    "leftDeviceForwardHaptic": true,
    "leftDeviceReverseHaptic": true,
    "rightDeviceForwardHaptic": true,
    "rightDeviceReverseHaptic": true
}
```

Example Body of Request to Disable All Motors
```json
{
    "action": "update",
    "leftDeviceForwardHaptic": false,
    "leftDeviceReverseHaptic": false,
    "rightDeviceForwardHaptic": false,
    "rightDeviceReverseHaptic": false
}
```

Example Body of Request to Selectively Enable Motors
```json
{
    "action": "update",
    "leftDeviceForwardHaptic": true,
    "leftDeviceReverseHaptic": false,
    "rightDeviceForwardHaptic": true,
    "rightDeviceReverseHaptic": false
}
```

Example Body of Request to Vibrate All Motors Twice and Then Turn Off
```json
{
    "action": "vibrate",
    "leftDeviceForwardHaptic": true,
    "leftDeviceReverseHaptic": true,
    "rightDeviceForwardHaptic": true,
    "rightDeviceReverseHaptic": true
}
```

Example Response
```json
{
    "status": "Sucess",
    "leftDeviceForwardHaptic": true,
    "leftDeviceReverseHaptic": true,
    "rightDeviceForwardHaptic": true,
    "rightDeviceReverseHaptic": true
}
```


## Known Issues

* Failed to Retrieve Services - Sometimes the Windows APIs for service retrieval fail to return services.  This can be worked around by repeatedly calling retrieve services, but has not been implemented in this project.  If you encounter this. Restart the application.  If the problem continues, power cycle the hardware as well.
* Failed to Find Devices - If the application can't find the devices after a reasonable period of time, it is possible that a clean exit from a previous session was not completed.  Power cycle the hardware and restart the application.
