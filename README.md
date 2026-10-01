# Filmauswertung Modern UI

A modern WPF-based desktop application for analyzing radiochromic film data in radiation dosimetry. This tool is designed for medical physics professionals and researchers working with film-based dose measurement systems.

## Overview

Filmauswertung Modern UI provides a comprehensive suite of tools for:
- **Film Calibration**: Create dose-response curves using polynomial or hyperbola fitting models
- **Dose Extraction**: Convert film images to calibrated dose maps
- **Data Analysis**: Statistical analysis and visualization of dose distributions
- **Export/Import**: Support for multiple file formats including JSON and OPG

## Key Features

### 1. Calibration Module
- Import irradiated film images for calibration
- Support for multiple fitting models:
  - **3rd Degree Polynomial Fit** (ESTRO expert recommendation): `dose = a0 + a1*OD + a2*OD² + a3*OD³`
  - **Hyperbola Fit** (vendor recommended): `dose = a2 + a1/(OD - a0)`
- Automatic optical density (OD) calculation
- Interactive calibration plots using OxyPlot
- Time interval tracking between irradiation and scan
- JSON export of calibration data

### 2. Calculate/Convert Module
- Load and process film images
- Apply dose conversion using calibration curves
- Smoothing filters (adjustable levels) for noise reduction
- Statistical analysis with histogram and peak detection
- Heatmap visualization of dose distributions
- Export results in multiple formats

### 3. File Management
- **Flexible Import Options**:
  - Single file import
  - Multiple files import
  - Batch folder processing
- **Export Options**:
  - Single file export with file dialogs
  - Batch folder export with automatic organization
- Support for various image formats

### 4. OPG File Support
- Parse OPG (OptiGray) format files
- Extract metadata (energy, data unit, data factor)
- Load and validate 2D pixel matrices with coordinate data
- FFF (Flattening Filter Free) detection

## Technology Stack

- **Language**: C# (.NET Framework)
- **UI Framework**: WPF (Windows Presentation Foundation)
- **Architecture**: MVVM (Model-View-ViewModel)
- **Visualization**: OxyPlot for data plotting and analysis
- **Data Format**: JSON for calibration data persistence

## Project Structure

```
Filmauswertung_ModernUI/
├── MVVM/
│   ├── Model/              # Data models (CutModel, Segment, Marker, etc.)
│   ├── View/               # XAML UI views
│   └── ViewModel/          # View logic and commands
├── Services/
│   ├── Exporter.cs         # Export functionality (single file, folder)
│   ├── Importer.cs         # Import functionality and dialog service
│   └── OpgLoader.cs        # OPG file parsing
├── Core/                   # Core interfaces and extensions
├── Converters/             # XAML value converters
├── Behaviors/              # XAML attached behaviors
├── Properties/             # Application resources
└── Fonts/                  # Custom fonts (Poppins)
```

## Getting Started

### Prerequisites
- .NET Framework 4.7.2 or higher
- Windows 7 or later
- Visual Studio 2019 or later (for development)

### Building

1. Clone the repository:
   ```bash
   git clone https://github.com/MaxKelm/Filmauswertung_ModernUI.git
   cd Filmauswertung_ModernUI
   ```

2. Open the solution in Visual Studio:
   ```bash
   start Filmauswertung_ModernUI.sln
   ```

3. Build the project:
   ```
   Build > Build Solution (Ctrl+Shift+B)
   ```

4. Run the application:
   ```
   Debug > Start Debugging (F5)
   ```

## Usage Guide

### Creating a Calibration

1. **Load Film Images**:
   - Click the import button in the Calibration tab
   - Select irradiated film images (JPG, PNG, etc.)
   - Include a background/unirradiated scan

2. **Enter Dose Values**:
   - Input calibration dose values separated by semicolons (e.g., `0; 0.5; 1.0; 2.0; 5.0`)
   - Use either ',' or '.' as decimal separator
   - Ensure one value is zero (background scan)

3. **Select Fitting Model**:
   - Choose between polynomial (4 parameters) or hyperbola fit (3 parameters)
   - Specify time interval between irradiation and measurement

4. **Process**:
   - Click "Process" to generate the calibration curve
   - Review the resulting plot

5. **Export**:
   - Export calibration data as JSON for later use

### Converting Film Images to Dose Maps

1. **Load Calibration**:
   - Import a previously saved calibration JSON file

2. **Load Film Images**:
   - Import the film images to be analyzed

3. **Apply Dose Conversion**:
   - Click "Calculate Dose" to convert images to dose using the calibration
   - Adjust smoothing level if needed (1 = no smoothing, 5 = maximum)

4. **Analyze Results**:
   - View dose statistics (peaks, median, mean, range)
   - Examine the heatmap visualization

5. **Export Results**:
   - Export dose maps and analysis results

## Data Formats

### Calibration Export (JSON)
```json
{
  "CalibrationDose": [0, 0.5, 1.0, 2.0],
  "CalibrationOD": [0, 0.1, 0.2, 0.35],
  "FitFunction": "Polynomial",
  "FitCoefficients": [0.1, 0.5, -0.02, 0.001],
  "Formula": "dose=a0+a1*OD+a2*OD^2+a3*OD^3",
  "TimeInterval": 24,
  "TimeUnit": "hours"
}
```

## Known Limitations

- OPG file format support is limited to specific metadata tags
- Currently Windows-only (WPF limitation)

## Future Enhancements

- Cross-platform support (WPF to MAUI migration)
- Additional fitting models
- Batch processing improvements
- Advanced image analysis filters
- Database support for calibration history

## License

This project uses:
- Poppins font licensed under SIL Open Font License (OFL)
- Windows API Code Pack for common file dialogs

## Contributing

Contributions are welcome! Please feel free to:
- Report bugs and issues
- Suggest new features
- Submit pull requests

## Author

MaxKelm

## Contact & Support

For questions or support, please open an issue on the GitHub repository.

---

**Last Updated**: October 1, 2026 (v1.0.1)
