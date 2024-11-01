# ScreamGuard

ScreamGuard is a .NET application designed to help manage your voice levels during calls, ensuring a quieter workspace for your colleagues. This tool is particularly useful for anyone who tends to speak loudly while on calls, helping to mitigate noise disturbances in shared environments.

<div style="text-align: center;">
    <img src="https://raw.githubusercontent.com/johnny-de/data/refs/heads/main/screamguard/call_alarm.png" alt="Screenshot of ScreamGuard with alarm" width="600" style="display: block; margin-left: auto; margin-right: auto;">
</div>

## Why ScreamGuard Exists

ScreamGuard was born out of necessity. After receiving numerous complaints from colleagues about my noise levels during calls, I realized the need for a solution that would help me maintain a reasonable voice level. By providing a way to monitor and control vocal volume, ScreamGuard aims to create a more pleasant and productive workspace for everyone.

## How ScreamGuard Works

ScreamGuard is designed to be intuitive and user-friendly, providing several configurable options to tailor its operation to your specific needs:

1. **Microphone Selection**: At the start, users can select their preferred microphone from a dropdown list. ScreamGuard will actively monitor the chosen microphone for audio input.

2. **Active Audio Stream Detection**: Upon startup, ScreamGuard listens for active audio streams. If no audio stream is detected, ScreamGuard remains inactive, allowing it to run continuously in the background without interrupting your work or requiring manual stops when not in use.

3. **Loudness Sampling**: When ScreamGuard is actively listening, it samples the loudness level from the microphone at a user-defined interval (default is every 100 milliseconds).

4. **Moving Median Calculation**: To offer a clearer picture of your overall loudness, ScreamGuard calculates a moving median based on the last few samples. This approach smooths out short-term fluctuations and provides a more stable representation of your vocal volume over time (default moving median over 10 samples).

5. **Visual Feedback**: If the moving median exceeds user-defined thresholds:
   - **Warning Level**: An orange border appears on the screen to indicate that your voice level is approaching the limit.
   - **Alarm Level**: A red border is displayed if the loudness level surpasses the alarm threshold, signaling immediate attention is needed.

### Settings

<div style="text-align: center;">
    <img src="https://raw.githubusercontent.com/johnny-de/data/refs/heads/main/screamguard/app_alarm.png" alt="Screenshot of ScreamGuard Settings" width="300" style="display: block; margin-left: auto; margin-right: auto;">
</div>

## Getting Started

You can build ScreamGuard from the source code provided in this repository. Alternatively, a pre-built executable is available for x64 systems at the following link:

[Download ScreamGuard](https://github.com/johnny-de/screamguard/raw/refs/heads/main/bin/Release/net8.0-windows/win-x64/publish/screamguard.exe)

## Bug reports / feature requests

If you want to report a bug or request a new feature, feel free to open a [new issue](https://github.com/johnny-de/screamguard/issues).