# Contributing to WinHUD

Thank you for your interest in contributing to WinHUD! We welcome contributions from everyone—whether it's reporting a bug, suggesting a new feature, or submitting a Pull Request (PR).

This document outlines the guidelines and workflows for contributing to the project.

## 🛠️ Getting Started

### Prerequisites
To build and run WinHUD, you need the following tools installed:

1.  **Visual Studio 2022** (or newer) with the **.NET Desktop Development** workload.
2.  **.NET 10.0 SDK** (Preview/Daily builds required).
3.  **Git Bash** (Recommended for running the Makefile on Windows).

### Setting Up the Environment
1.  **Clone the repository:**

    ```bash
    git clone [https://github.com/yourusername/WinHUD.git](https://github.com/yourusername/WinHUD.git)
    cd WinHUD
    ```

2.  **Open the Solution:**
    Open `WinHUD.slnx` in Visual Studio.

## 🏗️ Building and Testing

We use a **Makefile** to standardize the build process. While you can hit "Start" in Visual Studio for debugging, we recommend testing the final build using the make commands before submitting a PR.

### Common Commands

| Command | Description |
| :--- | :--- |
| `make all` | Cleans the project and builds both the Single-File Executable and Portable Archive. |
| `make clean` | Removes all build artifacts (`dist/` folder). |

### Manual Build

If you prefer not to use Make, you can build the release version using the .NET CLI:

```bash
dotnet publish WinHUD/WinHUD.csproj -c Release -r win-x64 --self-contained true
```

## 💻 Development Workflow

1. **Create a Branch:**

    Always create a new branch for your work. Do not commit directly to `master`.

    ```bash
    git checkout -b feature/my-new-feature
    # or
    git checkout -b fix/bug-description
    ```

2. **Make Your Changes:**

    - Keep code style consistent with existing C# files.
    - If modifying UI (`MainWindow.xaml`), ensure **Ghost Mode** (click-through, transparency and invisibility among the other windows) and **Dynamic Contrast** still function correctly.
    - If adding new hardware sensors, verify compatibility via `LibreHardwareMonitorLib`.

3. **Test Your Changes:**

    - Run the app and verify the overlay appears when a Steam game starts.
    - Toggle the HUD using `Alt + Shift + H`.
    - Check the System Tray icon context menu.

## 📮 Submitting a Pull Request

1.  Push your branch to GitHub.
2.  Open a Pull Request against the `master` branch.
3.  **Description:** Clearly explain what your PR does.
    - *For Features:* Explain the "why" and "how".
    - *For Bugs:* Link to the Issue it fixes (e.g., `Fixes #123`)
4.  **Screenshots:** If you changed the UI, please include a screenshot or GIF.

## 🐛 Reporting Bugs

If you find a bug, please create a new Issue on GitHub. Include:

-  **Steps to reproduce** the bug.
-  **Expected behavior** vs. **Actual behavior**.
-  Your Windows version and .NET version.
-  Logs (if applicable).

## 🤝 Code of Conduct

Please note that this project is released with a [Code of Conduct](CODE_OF_CONDUCT.md). By participating in this project you agree to abide by its terms.

Thank you for helping make WinHUD better!
