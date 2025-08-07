# Eclipse Plan Check Script

A comprehensive plan verification tool for Varian Eclipse Treatment Planning System using ESAPI.

## Overview

This script provides automated plan checking functionality to help medical physicists and dosimetrists verify treatment plans before delivery. It performs comprehensive analysis across multiple categories and presents results in an easy-to-read tabbed interface.

## Features

### 📋 Plan Information
- Patient demographics and plan details
- Course and structure set information
- Image resolution and metadata
- Plan creation and approval tracking

### 💊 Dose Information
- Prescription details (dose per fraction, total dose)
- Dose calculation status and grid information
- Algorithm verification (photon/electron models)
- Plan normalization settings

### ⚡ Beam Information
- Complete beam inventory (treatment vs setup)
- Detailed beam parameters (energy, angles, MU, field sizes)
- Safety checks for unusual monitor unit values
- Total treatment time calculations

### 🎯 Structure Information
- Automatic categorization (targets, OARs, body, supports)
- Volume calculations and verification
- Missing structure identification
- Empty structure warnings

### ✅ Status & Safety Checks
- Treatment approval status
- Critical safety verification
- Treatment readiness assessment
- Comprehensive error reporting

## Installation

1. **Download the script:**
   - Download `PlanCheck Template Simple.cs` from this repository

2. **Deploy to Eclipse:**
   - Copy the script to your Eclipse scripts directory:
     ```
     \\eclipsefsprd\va_data$\ProgramData\Vision\PublishedScripts\
     ```

3. **Run from Eclipse:**
   - Open a patient and plan in Eclipse
   - Navigate to Tools → Scripts
   - Select and run "PlanCheck Template Simple"

## Usage

1. **Load a patient and plan** in Eclipse
2. **Run the script** from the Tools → Scripts menu
3. **Review results** in the tabbed interface:
   - Plan Info: Basic plan and patient information
   - Dose: Prescription and calculation details
   - Beams: Treatment beam analysis
   - Structures: Target and OAR verification
   - Status: Safety checks and treatment readiness
4. **Make corrections** in Eclipse as needed
5. **Rerun the script** to verify fixes

## System Requirements

- **Eclipse Version:** 13.6 or later
- **ESAPI Version:** 1.0.300.11 or compatible
- **.NET Framework:** 4.5 or later
- **Permissions:** Read access to Eclipse patient database

## Key Benefits

### 🔍 Comprehensive Analysis
- Covers all critical plan verification areas
- Automated safety checks reduce human error
- Consistent verification workflow

### 🚀 Improved Workflow
- Non-blocking design keeps Eclipse functional
- Clear visual indicators (✅ ⚠️ ❌)
- Instant plan overview across multiple tabs

### 📈 Quality Assurance
- Standardized checking procedures
- Detailed error reporting
- Treatment readiness verification

## Visual Indicators

- ✅ **Green checkmark:** Verified/acceptable values
- ⚠️ **Yellow warning:** Attention needed but not blocking
- ❌ **Red error:** Critical issue requiring correction

## Troubleshooting

### Script Won't Run
- Ensure patient and plan are loaded in Eclipse
- Verify script is in the correct Eclipse scripts directory
- Check that plan has required components (structure set, dose calculation)

### "Refresh" Errors
- The refresh button is disabled by design
- To get updated results after making changes, close the window and rerun the script
- This ensures fresh data from Eclipse

### Missing Information
- Some fields may not be available in older ESAPI versions
- The script gracefully handles missing properties
- Contact your system administrator for ESAPI version information

## Contributing

This is an open-source project. Contributions are welcome!

### How to Contribute
1. Fork this repository
2. Create a feature branch
3. Make your changes
4. Test thoroughly with Eclipse
5. Submit a pull request

### Areas for Enhancement
- Additional safety checks
- DVH analysis integration
- Plan comparison features
- Custom institutional protocols
- Automated report generation

## Version History

### v1.0.0
- Initial release with comprehensive plan checking
- Five-tab interface design
- ESAPI compatibility fixes
- Non-blocking Eclipse integration

## Support

For questions, issues, or feature requests:
- Create an issue in this repository
- Contact your local medical physics team
- Refer to Eclipse ESAPI documentation

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Disclaimer

**IMPORTANT:** This tool is designed to assist in plan verification but does not replace the clinical judgment of qualified medical professionals. All plans must be reviewed and approved by qualified medical physicists and physicians before treatment delivery.

## Acknowledgments

- Varian Medical Systems for the Eclipse Treatment Planning System
- The medical physics community for feedback and testing
- Contributors to the Eclipse ESAPI framework

---

**Note:** Always verify the accuracy of automated checks with manual review. This tool enhances but does not replace professional clinical review.
