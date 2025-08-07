# Changelog

All notable changes to the Eclipse Plan Check Script project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2025-01-08

### Added
- Initial release of comprehensive Eclipse plan checking script
- Five-tab interface design for organized plan review
- Plan Information tab with patient demographics and plan details
- Dose Information tab with prescription and calculation verification
- Beam Information tab with detailed treatment beam analysis
- Structure Information tab with automatic categorization and volume checks
- Status & Safety Checks tab with treatment readiness assessment
- Visual indicators for verification status (✅ ⚠️ ❌)
- Non-blocking design that keeps Eclipse functional during analysis
- ESAPI compatibility for Eclipse 13.6+ with version 1.0.300.11
- Comprehensive error handling and graceful degradation
- Automatic safety checks for common planning errors
- Treatment readiness assessment with clear pass/fail criteria

### Fixed
- Compilation errors with older ESAPI versions
- Text orientation issues in RichTextBox display
- ScriptContext disposal errors on refresh attempts
- DateTime formatting compatibility issues
- Property access errors for version-specific ESAPI features

### Changed
- Replaced RichTextBox with TextBox for reliable text display
- Disabled refresh functionality to prevent ScriptContext errors
- Simplified property access for better ESAPI version compatibility
- Updated workflow to require script rerun for data updates

### Technical Notes
- Compatible with ESAPI version 1.0.300.11
- Requires .NET Framework 4.5 or later
- Designed for Eclipse Treatment Planning System 13.6+
- Uses StringBuilder for efficient text concatenation
- Implements proper exception handling throughout

### Known Issues
- Refresh button disabled due to ScriptContext lifecycle limitations
- Some advanced ESAPI properties not available in older versions
- Requires manual script rerun to get updated plan data

### Security
- Comprehensive .gitignore to prevent PHI exposure
- Patient data protection measures in place
- No patient information logged or stored

---

## Future Releases

### Planned Features
- DVH analysis integration
- Automated report generation
- Plan comparison functionality
- Custom institutional protocol checks
- Advanced beam arrangement analysis
- Structure nomenclature verification
- Dose constraint checking
- Plan complexity metrics

### Under Consideration
- Export functionality for plan check reports
- Integration with ARIA database
- Multi-plan comparison tools
- Automated plan scoring systems
- Custom warning/error thresholds
- Institution-specific check configurations
