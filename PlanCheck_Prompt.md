# Plan Check Script Implementation Prompt

## Overview
This prompt guides the implementation of a comprehensive plan check script for Eclipse Treatment Planning System (TPS) that incorporates key verification items from both VMAT Check and 3D Check processes.

## Key Check Categories

### 1. Basic Plan Information
- **Plan ID and Status**: Verify plan identification and approval status
- **Dose and Fractions**: Check prescription dose per fraction and total number of fractions
- **Treatment Orientation**: Confirm patient positioning (HFS, HFP, etc.)

### 2. Reference Points and Structures
- **Plan Reference Point**: Ensure plan-specific reference point exists
- **Body Contour**: Verify body structure is present and properly defined
- **Reference Points**: Check for required reference points (ISO, CalcPt, etc.)

### 3. Beam Configuration
- **Setup Fields**: Verify presence and configuration of setup fields
- **Beam Names**: Check beam naming conventions and consistency
- **DRR Names**: Ensure DRR images are properly named and linked
- **MLC Configuration**: Verify MLC settings for IMRT/VMAT plans
- **Isocenter Configuration**: Check for single vs. multiple isocenters

### 4. Dose and Treatment Parameters
- **Dose Limits**: Verify total dose and fractionation
- **Tolerance Tables**: Ensure all treatment beams have appropriate tolerance tables
- **Structure Dose Analysis**: Check D2cc for OARs, D0.1cc for critical structures

### 5. Quality Assurance Requirements
- **QA Scheduling**: Check if IMRT QA is required and scheduled
- **Machine Assignment**: Verify correct treatment machine is assigned
- **Documentation**: Ensure required documents are saved and approved

### 6. Special Considerations
- **DIBH Plans**: Check for breath-hold specific requirements
- **SBRT Plans**: Verify stereotactic treatment parameters
- **Bolus Usage**: Check for bolus requirements and setup

## Implementation Guidelines

### Error Handling
- Use try-catch blocks for all checks to prevent script crashes
- Provide meaningful error messages for troubleshooting
- Continue execution even if individual checks fail

### Status Indicators
- ✓ (Checkmark): Passed check
- ⚠ (Warning): Warning or issue detected
- ℹ (Info): Informational message
- ❌ (Error): Critical error or failure

### Output Format
- Use StringBuilder for efficient string concatenation
- Organize output with clear section headers
- Include plan identification information
- Provide actionable recommendations

## Key Check Items from VMAT Check

1. **TBI Planning Diagram**: Verify planning diagram exists for TBI plans
2. **Plan Diag Rx Name**: Check prescription name matches current prescription
3. **IMRT Collimator**: Ensure no more than 1 IMRT beam has collimator zero
4. **Plan Rx Link**: Verify correct prescription is linked in Aria
5. **Setup Field MLC**: Check setup fields don't have MLC without proper labels
6. **180E**: Verify use of 180E for appropriate plans
7. **Couch integral**: Check couch position limits
8. **Plan Status**: Verify plan approval status
9. **Couch Lat Exceeded**: Check couch lateral position limits
10. **Setup Field Iso**: Ensure only 1 isocenter for setup fields
11. **IMRT One Iso**: Verify single isocenter for IMRT plans
12. **Target Contour**: Check target structure definition
13. **MU Limit**: Verify minimum MU requirements
14. **Body Contour**: Ensure body contour is of type Body
15. **DIBH ARC Length**: Check arc length for DIBH plans
16. **Rx Dose Per Fx**: Verify dose per fraction limits
17. **Triggered Markers**: Check for triggered imaging requirements
18. **Collision Error**: Verify no collision issues
19. **Couch Long**: Check couch longitudinal position limits
20. **Primary RefPoint**: Verify primary reference point
21. **Breath Hold CT**: Check breath-hold CT requirements
22. **CTImageSlices**: Verify correct number of CT slices
23. **SetupFieldsGeometry**: Check setup field geometry
24. **Tolerance Table**: Verify tolerance table assignments
25. **Ref Points Names**: Check reference point naming
26. **RefPoint Dose**: Verify reference point dose
27. **Dose Limits**: Check dose limit compliance
28. **Plan Ref Point**: Verify plan reference point
29. **Prescribed Percentage**: Check prescribed dose percentage
30. **Blocks/Split Fields**: Verify block and split field configuration
31. **Bolus**: Check bolus requirements
32. **Time Stamp**: Verify timestamp accuracy
33. **Intent Status**: Check intent approval status
34. **Dynamic Wedge MU**: Verify dynamic wedge MU
35. **Y1 Y2 Symmetry**: Check collimator symmetry

