# CorsairLink plugin for Fan Control

The unofficial CorsairLink plugin for [Fan Control](https://github.com/Rem0o/FanControl.Releases).

## Device Support

| Device                         | Status                    | Read Fan/Pump RPM | Set Fan/Pump Power | Read Temp Sensor |
| ------------------------------ | ------------------------- | ----------------- | ------------------ | ---------------- |
| Commander PRO                  | Full Support              | ✔                 | ✔                  | ✔                |
| Commander PRO (Obsidian 1000D) | Full Support              | ✔                 | ✔                  | ✔                |
| Commander CORE XT              | Full Support <sup>1</sup> | ✔                 | ✔                  | ✔                |
| Commander CORE (PID `0c1c`)    | Full Support <sup>2</sup> | ✔                 | ✔                  | ✔                |
| Commander CORE (PID `0c32`)    | Planned                   |                   |                    |                  |

1. The speed ramping in the Commander CORE XT is too slow for automatic sensor pairing and start/stop detection to function in Fan Control. Therefore, assisted setup will fail. Each control will need to be paired with its corresponding sensor manually and its start/stop values will need to be set manually.

2. Support is currently experimental. Please provide your feedback in issues and discussions.

## Installation

⚠ This plugin will not function correctly if Corsair iCUE (specifically, the "Corsair Service" service) is running. This service should be stopped before running Fan Control. Running other programs that attempt to communicate with these devices while Fan Control is running is not currently a supported scenario.

1. Unblock the downloaded ZIP file. (Right-click, Properties, check Unblock, OK)
2. Exit Fan Control.
3. Copy `FanControl.CorsairLink.dll` to the Fan Control `Plugins` directory.
4. Start Fan Control.
5. Run assisted setup or sensor detection.
