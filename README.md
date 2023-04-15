# CorsairLink plugin for Fan Control

The unofficial CorsairLink plugin for [Fan Control](https://github.com/Rem0o/FanControl.Releases).

[![Support](https://img.shields.io/badge/Support-Venmo-blue?style=for-the-badge&logo=venmo&color=3D95CE)](https://www.venmo.com/u/EvanMulawski)
[![Support](https://img.shields.io/badge/Support-Buy_Me_A_Coffee-yellow?style=for-the-badge&logo=buy%20me%20a%20coffee&color=FFDD00)](https://www.buymeacoffee.com/evanmulawski)

## Device Support

| Device                          | PID        | Status                          | Read Fan/Pump RPM | Set Fan/Pump Power | Read Temp Sensor |
| ------------------------------- | ---------- | ------------------------------- | ----------------- | ------------------ | ---------------- |
| Commander PRO                   | `0c10`     | Full Support                    | ✅                | ✅                 | ✅               |
| Commander PRO (Obsidian 1000D)  | `1d00`     | Full Support                    | ✅                | ✅                 | ✅               |
| Commander CORE XT               | `0c2a`     | Full Support <sup>1,2</sup>     | ✅                | ✅                 | ✅               |
| Commander CORE (ELITE CAPELLIX) | `0c1c`     | Full Support <sup>1,2</sup>     | ✅                | ✅                 | ✅               |
| Commander CORE                  | `0c32`     | Full Support <sup>1,2</sup>     | ✅                | ✅                 | ✅               |
| Commander Mini                  | `0c04(3d)` | v1.2.x Pre-Release <sup>3</sup> | ✅                | ✅                 | ✅               |
| Hydro H60i Elite                | `0c34`     | Full Support <sup>1</sup>       | ✅                | ✅ <sup>4</sup>    | ✅ <sup>5</sup>  |
| Hydro H60i Pro XT               | `0c29`     | Full Support <sup>1</sup>       | ✅                | ✅ <sup>4</sup>    | ✅ <sup>5</sup>  |
| Hydro H60i Pro XT               | `0c30`     | Full Support <sup>1</sup>       | ✅                | ✅ <sup>4</sup>    | ✅ <sup>5</sup>  |
| Hydro H100i Elite               | `0c35`     | Full Support <sup>1</sup>       | ✅                | ✅ <sup>4</sup>    | ✅ <sup>5</sup>  |
| Hydro H100i Platinum            | `0c18`     | Full Support <sup>1</sup>       | ✅                | ✅ <sup>4</sup>    | ✅ <sup>5</sup>  |
| Hydro H100i Platinum SE         | `0c19`     | Full Support <sup>1</sup>       | ✅                | ✅ <sup>4</sup>    | ✅ <sup>5</sup>  |
| Hydro H100i Pro XT              | `0c20`     | Full Support <sup>1</sup>       | ✅                | ✅ <sup>4</sup>    | ✅ <sup>5</sup>  |
| Hydro H100i Pro XT              | `0c2d`     | Full Support <sup>1</sup>       | ✅                | ✅ <sup>4</sup>    | ✅ <sup>5</sup>  |
| Hydro H115i Elite               | `0c36`     | Full Support <sup>1</sup>       | ✅                | ✅ <sup>4</sup>    | ✅ <sup>5</sup>  |
| Hydro H115i Platinum            | `0c17`     | Full Support <sup>1</sup>       | ✅                | ✅ <sup>4</sup>    | ✅ <sup>5</sup>  |
| Hydro H115i Pro XT              | `0c21`     | Full Support <sup>1</sup>       | ✅                | ✅ <sup>4</sup>    | ✅ <sup>5</sup>  |
| Hydro H115i Pro XT              | `0c2e`     | Full Support <sup>1</sup>       | ✅                | ✅ <sup>4</sup>    | ✅ <sup>5</sup>  |
| Hydro H150i Elite               | `0c37`     | Full Support <sup>1</sup>       | ✅                | ✅ <sup>4</sup>    | ✅ <sup>5</sup>  |
| Hydro H150i Pro XT              | `0c22`     | Full Support <sup>1</sup>       | ✅                | ✅ <sup>4</sup>    | ✅ <sup>5</sup>  |
| Hydro H150i Pro XT              | `0c2f`     | Full Support <sup>1</sup>       | ✅                | ✅ <sup>4</sup>    | ✅ <sup>5</sup>  |
| Hydro H80i                      | `0c04(3b)` | v1.2.x Pre-Release <sup>3</sup> | ✅                | ✅ <sup>7</sup>    | ✅ <sup>5</sup>  |
| Hydro H100i                     | `0c04(3c)` | v1.2.x Pre-Release <sup>3</sup> | ✅                | ✅ <sup>7</sup>    | ✅ <sup>5</sup>  |
| Hydro H100i GT                  | `0c04(40)` | v1.2.x Pre-Release <sup>3</sup> | ✅                | ✅ <sup>7</sup>    | ✅ <sup>5</sup>  |
| Hydro H110i                     | `0c04(42)` | v1.2.x Pre-Release <sup>3</sup> | ✅                | ✅ <sup>7</sup>    | ✅ <sup>5</sup>  |
| Hydro H110i GT                  | `0c04(41)` | v1.2.x Pre-Release <sup>3</sup> | ✅                | ✅ <sup>7</sup>    | ✅ <sup>5</sup>  |
| Cooling Node                    | `0c04(38)` | Support Upon Request            |                   |                    |                  |
| Hydro H80                       | `0c04(37)` | Support Upon Request            |                   |                    |                  |
| Hydro H100                      | `0c04(3a)` | Support Upon Request            |                   |                    |                  |
| Hydro H80i GT                   | `0c02`     | No Support <sup>6</sup>         | ❌                | ❌                 | ❌               |
| Hydro H80i GT V2                | `0c08`     | No Support <sup>6</sup>         | ❌                | ❌                 | ❌               |
| Hydro H80i Pro                  | `0c16`     | No Support <sup>6</sup>         | ❌                | ❌                 | ❌               |
| Hydro H100i GT V2               | `0c09`     | No Support <sup>6</sup>         | ❌                | ❌                 | ❌               |
| Hydro H100i GTX                 | `0c03`     | No Support <sup>6</sup>         | ❌                | ❌                 | ❌               |
| Hydro H100i Pro                 | `0c15`     | No Support <sup>6</sup>         | ❌                | ❌                 | ❌               |
| Hydro H110i GT V2               | `0c0a`     | No Support <sup>6</sup>         | ❌                | ❌                 | ❌               |
| Hydro H110i GTX                 | `0c07`     | No Support <sup>6</sup>         | ❌                | ❌                 | ❌               |
| Hydro H115i Pro                 | `0c13`     | No Support <sup>6</sup>         | ❌                | ❌                 | ❌               |
| Hydro H150i Pro                 | `0c12`     | No Support <sup>6</sup>         | ❌                | ❌                 | ❌               |

1. Software mode only.

2. The speed ramping in the Commander CORE and CORE XT is too slow for automatic sensor pairing and start/stop detection to function in Fan Control. Therefore, assisted setup will fail. Each control will need to be paired with its corresponding sensor manually and its start/stop values will need to be set manually.

3. Support is currently experimental and only available in a pre-release (see [Releases](https://github.com/EvanMulawski/FanControl.CorsairLink/releases)). Please provide your feedback in issues and discussions.

4. Variable pump speed using the device's Quiet, Balanced, and Performance presets according to the following control value map:

   ```
   0-33% => Quiet
   34-67% => Balanced
   68-100% => Performance
   ```

5. Reads the liquid temperature.

6. The USB device class is not HID and support cannot be added.

7. Pump control is experimental pending feedback. Please provide your feedback in issues and discussions.

## Installation

⚠ This plugin will not function correctly if Corsair iCUE (specifically, the "Corsair Service" service) is running. This service should be stopped before running Fan Control. Running other programs that attempt to communicate with these devices while Fan Control is running is not currently a supported scenario.

⚠ This plugin requires the .NET Framework build of Fan Control. Install Fan Control using the `FanControl_net_4_8.zip` release files.

1. Download a [release](https://github.com/EvanMulawski/FanControl.CorsairLink/releases).
2. Unblock the downloaded ZIP file. (Right-click, Properties, check Unblock, OK)
3. Exit Fan Control.
4. Copy `FanControl.CorsairLink.dll` to the Fan Control `Plugins` directory.
5. Start Fan Control.