## Key Check Items from 3D Check

1. **First Segmt Reorder**: Check segment ordering for merged fields
2. **First Segmt Split**: Verify segments match combined aperture
3. **Beam Names**: Check beam naming conventions
4. **DRR Names**: Verify DRR naming consistency
5. **MLC leaves**: Check MLC leaf configuration
6. **180E**: Verify 180E usage
7. **Couch integral**: Check couch position limits
8. **Plan Status**: Verify plan approval status
9. **Couch Lat Exceeded**: Check couch lateral position limits
10. **Setup Field Iso**: Ensure single isocenter for setup fields
11. **IMRT One Iso**: Verify single isocenter for IMRT plans
12. **Target Contour**: Check target structure definition
13. **MU Limit**: Verify minimum MU requirements
14. **Body Contour**: Ensure body contour is of type Body
15. **DIBH ARC Length**: Check arc length for DIBH plans
16. **Rx Dose Per Fx**: Verify dose per fraction limits
17. **Triggered Markers**: Check for triggered imaging requirements
18. **Collision Error**: Verify no collision issues
19. **Couch Long**: Check couch longitudinal position limits
20. **Primary RefPoint**: Verify primary reference point
21. **Breath Hold CT**: Check breath-hold CT requirements
22. **CTImageSlices**: Verify correct number of CT slices
23. **SetupFieldsGeometry**: Check setup field geometry
24. **Tolerance Table**: Verify tolerance table assignments
25. **Ref Points Names**: Check reference point naming
26. **RefPoint Dose**: Verify reference point dose
27. **Dose Limits**: Check dose limit compliance
28. **Plan Ref Point**: Verify plan reference point
29. **Prescribed Percentage**: Check prescribed dose percentage
30. **Blocks/Split Fields**: Verify block and split field configuration
31. **Bolus**: Check bolus requirements
32. **Time Stamp**: Verify timestamp accuracy
33. **Intent Status**: Check intent approval status
34. **Dynamic Wedge MU**: Verify dynamic wedge MU
35. **Y1 Y2 Symmetry**: Check collimator symmetry

## Usage Instructions

1. **Load the Script**: Open the PlanCheck Template.cs in Eclipse Script Editor
2. **Modify as Needed**: Customize checks based on institutional requirements
3. **Test the Script**: Run on test plans to verify functionality
4. **Deploy**: Use in clinical environment for plan verification

## Customization Options

### Adding New Checks
- Create new private methods following the existing pattern
- Add method calls in the main Execute method
- Include proper error handling and status reporting

### Modifying Existing Checks
- Update check logic based on institutional policies
- Adjust warning thresholds and limits
- Modify output format and messaging

### Institutional Specific Requirements
- Add site-specific naming conventions
- Include institutional dose limits
- Incorporate local QA requirements

## Troubleshooting

### Common Issues
1. **Script Crashes**: Check for null references and add proper error handling
2. **Missing Data**: Verify all required plan components are loaded
3. **Performance Issues**: Optimize loops and data access patterns
4. **Output Format**: Ensure StringBuilder is properly configured

### Debugging Tips
- Use MessageBox.Show for debugging output
- Check Eclipse script logs for error messages
- Verify plan data completeness before running checks
- Test with various plan types and configurations

## Conclusion

This comprehensive plan check script provides a robust foundation for automated plan verification in Eclipse TPS. By incorporating key items from both VMAT Check and 3D Check processes, it ensures thorough plan validation while maintaining efficiency and usability.

The modular design allows for easy customization and extension based on institutional requirements and clinical needs. 