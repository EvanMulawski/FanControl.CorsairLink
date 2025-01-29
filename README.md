# CorsairLink plugin for Fan Control

The unofficial CorsairLink plugin for [Fan Control](https://github.com/Rem0o/FanControl.Releases).

[![Support](https://img.shields.io/badge/Support-Venmo-blue?style=for-the-badge&logo=venmo&color=3D95CE)](https://www.venmo.com/u/EvanMulawski)
[![Support](https://img.shields.io/badge/Support-Buy_Me_A_Coffee-yellow?style=for-the-badge&logo=buy%20me%20a%20coffee&color=FFDD00)](https://www.buymeacoffee.com/evanmulawski)

> [!IMPORTANT]
> This project is under active development, with frequent Beta releases that address issues and incorporate user feedback. These releases may include device-specific fixes and enhancements. Review the [release notes for each version](https://github.com/EvanMulawski/FanControl.CorsairLink/releases) before selecting one to use and regularly check back for updates.

## Device Support

| Device                         | PID        | Implementation | Status                          | Read Fan/Pump RPM                 | Set Fan/Pump Power                | Read Temp Sensor                  |
| ------------------------------ | ---------- | -------------- | ------------------------------- | --------------------------------- | --------------------------------- | --------------------------------- |
| iCUE LINK Hub                  | `0c3f`     | ICueLink       | Full Support <sup>1</sup>       | [See devices](#icue-link-support) | [See devices](#icue-link-support) | [See devices](#icue-link-support) |
| Commander PRO                  | `0c10`     | CommanderPro   | Full Support <sup>1</sup>       | ✅                                | ✅                                | ✅                                |
| Commander PRO (Obsidian 1000D) | `1d00`     | CommanderPro   | Full Support <sup>1</sup>       | ✅                                | ✅                                | ✅                                |
| Commander CORE XT              | `0c2a`     | CommanderCore  | Full Support <sup>1,2</sup>     | ✅                                | ✅                                | ✅                                |
| Commander CORE                 | `0c1c`     | CommanderCore  | Full Support <sup>1,2</sup>     | ✅                                | ✅ <sup>10</sup>                  | ✅                                |
| Commander CORE (2022)          | `0c32`     | CommanderCore  | Full Support <sup>1,2</sup>     | ✅                                | ✅ <sup>10</sup>                  | ✅                                |
| Commander Mini                 | `0c04(3d)` | Coolit         | Full Support <sup>1</sup>       | ✅                                | ✅                                | ✅                                |
| Hydro H60i Elite               | `0c34`     | HydroPlatinum  | Full Support <sup>1</sup>       | ✅                                | ✅ <sup>4</sup>                   | ✅ <sup>5</sup>                   |
| Hydro H60i Pro XT              | `0c29`     | HydroPlatinum  | Full Support <sup>1</sup>       | ✅                                | ✅ <sup>4</sup>                   | ✅ <sup>5</sup>                   |
| Hydro H60i Pro XT              | `0c30`     | HydroPlatinum  | Full Support <sup>1</sup>       | ✅                                | ✅ <sup>4</sup>                   | ✅ <sup>5</sup>                   |
| Hydro H100i Elite (Black)      | `0c35`     | HydroPlatinum  | Full Support <sup>1</sup>       | ✅                                | ✅ <sup>4</sup>                   | ✅ <sup>5</sup>                   |
| Hydro H100i Elite (White)      | `0c40`     | HydroPlatinum  | Full Support <sup>1</sup>       | ✅                                | ✅ <sup>4</sup>                   | ✅ <sup>5</sup>                   |
| Hydro H100i Platinum           | `0c18`     | HydroPlatinum  | Full Support <sup>1</sup>       | ✅                                | ✅ <sup>4</sup>                   | ✅ <sup>5</sup>                   |
| Hydro H100i Platinum SE        | `0c19`     | HydroPlatinum  | Full Support <sup>1</sup>       | ✅                                | ✅ <sup>4</sup>                   | ✅ <sup>5</sup>                   |
| Hydro H100i Pro XT             | `0c20`     | HydroPlatinum  | Full Support <sup>1</sup>       | ✅                                | ✅ <sup>4</sup>                   | ✅ <sup>5</sup>                   |
| Hydro H100i Pro XT             | `0c2d`     | HydroPlatinum  | Full Support <sup>1</sup>       | ✅                                | ✅ <sup>4</sup>                   | ✅ <sup>5</sup>                   |
| Hydro H115i Elite              | `0c36`     | HydroPlatinum  | Full Support <sup>1</sup>       | ✅                                | ✅ <sup>4</sup>                   | ✅ <sup>5</sup>                   |
| Hydro H115i Platinum           | `0c17`     | HydroPlatinum  | Full Support <sup>1</sup>       | ✅                                | ✅ <sup>4</sup>                   | ✅ <sup>5</sup>                   |
| Hydro H115i Pro XT             | `0c21`     | HydroPlatinum  | Full Support <sup>1</sup>       | ✅                                | ✅ <sup>4</sup>                   | ✅ <sup>5</sup>                   |
| Hydro H115i Pro XT             | `0c2e`     | HydroPlatinum  | Full Support <sup>1</sup>       | ✅                                | ✅ <sup>4</sup>                   | ✅ <sup>5</sup>                   |
| Hydro H150i Elite (Black)      | `0c37`     | HydroPlatinum  | Full Support <sup>1</sup>       | ✅                                | ✅ <sup>4</sup>                   | ✅ <sup>5</sup>                   |
| Hydro H150i Elite (White)      | `0c41`     | HydroPlatinum  | Full Support <sup>1</sup>       | ✅                                | ✅ <sup>4</sup>                   | ✅ <sup>5</sup>                   |
| Hydro H150i Pro XT             | `0c22`     | HydroPlatinum  | Full Support <sup>1</sup>       | ✅                                | ✅ <sup>4</sup>                   | ✅ <sup>5</sup>                   |
| Hydro H150i Pro XT             | `0c2f`     | HydroPlatinum  | Full Support <sup>1</sup>       | ✅                                | ✅ <sup>4</sup>                   | ✅ <sup>5</sup>                   |
| One                            | `0c14`     | HydroPlatinum  | Partial Support <sup>1,11</sup> | ✅                                | ⚠️ <sup>4,11</sup>                | ✅ <sup>5</sup>                   |
| Hydro H80i                     | `0c04(3b)` | Coolit         | Full Support <sup>1</sup>       | ✅                                | ✅                                | ✅ <sup>5</sup>                   |
| Hydro H100i                    | `0c04(3c)` | Coolit         | Full Support <sup>1</sup>       | ✅                                | ✅                                | ✅ <sup>5</sup>                   |
| Hydro H100i GT                 | `0c04(40)` | Coolit         | Full Support <sup>1</sup>       | ✅                                | ✅                                | ✅ <sup>5</sup>                   |
| Hydro H110i                    | `0c04(42)` | Coolit         | Full Support <sup>1</sup>       | ✅                                | ✅                                | ✅ <sup>5</sup>                   |
| Hydro H110i GT                 | `0c04(41)` | Coolit         | Full Support <sup>1</sup>       | ✅                                | ✅                                | ✅ <sup>5</sup>                   |
| Hydro H80i GT                  | `0c02`     | HydroAsetek    | Full Support <sup>1,8</sup>     | ✅                                | ✅                                | ✅ <sup>5</sup>                   |
| Hydro H80i GT V2               | `0c08`     | HydroAsetek    | Full Support <sup>1,8</sup>     | ✅                                | ✅                                | ✅ <sup>5</sup>                   |
| Hydro H80i Pro                 | `0c16`     | HydroAsetekPro | Full Support <sup>1,8,9</sup>   | ✅                                | ✅                                | ✅ <sup>5</sup>                   |
| Hydro H100i GT V2              | `0c09`     | HydroAsetek    | Full Support <sup>1,8</sup>     | ✅                                | ✅                                | ✅ <sup>5</sup>                   |
| Hydro H100i GTX                | `0c03`     | HydroAsetek    | Full Support <sup>1,8</sup>     | ✅                                | ✅                                | ✅ <sup>5</sup>                   |
| Hydro H100i Pro                | `0c15`     | HydroAsetekPro | Full Support <sup>1,8,9</sup>   | ✅                                | ✅                                | ✅ <sup>5</sup>                   |
| Hydro H110i GT V2              | `0c0a`     | HydroAsetek    | Full Support <sup>1,8</sup>     | ✅                                | ✅                                | ✅ <sup>5</sup>                   |
| Hydro H110i GTX                | `0c07`     | HydroAsetek    | Full Support <sup>1,8</sup>     | ✅                                | ✅                                | ✅ <sup>5</sup>                   |
| Hydro H115i Pro                | `0c13`     | HydroAsetekPro | Full Support <sup>1,8,9</sup>   | ✅                                | ✅                                | ✅ <sup>5</sup>                   |
| Hydro H150i Pro                | `0c12`     | HydroAsetekPro | Full Support <sup>1,8,9</sup>   | ✅                                | ✅                                | ✅ <sup>5</sup>                   |
| XC7 LCD Water Block            | `0c42`     | HidCooling     | Full Support                    | n/a                               | n/a                               | ✅ <sup>5</sup>                   |
| HX550i                         | `1c03`     | HidPsu         | Full Support <sup>7</sup>       | ✅                                | ✅ <sup>6</sup>                   | ✅                                |
| HX650i                         | `1c04`     | HidPsu         | Full Support <sup>7</sup>       | ✅                                | ✅ <sup>6</sup>                   | ✅                                |
| HX750i                         | `1c05`     | HidPsu         | Full Support <sup>7</sup>       | ✅                                | ✅ <sup>6</sup>                   | ✅                                |
| HX850i                         | `1c06`     | HidPsu         | Full Support <sup>7</sup>       | ✅                                | ✅ <sup>6</sup>                   | ✅                                |
| HX1000i                        | `1c07`     | HidPsu         | Full Support <sup>7</sup>       | ✅                                | ✅ <sup>6</sup>                   | ✅                                |
| HX1200i                        | `1c08`     | HidPsu         | Full Support <sup>7</sup>       | ✅                                | ✅ <sup>6</sup>                   | ✅                                |
| HX1200i (2023)                 | `1c23`     | HidPsu         | Full Support <sup>7</sup>       | ✅                                | ✅ <sup>6</sup>                   | ✅                                |
| HX1000i (2021)                 | `1c1e`     | HidPsu         | Full Support <sup>7</sup>       | ✅                                | ✅ <sup>6</sup>                   | ✅                                |
| HX1500i (2021)                 | `1c1f`     | HidPsu         | Full Support <sup>7</sup>       | ✅                                | ✅ <sup>6</sup>                   | ✅                                |
| RM550i                         | `1c09`     | HidPsu         | Full Support <sup>7</sup>       | ✅                                | ✅ <sup>6</sup>                   | ✅                                |
| RM650i                         | `1c0a`     | HidPsu         | Full Support <sup>7</sup>       | ✅                                | ✅ <sup>6</sup>                   | ✅                                |
| RM750i                         | `1c0b`     | HidPsu         | Full Support <sup>7</sup>       | ✅                                | ✅ <sup>6</sup>                   | ✅                                |
| RM850i                         | `1c0c`     | HidPsu         | Full Support <sup>7</sup>       | ✅                                | ✅ <sup>6</sup>                   | ✅                                |
| RM1000i                        | `1c0d`     | HidPsu         | Full Support <sup>7</sup>       | ✅                                | ✅ <sup>6</sup>                   | ✅                                |
| AX850i                         | `1c0e`     | FlexUsbPsu     | Full Support <sup>8</sup>       | ✅                                | ✅ <sup>6</sup>                   | ✅                                |
| AX1000i                        | `1c0f`     | FlexUsbPsu     | Full Support <sup>8</sup>       | ✅                                | ✅ <sup>6</sup>                   | ✅                                |
| AX1300i                        | `1c10`     | FlexUsbPsu     | Full Support <sup>8</sup>       | ✅                                | ✅ <sup>6</sup>                   | ✅                                |
| AX1500i                        | `1c02`     | FlexUsbPsu     | Full Support <sup>8</sup>       | ✅                                | ✅ <sup>6</sup>                   | ✅                                |
| AX1600i                        | `1c11`     | FlexUsbPsu     | Full Support <sup>8</sup>       | ✅                                | ✅ <sup>6</sup>                   | ✅                                |
| AX760i/AX860i/AX1200i          | `1c00`     | FlexUsbPsu     | Full Support <sup>8</sup>       | ✅                                | ✅ <sup>6</sup>                   | ✅                                |
| Cooling Node                   | `0c04(38)` | -              | Support Upon Request            |                                   |                                   |                                   |
| Hydro H80                      | `0c04(37)` | -              | Support Upon Request            |                                   |                                   |                                   |
| Hydro H100                     | `0c04(3a)` | -              | Support Upon Request            |                                   |                                   |                                   |

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

9. The default safety profile stored on the device may be overridden by setting the `FANCONTROL_CORSAIRLINK_HYDRO_ASETEK_PRO_SAFETY_PROFILE_OVERRIDE_ENABLED` [environment variable](#configuration) to `1` and restarting Fan Control. This will write a new safety profile to the device:

   |            | Temp | Fan Duty |
   | ---------- | ---- | -------- |
   | Activate   | 58°C | 100%     |
   | Deactivate | 57°C | -        |

10. As of v1.4.3, the minimum pump power is 50% (regardless of the requested power in Fan Control) to prevent a "pump failure" state and noise/resonance that may occur due to low pump RPM. Additionally, the default pump power is 100%. As of v1.6.0, the default minimum pump power is 50% but users may override this by setting the `FANCONTROL_CORSAIRLINK_MIN_PUMP_DUTY` [environment variable](#configuration) to the desired minimum pump power.

11. Corsair One computers using the Hydro Platinum controller are supported. For liquid-cooled GPU models, GPU pump RPM and liquid temperature sensors are available, but GPU pump speed control is not yet supported. All supported models support CPU pump speed control.

### iCUE LINK Support

Support for the iCUE LINK Hub was added in v1.5.0. The following LINK devices are supported:

| Device                   | Type Code | Model Code | Status       | Read Fan/Pump RPM | Set Fan/Pump Power | Read Temp Sensor |
| ------------------------ | --------- | ---------- | ------------ | ----------------- | ------------------ | ---------------- |
| QX Fan                   | `01`      | `00`       | Full Support | ✅                | ✅                 | ✅               |
| LX Fan                   | `02`      | `00`       | Full Support | ✅                | ✅                 | n/a              |
| H100i (Black)            | `07`      | `00`       | Full Support | ✅                | ✅ <sup>1</sup>    | ✅               |
| H115i (Black)            | `07`      | `01`       | Full Support | ✅                | ✅ <sup>1</sup>    | ✅               |
| H150i (Black)            | `07`      | `02`       | Full Support | ✅                | ✅ <sup>1</sup>    | ✅               |
| H170i (Black)            | `07`      | `03`       | Full Support | ✅                | ✅ <sup>1</sup>    | ✅               |
| H100i (White)            | `07`      | `04`       | Full Support | ✅                | ✅ <sup>1</sup>    | ✅               |
| H150i (White)            | `07`      | `05`       | Full Support | ✅                | ✅ <sup>1</sup>    | ✅               |
| XC7 (Stealth Gray)       | `09`      | `00`       | Full Support | n/a               | n/a                | ✅               |
| XC7 (White)              | `09`      | `01`       | Full Support | n/a               | n/a                | ✅               |
| XD5 (Stealth Gray)       | `0c`      | `00`       | Full Support | ✅                | ✅ <sup>1</sup>    | ✅               |
| XD5 (White)              | `0c`      | `01`       | Full Support | ✅                | ✅ <sup>1</sup>    | ✅               |
| RX RGB Fan               | `0f`      | `00`       | Full Support | ✅                | ✅                 | n/a              |
| CapSwap Module - VRM Fan | `10`      | `00`       | Full Support | ✅                | ✅                 | n/a              |
| TITAN AIO                | `11`      | `00`-`05`  | Full Support | ✅                | ✅                 | ✅               |
| RX Fan                   | `13`      | `00`       | Full Support | ✅                | ✅                 | n/a              |

Don't see your device listed? Open an [issue](https://github.com/EvanMulawski/FanControl.CorsairLink/issues) and provide a USB packet capture.

1. The minimum pump power is 50% (regardless of the requested power in Fan Control). As a result, Fan Control's automatic control-sensor pairing may fail for these devices and the sensor must be paired manually. As of v1.6.0, the default minimum pump power is 50% but users may override this by setting the `FANCONTROL_CORSAIRLINK_MIN_PUMP_DUTY` [environment variable](#configuration) to the desired minimum pump power.

## Installation

> [!WARNING]
> This plugin will not function correctly if Corsair iCUE and the "Corsair Service" service is running. These programs should be stopped before running Fan Control. Running [incompatible programs](#compatibility) that attempt to communicate with these devices while Fan Control is running is not a supported scenario.

> [!WARNING]
> All versions of this plugin prior to v1.7.0 require the .NET Framework build of Fan Control. Install Fan Control using the `FanControl_*_net_4_8.zip` release files or the `FanControl_*_net_4_8_Installer.exe` installer only.

> [!NOTE]
> Support for the .NET 8 build of Fan Control was added in v1.7.0-beta.1. As of this version, plugin builds are located within their respective directories. In step 4 below, the `FanControl.CorsairLink.dll` file will be located within the `net48` or `net8.0` directory. Choose the build that matches your Fan Control installation.

1. Download a [release](https://github.com/EvanMulawski/FanControl.CorsairLink/releases).
2. Unblock the downloaded ZIP file. (Right-click, Properties, check Unblock, OK)
3. Exit Fan Control.
4. Copy `FanControl.CorsairLink.dll` to Fan Control's `Plugins` directory.
5. Start Fan Control.

### SiUsbXpress Driver

> [!TIP]
> The SiUsbXpress driver is only necessary if:
>
> - the Status of your device in the [Device Support](#device-support) matrix has the **8** footnote superscript, e.g. Full Support <sup>8</sup>
> - the driver has not been installed previously (by Corsair iCUE or manually)

> [!NOTE]
> The driver is only supported on:
>
> - Windows 10 64-bit
> - Windows 11 64-bit

To install the driver:

1. Ensure Fan Control is closed.
2. Ensure `FanControl.CorsairLink.dll` has been copied to Fan Control's `Plugins` directory.
3. Copy `SiUSBXp.dll` from the `Corsair-SiUsbXpress-Driver` directory to Fan Control's `Plugins` directory.
4. Right-click the `CorsairSiUSBXp.inf` file in the `Corsair-SiUsbXpress-Driver` directory and select **Install**. (This is a signed driver shipped with Corsair iCUE.)
5. Start Fan Control.

## Configuration

This plugin reads the following Windows environment variables:

| Name                                                                      | Description                                                    | Values                                      |
| ------------------------------------------------------------------------- | -------------------------------------------------------------- | ------------------------------------------- |
| `FANCONTROL_CORSAIRLINK_DEBUG_LOGGING_ENABLED`                            | Enables debug logging to the `CorsairLink.log` file.           | `1` = enabled, `0` = disabled               |
| `FANCONTROL_CORSAIRLINK_DIRECT_LIGHTING_DEFAULT_BRIGHTNESS` <sup>1</sup>  | Sets the LED brightness on supported models.                   | percent, e.g. `50`                          |
| `FANCONTROL_CORSAIRLINK_DIRECT_LIGHTING_DEFAULT_RGB` <sup>1</sup>         | Sets the LED color on supported models.                        | RGB color in `R,G,B` format, e.g. `0,255,0` |
| `FANCONTROL_CORSAIRLINK_ERROR_NOTIFICATIONS_DISABLED`                     | Disables critical error notifications.                         | `1` = disabled, `0` = enabled               |
| `FANCONTROL_CORSAIRLINK_HYDRO_ASETEK_PRO_SAFETY_PROFILE_OVERRIDE_ENABLED` | Overrides the pump safety profile on Hydro Asetek Pro devices. | `1` = override, `0` = do not override       |
| `FANCONTROL_CORSAIRLINK_MIN_PUMP_DUTY` <sup>2</sup>                       | Sets the minimum pump power on supported models.               | percent, e.g. `50`                          |

1. Supported models: Hydro Platinum (v1.6.0+)
2. Supported models: iCUE LINK, Commander CORE (v1.6.0+)

> [!TIP]
> To set these values, start Run or Command Prompt and run `rundll32 sysdm.cpl,EditEnvironmentVariables`. Alternatively, use the `setx` command.

> [!TIP]
> Fan Control must be restarted for changes to these environment variables to take effect.

## Interoperability

This plugin implements a standard global mutex (`Global\CorsairLinkReadWriteGuardMutex`) to synchronize device communication.

### Compatibility

| Application      | Compatibility                                                                                                                                            | Notes                                                      |
| ---------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------- | ---------------------------------------------------------- |
| HWiNFO           | ✅ 5.34+                                                                                                                                                 |                                                            |
| SIV              | ✅ 5.17+                                                                                                                                                 |                                                            |
| SignalRGB        | ✅ Commander PRO/CORE/CORE XT: 2.2.29+, iCUE LINK: 2.3.13+, Hydro Platinum: 2.3.45+                                                                      | Commander PRO/CORE/CORE XT, Hydro Platinum, iCUE LINK only |
| OpenRGB          | ⚠️ 1.0+ (added in [f6723975](https://gitlab.com/CalcProgrammer1/OpenRGB/-/commit/f672397563cc8e1fd6d6e4c7a44a196ae42c11c7/pipelines?ref=master))         | Commander PRO/CORE/CORE XT, Hydro Platinum only            |
| Corsair iCUE     | ❌ Not compatible ([more info](https://forum.corsair.com/forums/topic/138062-corsair-link-doesnt-work-with-hwinfo64/?do=findComment&comment=824447))     |                                                            |
| Citrix Workspace | ❌ Not compatible ([more info](https://help.corsair.com/hc/en-us/articles/14242499752589-iCUE-Device-recognition-issue-with-Citrix-Workspace-installed)) |                                                            |

Note: Any third-party software that properly implements the standard mutex for your devices will likely be compatible.
