# WinHUD

**WinHUD** is a lightweight, non-intrusive system performance overlay for Windows. It provides real-time monitoring of critical system metrics (CPU, GPU, RAM, Disk, Network) without cluttering your screen or stealing focus from your games.

Designed with a "set it and forget it" philosophy, WinHUD automatically detects when you are gaming, adjusts its text color based on the background brightness, and stays out of your way.

## 🌟 Key Features

### 📊 Real-Time Hardware Monitoring

Get instant feedback on your system's performance with updates every second:
* **CPU Usage:** Tracks total processor utilization percentage.
* **GPU Usage:** Monitor graphics card load (Supports **NVIDIA, AMD, and Intel** GPUs via `LibreHardwareMonitorLib`).
* **RAM Status:** Displays available system memory.
* **Disk Activity:** Shows total read/write speed and individual load percentages for all physical drives.
* **Network Speed:** Real-time upload/download bandwidth monitoring.

### 🎮 Gamer-Centric Design

* **Ghost Mode:** The overlay is completely click-through. You can click "through" the text to interact with your game or desktop behind it.
* **Adaptive Visibility:** Automatically appears when the **Steam Overlay** (`gameoverlayui64.exe`) is detected.
* **Manual Toggle:** Use the global hotkey **`Alt + Shift + H`** to show or hide the HUD at any time.

### 👁️ Smart Visuals

* **Dynamic Contrast:** The HUD analyzes the pixels behind it 60 times a second. It switches between **Bright Green** (for dark backgrounds) and **Dark Grey** (for light backgrounds) to ensure perfect readability.
* **Minimalist Design:** Uses a clean, transparent interface that sits quietly in the bottom-left corner of your screen.

### 🛠️ Smart Control

* **Multi-Monitor Support:** Use the **System Tray Icon** to move the HUD to any connected monitor instantly.
* **Persistent Settings:** Your preferred monitor selection is saved automatically and restored upon the next launch.

---

## 📥 Installation

### Option 1: Single Executable (Recommended)

1.  Download the latest `WinHUD-vX.X.X-win-x64.exe` from the [Releases](../../releases) page.
2.  Place it anywhere (e.g., your Desktop or a Tools folder).
3.  Run it. That's it!

### Option 2: Portable Archive

1.  Download `WinHUD-vX.X.X-win-x64.zip`.
2.  Extract the folder to your preferred location.
3.  Run `WinHUD.exe`.

> **Note:** WinHUD adds itself to your Windows Startup apps automatically, so you don't need to launch it manually every time you reboot.

---

## 🕹️ Usage

| Action | Command |
| :--- | :--- |
| **Toggle Overlay** | Press **`Alt + Shift + H`** |
| **Move Screen** | Right-click the **Tray Icon** (near your clock) and select a monitor. |
| **Exit App** | Right-click the **Tray Icon** $\rightarrow$ `Exit`. |

### Configuration

WinHUD saves your preferences (like the selected monitor) to a configuration file located at:
`%AppData%\WinHUD\config.json`

---

## 🏗️ Building from Source

### Prerequisites

* **.NET 10.0 SDK** (Preview/Daily builds required).
* **Visual Studio 2022** (or newer).
* **Git Bash** (recommended for running the Makefile).

### Using the Makefile

This project includes a comprehensive `Makefile` to handle versioning, building, and packaging.

1.  **Clone the repository:**

    ```bash
    git clone https://github.com/Seintian/WinHUD.git
    cd WinHUD
    ```

2.  **Build a Release:**

    To build the single-file executable and portable archives:

    ```bash
    make all
    ```

    *Artifacts will be placed in the `dist/` folder.*

3.  **Publish a New Version:**

    Update the version in `WinHUD/WinHUD.csproj` manually, then run:

    ```bash
    make release
    ```

    *This builds the app, creates a git tag, and pushes it to your repository.*

### Manual Build (Visual Studio)

1.  Open `WinHUD.slnx` in Visual Studio.
2.  Select `Release` configuration.
3.  Right-click the `WinHUD` project $\rightarrow$ **Publish**.

---

## 🤝 Credits & Dependencies

* [LibreHardwareMonitor](https://github.com/LibreHardwareMonitor/LibreHardwareMonitor): Used for reading GPU, CPU, and hardware sensors.
* **System.Drawing.Common** & **WPF**: Core UI and screen capture technologies.

## 📄 License

Distributed under the GPL License. See `LICENSE` for more information.
