# Contributing to Eclipse Plan Check Script

Thank you for your interest in contributing to the Eclipse Plan Check Script! This project aims to improve treatment plan verification workflows for the medical physics community.

## Table of Contents
- [Code of Conduct](#code-of-conduct)
- [How to Contribute](#how-to-contribute)
- [Development Setup](#development-setup)
- [Submission Guidelines](#submission-guidelines)
- [Coding Standards](#coding-standards)
- [Testing](#testing)
- [Documentation](#documentation)

## Code of Conduct

This project follows a code of conduct to ensure a welcoming environment:

- **Be respectful:** Treat all contributors with respect and professionalism
- **Be collaborative:** Work together to improve patient safety and care quality
- **Be patient-focused:** All contributions should prioritize patient safety
- **Follow regulations:** Ensure all contributions comply with medical device regulations
- **Protect PHI:** Never include patient data in contributions

## How to Contribute

### 🐛 Reporting Bugs

1. **Check existing issues** first to avoid duplicates
2. **Use the bug report template** when creating new issues
3. **Include system information:**
   - Eclipse version
   - ESAPI version
   - .NET Framework version
   - Operating system
4. **Provide reproduction steps** with sample (anonymized) data if possible
5. **Include error messages** and log files

### 💡 Suggesting Features

1. **Check the roadmap** to see if it's already planned
2. **Open a feature request** with detailed description
3. **Explain the clinical benefit** and use cases
4. **Consider implementation complexity** and maintenance impact

### 🔧 Code Contributions

1. **Fork the repository**
2. **Create a feature branch** (`git checkout -b feature/amazing-feature`)
3. **Make your changes** following our coding standards
4. **Test thoroughly** with Eclipse
5. **Commit your changes** (`git commit -m 'Add amazing feature'`)
6. **Push to the branch** (`git push origin feature/amazing-feature`)
7. **Open a Pull Request**

## Development Setup

### Prerequisites

- **Visual Studio 2019+** or VS Code with C# extension
- **Eclipse Treatment Planning System** (13.6+)
- **ESAPI SDK** (contact Varian for access)
- **.NET Framework 4.5+**
- **Git** for version control

### Local Development

```bash
# Clone the repository
git clone https://github.com/yourusername/eclipse-plan-check.git
cd eclipse-plan-check

# Create development branch
git checkout -b feature/your-feature-name

# Set up Eclipse script testing environment
# Copy script to Eclipse scripts directory for testing
cp "PlanCheck Template Simple.cs" "\\eclipsefsprd\va_data$\ProgramData\Vision\PublishedScripts\"
```

### Testing Environment

- **Use test patients only** - never use real patient data
- **Test with various plan types:** IMRT, VMAT, 3D-CRT, SRS, SBRT
- **Verify different anatomical sites:** brain, H&N, thorax, abdomen, pelvis
- **Test edge cases:** plans without dose, missing structures, unusual beam arrangements

## Submission Guidelines

### Pull Request Process

1. **Update documentation** for any user-facing changes
2. **Add tests** for new functionality
3. **Ensure compatibility** with supported Eclipse/ESAPI versions
4. **Update CHANGELOG.md** with your changes
5. **Request review** from project maintainers

### Pull Request Checklist

- [ ] Code follows project style guidelines
- [ ] Self-review completed
- [ ] Changes tested in Eclipse environment
- [ ] Documentation updated
- [ ] CHANGELOG.md updated
- [ ] No patient data included
- [ ] Backwards compatibility maintained

## Coding Standards

### C# Style Guidelines

```csharp
// Use descriptive variable names
var treatmentBeams = plan.Beams.Where(b => !b.IsSetupField).ToList();

// Include comprehensive error handling
try
{
    var dosePerFraction = plan.DosePerFraction.Dose;
    sb.AppendLine("✓ Dose per fraction: " + dosePerFraction.ToString("F1") + " cGy");
}
catch (Exception ex)
{
    sb.AppendLine("❌ Error retrieving dose information: " + ex.Message);
}

// Use clear comments for complex logic
// Check for reasonable MU values to identify potential calculation errors
foreach (var beam in treatmentBeams)
{
    if (beam.Meterset.Value > 1000)
    {
        sb.AppendLine("⚠ WARNING: High MU value for beam " + beam.Id);
    }
}
```

### Documentation Standards

- **XML comments** for all public methods
- **Inline comments** for complex algorithms
- **Clear variable names** that explain purpose
- **Error messages** that help users understand issues

### ESAPI Best Practices

- **Always check for null** before accessing ESAPI objects
- **Use try-catch blocks** for property access that might fail
- **Handle version differences** gracefully
- **Dispose resources** properly when applicable

## Testing

### Required Tests

1. **Basic functionality:** Script runs without errors
2. **Multiple plan types:** IMRT, VMAT, 3D-CRT, electrons
3. **Edge cases:** Empty structures, missing dose, unusual beam arrangements
4. **Error handling:** Invalid plans, missing data
5. **UI responsiveness:** Window behavior, button functionality

### Test Data Requirements

- **Use only test patients** with anonymized data
- **Cover various anatomical sites**
- **Include both simple and complex plans**
- **Test approved and unapproved plans**
- **Include plans with common issues**

### Testing Checklist

- [ ] Script compiles without warnings
- [ ] All tabs display correctly
- [ ] Visual indicators work properly (✅ ⚠️ ❌)
- [ ] Eclipse remains functional during script execution
- [ ] Error handling works for edge cases
- [ ] Performance is acceptable for large plans

## Documentation

### Documentation Types

1. **Code documentation:** XML comments and inline comments
2. **User documentation:** README.md updates
3. **API documentation:** Method and property descriptions
4. **Clinical documentation:** Safety considerations and workflows

### Documentation Requirements

- **Update README.md** for user-facing changes
- **Add examples** for new features
- **Include screenshots** for UI changes
- **Document breaking changes** clearly
- **Maintain clinical accuracy**

## Review Process

### Review Criteria

1. **Clinical safety:** Does this improve or maintain patient safety?
2. **Code quality:** Is the code well-written and maintainable?
3. **Compatibility:** Does this work with supported Eclipse versions?
4. **Documentation:** Are changes properly documented?
5. **Testing:** Has this been adequately tested?

### Review Timeline

- **Initial review:** Within 1 week
- **Follow-up reviews:** Within 3 days
- **Final approval:** After all feedback addressed

## Getting Help

### Resources

- **Eclipse ESAPI Documentation:** Contact Varian for official documentation
- **Project Issues:** Use GitHub issues for questions
- **Medical Physics Community:** AAPM forums and conferences
- **Code Questions:** Tag maintainers in GitHub discussions

### Contact

- **Project Maintainers:** Tag @maintainers in issues
- **Medical Physics Questions:** Consult with qualified medical physicists
- **Technical Issues:** Create detailed GitHub issues

---

## Recognition

Contributors will be:
- Listed in the project README
- Acknowledged in release notes
- Invited to join the maintainer team for significant contributions

Thank you for helping improve treatment plan verification for the medical physics community! 🏥
