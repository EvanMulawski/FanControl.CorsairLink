# CorsairLink plugin for Fan Control

The unofficial CorsairLink plugin for [Fan Control](https://github.com/Rem0o/FanControl.Releases).

## Device Support

| Device                                | PID    | Status                      | Read Fan/Pump RPM | Set Fan/Pump Power | Read Temp Sensor |
| ------------------------------------- | ------ | --------------------------- | ----------------- | ------------------ | ---------------- |
| Commander PRO                         | `0c10` | Full Support                | ✅                | ✅                 | ✅               |
| Commander PRO (Obsidian 1000D)        | `1d00` | Full Support                | ✅                | ✅                 | ✅               |
| Commander CORE XT                     | `0c2a` | Full Support <sup>1,2</sup> | ✅                | ✅                 | ✅               |
| Commander CORE (H150i ELITE CAPELLIX) | `0c1c` | Full Support <sup>1</sup>   | ✅                | ✅                 | ✅               |
| Commander CORE                        | `0c32` | Full Support <sup>1</sup>   | ✅                | ✅                 | ✅               |
| Hydro H60i Pro XT                     | `0c29` | Full Support <sup>1,3</sup> | ✅                | ✅ <sup>4</sup>    | ✅ <sup>5</sup>  |
| Hydro H100i Platinum                  | `0c18` | Full Support <sup>1,3</sup> | ✅                | ✅ <sup>4</sup>    | ✅ <sup>5</sup>  |
| Hydro H100i Platinum SE               | `0c19` | Full Support <sup>1,3</sup> | ✅                | ✅ <sup>4</sup>    | ✅ <sup>5</sup>  |
| Hydro H100i Pro XT                    | `0c20` | Full Support <sup>1,3</sup> | ✅                | ✅ <sup>4</sup>    | ✅ <sup>5</sup>  |
| Hydro H100i Elite                     | `0c35` | Full Support <sup>1,3</sup> | ✅                | ✅ <sup>4</sup>    | ✅ <sup>5</sup>  |
| Hydro H115i Pro XT                    | `0c21` | Full Support <sup>1,3</sup> | ✅                | ✅ <sup>4</sup>    | ✅ <sup>5</sup>  |
| Hydro H115i Platinum                  | `0c17` | Full Support <sup>1,3</sup> | ✅                | ✅ <sup>4</sup>    | ✅ <sup>5</sup>  |
| Hydro H150i Pro XT                    | `0c22` | Full Support <sup>1,3</sup> | ✅                | ✅ <sup>4</sup>    | ✅ <sup>5</sup>  |
| Hydro iCUE (Unknown Product)          | `0c2d` | Full Support <sup>1,3</sup> | ✅                | ✅ <sup>4</sup>    | ✅ <sup>5</sup>  |
| Hydro iCUE (Unknown Product)          | `0c2e` | Full Support <sup>1,3</sup> | ✅                | ✅ <sup>4</sup>    | ✅ <sup>5</sup>  |
| Hydro iCUE (Unknown Product)          | `0c30` | Full Support <sup>1,3</sup> | ✅                | ✅ <sup>4</sup>    | ✅ <sup>5</sup>  |
| Hydro iCUE (Unknown Product)          | `0c34` | Full Support <sup>1,3</sup> | ✅                | ✅ <sup>4</sup>    | ✅ <sup>5</sup>  |
| Hydro iCUE (Unknown Product)          | `0c36` | Full Support <sup>1,3</sup> | ✅                | ✅ <sup>4</sup>    | ✅ <sup>5</sup>  |
| Hydro iCUE (Unknown Product)          | `0c2f` | Full Support <sup>1,3</sup> | ✅                | ✅ <sup>4</sup>    | ✅ <sup>5</sup>  |

1. Software mode only.

2. The speed ramping in the Commander CORE XT is too slow for automatic sensor pairing and start/stop detection to function in Fan Control. Therefore, assisted setup will fail. Each control will need to be paired with its corresponding sensor manually and its start/stop values will need to be set manually.

3. Support is currently experimental. Please provide your feedback in issues and discussions.

4. Variable pump speed using the device's Quiet, Balanced, and Performance presets according to the following control value map:

   ```
   0-33% => Quiet
   34-67% => Balanced
   68-100% => Performance
   ```

5. Reads the liquid temperature.

## Installation

⚠ This plugin will not function correctly if Corsair iCUE (specifically, the "Corsair Service" service) is running. This service should be stopped before running Fan Control. Running other programs that attempt to communicate with these devices while Fan Control is running is not currently a supported scenario.

1. Download a [release](https://github.com/EvanMulawski/FanControl.CorsairLink/releases).
2. Unblock the downloaded ZIP file. (Right-click, Properties, check Unblock, OK)
3. Exit Fan Control.
4. Copy `FanControl.CorsairLink.dll` to the Fan Control `Plugins` directory.
5. Start Fan Control.
