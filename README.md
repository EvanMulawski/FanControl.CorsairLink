# CorsairLink plugin for Fan Control

The unofficial CorsairLink plugin for [Fan Control](https://github.com/Rem0o/FanControl.Releases).

## Device Support

| Device                         | Status                         | Read Fan/Pump RPM | Set Fan/Pump Power | Read Temp Sensor |
| ------------------------------ | ------------------------------ | ----------------- | ------------------ | ---------------- |
| Commander PRO                  | Full Support                   | ✔                 | ✔                  | ✔                |
| Commander PRO (Obsidian 1000D) | Full Support                   | ✔                 | ✔                  | ✔                |
| Commander CORE XT              | Full Support 1️⃣                | ✔                 | ✔                  | ✔                |
| Commander CORE                 | Full Support (Experimental 🧪) | ✔                 | ✔                  | ✔                |
| Commander ST                   | Full Support (Experimental 🧪) | ✔                 | ✔                  | ✔                |

## Installation

1. Unblock the downloaded ZIP file. (Right-click, Properties, check Unblock, OK)
2. Exit Fan Control.
3. Copy `FanControl.CorsairLink.dll` to the Fan Control `Plugins` directory.
4. Start Fan Control.
5. Run assisted setup or sensor detection.

## Notes

⚠ This plugin will not function correctly if Corsair iCUE (specifically, the "Corsair Service" service) is running. This service should be stopped before running Fan Control. Running other programs that attempt to communicate with these devices is not currently a supported scenario.

🧪 Support is currently experimental and not validated. Validation feedback for these devices can be provided in the form of issues and discussions.

1️⃣ The speed ramping in the Commander CORE XT is too slow for automatic sensor pairing to function in Fan Control. Therefore, assisted setup will fail. Each control will need to be paired with its corresponding sensor manually.
