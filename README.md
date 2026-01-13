# BIM'ism AI Agent for Revit 2025 🚀

The official Revit AI Agent by BIM'ism. A powerful C# Revit Add-in that combines a professional AI-driven chat interface with high-performance native structural rebar tools.

![Ribbon Icons](https://raw.githubusercontent.com/BIMism/BIM-ism-AI/main/ai_agent.png)

## Key Features

### 💬 AI Copilot
- **Live Chat Interface**: Dockable pane for real-time BIM assistance.
- **Smart Execution**: Powerful C# script execution for Revit automation.
- **Gemini Integration**: Pre-configured for high-quality architectural logic.

### 🏗️ Native Structural Tools
Dedicated, native rebar generators for structural elements:
- **Beam Rebar**: Advanced UI with Stirrups, Diameter, and Spacing controls.
- **Column Rebar**: High-precision vertical reinforcement.
- **Footing Rebar**: Fast foundation reinforcement.
- **Slab & Wall Rebar**: Grid-based automated mesh generation.

### 🎨 Professional UI
- **Custom Icons**: 8 unique, color-coded 32x32 pixel icons for high visibility.
- **BIM'ism Theme**: Premium design matching professional CAD standards.

### 🔄 Auto-Update System
- **GitHub Integration**: Automatically checks for new versions on startup.
- **One-Click Update**: No manual installation needed for future versions.

## Installation

### Option 1: One-Click Installer (Recommended) 🚀
1. Download `BIMism_AI_Agent_Installer.exe` from the root of this repo (or from Releases).
2. Run the `.exe` on any PC with Revit 2025.
3. It will automatically detect the folders and install the plugin.

### Option 2: Manual Installation
1. Download `RevitAIAgent.zip` from the [Releases](https://github.com/BIMism/BIM-ism-AI/releases) section.
2. Extract to: `%AppData%\Autodesk\Revit\Addins\2025\`
3. Restart Revit.

## Deployment for Developers

### Prerequisites
- .NET 8.0 SDK
- Revit 2025 Installed

### Build & Deploy
1. Clone the repo:
   `git clone https://github.com/BIMism/BIM-ism-AI.git`
2. Build the project:
   `dotnet build`
3. Use the provided deployment script:
   `.\deploy.ps1`

## Auto-Update Manifest
The plugin tracks updates via `version.json` in this repository. To release a new version, simply update the JSON and create a new GitHub release tag.

---
© 2026 BIM'ism - Advanced Agentic Coding for AEC
