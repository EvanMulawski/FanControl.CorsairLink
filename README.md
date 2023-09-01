# CorsairLink plugin for Fan Control

The unofficial CorsairLink plugin for [Fan Control](https://github.com/Rem0o/FanControl.Releases).

[![Support](https://img.shields.io/badge/Support-Venmo-blue?style=for-the-badge&logo=venmo&color=3D95CE)](https://www.venmo.com/u/EvanMulawski)
[![Support](https://img.shields.io/badge/Support-Buy_Me_A_Coffee-yellow?style=for-the-badge&logo=buy%20me%20a%20coffee&color=FFDD00)](https://www.buymeacoffee.com/evanmulawski)

## Device Support

| Device                          | Type       | PID        | Status                               | Read Fan/Pump RPM | Set Fan/Pump Power | Read Temp Sensor |
| ------------------------------- | ---------- | ---------- | ------------------------------------ | ----------------- | ------------------ | ---------------- |
| Commander PRO                   | Controller | `0c10`     | Full Support <sup>1</sup>            | ✅                | ✅                 | ✅               |
| Commander PRO (Obsidian 1000D)  | Controller | `1d00`     | Full Support <sup>1</sup>            | ✅                | ✅                 | ✅               |
| Commander CORE XT               | Controller | `0c2a`     | Full Support <sup>1,2</sup>          | ✅                | ✅                 | ✅               |
| Commander CORE (ELITE CAPELLIX) | Controller | `0c1c`     | Full Support <sup>1,2</sup>          | ✅                | ✅                 | ✅               |
| Commander CORE                  | Controller | `0c32`     | Full Support <sup>1,2</sup>          | ✅                | ✅                 | ✅               |
| Commander Mini                  | Controller | `0c04(3d)` | Full Support <sup>1</sup>            | ✅                | ✅                 | ✅               |
| Hydro H60i Elite                | AIO        | `0c34`     | Full Support <sup>1</sup>            | ✅                | ✅ <sup>4</sup>    | ✅ <sup>5</sup>  |
| Hydro H60i Pro XT               | AIO        | `0c29`     | Full Support <sup>1</sup>            | ✅                | ✅ <sup>4</sup>    | ✅ <sup>5</sup>  |
| Hydro H60i Pro XT               | AIO        | `0c30`     | Full Support <sup>1</sup>            | ✅                | ✅ <sup>4</sup>    | ✅ <sup>5</sup>  |
| Hydro H100i Elite               | AIO        | `0c35`     | Full Support <sup>1</sup>            | ✅                | ✅ <sup>4</sup>    | ✅ <sup>5</sup>  |
| Hydro H100i Platinum            | AIO        | `0c18`     | Full Support <sup>1</sup>            | ✅                | ✅ <sup>4</sup>    | ✅ <sup>5</sup>  |
| Hydro H100i Platinum SE         | AIO        | `0c19`     | Full Support <sup>1</sup>            | ✅                | ✅ <sup>4</sup>    | ✅ <sup>5</sup>  |
| Hydro H100i Pro XT              | AIO        | `0c20`     | Full Support <sup>1</sup>            | ✅                | ✅ <sup>4</sup>    | ✅ <sup>5</sup>  |
| Hydro H100i Pro XT              | AIO        | `0c2d`     | Full Support <sup>1</sup>            | ✅                | ✅ <sup>4</sup>    | ✅ <sup>5</sup>  |
| Hydro H115i Elite               | AIO        | `0c36`     | Full Support <sup>1</sup>            | ✅                | ✅ <sup>4</sup>    | ✅ <sup>5</sup>  |
| Hydro H115i Platinum            | AIO        | `0c17`     | Full Support <sup>1</sup>            | ✅                | ✅ <sup>4</sup>    | ✅ <sup>5</sup>  |
| Hydro H115i Pro XT              | AIO        | `0c21`     | Full Support <sup>1</sup>            | ✅                | ✅ <sup>4</sup>    | ✅ <sup>5</sup>  |
| Hydro H115i Pro XT              | AIO        | `0c2e`     | Full Support <sup>1</sup>            | ✅                | ✅ <sup>4</sup>    | ✅ <sup>5</sup>  |
| Hydro H150i Elite               | AIO        | `0c37`     | Full Support <sup>1</sup>            | ✅                | ✅ <sup>4</sup>    | ✅ <sup>5</sup>  |
| Hydro H150i Pro XT              | AIO        | `0c22`     | Full Support <sup>1</sup>            | ✅                | ✅ <sup>4</sup>    | ✅ <sup>5</sup>  |
| Hydro H150i Pro XT              | AIO        | `0c2f`     | Full Support <sup>1</sup>            | ✅                | ✅ <sup>4</sup>    | ✅ <sup>5</sup>  |
| Hydro H80i                      | AIO        | `0c04(3b)` | Full Support <sup>1</sup>            | ✅                | ✅                 | ✅ <sup>5</sup>  |
| Hydro H100i                     | AIO        | `0c04(3c)` | Full Support <sup>1</sup>            | ✅                | ✅                 | ✅ <sup>5</sup>  |
| Hydro H100i GT                  | AIO        | `0c04(40)` | Full Support <sup>1</sup>            | ✅                | ✅                 | ✅ <sup>5</sup>  |
| Hydro H110i                     | AIO        | `0c04(42)` | Full Support <sup>1</sup>            | ✅                | ✅                 | ✅ <sup>5</sup>  |
| Hydro H110i GT                  | AIO        | `0c04(41)` | Full Support <sup>1</sup>            | ✅                | ✅                 | ✅ <sup>5</sup>  |
| HX550i                          | PSU        | `1c03`     | Full Support <sup>7</sup>            | ✅                | ✅ <sup>6</sup>    | ✅               |
| HX650i                          | PSU        | `1c04`     | Full Support <sup>7</sup>            | ✅                | ✅ <sup>6</sup>    | ✅               |
| HX750i                          | PSU        | `1c05`     | Full Support <sup>7</sup>            | ✅                | ✅ <sup>6</sup>    | ✅               |
| HX850i                          | PSU        | `1c06`     | Full Support <sup>7</sup>            | ✅                | ✅ <sup>6</sup>    | ✅               |
| HX1000i                         | PSU        | `1c07`     | Full Support <sup>7</sup>            | ✅                | ✅ <sup>6</sup>    | ✅               |
| HX1200i                         | PSU        | `1c08`     | Full Support <sup>7</sup>            | ✅                | ✅ <sup>6</sup>    | ✅               |
| HX1000i (2021)                  | PSU        | `1c1e`     | Full Support <sup>7</sup>            | ✅                | ✅ <sup>6</sup>    | ✅               |
| HX1500i (2021)                  | PSU        | `1c1f`     | Full Support <sup>7</sup>            | ✅                | ✅ <sup>6</sup>    | ✅               |
| RM550i                          | PSU        | `1c09`     | Full Support <sup>7</sup>            | ✅                | ✅ <sup>6</sup>    | ✅               |
| RM650i                          | PSU        | `1c0a`     | Full Support <sup>7</sup>            | ✅                | ✅ <sup>6</sup>    | ✅               |
| RM750i                          | PSU        | `1c0b`     | Full Support <sup>7</sup>            | ✅                | ✅ <sup>6</sup>    | ✅               |
| RM850i                          | PSU        | `1c0c`     | Full Support <sup>7</sup>            | ✅                | ✅ <sup>6</sup>    | ✅               |
| RM1000i                         | PSU        | `1c0d`     | Full Support <sup>7</sup>            | ✅                | ✅ <sup>6</sup>    | ✅               |
| AX850i                          | PSU        | `1c0e`     | Full Support <sup>8</sup>            | ✅                | ✅ <sup>6</sup>    | ✅               |
| AX1000i                         | PSU        | `1c0f`     | Full Support <sup>8</sup>            | ✅                | ✅ <sup>6</sup>    | ✅               |
| AX1300i                         | PSU        | `1c10`     | Full Support <sup>8</sup>            | ✅                | ✅ <sup>6</sup>    | ✅               |
| AX1500i                         | PSU        | `1c02`     | Full Support <sup>8</sup>            | ✅                | ✅ <sup>6</sup>    | ✅               |
| AX1600i                         | PSU        | `1c11`     | Full Support <sup>8</sup>            | ✅                | ✅ <sup>6</sup>    | ✅               |
| AX760i/AX860i/AX1200i           | PSU        | `1c00`     | Full Support <sup>8</sup>            | ✅                | ✅ <sup>6</sup>    | ✅               |
| Hydro H80i GT                   | AIO        | `0c02`     | 1.4.x Pre-release <sup>1,3,8</sup>   | ✅                | ✅                 | ✅ <sup>5</sup>  |
| Hydro H80i GT V2                | AIO        | `0c08`     | 1.4.x Pre-release <sup>1,3,8</sup>   | ✅                | ✅                 | ✅ <sup>5</sup>  |
| Hydro H80i Pro                  | AIO        | `0c16`     | 1.4.x Pre-release <sup>1,3,8,9</sup> | ✅                | ✅                 | ✅ <sup>5</sup>  |
| Hydro H100i GT V2               | AIO        | `0c09`     | 1.4.x Pre-release <sup>1,3,8</sup>   | ✅                | ✅                 | ✅ <sup>5</sup>  |
| Hydro H100i GTX                 | AIO        | `0c03`     | 1.4.x Pre-release <sup>1,3,8</sup>   | ✅                | ✅                 | ✅ <sup>5</sup>  |
| Hydro H100i Pro                 | AIO        | `0c15`     | 1.4.x Pre-release <sup>1,3,8,9</sup> | ✅                | ✅                 | ✅ <sup>5</sup>  |
| Hydro H110i GT V2               | AIO        | `0c0a`     | 1.4.x Pre-release <sup>1,3,8</sup>   | ✅                | ✅                 | ✅ <sup>5</sup>  |
| Hydro H110i GTX                 | AIO        | `0c07`     | 1.4.x Pre-release <sup>1,3,8</sup>   | ✅                | ✅                 | ✅ <sup>5</sup>  |
| Hydro H115i Pro                 | AIO        | `0c13`     | 1.4.x Pre-release <sup>1,3,8,9</sup> | ✅                | ✅                 | ✅ <sup>5</sup>  |
| Hydro H150i Pro                 | AIO        | `0c12`     | 1.4.x Pre-release <sup>1,3,8,9</sup> | ✅                | ✅                 | ✅ <sup>5</sup>  |
| Cooling Node                    | Controller | `0c04(38)` | Support Upon Request                 |                   |                    |                  |
| Hydro H80                       | AIO        | `0c04(37)` | Support Upon Request                 |                   |                    |                  |
| Hydro H100                      | AIO        | `0c04(3a)` | Support Upon Request                 |                   |                    |                  |

1. Software mode only. Device lighting will be software-based.

2. The speed ramping in the Commander CORE and CORE XT is too slow for automatic sensor pairing and start/stop detection to function in Fan Control. Therefore, assisted setup will fail. Each control will need to be paired with its corresponding sensor manually and its start/stop values will need to be set manually.

3. Support is currently experimental and only available in a pre-release (see [Releases](https://github.com/EvanMulawski/FanControl.CorsairLink/releases)). Please provide your feedback in issues and discussions.

4. Variable pump speed using the device's Quiet, Balanced, and Performance presets based on the control's percent value:

   | Control % | Pump Mode   |
   | --------- | ----------- |
   | 0-33%     | Quiet       |
   | 34-67%    | Balanced    |
   | 68-100%   | Performance |

5. Reads the liquid temperature.

6. When the fan power is set to 0%, control of the fan will be returned to the PSU allowing zero-RPM operation. When the fan power is set to 1% or higher, control of the fan will be returned to Fan Control. The minimum duty depends on the model:

   | PSU Model | Min. Duty |
   | --------- | --------- |
   | HXi/RMi   | 30%       |
   | AXi       | 15%       |

7. The LibreHardwareMonitor "PSU (Corsair)" sensor source must be disabled in Fan Control's Sensor Settings.

8. Requires driver installation and Windows 10/11 64-bit OS. See [instructions](#siusbxpress-driver) (also included in download).

9. The default safety profile stored on the device may be overridden by setting the `FANCONTROL_CORSAIRLINK_HYDRO_ASETEK_PRO_SAFETY_PROFILE_OVERRIDE_ENABLED` environment variable to `1` and restarting Fan Control. This will write a new safety profile to the device:

   |            | Temp | Fan Duty |
   | ---------- | ---- | -------- |
   | Activate   | 58°C | 100%     |
   | Deactivate | 57°C | -        |

## Installation

⚠ This plugin will not function correctly if Corsair iCUE (specifically, the "Corsair Service" service) is running. This service should be stopped before running Fan Control. Running [incompatible programs](#compatibility) that attempt to communicate with these devices while Fan Control is running is not a supported scenario.

⚠ This plugin requires the .NET Framework build of Fan Control. Install Fan Control using the `FanControl_net_4_8.zip` release files.

1. Download a [release](https://github.com/EvanMulawski/FanControl.CorsairLink/releases).
2. Unblock the downloaded ZIP file. (Right-click, Properties, check Unblock, OK)
3. Exit Fan Control.
4. Copy `FanControl.CorsairLink.dll` to Fan Control's `Plugins` directory.
5. Start Fan Control.

### SiUsbXpress Driver

The SiUsbXpress driver is only necessary if:

- the Status of your device in the [Device Support](#device-support) matrix has the **8** footnote superscript, e.g. Full Support <sup>8</sup>
- the driver has not been installed previously (by Corsair iCUE or manually)

The driver is only supported on:

- Windows 10 64-bit
- Windows 11 64-bit

To install the driver:

1. Ensure Fan Control is closed.
2. Ensure `FanControl.CorsairLink.dll` has been copied to Fan Control's `Plugins` directory.
3. Copy `SiUSBXp.dll` from the `Corsair-SiUsbXpress-Driver` directory to Fan Control's `Plugins` directory.
4. Right-click the `CorsairSiUSBXp.inf` file in the `Corsair-SiUsbXpress-Driver` directory and select **Install**. (This is a signed driver shipped with Corsair iCUE.)
5. Start Fan Control.

## Interoperability

This plugin implements a standard global mutex (`Global\CorsairLinkReadWriteGuardMutex`) to synchronize device communication.

### Compatibility

| Application  | Compatibility                                                                                                                         | Notes                           |
| ------------ | ------------------------------------------------------------------------------------------------------------------------------------- | ------------------------------- |
| HWiNFO       | ✅ 5.34+                                                                                                                              |                                 |
| SIV          | ✅ 5.17+                                                                                                                              |                                 |
| SignalRGB    | ⚠️ 2.2.29+                                                                                                                            | Commander PRO/CORE/CORE XT only |
| OpenRGB      | ⚠️ [6c8da3a0](https://gitlab.com/CalcProgrammer1/OpenRGB/-/commit/6c8da3a04f14490f8d944dfa9471386c697681cc)                           | Commander CORE only             |
| Corsair iCUE | ❌ ([more info](https://forum.corsair.com/forums/topic/138062-corsair-link-doesnt-work-with-hwinfo64/?do=findComment&comment=824447)) |                                 |

Note: Any third-party software that properly implements the standard mutex for your devices will likely be compatible.
