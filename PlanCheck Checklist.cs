/*
 * Eclipse Plan Verification Checklist Script
 * ==========================================
 * 
 * Purpose: Comprehensive treatment plan verification tool for Eclipse TPS
 * Author: Plan Check Automation Team
 * Version: 1.0.0.1
 * 
 * Description:
 * This script provides a comprehensive verification checklist for treatment plans
 * in Eclipse. It performs automated checks across multiple domains including:
 * - Plan information and prescription details
 * - Dose calculation and normalization
 * - Beam parameters and delivery verification
 * - Structure analysis and artifact detection
 * - Isocenter positioning and coordinate verification
 * - Plan approval status and safety checks
 * 
 * The tool presents results in a tabbed interface for efficient review during
 * plan approval workflows, with specific alerts for critical safety issues.
 * 
 * Dependencies: VMS.TPS.Common.Model.API (Eclipse Scripting API)
 * Compatibility: Eclipse 15.6+ (C# 4.0 compatible syntax)
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;

[assembly: AssemblyVersion("1.0.0.1")]

namespace VMS.TPS
{
    /// <summary>
    /// Main script entry point for Eclipse Plan Verification Checklist
    /// This class is instantiated by the Eclipse scripting engine
    /// </summary>
    public class Script
    {
        public Script()
        {
        }

        /// <summary>
        /// Main execution method called by Eclipse scripting engine
        /// Creates and displays the plan verification window
        /// </summary>
        /// <param name="context">Eclipse script context containing patient, course, and plan data</param>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Execute(ScriptContext context)
        {
            try
            {
                // Validate that a plan is currently loaded in Eclipse
                if (context.PlanSetup == null)
                {
                    MessageBox.Show("No plan is currently loaded. Please load a plan and try again.", 
                                  "Plan Check Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Create and show the plan verification window
                // Using Show() instead of ShowDialog() allows Eclipse to remain usable
                var planCheckWindow = new PlanCheckWindow(context);
                planCheckWindow.Show();
                
                // Script execution completes, but window remains open independently
            }
            catch (Exception ex)
            {
                // Catch any unhandled exceptions and display user-friendly error message
                MessageBox.Show("Script execution error: " + ex.ToString(), 
                              "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    /// <summary>
    /// Main window class for the Plan Verification Checklist application
    /// Creates a tabbed interface displaying comprehensive plan verification results
    /// </summary>
    public class PlanCheckWindow : Window
    {
        #region Private Fields
        
        /// <summary>Eclipse script context containing patient, course, and plan data</summary>
        private ScriptContext scriptContext;
        
        /// <summary>Patient ID for display in window title and headers</summary>
        private string patientId;
        
        /// <summary>Course ID for display in window title and headers</summary>
        private string courseId;
        
        /// <summary>Plan ID for display in window title and headers</summary>
        private string planId;
        
        /// <summary>Main tab control containing verification results</summary>
        private TabControl tabControl;
        
        /// <summary>Dictionary mapping tab names to their corresponding TextBox controls for content display</summary>
        private Dictionary<string, TextBox> tabTextBoxes = new Dictionary<string, TextBox>();
        
        #endregion

        #region Constructor
        
        /// <summary>
        /// Initializes a new instance of the PlanCheckWindow
        /// </summary>
        /// <param name="scriptContext">Eclipse script context with plan data</param>
        public PlanCheckWindow(ScriptContext scriptContext)
        {
            // Store script context and extract key identifiers
            this.scriptContext = scriptContext;
            this.patientId = scriptContext.Patient.Id;
            this.courseId = scriptContext.Course.Id;
            this.planId = scriptContext.PlanSetup.Id;
            
            // Initialize the user interface
            InitializeComponents();
            
            // Perform plan verification and populate results
            RunPlanCheck();
        }
        
        #endregion

        #region UI Initialization Methods
        
        /// <summary>
        /// Initializes all window components including layout, header, tabs, and buttons
        /// Creates a three-row grid layout: Header | Tabs | Buttons
        /// </summary>
        private void InitializeComponents()
        {
            // Configure main window properties
            this.Title = "Plan Check - " + planId;
            this.Width = 900;
            this.Height = 700;
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            this.Background = Brushes.WhiteSmoke;
            this.Topmost = true; // Keep window on top of all other windows

            // Create main grid layout with three rows
            var mainGrid = new Grid();
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });         // Header row
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Content tabs (expandable)
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });         // Button row
            this.Content = mainGrid;

            // Build UI components in order
            CreateHeader(mainGrid);        // Patient/plan identification header
            CreateTabbedContent(mainGrid); // Verification result tabs
            CreateActionButtons(mainGrid); // Close/refresh buttons
        }

        /// <summary>
        /// Creates the application header with title, patient information, and usage instructions
        /// Displays in the top row of the main grid
        /// </summary>
        /// <param name="mainGrid">Main grid container</param>
        private void CreateHeader(Grid mainGrid)
        {
            // Create header container with light blue background
            var headerPanel = new StackPanel
            {
                Background = Brushes.LightBlue,
                Margin = new Thickness(0)
            };

            // Main application title
            var titleLabel = new Label
            {
                Content = "Eclipse Plan Verification Checklist",
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                Background = Brushes.Transparent
            };

            // Patient, course, and plan identification
            var patientInfoLabel = new Label
            {
                Content = "Patient: " + patientId + " | Course: " + courseId + " | Plan: " + planId,
                FontSize = 20,
                HorizontalAlignment = HorizontalAlignment.Center,
                Background = Brushes.Transparent
            };

            // Usage instruction (refresh behavior)
            var instructionLabel = new Label
            {
                Content = "⚠️ Note: To get updated information, please close this window and rerun the script.",
                FontSize = 20,
                FontStyle = FontStyles.Italic,
                Foreground = Brushes.DarkRed,
                HorizontalAlignment = HorizontalAlignment.Center,
                Background = Brushes.Transparent,
                Margin = new Thickness(0, 0, 0, 5)
            };

            // Add labels to header panel in order
            headerPanel.Children.Add(titleLabel);
            headerPanel.Children.Add(patientInfoLabel);
            headerPanel.Children.Add(instructionLabel);

            // Position header in top row of main grid
            Grid.SetRow(headerPanel, 0);
            mainGrid.Children.Add(headerPanel);
        }

        private void CreateTabbedContent(Grid mainGrid)
        {
            tabControl = new TabControl
            {
                Background = Brushes.White,
                Margin = new Thickness(10)
            };

            string[] tabs = { "Plan Info", "Dose", "Beams", "Structures", "Isocenter", "Status" };
            
            foreach (string tabName in tabs)
            {
                var tabItem = new TabItem
                {
                    Header = tabName,
                    FontSize = 20,
                    FontWeight = FontWeights.Bold,
                    Background = Brushes.LightGray
                };

                var scrollViewer = new ScrollViewer
                {
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                    HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                    Background = Brushes.White
                };

                var textBox = new TextBox
                {
                    FontFamily = new FontFamily("Consolas"),
                    FontSize = 16,
                    FontWeight = FontWeights.Normal,
                    Background = Brushes.White,
                    BorderThickness = new Thickness(0),
                    Padding = new Thickness(15),
                    TextWrapping = TextWrapping.Wrap,
                    AcceptsReturn = true,
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                    HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                    IsReadOnly = true,
                    Text = ""
                };

                scrollViewer.Content = textBox;
                tabItem.Content = scrollViewer;

                tabControl.Items.Add(tabItem);
                tabTextBoxes[tabName] = textBox;
            }

            Grid.SetRow(tabControl, 1);
            mainGrid.Children.Add(tabControl);
        }

        private void CreateActionButtons(Grid mainGrid)
        {
            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(10)
            };

            var refreshButton = new Button
            {
                Content = "Rerun Script for Updates",
                Width = 180,
                Height = 30,
                Margin = new Thickness(5),
                IsEnabled = false // Disabled because ScriptContext is disposed after script exits
            };

            var closeButton = new Button
            {
                Content = "Close",
                Width = 80,
                Height = 30,
                Margin = new Thickness(5)
            };
            closeButton.Click += (sender, e) => this.Close();

            buttonPanel.Children.Add(refreshButton);
            buttonPanel.Children.Add(closeButton);

            Grid.SetRow(buttonPanel, 2);
            mainGrid.Children.Add(buttonPanel);
        }

        private void RunPlanCheck()
        {
            try
            {
                var plan = scriptContext.PlanSetup;
                
                UpdateTab("Plan Info", plan);
                UpdateTab("Dose", plan);
                UpdateTab("Beams", plan);
                UpdateTab("Structures", plan);
                UpdateTab("Isocenter", plan);
                UpdateTab("Status", plan);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error running plan check: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateTab(string tabName, PlanSetup plan)
        {
            var sb = new StringBuilder();

            switch (tabName)
            {
                case "Plan Info":
                    CheckPlanInformation(plan, sb);
                    break;
                case "Dose":
                    CheckDoseInformation(plan, sb);
                    break;
                case "Beams":
                    CheckBeamInformation(plan, sb);
                    break;
                case "Structures":
                    CheckStructureInformation(plan, sb);
                    break;
                case "Isocenter":
                    CheckIsocenterInformation(plan, sb);
                    break;
                case "Status":
                    CheckPlanStatus(plan, sb);
                    break;
            }

            TextBox textBox = null;
            if (tabTextBoxes.TryGetValue(tabName, out textBox))
            {
                textBox.Text = FormatTextForDisplay(sb.ToString());
            }
        }

        private string FormatTextForDisplay(string text)
        {
            var lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.None);
            var formattedLines = new List<string>();
            
            foreach (var line in lines)
            {
                // Check if line is a main section title (numbered sections)
                if (line.Contains(". ") && (line.Contains("INFORMATION:") || line.Contains("STATUS")))
                {
                    formattedLines.Add("═══ " + line.ToUpper() + " ═══");
                }
                // Check if line is a subsection title (contains specific keywords)
                else if (line.Contains("CHECKLIST:") || line.Contains("REQUIREMENTS:") || 
                         line.Contains("VERIFICATION:") || line.Contains("ASSESSMENT:") || 
                         line.Contains("ANALYSIS:") || line.Contains("DISTRIBUTION:") ||
                         line.Contains("DETAILS:") || line.Contains("SUMMARY:"))
                {
                    formattedLines.Add("*** " + line + " ***");
                }
                // Check for icon-based section headers
                else if ((line.Contains("💊") || line.Contains("🎯") || line.Contains("🔍") ||
                         line.Contains("📋") || line.Contains("🛡️") || line.Contains("🏭") ||
                         line.Contains("📊") || line.Contains("📷") || line.Contains("🖼️") ||
                         line.Contains("🛏️") || line.Contains("📍")) && 
                         (line.Contains(":") && line.Length < 100))
                {
                    formattedLines.Add(">>> " + line + " <<<");
                }
                else
                {
                    formattedLines.Add(line);
                }
            }
            
            return string.Join(Environment.NewLine, formattedLines);
        }

        #endregion

        #region Plan Verification Methods
        
        /// <summary>
        /// Analyzes plan information, prescription details, and clinical parameters
        /// 
        /// Verification Areas:
        /// - Basic plan metadata (ID, name, status, orientation)
        /// - Course and patient information
        /// - Prescription details (dose, fractions, normalization)
        /// - SiB (Simultaneous Integrated Boost) detection
        /// - Mosaiq prescription checklist with auto-populated values
        /// - Treatment technique classification (VMAT, IMRT, 3D-CRT)
        /// - Breathing motion management detection
        /// - Patient positioning and laterality analysis
        /// 
        /// Critical Safety Checks:
        /// - Validates prescription parameters
        /// - Detects non-standard patient positioning
        /// - Identifies breathing motion compensation requirements
        /// - Analyzes laterality for potential field labeling issues
        /// </summary>
        /// <param name="plan">Treatment plan to analyze</param>
        /// <param name="sb">StringBuilder for output formatting</param>
        private void CheckPlanInformation(PlanSetup plan, StringBuilder sb)
        {
            sb.AppendLine("1. PLAN INFORMATION:");
            sb.AppendLine("====================================");
            
            try
            {
                // Basic Plan Information
                sb.AppendLine("✓ Plan ID: " + plan.Id);
                sb.AppendLine("✓ Plan Name: " + (plan.Name ?? "N/A"));
                sb.AppendLine("✓ Plan Status: " + plan.ApprovalStatus.ToString());
                sb.AppendLine("✓ Treatment Orientation: " + plan.TreatmentOrientation.ToString());
                sb.AppendLine("✓ Plan Type: " + plan.PlanType.ToString());
                
                // Course and Patient Information
                if (plan.Course != null)
                {
                    sb.AppendLine("✓ Course ID: " + plan.Course.Id);
                    if (plan.Course.Patient != null)
                    {
                        sb.AppendLine("✓ Patient ID: " + plan.Course.Patient.Id);
                        sb.AppendLine("✓ Patient Name: " + plan.Course.Patient.Name);
                        if (plan.Course.Patient.DateOfBirth != null)
                        {
                            sb.AppendLine("✓ Date of Birth: " + plan.Course.Patient.DateOfBirth.ToString());
                        }
                    }
                }
                
                // Plan Dates
                if (plan.PlanningApprovalDate != null)
                {
                    sb.AppendLine("✓ Planning Approval Date: " + plan.PlanningApprovalDate.ToString());
                }
                
                if (plan.TreatmentApprovalDate != null)
                {
                    sb.AppendLine("✓ Treatment Approval Date: " + plan.TreatmentApprovalDate.ToString());
                }
                
                // Prescription Information with actual data
                sb.AppendLine();
                sb.AppendLine("💊 PRESCRIPTION DETAILS:");
                sb.AppendLine("========================");
                
                if (plan.DosePerFraction != null)
                {
                    sb.AppendLine("✓ Dose Per Fraction: " + plan.DosePerFraction.Dose.ToString("F1") + " cGy");
                }
                
                if (plan.NumberOfFractions != null)
                {
                    sb.AppendLine("✓ Number of Fractions: " + plan.NumberOfFractions.ToString());
                }
                
                if (plan.TotalDose != null)
                {
                    sb.AppendLine("✓ Total Dose: " + plan.TotalDose.Dose.ToString("F1") + " cGy (" + (plan.TotalDose.Dose / 100).ToString("F1") + " Gy)");
                }
                
                // Plan Normalization Information
                if (plan.PlanNormalizationValue != 0)
                {
                    sb.AppendLine("✓ Plan Normalization: " + plan.PlanNormalizationValue.ToString("F1") + "%");
                }
                
                if (plan.PlanNormalizationMethod != null)
                {
                    sb.AppendLine("✓ Normalization Method: " + plan.PlanNormalizationMethod.ToString());
                }
                
                // Check for SiB (Simultaneous Integrated Boost)
                bool sibDetected = false;
                if (plan.StructureSet != null)
                {
                    var structures = plan.StructureSet.Structures;
                    var ptvStructures = structures.Where(s => s.DicomType == "PTV" || s.Id.ToUpper().Contains("PTV")).ToList();
                    
                    if (ptvStructures.Count > 1)
                    {
                        sibDetected = true;
                        sb.AppendLine("⚠️ MULTIPLE PTV DETECTED - Possible SiB Plan:");
                        foreach (var ptv in ptvStructures)
                        {
                            sb.AppendLine("   • " + ptv.Id + " (Volume: " + ptv.Volume.ToString("F1") + " cm³)");
                        }
                    }
                }
                
                sb.AppendLine();
                sb.AppendLine("📋 MOSAIQ PRESCRIPTION CHECKLIST:");
                sb.AppendLine("================================");
                
                // Extract beam information for modality
                var beams = plan.Beams.Where(b => !b.IsSetupField).ToList();
                var energies = new List<string>();
                var modalities = new List<string>();
                
                foreach (var beam in beams)
                {
                    if (!energies.Contains(beam.EnergyModeDisplayName))
                        energies.Add(beam.EnergyModeDisplayName);
                    
                    string modality = beam.EnergyModeDisplayName.Contains("E") ? "Electrons" : "Photons";
                    if (!modalities.Contains(modality))
                        modalities.Add(modality);
                }
                
                // Determine technique based on actual plan characteristics
                string technique = "Unknown";
                var techniqueAnalysis = new List<string>();
                
                // Check for SRS/SBRT first
                if (plan.PlanType.ToString().Contains("SRS") || plan.Id.ToUpper().Contains("SRS"))
                {
                    technique = "SRS";
                    techniqueAnalysis.Add("SRS detected from plan type/ID");
                }
                else if (plan.PlanType.ToString().Contains("SBRT") || plan.Id.ToUpper().Contains("SBRT"))
                {
                    technique = "SBRT";
                    techniqueAnalysis.Add("SBRT detected from plan type/ID");
                }
                else
                {
                    // Analyze beam characteristics
                    bool hasVMATBeams = false;
                    bool hasIMRTBeams = false;
                    bool hasStaticBeams = false;
                    int arcBeamCount = 0;
                    int staticBeamCount = 0;
                    
                    foreach (var beam in beams)
                    {
                        // Check beam energy/modality first
                        string energyMode = beam.EnergyModeDisplayName.ToUpper();
                        string mlcPlanType = beam.MLCPlanType.ToString().ToUpper();
                        
                        // Handle Electron and Proton beams
                        if (energyMode.Contains("E") && (energyMode.Contains("MEV") || char.IsDigit(energyMode[energyMode.Length-1])))
                        {
                            // Electron beam (e.g., "6E", "9E", "12E", "15E", "18E", "6MeV", etc.)
                            staticBeamCount++;
                            hasStaticBeams = true; // Electrons are always static fields
                        }
                        else if (energyMode.Contains("PROTON") || energyMode.Contains("P") || mlcPlanType.Contains("PROTON"))
                        {
                            // Proton beam
                            staticBeamCount++;
                            hasStaticBeams = true; // Protons are typically static fields
                        }
                        else
                        {
                            // Photon beams - determine technique based on MLC plan type
                            if (mlcPlanType.Contains("VMAT"))
                            {
                                arcBeamCount++;
                                hasVMATBeams = true;
                            }
                            else if (mlcPlanType.Contains("IMRT"))
                            {
                                staticBeamCount++;
                                hasIMRTBeams = true;
                            }
                            else if (mlcPlanType.Contains("STATIC") || 
                                     mlcPlanType.Contains("ARC") || 
                                     mlcPlanType.Contains("DYNAMIC") ||
                                     mlcPlanType.Contains("CONFORMALSTATICANGLE") ||
                                     mlcPlanType.Contains("CONFORMALARC"))
                            {
                                if (mlcPlanType.Contains("ARC"))
                                {
                                    arcBeamCount++;
                                }
                                else
                                {
                                    staticBeamCount++;
                                }
                                hasStaticBeams = true;
                            }
                            else
                            {
                                // Default all other types to 3D-CRT static beams
                                staticBeamCount++;
                                hasStaticBeams = true;
                            }
                        }
                    }
                    
                    // Check for electron or proton beams first
                    var energyModes = beams.Select(b => b.EnergyModeDisplayName.ToUpper()).ToList();
                    bool hasElectronBeams = energyModes.Any(e => e.Contains("E") && (e.Contains("MEV") || char.IsDigit(e[e.Length-1])));
                    bool hasProtonBeams = energyModes.Any(e => e.Contains("PROTON") || e.Contains("P"));
                    
                    // Determine technique based on analysis
                    if (hasElectronBeams && hasProtonBeams)
                    {
                        technique = "Electron + Proton";
                        techniqueAnalysis.Add("Mixed modality: Electron and Proton beams detected");
                    }
                    else if (hasElectronBeams)
                    {
                        technique = "Electron";
                        techniqueAnalysis.Add("Electron beam therapy detected");
                        var electronEnergies = energyModes.Where(e => e.Contains("E")).Distinct();
                        techniqueAnalysis.Add("Electron energies: " + string.Join(", ", electronEnergies));
                    }
                    else if (hasProtonBeams)
                    {
                        technique = "Proton";
                        techniqueAnalysis.Add("Proton beam therapy detected");
                    }
                    else if (hasVMATBeams)
                    {
                        technique = "VMAT";
                        techniqueAnalysis.Add("Arc beams detected: " + arcBeamCount + " arc beam(s)");
                        if (hasIMRTBeams || hasStaticBeams)
                        {
                            techniqueAnalysis.Add("Mixed technique: VMAT + static beams");
                        }
                    }
                    else if (hasIMRTBeams)
                    {
                        technique = "IMRT";
                        techniqueAnalysis.Add("IMRT beams detected: MLC-based intensity modulation");
                    }
                    else if (hasStaticBeams)
                    {
                        technique = "3D-CRT";
                        techniqueAnalysis.Add("Static beams detected: " + staticBeamCount + " static beam(s)");
                    }
                    else
                    {
                        technique = "3D-CRT"; // Default fallback for photon beams
                        techniqueAnalysis.Add("Defaulted to 3D-CRT - static beam configuration");
                    }
                    
                    // Additional analysis based on beam count and naming
                    if (beams.Count == 1 && hasVMATBeams)
                    {
                        techniqueAnalysis.Add("Single arc VMAT");
                    }
                    else if (beams.Count == 2 && hasVMATBeams)
                    {
                        techniqueAnalysis.Add("Dual arc VMAT");
                    }
                    else if (beams.Count > 2 && hasVMATBeams)
                    {
                        techniqueAnalysis.Add("Multi-arc VMAT (" + arcBeamCount + " arcs)");
                    }
                }
                
                sb.AppendLine("SUGGESTED VALUES (verify and enter in Mosaiq):");
                sb.AppendLine("□ Rx Site: " + (plan.StructureSet != null ? plan.StructureSet.Id : "____________________"));
                sb.AppendLine("□ Technique: " + technique);
                
                // Show technique analysis
                if (techniqueAnalysis.Count > 0)
                {
                    sb.AppendLine();
                    sb.AppendLine("🔍 TECHNIQUE ANALYSIS:");
                    foreach (var analysis in techniqueAnalysis)
                    {
                        sb.AppendLine("   • " + analysis);
                    }
                }
                sb.AppendLine("□ Modality: " + string.Join("/", modalities));
                sb.AppendLine("□ Energy: " + string.Join(", ", energies));
                sb.AppendLine("□ Rx Dose: " + (plan.TotalDose != null ? plan.TotalDose.Dose.ToString("F1") + " cGy" : "____________________"));
                sb.AppendLine("□ Fractionation Dose: " + (plan.DosePerFraction != null ? plan.DosePerFraction.Dose.ToString("F1") + " cGy" : "____________________"));
                sb.AppendLine("□ Number of Fractions: " + (plan.NumberOfFractions != null ? plan.NumberOfFractions.ToString() : "____________________"));
                sb.AppendLine("□ SiB Status: " + (sibDetected ? "YES - Multiple PTV detected" : "NO - Single target"));
                sb.AppendLine("□ Pattern: Daily");
                
                sb.AppendLine();
                sb.AppendLine("✅ VERIFICATION CHECKLIST:");
                sb.AppendLine("□ Rx Site matches treatment area");
                sb.AppendLine("□ Technique correctly selected");
                sb.AppendLine("□ Modality matches beam energies");
                sb.AppendLine("□ Total dose entered correctly");
                sb.AppendLine("□ Dose per fraction matches");
                sb.AppendLine("□ Number of fractions correct");
                sb.AppendLine("□ SiB designation verified if applicable");
                sb.AppendLine("□ Pattern (Daily/Weekly) selected");
                
                sb.AppendLine();
                sb.AppendLine("💊 BREATHING MOTION MANAGEMENT:");
                sb.AppendLine("===============================");
                
                // Enhanced breathing management detection
                bool breathingCompensationDetected = false;
                var breathingIndicators = new List<string>();
                string planNameUpper = plan.Name != null ? plan.Name.ToUpper() : "";
                string planIdUpper = plan.Id.ToUpper();
                
                // Check plan ID/name for breathing indicators
                if (planIdUpper.Contains("ABC") || planNameUpper.Contains("ABC"))
                {
                    breathingCompensationDetected = true;
                    breathingIndicators.Add("ABC (Active Breathing Coordinator) detected in plan name/ID");
                }
                if (planIdUpper.Contains("IABC") || planNameUpper.Contains("IABC"))
                {
                    breathingCompensationDetected = true;
                    breathingIndicators.Add("iABC (inhale ABC) specifically detected");
                }
                if (planIdUpper.Contains("EABC") || planNameUpper.Contains("EABC"))
                {
                    breathingCompensationDetected = true;
                    breathingIndicators.Add("eABC (exhale ABC) specifically detected");
                }
                if (planIdUpper.Contains("BREATH") || planNameUpper.Contains("BREATH") || 
                    planIdUpper.Contains("BH") || planNameUpper.Contains("BH"))
                {
                    breathingCompensationDetected = true;
                    breathingIndicators.Add("Breath Hold technique detected");
                }
                if (planIdUpper.Contains("FB") || planNameUpper.Contains("FB"))
                {
                    breathingIndicators.Add("Free Breathing (FB) detected in plan name/ID");
                }
                
                // Check image information for 4DCT
                if (plan.StructureSet != null && plan.StructureSet.Image != null)
                {
                    string imageId = plan.StructureSet.Image.Id.ToUpper();
                    if (imageId.Contains("4D") || imageId.Contains("AVG") || imageId.Contains("AVERAGE"))
                    {
                        breathingCompensationDetected = true;
                        breathingIndicators.Add("4DCT or averaged CT detected: " + plan.StructureSet.Image.Id);
                    }
                }
                
                if (breathingCompensationDetected)
                {
                    sb.AppendLine("⚠️ BREATHING MOTION MANAGEMENT DETECTED:");
                    foreach (var indicator in breathingIndicators)
                    {
                        sb.AppendLine("   • " + indicator);
                    }
                    sb.AppendLine();
                    sb.AppendLine("📋 BREATHING TECHNIQUE VERIFICATION:");
                    sb.AppendLine("   □ iABC (inhale Active Breathing Coordinator)");
                    sb.AppendLine("   □ eABC (exhale Active Breathing Coordinator)");
                    sb.AppendLine("   □ Breath Hold technique");
                    sb.AppendLine("   □ 4DCT with motion management");
                    sb.AppendLine("   □ Technique documented in Mosaiq");
                    sb.AppendLine("   □ Motion management instructions clear");
                    sb.AppendLine("   □ Patient coaching protocol established");
                }
                else
                {
                    sb.AppendLine("✓ No specific breathing management indicators detected");
                    sb.AppendLine("   Verify breathing technique used:");
                    sb.AppendLine("   □ Standard free breathing simulation");
                    sb.AppendLine("   □ No breathing motion compensation needed");
                    sb.AppendLine("   □ Patient able to maintain position consistently");
                }
                
                sb.AppendLine();
                sb.AppendLine("🎯 PATIENT POSITIONING & LATERALITY:");
                sb.AppendLine("====================================");
                
                // Enhanced patient positioning analysis
                string patientOrientation = plan.TreatmentOrientation.ToString().ToUpper();
                sb.AppendLine("✓ Treatment Orientation: " + plan.TreatmentOrientation.ToString());
                
                // Detailed positioning analysis
                var positioningAnalysis = new List<string>();
                bool specialPositioning = false;
                var positionAlerts = new List<string>();
                
                if (patientOrientation.Contains("PRONE"))
                {
                    specialPositioning = true;
                    positionAlerts.Add("PRONE positioning detected");
                    positionAlerts.Add("Verify field labels for anterior/posterior orientation");
                    positionAlerts.Add("Check patient support equipment compatibility");
                }
                
                if (patientOrientation.Contains("FEET") && patientOrientation.Contains("FIRST"))
                {
                    specialPositioning = true;
                    positionAlerts.Add("FEET FIRST positioning detected");
                    positionAlerts.Add("Verify field labels and setup field labels");
                    positionAlerts.Add("Confirm gantry rotation directions");
                }
                
                if (patientOrientation.Contains("DECUB"))
                {
                    specialPositioning = true;
                    positionAlerts.Add("DECUBITUS positioning detected");
                    positionAlerts.Add("Verify field labels and patient setup");
                    positionAlerts.Add("Check for pressure point management");
                }
                
                // Check for laterality in plan/structure names
                var lateralityIndicators = new List<string>();
                if (plan.Name != null && (plan.Name.ToUpper().Contains("LEFT") || plan.Name.ToUpper().Contains("RIGHT")))
                {
                    lateralityIndicators.Add("Laterality in plan name: " + plan.Name);
                }
                if (plan.Id.ToUpper().Contains("LEFT") || plan.Id.ToUpper().Contains("RIGHT") ||
                    plan.Id.ToUpper().Contains("LT") || plan.Id.ToUpper().Contains("RT"))
                {
                    lateralityIndicators.Add("Laterality in plan ID: " + plan.Id);
                }
                
                // Check structures for laterality
                if (plan.StructureSet != null)
                {
                    var lateralStructures = plan.StructureSet.Structures.Where(s =>
                        s.Id.ToUpper().Contains("LEFT") || s.Id.ToUpper().Contains("RIGHT") ||
                        s.Id.ToUpper().Contains("_L") || s.Id.ToUpper().Contains("_R") ||
                        s.Id.ToUpper().Contains("LT_") || s.Id.ToUpper().Contains("RT_")).ToList();
                    
                    if (lateralStructures.Count > 0)
                    {
                        lateralityIndicators.Add("Lateral structures found: " + lateralStructures.Count + " structures");
                    }
                }
                
                if (lateralityIndicators.Count > 0)
                {
                    sb.AppendLine();
                    sb.AppendLine("🔍 LATERALITY ANALYSIS:");
                    foreach (var indicator in lateralityIndicators)
                    {
                        sb.AppendLine("   • " + indicator);
                    }
                    sb.AppendLine("   □ Verify laterality matches clinical target");
                    sb.AppendLine("   □ Confirm field labels indicate correct side");
                    sb.AppendLine("   □ Check setup instructions specify laterality");
                }
                
                if (specialPositioning)
                {
                    sb.AppendLine();
                    sb.AppendLine("⚠️ SPECIAL POSITIONING DETECTED:");
                    foreach (var alert in positionAlerts)
                    {
                        sb.AppendLine("   • " + alert);
                    }
                    sb.AppendLine();
                    sb.AppendLine("📋 POSITIONING VERIFICATION CHECKLIST:");
                    sb.AppendLine("   □ Field labels reviewed and correct for positioning");
                    sb.AppendLine("   □ Setup field labels appropriate for orientation");
                    sb.AppendLine("   □ Patient positioning matches simulation");
                    sb.AppendLine("   □ Immobilization devices compatible with positioning");
                    if (lateralityIndicators.Count > 0)
                    {
                        sb.AppendLine("   □ Special positioning instructions documented");
                    }
                }
                else
                {
                    sb.AppendLine("✓ Standard positioning detected");
                    sb.AppendLine("   □ Verify positioning matches simulation setup");
                }
                
            }
            catch (Exception ex)
            {
                sb.AppendLine("❌ Error retrieving plan information: " + ex.Message);
            }
        }

        private void CheckDoseInformation(PlanSetup plan, StringBuilder sb)
        {
            sb.AppendLine("2. DOSE INFORMATION:");
            sb.AppendLine("====================================");
            
            try
            {
                // Prescription Information
                if (plan.DosePerFraction != null)
                {
                    sb.AppendLine("✓ Dose Per Fraction: " + plan.DosePerFraction.Dose.ToString("F1") + " cGy");
                }
                
                if (plan.NumberOfFractions != null)
                {
                    sb.AppendLine("✓ Number of Fractions: " + plan.NumberOfFractions.ToString());
                }
                
                if (plan.TotalDose != null)
                {
                    sb.AppendLine("✓ Total Dose: " + plan.TotalDose.Dose.ToString("F1") + " cGy");
                }

                // Dose Calculation Status
                if (plan.Dose != null)
                {
                    sb.AppendLine("✓ Dose Distribution: Calculated");
                    sb.AppendLine("✓ Dose Grid Resolution: " + plan.Dose.XRes.ToString("F1") + " x " + plan.Dose.YRes.ToString("F1") + " x " + plan.Dose.ZRes.ToString("F1") + " mm");
                    sb.AppendLine("✓ Dose Grid Size: " + plan.Dose.XSize + " x " + plan.Dose.YSize + " x " + plan.Dose.ZSize + " voxels");
                }
                else
                {
                    sb.AppendLine("❌ ERROR: No dose calculation available");
                }
                
                // Algorithm Information
                if (plan.PhotonCalculationModel != null)
                {
                    sb.AppendLine("✓ Photon Calculation Model: " + plan.PhotonCalculationModel);
                }
                
                if (plan.ElectronCalculationModel != null)
                {
                    sb.AppendLine("✓ Electron Calculation Model: " + plan.ElectronCalculationModel);
                }
                
                // Plan Normalization
                if (plan.PlanNormalizationValue != 0)
                {
                    sb.AppendLine("✓ Plan Normalization: " + plan.PlanNormalizationValue.ToString("F1") + "%");
                }
                
                if (plan.PlanNormalizationMethod != null)
                {
                    sb.AppendLine("✓ Normalization Method: " + plan.PlanNormalizationMethod.ToString());
                }
                
                // Enhanced Target Structure Analysis with SiB Detection
                if (plan.StructureSet != null && plan.Dose != null)
                {
                    sb.AppendLine();
                    sb.AppendLine("🎯 TARGET STRUCTURE DOSE ANALYSIS:");
                    sb.AppendLine("==================================");
                    
                    var structures = plan.StructureSet.Structures.ToList();
                    var ptvs = structures.Where(s => s.DicomType == "PTV" || s.Id.ToUpper().Contains("PTV")).ToList();
                    var ctvs = structures.Where(s => s.DicomType == "CTV" || s.Id.ToUpper().Contains("CTV")).ToList();
                    var gtvs = structures.Where(s => s.DicomType == "GTV" || s.Id.ToUpper().Contains("GTV")).ToList();
                    var itvs = structures.Where(s => s.DicomType == "ITV" || s.Id.ToUpper().Contains("ITV")).ToList();
                    var allTargets = ptvs.Concat(ctvs).Concat(gtvs).Concat(itvs).ToList();
                    
                    sb.AppendLine("✓ Total Target Structures Found: " + allTargets.Count);
                    sb.AppendLine("   • PTVs: " + ptvs.Count);
                    sb.AppendLine("   • CTVs: " + ctvs.Count);
                    sb.AppendLine("   • GTVs: " + gtvs.Count);
                    sb.AppendLine("   • ITVs: " + itvs.Count);
                    
                    // SiB Detection and Alert
                    if (ptvs.Count > 1)
                    {
                        sb.AppendLine();
                        sb.AppendLine("🚨 MULTIPLE PTV DETECTED - POSSIBLE SiB (SIMULTANEOUS INTEGRATED BOOST) PLAN!");
                        sb.AppendLine("================================================================");
                        sb.AppendLine("⚠️ VERIFY: Multiple dose levels in single plan");
                        sb.AppendLine("⚠️ CONFIRM: Proper dose prescription for each PTV");
                        sb.AppendLine("⚠️ CHECK: Mosaiq setup for SiB technique");
                        sb.AppendLine();
                    }
                    
                    // Comprehensive Target Structure V95% Analysis with Individual Prescription Detection
                    if (allTargets.Any())
                    {
                        sb.AppendLine("📊 TARGET STRUCTURE V95% DOSE COVERAGE:");
                        sb.AppendLine("======================================");
                        
                        // Sort targets by type for organized display
                        var sortedTargets = ptvs.Concat(ctvs).Concat(gtvs).Concat(itvs).ToList();
                        
                        foreach (var target in sortedTargets.Take(10))
                        {
                            try
                            {
                                if (!target.IsEmpty)
                                {
                                    var targetMaxDose = plan.GetDoseAtVolume(target, 0.0, VolumePresentation.Relative, DoseValuePresentation.Absolute);
                                    var meanDose = plan.GetDVHCumulativeData(target, DoseValuePresentation.Absolute, VolumePresentation.Relative, 1.0).MeanDose;
                                    
                                    // Structure type identification
                                    string structType = "";
                                    if (target.DicomType == "PTV" || target.Id.ToUpper().Contains("PTV")) structType = "PTV";
                                    else if (target.DicomType == "CTV" || target.Id.ToUpper().Contains("CTV")) structType = "CTV";
                                    else if (target.DicomType == "GTV" || target.Id.ToUpper().Contains("GTV")) structType = "GTV";
                                    else if (target.DicomType == "ITV" || target.Id.ToUpper().Contains("ITV")) structType = "ITV";
                                    
                                    // Individual prescription detection for accurate V95% calculation
                                    DoseValue targetPrescriptionDose = plan.TotalDose; // Default fallback
                                    bool foundIndividualDose = false;
                                    
                                    // For PTVs, try to detect individual prescription doses
                                    if (structType == "PTV")
                                    {
                                        try
                                        {
                                            string targetName = target.Id.ToUpper();
                                            
                                            // Look for dose numbers in target name (5400, 6000, etc.)
                                            var numbers = System.Text.RegularExpressions.Regex.Matches(targetName, @"\d{4,5}");
                                            if (numbers.Count > 0)
                                            {
                                                string doseString = numbers[0].Value;
                                                if (doseString.Length == 4) // e.g., "5400" = 54.00 Gy
                                                {
                                                    double detectedDose = double.Parse(doseString);
                                                    targetPrescriptionDose = new DoseValue(detectedDose, DoseValue.DoseUnit.cGy);
                                                    foundIndividualDose = true;
                                                }
                                                else if (doseString.Length == 5) // e.g., "60000" = 60.00 Gy  
                                                {
                                                    double detectedDose = double.Parse(doseString) / 100.0;
                                                    targetPrescriptionDose = new DoseValue(detectedDose, DoseValue.DoseUnit.cGy);
                                                    foundIndividualDose = true;
                                                }
                                            }
                                            
                                            // Alternative: Look for decimal format (54.0, 60.0, etc.)
                                            if (!foundIndividualDose)
                                            {
                                                var decimalNumbers = System.Text.RegularExpressions.Regex.Matches(targetName, @"\d{2}\.\d");
                                                if (decimalNumbers.Count > 0)
                                                {
                                                    double detectedDose = double.Parse(decimalNumbers[0].Value) * 100.0;
                                                    targetPrescriptionDose = new DoseValue(detectedDose, DoseValue.DoseUnit.cGy);
                                                    foundIndividualDose = true;
                                                }
                                            }
                                        }
                                        catch
                                        {
                                            // If name parsing fails, use mean dose as approximation
                                            targetPrescriptionDose = new DoseValue(meanDose.Dose * 1.05, meanDose.Unit);
                                            foundIndividualDose = true;
                                        }
                                    }
                                    
                                    // Calculate V95% coverage against appropriate prescription
                                    var v95Dose = new DoseValue(targetPrescriptionDose.Dose * 0.95, targetPrescriptionDose.Unit);
                                    var volumeAt95 = plan.GetVolumeAtDose(target, v95Dose, VolumePresentation.Relative);
                                    
                                    sb.AppendLine();
                                    sb.AppendLine("📍 " + target.Id + " (" + structType + "):");
                                    sb.AppendLine("   ✓ Volume: " + target.Volume.ToString("F1") + " cc");
                                    
                                    // Show prescription detection results
                                    if (structType == "PTV")
                                    {
                                        if (foundIndividualDose)
                                        {
                                            sb.AppendLine("   ✓ Detected Prescription: " + (targetPrescriptionDose.Dose / 100.0).ToString("F1") + " Gy");
                                        }
                                        else
                                        {
                                            sb.AppendLine("   ⚠️ Using Plan Total Dose: " + (targetPrescriptionDose.Dose / 100.0).ToString("F1") + " Gy");
                                        }
                                    }
                                    
                                    sb.AppendLine("   ✓ V95% Coverage: " + volumeAt95.ToString("F1") + "%");
                                    sb.AppendLine("   ✓ V95% Dose: " + (v95Dose.Dose / 100.0).ToString("F1") + " Gy");
                                    sb.AppendLine("   ✓ Max Dose: " + (targetMaxDose.Dose / 100.0).ToString("F1") + " Gy");
                                    sb.AppendLine("   ✓ Mean Dose: " + (meanDose.Dose / 100.0).ToString("F1") + " Gy");
                                    
                                    // Coverage assessment for PTVs
                                    if (structType == "PTV")
                                    {
                                        if (volumeAt95 >= 95.0)
                                        {
                                            sb.AppendLine("   ✅ Coverage Status: EXCELLENT (≥95%)");
                                        }
                                        else if (volumeAt95 >= 90.0)
                                        {
                                            sb.AppendLine("   ⚠️ Coverage Status: ACCEPTABLE (90-95%)");
                                        }
                                        else
                                        {
                                            sb.AppendLine("   🚨 Coverage Status: POOR (<90%) - REVIEW REQUIRED");
                                        }
                                    }
                                }
                            }
                            catch
                            {
                                sb.AppendLine();
                                sb.AppendLine("📍 " + target.Id + ": Error calculating dose statistics");
                            }
                        }
                        
                        if (ptvs.Count > 1)
                        {
                            sb.AppendLine();
                            sb.AppendLine("🔍 SiB PLAN VERIFICATION CHECKLIST:");
                            sb.AppendLine("==================================");
                            sb.AppendLine("   □ Verify each PTV has appropriate dose prescription");
                            sb.AppendLine("   □ Confirm dose gradients between PTVs are acceptable");
                            sb.AppendLine("   □ Check OAR constraints are met for highest dose level");
                            sb.AppendLine("   □ Verify Mosaiq prescription setup for multiple dose levels");
                            sb.AppendLine("   □ Confirm treatment planning approval for SiB technique");
                            sb.AppendLine("   □ Document clinical rationale for simultaneous boost");
                        }
                        
                        // Optimization Analysis for Multi-Target Plans
                        sb.AppendLine();
                        sb.AppendLine("⚙️ OPTIMIZATION ANALYSIS:");
                        sb.AppendLine("========================");
                        
                        try
                        {
                            // Check for optimization objectives
                            if (plan.OptimizationSetup != null)
                            {
                                var objectives = plan.OptimizationSetup.Objectives;
                                if (objectives != null && objectives.Any())
                                {
                                    var totalObjectives = objectives.Count();
                                    var lowerObjectives = objectives.Where(obj => obj.Priority > 1).Count();
                                    var upperObjectives = objectives.Where(obj => obj.Priority == 1).Count();
                                    var mediumObjectives = objectives.Where(obj => obj.Priority > 1 && obj.Priority <= 100).Count();
                                    var lowObjectives = objectives.Where(obj => obj.Priority > 100).Count();
                                    
                                    sb.AppendLine("✓ Total Optimization Objectives: " + totalObjectives);
                                    sb.AppendLine("✓ Upper Objectives (Priority 1): " + upperObjectives);
                                    sb.AppendLine("✓ Medium Objectives (Priority 2-100): " + mediumObjectives);
                                    sb.AppendLine("✓ Lower Objectives (Priority >100): " + lowObjectives);
                                    
                                    // Analysis of optimization strategy
                                    if (lowerObjectives > 0)
                                    {
                                        if (ptvs.Count > 1)
                                        {
                                            sb.AppendLine();
                                            sb.AppendLine("✅ MULTI-TARGET OPTIMIZATION DETECTED:");
                                            sb.AppendLine("   ✓ " + lowerObjectives + " lower priority objectives found");
                                            sb.AppendLine("   ✓ Plan appears optimized for multiple targets");
                                            sb.AppendLine("   ✓ Hierarchical optimization strategy used");
                                            sb.AppendLine("   □ Verify objective priorities match clinical intent");
                                        }
                                        else
                                        {
                                            sb.AppendLine();
                                            sb.AppendLine("⚠️ LOWER OBJECTIVES WITH SINGLE TARGET:");
                                            sb.AppendLine("   ⚠️ " + lowerObjectives + " lower priority objectives found");
                                            sb.AppendLine("   ⚠️ May indicate OAR sparing optimization");
                                            sb.AppendLine("   □ Verify optimization strategy is appropriate");
                                        }
                                    }
                                    else if (ptvs.Count > 1)
                                    {
                                        sb.AppendLine();
                                        sb.AppendLine("🚨 POTENTIAL OPTIMIZATION CONCERN:");
                                        sb.AppendLine("   🚨 Multiple PTVs detected but NO lower objectives found");
                                        sb.AppendLine("   🚨 Plan may not be optimized for multiple targets");
                                        sb.AppendLine("   □ REVIEW: Consider adding lower priority objectives");
                                        sb.AppendLine("   □ VERIFY: All PTVs have appropriate dose objectives");
                                        sb.AppendLine("   □ CHECK: Optimization strategy for SiB plan");
                                    }
                                    else
                                    {
                                        sb.AppendLine();
                                        sb.AppendLine("✅ SINGLE TARGET OPTIMIZATION:");
                                        sb.AppendLine("   ✓ All objectives at same priority level");
                                        sb.AppendLine("   ✓ Appropriate for single target plan");
                                    }
                                    
                                    // Detailed objective breakdown
                                    if (totalObjectives > 0)
                                    {
                                        sb.AppendLine();
                                        sb.AppendLine("📋 OBJECTIVE DETAILS:");
                                        sb.AppendLine("--------------------");
                                        
                                        // Group objectives by structure
                                        var objectivesByStructure = objectives.GroupBy(obj => obj.StructureId).Take(8);
                                        
                                        foreach (var structureGroup in objectivesByStructure)
                                        {
                                            var structureName = structureGroup.Key;
                                            var structureObjectives = structureGroup.ToList();
                                            
                                            sb.AppendLine("📍 " + structureName + ":");
                                            
                                            foreach (var obj in structureObjectives.Take(3))
                                            {
                                                try
                                                {
                                                    string objType = obj.GetType().Name;
                                                    string priority = "Priority " + obj.Priority.ToString();
                                                    
                                                    sb.AppendLine("   • " + objType + " (" + priority + ")");
                                                }
                                                catch
                                                {
                                                    sb.AppendLine("   • Objective (" + "Priority " + obj.Priority.ToString() + ")");
                                                }
                                            }
                                            
                                            if (structureObjectives.Count > 3)
                                            {
                                                sb.AppendLine("   • ... and " + (structureObjectives.Count - 3) + " more objectives");
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    sb.AppendLine("❌ No optimization objectives found");
                                    if (ptvs.Count > 1)
                                    {
                                        sb.AppendLine("🚨 CRITICAL: Multi-PTV plan without optimization objectives!");
                                    }
                                }
                            }
                            else
                            {
                                sb.AppendLine("❌ No optimization setup available");
                                sb.AppendLine("⚠️ Plan may not be optimized (imported or manual plan)");
                                if (ptvs.Count > 1)
                                {
                                    sb.AppendLine("🚨 Multi-PTV plan without optimization data - verify manually");
                                }
                            }
                        }
                        catch (Exception optEx)
                        {
                            sb.AppendLine("⚠️ Unable to analyze optimization objectives: " + optEx.Message);
                            sb.AppendLine("□ Manual verification of optimization strategy required");
                        }
                    }
                    else
                    {
                        sb.AppendLine("❌ No target structures found for dose analysis");
                    }
                }
                else
                {
                    sb.AppendLine();
                    sb.AppendLine("❌ Structure set or dose calculation not available for target analysis");
                }
                
            }
            catch (Exception ex)
            {
                sb.AppendLine("❌ Error retrieving dose information: " + ex.Message);
            }
        }

        /// <summary>
        /// Comprehensive analysis of beam parameters and delivery characteristics
        /// 
        /// Verification Areas:
        /// - Beam counts (total, treatment, setup beams)
        /// - Monitor unit calculations and limits
        /// - Treatment unit verification and machine assignments
        /// - Bolus detection and documentation requirements
        /// - Energy mode analysis and distribution
        /// - Gantry angle patterns with rotation direction (CW/CCW)
        /// - MU limit alerts for 6X-FFF beams (>1400 MU)
        /// - Setup beam jaw sizes and DRR assignments
        /// - Individual beam parameter summary
        /// 
        /// Critical Safety Checks:
        /// - Multiple treatment unit coordination
        /// - Bolus documentation in Mosaiq
        /// - MU limits for FFF beams
        /// - Setup beam verification requirements
        /// - Jaw size reporting in clinical units (cm)
        /// </summary>
        /// <param name="plan">Treatment plan to analyze</param>
        /// <param name="sb">StringBuilder for output formatting</param>
        private void CheckBeamInformation(PlanSetup plan, StringBuilder sb)
        {
            sb.AppendLine("3. BEAM INFORMATION:");
            sb.AppendLine("====================================");
            
            try
            {
                var beams = plan.Beams.ToList();
                sb.AppendLine("✓ Total Beams: " + beams.Count);
                
                var treatmentBeams = beams.Where(b => !b.IsSetupField).ToList();
                var setupBeams = beams.Where(b => b.IsSetupField).ToList();
                
                sb.AppendLine("✓ Treatment Beams: " + treatmentBeams.Count);
                sb.AppendLine("✓ Setup Beams: " + setupBeams.Count);
                
                // Calculate total MU
                double totalMU = treatmentBeams.Sum(b => b.Meterset.Value);
                sb.AppendLine("✓ Total Monitor Units: " + totalMU.ToString("F1") + " MU");
                sb.AppendLine();
                
                // Treatment Unit Verification
                sb.AppendLine("🏭 TREATMENT UNIT VERIFICATION:");
                sb.AppendLine("==============================");
                
                var treatmentMachines = new List<string>();
                var machineBeamCounts = new Dictionary<string, int>();
                
                foreach (var beam in treatmentBeams)
                {
                    string machineId = beam.TreatmentUnit.Id;
                    if (!treatmentMachines.Contains(machineId))
                    {
                        treatmentMachines.Add(machineId);
                        machineBeamCounts[machineId] = 0;
                    }
                    machineBeamCounts[machineId]++;
                }
                
                if (treatmentMachines.Count > 0)
                {
                    sb.AppendLine("✓ Treatment Units Used:");
                    foreach (var machine in treatmentMachines)
                    {
                        sb.AppendLine("   • " + machine + " (" + machineBeamCounts[machine] + " beams)");
                    }
                    
                    if (treatmentMachines.Count > 1)
                    {
                        sb.AppendLine();
                        sb.AppendLine("⚠️ MULTIPLE TREATMENT UNITS DETECTED:");
                        sb.AppendLine("   Verify this is intentional and properly coordinated");
                        sb.AppendLine("   Check for potential scheduling conflicts");
                    }
                    
                    sb.AppendLine();
                    sb.AppendLine("🚨 TREATMENT UNIT REQUIREMENTS FOR MOSAIQ:");
                    sb.AppendLine("   ➤ Verify treatment unit matches planned machine");
                    
                    sb.AppendLine();
                    sb.AppendLine("📋 TREATMENT UNIT CHECKLIST:");
                    foreach (var machine in treatmentMachines)
                    {
                        sb.AppendLine("   □ " + machine + " selected in Mosaiq prescription");
                        sb.AppendLine("   □ " + machine + " available for treatment schedule");
                        sb.AppendLine("   □ " + machine + " QA current and complete");
                    }
                    sb.AppendLine("   □ Beam delivery sequence verified");
                    sb.AppendLine("   □ Machine-specific accessories available");
                }
                
                // Bolus Assessment
                sb.AppendLine();
                sb.AppendLine("🛡️ BOLUS ASSESSMENT:");
                sb.AppendLine("====================");
                
                var bolusBeams = treatmentBeams.Where(b => b.Boluses != null && b.Boluses.Any()).ToList();
                bool bolusDetected = bolusBeams.Count > 0;
                
                if (bolusDetected)
                {
                    sb.AppendLine("⚠️ BOLUS DETECTED in treatment beams");
                    sb.AppendLine("   Beams with bolus: " + bolusBeams.Count + " of " + treatmentBeams.Count);
                    
                    var allBolusStructures = new List<string>();
                    foreach (var beam in bolusBeams)
                    {
                        sb.AppendLine("   • Beam " + beam.Id + ": " + beam.Boluses.Count() + " bolus structure(s)");
                        foreach (var bolus in beam.Boluses)
                        {
                            if (!allBolusStructures.Contains(bolus.Id))
                                allBolusStructures.Add(bolus.Id);
                        }
                    }
                    
                    sb.AppendLine();
                    sb.AppendLine("📋 BOLUS STRUCTURES IDENTIFIED:");
                    foreach (var bolusId in allBolusStructures)
                    {
                        sb.AppendLine("   • " + bolusId);
                    }
                    
                    sb.AppendLine();
                    sb.AppendLine("🚨 BOLUS DOCUMENTATION REQUIRED:");
                    sb.AppendLine("   □ Bolus information documented in Mosaiq");
                    sb.AppendLine("   □ Bolus setup instructions clear and detailed");
                    sb.AppendLine("   □ Bolus material available in department");
                    sb.AppendLine("   □ Bolus thickness and placement specified");
                    sb.AppendLine("   □ Daily setup verification process established");
                    sb.AppendLine("   □ Bolus positioning reproducibility verified");
                    sb.AppendLine("   □ Alternative bolus materials identified if needed");
                    
                    sb.AppendLine();
                    sb.AppendLine("   BOLUS SETUP REMINDERS:");
                    sb.AppendLine("     • Bolus must be consistently positioned for each fraction");
                    sb.AppendLine("     • Document bolus material type and thickness");
                    sb.AppendLine("     • Verify bolus contact with skin surface");
                    sb.AppendLine("     • Check for air gaps between bolus and patient");
                    sb.AppendLine("     • Ensure bolus doesn't interfere with other equipment");
                }
                else
                {
                    sb.AppendLine("✓ No bolus detected in treatment beams");
                    sb.AppendLine("   Standard treatment delivery without bolus");
                    sb.AppendLine("   □ Verify no additional beam modifiers needed");
                }
                
                // Enhanced beam details
                sb.AppendLine();
                sb.AppendLine("📊 DETAILED BEAM ANALYSIS:");
                sb.AppendLine("==========================");
                
                // Energy analysis
                var energyGroups = treatmentBeams.GroupBy(b => b.EnergyModeDisplayName).ToList();
                sb.AppendLine("✓ Energy Modes Used:");
                foreach (var energyGroup in energyGroups)
                {
                    var beamCount = energyGroup.Count();
                    var energyMU = energyGroup.Sum(b => b.Meterset.Value);
                    sb.AppendLine("   • " + energyGroup.Key + ": " + beamCount + " beams, " + energyMU.ToString("F1") + " MU");
                }
                
                // Enhanced gantry angle analysis with rotation direction
                sb.AppendLine();
                sb.AppendLine("✓ Gantry Angle Distribution:");
                var gantryAngles = new List<string>();
                var muAlerts = new List<string>();
                
                foreach (var beam in treatmentBeams.Take(10)) // Limit to avoid clutter
                {
                    if (beam.ControlPoints != null && beam.ControlPoints.Any())
                    {
                        var startAngle = beam.ControlPoints.First().GantryAngle;
                        var endAngle = beam.ControlPoints.Last().GantryAngle;
                        double beamMU = beam.Meterset.Value;
                        string energy = beam.EnergyModeDisplayName;
                        
                        if (Math.Abs(startAngle - endAngle) < 1) // Static beam
                        {
                            gantryAngles.Add(beam.Id + ": " + startAngle.ToString("F0") + "° (Static)");
                        }
                        else // Arc beam
                        {
                            // Determine rotation direction
                            string direction = "";
                            double angleDiff = endAngle - startAngle;
                            
                            // Handle angle wrapping (0°/360°)
                            if (angleDiff > 180)
                                angleDiff -= 360;
                            else if (angleDiff < -180)
                                angleDiff += 360;
                            
                            if (angleDiff > 0)
                                direction = " (CW)";
                            else if (angleDiff < 0)
                                direction = " (CCW)";
                            
                            gantryAngles.Add(beam.Id + ": " + startAngle.ToString("F0") + "° → " + endAngle.ToString("F0") + "°" + direction);
                        }
                        
                        // Check MU limits for FFF beams
                        if (energy.Contains("FFF") && energy.Contains("6X"))
                        {
                            if (beamMU > 1400)
                            {
                                muAlerts.Add("⚠️ " + beam.Id + ": " + beamMU.ToString("F1") + " MU (>1400 MU limit for 6X-FFF)");
                            }
                        }
                    }
                }
                
                foreach (var angle in gantryAngles)
                {
                    sb.AppendLine("   • " + angle);
                }
                
                // Show MU alerts if any
                if (muAlerts.Count > 0)
                {
                    sb.AppendLine();
                    sb.AppendLine("🚨 MU LIMIT ALERTS:");
                    foreach (var alert in muAlerts)
                    {
                        sb.AppendLine("   " + alert);
                    }
                    sb.AppendLine("   NOTE: 6X-FFF beams should be limited to ≤1400 MU for optimal delivery");
                }
                
                if (treatmentBeams.Count > 10)
                {
                    sb.AppendLine("   ... and " + (treatmentBeams.Count - 10) + " more beams");
                }
                
                // Collimator analysis
                sb.AppendLine();
                sb.AppendLine("✓ Treatment Beam Summary:");
                foreach (var beam in treatmentBeams.Take(8))
                {
                    sb.AppendLine("Beam: " + beam.Id);
                    sb.AppendLine("  ✓ Energy: " + beam.EnergyModeDisplayName);
                    sb.AppendLine("  ✓ Monitor Units: " + beam.Meterset.Value.ToString("F1") + " MU");
                    sb.AppendLine("  ✓ Treatment Unit: " + beam.TreatmentUnit.Id);
                    sb.AppendLine("  ✓ Dose Rate: " + beam.DoseRate + " MU/min");
                    
                    if (beam.ControlPoints != null && beam.ControlPoints.Any())
                    {
                        var firstCP = beam.ControlPoints.First();
                        sb.AppendLine("  ✓ Gantry Angle: " + firstCP.GantryAngle.ToString("F1") + "°");
                        sb.AppendLine("  ✓ Collimator Angle: " + firstCP.CollimatorAngle.ToString("F1") + "°");
                        sb.AppendLine("  ✓ Couch Angle: " + firstCP.PatientSupportAngle.ToString("F1") + "°");
                        
                        // SSD information (with error handling) - Display in cm
                        try
                        {
                            sb.AppendLine("  ✓ SSD: " + (beam.SSD / 10.0).ToString("F1") + " cm");
                        }
                        catch
                        {
                            sb.AppendLine("  ✓ SSD: Not available");
                        }
                    }
                    
                    // DRR verification for treatment beams
                    if (beam.ReferenceImage != null)
                    {
                        sb.AppendLine("  ✅ DRR: " + beam.ReferenceImage.Id);
                    }
                    else
                    {
                        sb.AppendLine("  ❌ DRR: Not assigned - VERIFY DRR CREATION");
                    }
                    
                    sb.AppendLine();
                }
                
                if (treatmentBeams.Count > 8)
                {
                    sb.AppendLine("... and " + (treatmentBeams.Count - 8) + " more beams");
                }
                
                // Enhanced setup beams information
                if (setupBeams.Count > 0)
                {
                    sb.AppendLine();
                    sb.AppendLine("📷 SETUP BEAM INFORMATION:");
                    sb.AppendLine("==========================");
                    
                    foreach (var setupBeam in setupBeams)
                    {
                        sb.AppendLine("Setup Beam: " + setupBeam.Id);
                        sb.AppendLine("  ✓ Energy: " + setupBeam.EnergyModeDisplayName);
                        sb.AppendLine("  ✓ Treatment Unit: " + setupBeam.TreatmentUnit.Id);
                        
                        if (setupBeam.ControlPoints != null && setupBeam.ControlPoints.Any())
                        {
                            var firstCP = setupBeam.ControlPoints.First();
                            sb.AppendLine("  ✓ Gantry Angle: " + firstCP.GantryAngle.ToString("F1") + "°");
                            sb.AppendLine("  ✓ Collimator Angle: " + firstCP.CollimatorAngle.ToString("F1") + "°");
                            
                            // Jaw positions
                            if (firstCP.JawPositions != null)
                            {
                                try
                                {
                                    double x1 = firstCP.JawPositions.X1;
                                    double x2 = firstCP.JawPositions.X2;
                                    double y1 = firstCP.JawPositions.Y1;
                                    double y2 = firstCP.JawPositions.Y2;
                                    
                                    double xJawSize = Math.Abs(x2 - x1) / 10.0; // Convert mm to cm
                                    double yJawSize = Math.Abs(y2 - y1) / 10.0; // Convert mm to cm
                                    
                                    sb.AppendLine("  ✓ X Jaw Size: " + xJawSize.ToString("F1") + " cm (X1=" + (x1/10.0).ToString("F1") + ", X2=" + (x2/10.0).ToString("F1") + " cm)");
                                    sb.AppendLine("  ✓ Y Jaw Size: " + yJawSize.ToString("F1") + " cm (Y1=" + (y1/10.0).ToString("F1") + ", Y2=" + (y2/10.0).ToString("F1") + " cm)");
                                }
                                catch
                                {
                                    sb.AppendLine("  ⚠️ Jaw positions: Not available");
                                }
                            }
                        }
                        
                        // Check for DRRs
                        try
                        {
                            if (setupBeam.ReferenceImage != null)
                            {
                                sb.AppendLine("  ✅ DRR: Available (" + setupBeam.ReferenceImage.Id + ")");
                            }
                            else
                            {
                                sb.AppendLine("  ❌ DRR: Not assigned");
                            }
                        }
                        catch
                        {
                            sb.AppendLine("  ⚠️ DRR: Status unknown");
                        }
                        
                        sb.AppendLine();
                    }
                    
                    // Setup beam verification checklist
                    sb.AppendLine("📋 SETUP BEAM VERIFICATION:");
                    sb.AppendLine("   □ All setup beams have appropriate jaw sizes");
                    sb.AppendLine("   □ DRRs assigned to all setup beams");
                    sb.AppendLine("   □ Setup beam angles cover treatment area");
                    sb.AppendLine("   □ Setup beam energy appropriate for imaging");
                    sb.AppendLine("   □ Gantry angles accessible for daily setup");
                }
                
            }
            catch (Exception ex)
            {
                sb.AppendLine("❌ Error retrieving beam information: " + ex.Message);
            }
        }

        private void CheckStructureInformation(PlanSetup plan, StringBuilder sb)
        {
            sb.AppendLine("4. STRUCTURE INFORMATION:");
            sb.AppendLine("====================================");
            
            try
            {
                if (plan.StructureSet == null)
                {
                    sb.AppendLine("❌ ERROR: No structure set associated with plan");
                    return;
                }
                
                var structures = plan.StructureSet.Structures.ToList();
                sb.AppendLine("✓ Total Structures: " + structures.Count);
                
                // Enhanced structure categorization
                var ptvs = structures.Where(s => s.DicomType == "PTV" || s.Id.ToUpper().Contains("PTV")).ToList();
                var ctvs = structures.Where(s => s.DicomType == "CTV" || s.Id.ToUpper().Contains("CTV")).ToList();
                var gtvs = structures.Where(s => s.DicomType == "GTV" || s.Id.ToUpper().Contains("GTV")).ToList();
                var itvs = structures.Where(s => s.DicomType == "ITV" || s.Id.ToUpper().Contains("ITV")).ToList();
                var targets = ptvs.Concat(ctvs).Concat(gtvs).Concat(itvs).ToList();
                var oars = structures.Where(s => s.DicomType == "ORGAN").ToList();
                var body = structures.Where(s => s.DicomType == "EXTERNAL" || s.Id.ToUpper().Contains("BODY")).ToList();
                
                sb.AppendLine("✓ Target Structures: " + targets.Count + " (PTV:" + ptvs.Count + ", CTV:" + ctvs.Count + ", GTV:" + gtvs.Count + ", ITV:" + itvs.Count + ")");
                sb.AppendLine("✓ Organ at Risk Structures: " + oars.Count);
                sb.AppendLine("✓ Body/External Structures: " + body.Count);
                
                sb.AppendLine();
                sb.AppendLine("🖼️ CT IMAGING & DENSITY OVERRIDE ASSESSMENT:");
                sb.AppendLine("============================================");
                
                // CT slice information
                if (plan.StructureSet.Image != null)
                {
                    var image = plan.StructureSet.Image;
                    sb.AppendLine("✓ CT Image ID: " + image.Id);
                    
                    // Get number of CT slices (Z planes)
                    if (image.ZSize > 0)
                    {
                        // Check for excessive slice count
                        if (image.ZSize > 399)
                        {
                            sb.AppendLine("🚨 Number of CT Slices: " + image.ZSize + " slices - EXCESSIVE SLICE COUNT!");
                            sb.AppendLine("⚠️ WARNING: High slice count detected (>" + "399 slices)");
                            sb.AppendLine("   □ Consider trimming CT to reduce calculation time");
                            sb.AppendLine("   □ Remove unnecessary superior/inferior slices");
                            sb.AppendLine("   □ Verify appropriate scan coverage for treatment site");
                            sb.AppendLine("   □ Check if full body scan was used unnecessarily");
                        }
                        else
                        {
                            sb.AppendLine("✓ Number of CT Slices: " + image.ZSize + " slices - Appropriate count");
                        }
                        
                        sb.AppendLine("✓ Slice Thickness: " + image.ZRes.ToString("F1") + " mm");
                        sb.AppendLine("✓ Image Resolution: " + image.XSize + " x " + image.YSize + " x " + image.ZSize + " voxels");
                        sb.AppendLine("✓ Voxel Size: " + image.XRes.ToString("F2") + " x " + image.YRes.ToString("F2") + " x " + image.ZRes.ToString("F1") + " mm");
                    }
                    else
                    {
                        sb.AppendLine("⚠️ CT slice information not available");
                    }
                }
                else
                {
                    sb.AppendLine("❌ No CT image associated with structure set");
                }
                
                sb.AppendLine();
                
                // Check for artifact and density override structures
                var artifactStructures = structures.Where(s => 
                    s.Id.ToUpper().Contains("ARTIFACT") ||
                    s.Id.ToUpper().Contains("ZARTIFACT") ||
                    s.Id.ToUpper().Contains("HD") ||
                    s.Id.ToUpper().Contains("ZHD") ||
                    s.Id.ToUpper().Contains("ZDENSITY") ||
                    s.Id.ToUpper().Contains("ZCONTRAST")).ToList();
                
                bool densityOverrideNeeded = artifactStructures.Count > 0;
                
                if (densityOverrideNeeded)
                {
                    sb.AppendLine("⚠️ ARTIFACT/DENSITY STRUCTURES DETECTED:");
                    foreach (var artifact in artifactStructures)
                    {
                        sb.AppendLine("   • " + artifact.Id);
                    }
                    
                    sb.AppendLine();
                    sb.AppendLine("🚨 DENSITY OVERRIDE VERIFICATION REQUIRED:");
                    sb.AppendLine("   □ HU assignments verified for all override structures");
                    sb.AppendLine("   □ Dose calculation accuracy confirmed in override regions");
                    sb.AppendLine("   □ High-Z materials (contrast, metal) properly overridden");
                    sb.AppendLine("   □ Prosthetic devices have appropriate density values");
                    sb.AppendLine("   □ CT artifacts minimized or compensated");
                }
                else
                {
                    sb.AppendLine("✓ No artifact or density override structures detected");
                    sb.AppendLine("   □ Verify CT image quality is adequate");
                    sb.AppendLine("   □ Check for uncontoured high-Z materials");
                }
                
                // Dose Statistics Analysis
                if (plan.Dose != null)
                {
                    sb.AppendLine();
                    sb.AppendLine("📊 DOSE STATISTICS & HOTSPOT ANALYSIS:");
                    sb.AppendLine("=====================================");
                    
                    // Plan maximum dose
                    var maxDose = plan.Dose.DoseMax3D;
                    if (maxDose.IsRelativeDoseValue)
                    {
                        sb.AppendLine("✓ Plan Maximum Dose: " + maxDose.Dose.ToString("F1") + "%");
                    }
                    else
                    {
                        sb.AppendLine("✓ Plan Maximum Dose: " + (maxDose.Dose / 100.0).ToString("F1") + " Gy");
                    }
                    
                    // Find which structure contains the maximum dose
                    bool maxDoseInPTV = false;
                    string maxDoseStructure = "Unknown";
                    
                    foreach (var structure in structures.Where(s => !s.IsEmpty))
                    {
                        try
                        {
                            var structureMaxDose = plan.GetDoseAtVolume(structure, 0.0, VolumePresentation.Relative, DoseValuePresentation.Absolute);
                            if (Math.Abs(structureMaxDose.Dose - maxDose.Dose) < 10.0) // Within 0.1 Gy
                            {
                                maxDoseStructure = structure.Id;
                                if (structure.DicomType == "PTV" || structure.Id.ToUpper().Contains("PTV"))
                                {
                                    maxDoseInPTV = true;
                                }
                                break;
                            }
                        }
                        catch
                        {
                            // Continue if dose calculation fails for this structure
                        }
                    }
                    
                    if (maxDoseInPTV)
                    {
                        sb.AppendLine("✅ Maximum dose located in: " + maxDoseStructure + " (PTV - GOOD)");
                    }
                    else
                    {
                        sb.AppendLine("⚠️ Maximum dose located in: " + maxDoseStructure + " (NOT in PTV)");
                        sb.AppendLine("   □ Verify hotspot location is clinically acceptable");
                        sb.AppendLine("   □ Consider optimization to reduce hotspot");
                    }
                    
                    // Hotspot volume analysis using 107% of prescription dose
                    sb.AppendLine();
                    sb.AppendLine("🔥 HOTSPOT ANALYSIS:");
                    try
                    {
                        // Get prescription dose for 107% calculation
                        var prescriptionDose = plan.TotalDose;
                        var hotspotThreshold = new DoseValue(prescriptionDose.Dose * 1.07, prescriptionDose.Unit); // 107% of prescription
                        
                        if (plan.StructureSet.Structures.Any())
                        {
                            var bodyStructure = structures.FirstOrDefault(s => s.DicomType == "EXTERNAL" || s.Id.ToUpper().Contains("BODY"));
                            if (bodyStructure != null && !bodyStructure.IsEmpty)
                            {
                                var hotspotVolume = plan.GetVolumeAtDose(bodyStructure, hotspotThreshold, VolumePresentation.AbsoluteCm3);
                                var hotspotDoseGy = hotspotThreshold.Dose / 100.0; // Convert to Gy
                                
                                if (hotspotVolume > 2.0) // >2cm threshold as requested
                                {
                                    sb.AppendLine("⚠️ Hotspot Volume (>107% Rx): " + hotspotVolume.ToString("F2") + " cc at " + hotspotDoseGy.ToString("F1") + " Gy");
                                    sb.AppendLine("🚨 HOTSPOT CHECK REQUIRED:");
                                    sb.AppendLine("   □ Verify hotspot >2cc is clinically reasonable");
                                    sb.AppendLine("   □ Check if hotspot is in critical structure");
                                    sb.AppendLine("   □ Consider plan optimization if hotspot excessive");
                                    sb.AppendLine("   □ Document hotspot justification if acceptable");
                                }
                                else
                                {
                                    sb.AppendLine("✅ Hotspot Volume (>107% Rx): " + hotspotVolume.ToString("F2") + " cc at " + hotspotDoseGy.ToString("F1") + " Gy - Acceptable");
                                }
                            }
                        }
                        
                        // Max dose >V0.035cc location analysis
                        sb.AppendLine();
                        sb.AppendLine("📍 MAX DOSE >V0.035cc LOCATION ANALYSIS:");
                        
                        string maxDoseStructureLocation = "Unknown";
                        bool foundMaxDoseStructure = false;
                        
                        // Check each structure to find which contains max dose >0.035cc
                        foreach (var structure in structures.Where(s => !s.IsEmpty))
                        {
                            try
                            {
                                // Get volume receiving max dose in this structure
                                var volumeAtMaxDose = plan.GetVolumeAtDose(structure, maxDose, VolumePresentation.AbsoluteCm3);
                                
                                if (volumeAtMaxDose > 0.035)
                                {
                                    maxDoseStructureLocation = structure.Id;
                                    foundMaxDoseStructure = true;
                                    
                                    if (structure.DicomType == "PTV" || structure.Id.ToUpper().Contains("PTV"))
                                    {
                                        sb.AppendLine("✅ Max dose >V0.035cc located in: " + structure.Id + " (PTV - ACCEPTABLE)");
                                        sb.AppendLine("   ✓ Volume at max dose: " + volumeAtMaxDose.ToString("F3") + " cc");
                                    }
                                    else if (structure.DicomType == "EXTERNAL" || structure.Id.ToUpper().Contains("BODY"))
                                    {
                                        sb.AppendLine("⚠️ Max dose >V0.035cc located in: " + structure.Id + " (BODY/EXTERNAL)");
                                        sb.AppendLine("   ⚠️ Volume at max dose: " + volumeAtMaxDose.ToString("F3") + " cc");
                                        sb.AppendLine("   □ Verify location within or near PTV");
                                    }
                                    else
                                    {
                                        sb.AppendLine("🚨 Max dose >V0.035cc located in: " + structure.Id + " (CRITICAL REVIEW NEEDED)");
                                        sb.AppendLine("   🚨 Volume at max dose: " + volumeAtMaxDose.ToString("F3") + " cc");
                                        sb.AppendLine("   □ URGENT: Verify if this location is clinically acceptable");
                                        sb.AppendLine("   □ Consider plan optimization to move hotspot");
                                        sb.AppendLine("   □ Document clinical justification if acceptable");
                                    }
                                    break; // Found the structure with max dose >0.035cc
                                }
                            }
                            catch
                            {
                                // Continue checking other structures
                            }
                        }
                        
                        if (!foundMaxDoseStructure)
                        {
                            sb.AppendLine("✅ Max dose volume <0.035cc - No significant hotspot volume detected");
                        }
                    }
                    catch
                    {
                        sb.AppendLine("⚠️ Unable to calculate hotspot analysis - manual verification required");
                    }
                    
                    // ═══════════════════════════════════════════════════════════════
                    // TARGET STRUCTURE DOSE ANALYSIS
                    // ═══════════════════════════════════════════════════════════════
                    sb.AppendLine();
                    sb.AppendLine("🎯 TARGET STRUCTURE DOSE ANALYSIS:");
                    sb.AppendLine("===================================");
                    
                    // PTV Analysis (V95% focus with individual prescription detection)
                    if (ptvs.Any())
                    {
                        sb.AppendLine();
                        sb.AppendLine("📊 PTV COVERAGE ANALYSIS:");
                        sb.AppendLine("-------------------------");
                        
                        foreach (var ptv in ptvs.Take(5))
                        {
                            try
                            {
                                if (!ptv.IsEmpty)
                                {
                                    sb.AppendLine("📍 " + ptv.Id + " (PTV):");
                                    
                                    var ptvMaxDose = plan.GetDoseAtVolume(ptv, 0.0, VolumePresentation.Relative, DoseValuePresentation.Absolute);
                                    var meanDose = plan.GetDVHCumulativeData(ptv, DoseValuePresentation.Absolute, VolumePresentation.Relative, 1.0).MeanDose;
                                    
                                    // Detect individual PTV prescription dose
                                    // Method 1: Try to extract from PTV name (e.g., PTV_5400 = 54.00 Gy)
                                    DoseValue ptvPrescriptionDose = plan.TotalDose; // Default fallback
                                    bool foundIndividualDose = false;
                                    
                                    try
                                    {
                                        string ptvName = ptv.Id.ToUpper();
                                        
                                        // Look for dose numbers in PTV name (5400, 6000, etc.)
                                        var numbers = System.Text.RegularExpressions.Regex.Matches(ptvName, @"\d{4,5}");
                                        if (numbers.Count > 0)
                                        {
                                            string doseString = numbers[0].Value;
                                            if (doseString.Length == 4) // e.g., "5400" = 54.00 Gy
                                            {
                                                double detectedDose = double.Parse(doseString);
                                                ptvPrescriptionDose = new DoseValue(detectedDose, DoseValue.DoseUnit.cGy);
                                                foundIndividualDose = true;
                                            }
                                            else if (doseString.Length == 5) // e.g., "60000" = 60.00 Gy  
                                            {
                                                double detectedDose = double.Parse(doseString) / 100.0;
                                                ptvPrescriptionDose = new DoseValue(detectedDose, DoseValue.DoseUnit.cGy);
                                                foundIndividualDose = true;
                                            }
                                        }
                                        
                                        // Alternative: Look for decimal format (54.0, 60.0, etc.)
                                        if (!foundIndividualDose)
                                        {
                                            var decimalNumbers = System.Text.RegularExpressions.Regex.Matches(ptvName, @"\d{2}\.\d");
                                            if (decimalNumbers.Count > 0)
                                            {
                                                double detectedDose = double.Parse(decimalNumbers[0].Value) * 100.0;
                                                ptvPrescriptionDose = new DoseValue(detectedDose, DoseValue.DoseUnit.cGy);
                                                foundIndividualDose = true;
                                            }
                                        }
                                    }
                                    catch
                                    {
                                        // If name parsing fails, use mean dose as approximation
                                        ptvPrescriptionDose = new DoseValue(meanDose.Dose * 1.05, meanDose.Unit); // Assume prescription is ~95% of mean
                                        foundIndividualDose = true;
                                    }
                                    
                                    // Calculate V95% coverage against individual prescription
                                    var coverage95Dose = new DoseValue(ptvPrescriptionDose.Dose * 0.95, ptvPrescriptionDose.Unit);
                                    var volumeAt95 = plan.GetVolumeAtDose(ptv, coverage95Dose, VolumePresentation.Relative);
                                    
                                    sb.AppendLine("   ✓ Volume: " + ptv.Volume.ToString("F1") + " cc");
                                    
                                    // PTV Length Analysis (Superior-Inferior extent)
                                    try
                                    {
                                        var bounds = ptv.MeshGeometry.Bounds;
                                        double ptvLength = Math.Abs(bounds.SizeZ) / 10.0; // Convert mm to cm
                                        int slicesOccupied = 0;
                                        
                                        // Calculate approximate slices occupied (if image available)
                                        if (plan.StructureSet != null && plan.StructureSet.Image != null)
                                        {
                                            double sliceThickness = plan.StructureSet.Image.ZRes; // mm
                                            slicesOccupied = (int)Math.Ceiling(bounds.SizeZ / sliceThickness);
                                        }
                                        
                                        sb.AppendLine("   ✓ PTV Length (S-I): " + ptvLength.ToString("F1") + " cm");
                                        if (slicesOccupied > 0)
                                        {
                                            sb.AppendLine("   ✓ CT Slices Occupied: " + slicesOccupied + " slices");
                                        }
                                        
                                        // Check Linac2 compatibility
                                        if (ptvLength > 20.0)
                                        {
                                            sb.AppendLine("   🚨 PTV LENGTH WARNING: " + ptvLength.ToString("F1") + " cm exceeds 20cm");
                                            sb.AppendLine("      ⚠️ LINAC2 NOT COMPATIBLE - Use Linac1 or split plan");
                                            sb.AppendLine("      □ Consider splitting into multiple isocenters");
                                            sb.AppendLine("      □ Verify maximum field size constraints");
                                        }
                                        else
                                        {
                                            sb.AppendLine("   ✅ Linac Compatibility: Length " + ptvLength.ToString("F1") + " cm ≤ 20cm (All linacs compatible)");
                                        }
                                    }
                                    catch
                                    {
                                        sb.AppendLine("   ⚠️ Unable to calculate PTV length - verify manually");
                                    }
                                    
                                    if (foundIndividualDose)
                                    {
                                        sb.AppendLine("   ✓ Detected Prescription: " + (ptvPrescriptionDose.Dose / 100.0).ToString("F1") + " Gy");
                                        sb.AppendLine("   ✓ V95% Dose Level: " + (coverage95Dose.Dose / 100.0).ToString("F1") + " Gy");
                                    }
                                    else
                                    {
                                        sb.AppendLine("   ⚠️ Using Plan Total Dose: " + (ptvPrescriptionDose.Dose / 100.0).ToString("F1") + " Gy");
                                        sb.AppendLine("   ⚠️ V95% Dose Level: " + (coverage95Dose.Dose / 100.0).ToString("F1") + " Gy (may be incorrect for SiB)");
                                    }
                                    
                                    // PTV Coverage assessment
                                    if (volumeAt95 >= 95.0)
                                    {
                                        sb.AppendLine("   ✅ V95%: " + volumeAt95.ToString("F1") + "% - EXCELLENT COVERAGE");
                                    }
                                    else if (volumeAt95 >= 90.0)
                                    {
                                        sb.AppendLine("   ⚠️ V95%: " + volumeAt95.ToString("F1") + "% - Acceptable (>90%)");
                                    }
                                    else
                                    {
                                        sb.AppendLine("   🚨 V95%: " + volumeAt95.ToString("F1") + "% - POOR COVERAGE (<90%)");
                                        sb.AppendLine("      □ URGENT: Review plan optimization");
                                    }
                                    
                                    sb.AppendLine("   ✓ Max Dose: " + (ptvMaxDose.Dose / 100.0).ToString("F1") + " Gy");
                                    sb.AppendLine("   ✓ Mean Dose: " + (meanDose.Dose / 100.0).ToString("F1") + " Gy");
                                    sb.AppendLine();
                                }
                            }
                            catch
                            {
                                sb.AppendLine("📍 " + ptv.Id + " (PTV): Error calculating coverage statistics");
                                sb.AppendLine();
                            }
                        }
                    }
                    
                    // CTV Analysis (V95%, V98%, V99%)
                    if (ctvs.Any())
                    {
                        sb.AppendLine("📊 CTV COVERAGE ANALYSIS:");
                        sb.AppendLine("-------------------------");
                        
                        foreach (var ctv in ctvs.Take(5))
                        {
                            try
                            {
                                if (!ctv.IsEmpty)
                                {
                                    sb.AppendLine("📍 " + ctv.Id + " (CTV):");
                                    
                                    var prescriptionDose = plan.TotalDose;
                                    var coverage95Dose = new DoseValue(prescriptionDose.Dose * 0.95, prescriptionDose.Unit);
                                    var coverage98Dose = new DoseValue(prescriptionDose.Dose * 0.98, prescriptionDose.Unit);
                                    var coverage99Dose = new DoseValue(prescriptionDose.Dose * 0.99, prescriptionDose.Unit);
                                    
                                    var volumeAt95 = plan.GetVolumeAtDose(ctv, coverage95Dose, VolumePresentation.Relative);
                                    var volumeAt98 = plan.GetVolumeAtDose(ctv, coverage98Dose, VolumePresentation.Relative);
                                    var volumeAt99 = plan.GetVolumeAtDose(ctv, coverage99Dose, VolumePresentation.Relative);
                                    var ctvMaxDose = plan.GetDoseAtVolume(ctv, 0.0, VolumePresentation.Relative, DoseValuePresentation.Absolute);
                                    var meanDose = plan.GetDVHCumulativeData(ctv, DoseValuePresentation.Absolute, VolumePresentation.Relative, 1.0).MeanDose;
                                    
                                    sb.AppendLine("   ✓ V95%: " + volumeAt95.ToString("F1") + "%");
                                    sb.AppendLine("   ✓ V98%: " + volumeAt98.ToString("F1") + "%");
                                    sb.AppendLine("   ✓ V99%: " + volumeAt99.ToString("F1") + "%");
                                    sb.AppendLine("   ✓ Volume: " + ctv.Volume.ToString("F1") + " cc");
                                    sb.AppendLine("   ✓ Max Dose: " + (ctvMaxDose.Dose / 100.0).ToString("F1") + " Gy");
                                    sb.AppendLine("   ✓ Mean Dose: " + (meanDose.Dose / 100.0).ToString("F1") + " Gy");
                                    sb.AppendLine();
                                }
                            }
                            catch
                            {
                                sb.AppendLine("📍 " + ctv.Id + " (CTV): Error calculating coverage statistics");
                                sb.AppendLine();
                            }
                        }
                    }
                    
                    // GTV Analysis (V95%, V98%, V99%)
                    if (gtvs.Any())
                    {
                        sb.AppendLine("📊 GTV COVERAGE ANALYSIS:");
                        sb.AppendLine("-------------------------");
                        
                        foreach (var gtv in gtvs.Take(5))
                        {
                            try
                            {
                                if (!gtv.IsEmpty)
                                {
                                    sb.AppendLine("📍 " + gtv.Id + " (GTV):");
                                    
                                    var prescriptionDose = plan.TotalDose;
                                    var coverage95Dose = new DoseValue(prescriptionDose.Dose * 0.95, prescriptionDose.Unit);
                                    var coverage98Dose = new DoseValue(prescriptionDose.Dose * 0.98, prescriptionDose.Unit);
                                    var coverage99Dose = new DoseValue(prescriptionDose.Dose * 0.99, prescriptionDose.Unit);
                                    
                                    var volumeAt95 = plan.GetVolumeAtDose(gtv, coverage95Dose, VolumePresentation.Relative);
                                    var volumeAt98 = plan.GetVolumeAtDose(gtv, coverage98Dose, VolumePresentation.Relative);
                                    var volumeAt99 = plan.GetVolumeAtDose(gtv, coverage99Dose, VolumePresentation.Relative);
                                    var gtvMaxDose = plan.GetDoseAtVolume(gtv, 0.0, VolumePresentation.Relative, DoseValuePresentation.Absolute);
                                    var meanDose = plan.GetDVHCumulativeData(gtv, DoseValuePresentation.Absolute, VolumePresentation.Relative, 1.0).MeanDose;
                                    
                                    sb.AppendLine("   ✓ V95%: " + volumeAt95.ToString("F1") + "%");
                                    sb.AppendLine("   ✓ V98%: " + volumeAt98.ToString("F1") + "%");
                                    sb.AppendLine("   ✓ V99%: " + volumeAt99.ToString("F1") + "%");
                                    sb.AppendLine("   ✓ Volume: " + gtv.Volume.ToString("F1") + " cc");
                                    sb.AppendLine("   ✓ Max Dose: " + (gtvMaxDose.Dose / 100.0).ToString("F1") + " Gy");
                                    sb.AppendLine("   ✓ Mean Dose: " + (meanDose.Dose / 100.0).ToString("F1") + " Gy");
                                    sb.AppendLine();
                                }
                            }
                            catch
                            {
                                sb.AppendLine("📍 " + gtv.Id + " (GTV): Error calculating coverage statistics");
                                sb.AppendLine();
                            }
                        }
                    }
                    
                    // ITV Analysis (V95%, V98%, V99%)
                    if (itvs.Any())
                    {
                        sb.AppendLine("📊 ITV COVERAGE ANALYSIS:");
                        sb.AppendLine("-------------------------");
                        
                        foreach (var itv in itvs.Take(5))
                        {
                            try
                            {
                                if (!itv.IsEmpty)
                                {
                                    sb.AppendLine("📍 " + itv.Id + " (ITV):");
                                    
                                    var prescriptionDose = plan.TotalDose;
                                    var coverage95Dose = new DoseValue(prescriptionDose.Dose * 0.95, prescriptionDose.Unit);
                                    var coverage98Dose = new DoseValue(prescriptionDose.Dose * 0.98, prescriptionDose.Unit);
                                    var coverage99Dose = new DoseValue(prescriptionDose.Dose * 0.99, prescriptionDose.Unit);
                                    
                                    var volumeAt95 = plan.GetVolumeAtDose(itv, coverage95Dose, VolumePresentation.Relative);
                                    var volumeAt98 = plan.GetVolumeAtDose(itv, coverage98Dose, VolumePresentation.Relative);
                                    var volumeAt99 = plan.GetVolumeAtDose(itv, coverage99Dose, VolumePresentation.Relative);
                                    var itvMaxDose = plan.GetDoseAtVolume(itv, 0.0, VolumePresentation.Relative, DoseValuePresentation.Absolute);
                                    var meanDose = plan.GetDVHCumulativeData(itv, DoseValuePresentation.Absolute, VolumePresentation.Relative, 1.0).MeanDose;
                                    
                                    sb.AppendLine("   ✓ V95%: " + volumeAt95.ToString("F1") + "%");
                                    sb.AppendLine("   ✓ V98%: " + volumeAt98.ToString("F1") + "%");
                                    sb.AppendLine("   ✓ V99%: " + volumeAt99.ToString("F1") + "%");
                                    sb.AppendLine("   ✓ Volume: " + itv.Volume.ToString("F1") + " cc");
                                    sb.AppendLine("   ✓ Max Dose: " + (itvMaxDose.Dose / 100.0).ToString("F1") + " Gy");
                                    sb.AppendLine("   ✓ Mean Dose: " + (meanDose.Dose / 100.0).ToString("F1") + " Gy");
                                    sb.AppendLine();
                                }
                            }
                            catch
                            {
                                sb.AppendLine("📍 " + itv.Id + " (ITV): Error calculating coverage statistics");
                                sb.AppendLine();
                            }
                        }
                    }
                    
                    if (!targets.Any())
                    {
                        sb.AppendLine("❌ No target structures found for coverage analysis");
                        sb.AppendLine();
                    }
                    
                    // ═══════════════════════════════════════════════════════════════
                    // ORGAN AT RISK (OAR) DOSE ANALYSIS
                    // ═══════════════════════════════════════════════════════════════
                    if (oars.Any())
                    {
                        sb.AppendLine();
                        sb.AppendLine("🛡️ ORGAN AT RISK (OAR) DOSE SUMMARY:");
                        sb.AppendLine("====================================");
                        
                        var keyOARs = oars.Where(s => 
                            s.Id.ToUpper().Contains("SPINAL") ||
                            s.Id.ToUpper().Contains("CORD") ||
                            s.Id.ToUpper().Contains("BRAIN") ||
                            s.Id.ToUpper().Contains("LUNG") ||
                            s.Id.ToUpper().Contains("HEART") ||
                            s.Id.ToUpper().Contains("LIVER") ||
                            s.Id.ToUpper().Contains("KIDNEY") ||
                            s.Id.ToUpper().Contains("ESOPHAG") ||
                            s.Id.ToUpper().Contains("RECTUM") ||
                            s.Id.ToUpper().Contains("BLADDER")).Take(6);
                            
                        foreach (var oar in keyOARs)
                        {
                            try
                            {
                                if (!oar.IsEmpty)
                                {
                                    var oarMaxDose = plan.GetDoseAtVolume(oar, 0.0, VolumePresentation.Relative, DoseValuePresentation.Absolute);
                                    var meanDose = plan.GetDVHCumulativeData(oar, DoseValuePresentation.Absolute, VolumePresentation.Relative, 1.0).MeanDose;
                                    var volume = oar.Volume;
                                    
                                    sb.AppendLine("📍 " + oar.Id + ":");
                                    sb.AppendLine("   ✓ Max Dose: " + (oarMaxDose.Dose / 100.0).ToString("F1") + " Gy");
                                    sb.AppendLine("   ✓ Mean Dose: " + (meanDose.Dose / 100.0).ToString("F1") + " Gy");
                                    sb.AppendLine("   ✓ Volume: " + volume.ToString("F1") + " cc");
                                    sb.AppendLine();
                                }
                            }
                            catch
                            {
                                sb.AppendLine("📍 " + oar.Id + ": Error calculating dose statistics");
                                sb.AppendLine();
                            }
                        }
                    }
                }
                else
                {
                    sb.AppendLine();
                    sb.AppendLine("❌ No dose distribution available - plan may not be calculated");
                }
                
            }
            catch (Exception ex)
            {
                sb.AppendLine("❌ Error retrieving structure information: " + ex.Message);
            }
        }

        /// <summary>
        /// Advanced isocenter positioning and coordinate system verification
        /// 
        /// Verification Areas:
        /// - Patient setup information and positioning analysis
        /// - User origin location and body containment verification
        /// - Multiple isocenter detection and shift calculations
        /// - Clinical coordinate interpretation (X=L/R, Y=A/P, Z=S/I)
        /// - Setup BB structure identification
        /// - Individual axis shift alerts (>20cm threshold)
        /// - Target structure containment verification
        /// - Treatment couch requirements by machine
        /// 
        /// Critical Safety Checks:
        /// - Non-standard positioning alerts (Prone, Feet First, Decubitus)
        /// - User origin inside body structure verification
        /// - Isocenter inside body and target structure verification
        /// - Large shift detection on individual axes (>20cm)
        /// - Coordinate system validity for patient positioning
        /// - Machine-specific couch requirements
        /// 
        /// Coordinate System Handling:
        /// - Displays shifts in cm instead of DICOM coordinates
        /// - Position-aware directional interpretation
        /// - Critical alerts for geometry outside body contours
        /// </summary>
        /// <param name="plan">Treatment plan to analyze</param>
        /// <param name="sb">StringBuilder for output formatting</param>
        private void CheckIsocenterInformation(PlanSetup plan, StringBuilder sb)
        {
            sb.AppendLine("6. ISOCENTER & SETUP VERIFICATION:");
            sb.AppendLine("====================================");
            
            try
            {
                // Declare beams variable early for use throughout the method
                var beams = plan.Beams.Where(b => !b.IsSetupField).ToList();
                

                
                // Patient setup information first
                sb.AppendLine();
                sb.AppendLine("🧑‍⚕️ PATIENT SETUP INFORMATION:");
                sb.AppendLine("==============================");
                
                string patientOrientation = plan.TreatmentOrientation.ToString();
                sb.AppendLine("✓ Treatment Orientation: " + patientOrientation);
                
                // Analyze patient positioning and provide laterality guidance
                bool isHeadFirstSupine = patientOrientation.ToUpper().Contains("HEADFIRSTSUPINE");
                bool isProne = patientOrientation.ToUpper().Contains("PRONE");
                bool isFeetFirst = patientOrientation.ToUpper().Contains("FEETFIRST") || patientOrientation.ToUpper().Contains("FEET");
                bool isDecubitus = patientOrientation.ToUpper().Contains("DECUB");
                
                if (!isHeadFirstSupine)
                {
                    sb.AppendLine();
                    sb.AppendLine("⚠️ NON-STANDARD PATIENT POSITIONING DETECTED:");
                    if (isProne)
                    {
                        sb.AppendLine("   🔄 PRONE POSITIONING - Laterality interpretation may be reversed");
                        sb.AppendLine("     • Left/Right coordinates are from patient's perspective");
                        sb.AppendLine("     • Anterior/Posterior may be inverted from standard supine");
                    }
                    if (isFeetFirst)
                    {
                        sb.AppendLine("   🔄 FEET FIRST POSITIONING - Coordinate system differences");
                        sb.AppendLine("     • Superior/Inferior directions may be inverted");
                        sb.AppendLine("     • Verify field labels and setup instructions");
                    }
                    if (isDecubitus)
                    {
                        sb.AppendLine("   🔄 DECUBITUS POSITIONING - Special coordinate consideration");
                        sb.AppendLine("     • Lateral positioning affects all coordinate interpretations");
                        sb.AppendLine("     • Verify setup reproducibility and immobilization");
                    }
                    
                    sb.AppendLine();
                    sb.AppendLine("🚨 COORDINATE VERIFICATION REQUIRED:");
                    sb.AppendLine("   □ Verify coordinate system matches patient positioning");
                    sb.AppendLine("   □ Confirm laterality interpretation is correct");
                    sb.AppendLine("   □ Check field labels match actual anatomy");
                    sb.AppendLine("   □ Validate setup instructions with positioning");
                }
                else
                {
                    sb.AppendLine("✅ STANDARD HEAD FIRST SUPINE POSITIONING");
                    sb.AppendLine("   Standard coordinate interpretation applies");
                }
                
                // User Origin and Shift Analysis - MOVED TO TOP
                sb.AppendLine();
                sb.AppendLine("📐 USER ORIGIN & SHIFT ANALYSIS:");
                sb.AppendLine("===============================");
                
                try
                {
                    // Check for setup BB structures
                    var setupStructures = plan.StructureSet != null ? 
                        plan.StructureSet.Structures.Where(s => s != null && !s.IsEmpty && 
                            (s.Id.ToUpper().Contains("BB") || s.Id.ToUpper().Contains("ZBB"))).ToList() : 
                        new List<VMS.TPS.Common.Model.API.Structure>();
                    
                    if (setupStructures.Count > 0)
                    {
                        sb.AppendLine("✅ Setup structures (BB) found:");
                        foreach (var structure in setupStructures.Take(3))
                        {
                            sb.AppendLine("   • " + structure.Id);
                        }
                    }
                    else
                    {
                        sb.AppendLine("⚠️ No setup structures (BB, zBB) found");
                    }
                    sb.AppendLine();
                    
                    var userOrigin = plan.StructureSet.Image.UserOrigin;
                    sb.AppendLine("✓ User Origin Position: (" + (userOrigin.x/10).ToString("F1") + ", " + (userOrigin.y/10).ToString("F1") + ", " + (userOrigin.z/10).ToString("F1") + ") cm");
                    
                    sb.AppendLine("   Clinical Coordinates (" + patientOrientation + "):");
                    if (isHeadFirstSupine)
                    {
                        sb.AppendLine("     • X (Left/Right): " + (userOrigin.x/10).ToString("F1") + " cm " + (userOrigin.x >= 0 ? "(Right)" : "(Left)"));
                        sb.AppendLine("     • Y (Ant/Post): " + (userOrigin.y/10).ToString("F1") + " cm " + (userOrigin.y >= 0 ? "(Anterior)" : "(Posterior)"));
                        sb.AppendLine("     • Z (Sup/Inf): " + (userOrigin.z/10).ToString("F1") + " cm " + (userOrigin.z >= 0 ? "(Superior)" : "(Inferior)"));
                    }
                    else
                    {
                        sb.AppendLine("     • X: " + (userOrigin.x/10).ToString("F1") + " cm (⚠️ Verify L/R interpretation)");
                        sb.AppendLine("     • Y: " + (userOrigin.y/10).ToString("F1") + " cm (⚠️ Verify A/P interpretation)");
                        sb.AppendLine("     • Z: " + (userOrigin.z/10).ToString("F1") + " cm (⚠️ Verify S/I interpretation)");
                    }
                    
                    // Check if user origin is inside body structures
                    sb.AppendLine("   User Origin Location Verification:");
                    var bodyStructures = plan.StructureSet.Structures.Where(s => 
                        s.DicomType == "EXTERNAL" || 
                        s.Id.ToUpper().Contains("BODY") || 
                        s.Id.ToUpper().Contains("EXTERNAL")).ToList();
                    
                    bool userOriginInBody = false;
                    foreach (var bodyStruct in bodyStructures)
                    {
                        try
                        {
                            if (!bodyStruct.IsEmpty && bodyStruct.IsPointInsideSegment(userOrigin))
                            {
                                userOriginInBody = true;
                                sb.AppendLine("     ✅ User Origin inside BODY structure: " + bodyStruct.Id);
                                break;
                            }
                        }
                        catch
                        {
                            // Continue checking other structures
                        }
                    }
                    
                    if (!userOriginInBody && bodyStructures.Count > 0)
                    {
                        sb.AppendLine("     🚨 CRITICAL: User Origin NOT inside BODY structure - VERIFY PLACEMENT");
                        sb.AppendLine("       This may cause coordinate system issues!");
                    }
                    else if (bodyStructures.Count == 0)
                    {
                        sb.AppendLine("     ⚠️ No BODY structure found - cannot verify user origin location");
                    }
                }
                catch
                {
                    sb.AppendLine("❌ User Origin: (coordinates unavailable)");
                    sb.AppendLine("   Cannot calculate shifts without user origin information");
                }
                
                // Enhanced isocenter information from all treatment beams
                sb.AppendLine();
                sb.AppendLine("🎯 ISOCENTER INFORMATION:");
                sb.AppendLine("========================");
                
                if (beams.Count > 0)
                {
                    // Collect all unique isocenter positions
                    var isocenterPositions = new Dictionary<string, VMS.TPS.Common.Model.Types.VVector>();
                    var isocenterBeamCounts = new Dictionary<string, int>();
                    
                    foreach (var beam in beams)
                    {
                        var isocenter = beam.IsocenterPosition;
                        string positionKey = (isocenter.x/10).ToString("F1") + "," + (isocenter.y/10).ToString("F1") + "," + (isocenter.z/10).ToString("F1");
                        
                        if (!isocenterPositions.ContainsKey(positionKey))
                        {
                            isocenterPositions[positionKey] = isocenter;
                            isocenterBeamCounts[positionKey] = 0;
                        }
                        isocenterBeamCounts[positionKey]++;
                    }
                    
                    sb.AppendLine("✓ Total Isocenters: " + isocenterPositions.Count);
                    sb.AppendLine();
                    
                    // Get user origin for shift calculations
                    var userOrigin = plan.StructureSet.Image.UserOrigin;
                    
                    int isocenterIndex = 1;
                    foreach (var kvp in isocenterPositions)
                    {
                        var isocenter = kvp.Value;
                        var beamCount = isocenterBeamCounts[kvp.Key];
                        
                        sb.AppendLine("📍 ISOCENTER " + isocenterIndex + " (" + beamCount + " beam" + (beamCount > 1 ? "s" : "") + "):");
                        
                        // Calculate shifts from user origin
                        double shiftX = (isocenter.x - userOrigin.x) / 10.0;
                        double shiftY = (isocenter.y - userOrigin.y) / 10.0;
                        double shiftZ = (isocenter.z - userOrigin.z) / 10.0;
                        double totalShift = Math.Sqrt(shiftX * shiftX + shiftY * shiftY + shiftZ * shiftZ);
                        
                        sb.AppendLine("   Shifts from User Origin (" + patientOrientation + "):");
                        if (isHeadFirstSupine)
                        {
                            sb.AppendLine("     • X Shift: " + shiftX.ToString("F1") + " cm " + (shiftX >= 0 ? "(Right)" : "(Left)"));
                            sb.AppendLine("     • Y Shift: " + shiftY.ToString("F1") + " cm " + (shiftY >= 0 ? "(Anterior)" : "(Posterior)"));
                            sb.AppendLine("     • Z Shift: " + shiftZ.ToString("F1") + " cm " + (shiftZ >= 0 ? "(Superior)" : "(Inferior)"));
                        }
                        else
                        {
                            sb.AppendLine("     • X Shift: " + shiftX.ToString("F1") + " cm (⚠️ Verify direction with " + patientOrientation + ")");
                            sb.AppendLine("     • Y Shift: " + shiftY.ToString("F1") + " cm (⚠️ Verify direction with " + patientOrientation + ")");
                            sb.AppendLine("     • Z Shift: " + shiftZ.ToString("F1") + " cm (⚠️ Verify direction with " + patientOrientation + ")");
                        }
                        // Check for large shifts (>20cm) on individual axes
                        var largeShifts = new List<string>();
                        if (Math.Abs(shiftX) > 20)
                            largeShifts.Add("X: " + Math.Abs(shiftX).ToString("F1") + " cm");
                        if (Math.Abs(shiftY) > 20)
                            largeShifts.Add("Y: " + Math.Abs(shiftY).ToString("F1") + " cm");
                        if (Math.Abs(shiftZ) > 20)
                            largeShifts.Add("Z: " + Math.Abs(shiftZ).ToString("F1") + " cm");
                        
                        if (largeShifts.Count > 0)
                        {
                            sb.AppendLine("     🚨 LARGE SHIFT ALERT (>20cm detected):");
                            foreach (var shift in largeShifts)
                            {
                                sb.AppendLine("       • " + shift);
                            }
                            sb.AppendLine("       VERIFY: Isocenter placement and coordinate system");
                        }
                        
                        // Check if isocenter is inside body and target structures
                        sb.AppendLine("   Isocenter Location Verification:");
                        
                        // Check if isocenter is inside body structures
                        var bodyStructures = plan.StructureSet.Structures.Where(s => 
                            s.DicomType == "EXTERNAL" || 
                            s.Id.ToUpper().Contains("BODY") || 
                            s.Id.ToUpper().Contains("EXTERNAL")).ToList();
                        
                        bool isocenterInBody = false;
                        foreach (var bodyStruct in bodyStructures)
                        {
                            try
                            {
                                if (!bodyStruct.IsEmpty && bodyStruct.IsPointInsideSegment(isocenter))
                                {
                                    isocenterInBody = true;
                                    sb.AppendLine("     ✅ Inside BODY structure: " + bodyStruct.Id);
                                    break;
                                }
                            }
                            catch
                            {
                                // Continue checking other structures
                            }
                        }
                        
                        if (!isocenterInBody && bodyStructures.Count > 0)
                        {
                            sb.AppendLine("     🚨 CRITICAL: Isocenter NOT inside BODY structure - VERIFY PLACEMENT");
                            sb.AppendLine("       Invalid treatment geometry - immediate review required!");
                        }
                        else if (bodyStructures.Count == 0)
                        {
                            sb.AppendLine("     ⚠️ No BODY structure found - cannot verify isocenter location");
                        }
                        
                        // Check if isocenter is inside target structures
                        var targetStructures = plan.StructureSet.Structures.Where(s => 
                            s.DicomType == "PTV" || s.DicomType == "CTV" || s.DicomType == "GTV" ||
                            s.Id.ToUpper().Contains("PTV") || s.Id.ToUpper().Contains("CTV") || 
                            s.Id.ToUpper().Contains("GTV") || s.Id.ToUpper().Contains("ITV")).ToList();
                        
                        var isocenterInTargets = new List<string>();
                        foreach (var targetStruct in targetStructures)
                        {
                            try
                            {
                                if (!targetStruct.IsEmpty && targetStruct.IsPointInsideSegment(isocenter))
                                {
                                    isocenterInTargets.Add(targetStruct.Id);
                                }
                            }
                            catch
                            {
                                // Continue checking other structures
                            }
                        }
                        
                        if (isocenterInTargets.Count > 0)
                        {
                            sb.AppendLine("     ✅ Inside target structure(s):");
                            foreach (var target in isocenterInTargets)
                            {
                                sb.AppendLine("       • " + target);
                            }
                        }
                        else if (targetStructures.Count > 0)
                        {
                            sb.AppendLine("     ⚠️ NOT inside any target structures");
                            sb.AppendLine("       Available targets: " + string.Join(", ", targetStructures.Select(s => s.Id).Take(3)));
                            if (targetStructures.Count > 3)
                            {
                                sb.AppendLine("       ... and " + (targetStructures.Count - 3) + " more");
                            }
                        }
                        else
                        {
                            sb.AppendLine("     ⚠️ No target structures (PTV/CTV/GTV/ITV) found");
                        }
                        
                        sb.AppendLine();
                        isocenterIndex++;
                    }
                    
                    // Show which beams use which isocenter
                    if (isocenterPositions.Count > 1)
                    {
                        sb.AppendLine("🔍 BEAM ISOCENTER ASSIGNMENTS:");
                        foreach (var kvp in isocenterPositions)
                        {
                            var isocenter = kvp.Value;
                            var beamsAtThisIso = beams.Where(b => 
                                Math.Abs(b.IsocenterPosition.x - isocenter.x) < 1.0 &&
                                Math.Abs(b.IsocenterPosition.y - isocenter.y) < 1.0 &&
                                Math.Abs(b.IsocenterPosition.z - isocenter.z) < 1.0).ToList();
                            
                            sb.AppendLine("   Isocenter (" + (isocenter.x/10).ToString("F1") + ", " + (isocenter.y/10).ToString("F1") + ", " + (isocenter.z/10).ToString("F1") + " cm):");
                            foreach (var beam in beamsAtThisIso)
                            {
                                sb.AppendLine("     • " + beam.Id);
                            }
                            sb.AppendLine();
                        }
                    }
                }
                else
                {
                    sb.AppendLine("❌ No treatment beams found");
                }
                
                // Treatment Couch Verification - MOVED TO BOTTOM
                sb.AppendLine();
                sb.AppendLine("🛏️ TREATMENT COUCH VERIFICATION:");
                sb.AppendLine("================================");
                
                var treatmentMachines = new List<string>();
                
                foreach (var beam in beams)
                {
                    string machineId = beam.TreatmentUnit.Id;
                    if (!treatmentMachines.Contains(machineId))
                    {
                        treatmentMachines.Add(machineId);
                    }
                }
                
                // Check for head/brain site
                string planNameUpper = plan.Name != null ? plan.Name.ToUpper() : "";
                string planIdUpper = plan.Id.ToUpper();
                
                bool isHeadBrainSite = planIdUpper.Contains("HEAD") || planNameUpper.Contains("HEAD") ||
                                      planIdUpper.Contains("BRAIN") || planNameUpper.Contains("BRAIN") ||
                                      planIdUpper.Contains("CNS") || planNameUpper.Contains("CNS") ||
                                      planIdUpper.Contains("SRS") || planNameUpper.Contains("SRS");
                
                if (isHeadBrainSite)
                {
                    sb.AppendLine("🧠 HEAD/BRAIN SITE DETECTED");
                    sb.AppendLine("✅ NO COUCH NEEDED for head/brain treatments");
                    sb.AppendLine("   □ Couch excluded from dose calculation");
                    sb.AppendLine("   □ Head rest/immobilization system verified");
                }
                else
                {
                    sb.AppendLine("📍 NON-HEAD SITE: Couch verification required");
                    
                    if (treatmentMachines.Count > 0)
                    {
                        sb.AppendLine();
                        sb.AppendLine("🏭 MACHINE-SPECIFIC COUCH REQUIREMENTS:");
                        foreach (var machine in treatmentMachines)
                        {
                            string machineUpper = machine.ToUpper();
                            sb.AppendLine("   Machine: " + machine);
                            
                            if (machineUpper.Contains("LINAC1") || machineUpper.Contains("TB1"))
                            {
                                sb.AppendLine("     ➤ REQUIRED: BrainLAB/iBeam Couch");
                            }
                            else if (machineUpper.Contains("LINAC2") || machineUpper.Contains("TB2"))
                            {
                                sb.AppendLine("     ➤ REQUIRED: Exact IGRT Couch (Thin)");
                            }
                            else
                            {
                                sb.AppendLine("     ➤ VERIFY: Check machine-specific couch requirements");
                            }
                        }
                    }
                    
                    sb.AppendLine();
                    sb.AppendLine("📋 COUCH VERIFICATION CHECKLIST:");
                    sb.AppendLine("   □ Correct couch model selected in Eclipse");
                    sb.AppendLine("   □ Couch included in dose calculation");
                    sb.AppendLine("   □ Couch attenuation data current");
                    sb.AppendLine("   □ No couch-gantry collision issues");
                }
                
            }
            catch (Exception ex)
            {
                sb.AppendLine("❌ Error retrieving isocenter information: " + ex.Message);
            }
        }

        /// <summary>
        /// Final plan approval status and treatment readiness verification
        /// 
        /// Verification Areas:
        /// - Plan approval status (Planning vs Treatment approved)
        /// - Approval dates and workflow tracking
        /// - Basic plan readiness assessment
        /// - Critical component validation
        /// 
        /// Safety Checks:
        /// - Dose calculation presence
        /// - Beam definitions completeness
        /// - Structure set association
        /// - Overall treatment readiness
        /// </summary>
        /// <param name="plan">Treatment plan to analyze</param>
        /// <param name="sb">StringBuilder for output formatting</param>
        private void CheckPlanStatus(PlanSetup plan, StringBuilder sb)
        {
            sb.AppendLine("7. COMPREHENSIVE PLAN VERIFICATION CHECKLIST:");
            sb.AppendLine("==============================================");
            
            try
            {
                // Plan Status Overview
                sb.AppendLine("📋 PLAN STATUS OVERVIEW:");
                sb.AppendLine("========================");
                sb.AppendLine("✓ Plan Approval Status: " + plan.ApprovalStatus.ToString());
                
                if (plan.PlanningApprovalDate != null)
                {
                    sb.AppendLine("✓ Planning Approval Date: " + plan.PlanningApprovalDate.ToString());
                }
                
                if (plan.TreatmentApprovalDate != null)
                {
                    sb.AppendLine("✓ Treatment Approval Date: " + plan.TreatmentApprovalDate.ToString());
                }
                
                // Automated Plan Verification Checklist
                sb.AppendLine();
                sb.AppendLine("🔍 AUTOMATED PLAN VERIFICATION RESULTS:");
                sb.AppendLine("=======================================");
                
                // Bolus Check
                bool hasBolus = false;
                if (plan.Beams != null && plan.Beams.Any())
                {
                    hasBolus = plan.Beams.Any(b => b.Boluses != null && b.Boluses.Any());
                }
                sb.AppendLine("🛡️ Bolus Check: " + (hasBolus ? "✅ BOLUS DETECTED - Add to Mosaiq fields & site setup notes" : "✅ No bolus detected"));
                
                // Couch Check
                bool hasCouch = false;
                string couchInfo = "No couch detected";
                if (plan.Beams != null && plan.Beams.Any())
                {
                    var beamWithCouch = plan.Beams.FirstOrDefault(b => b.TreatmentUnit != null && 
                        (b.TreatmentUnit.Id.ToUpper().Contains("COUCH") || 
                         (plan.StructureSet != null && plan.StructureSet.Structures.Any(s => s.Id.ToUpper().Contains("COUCH")))));
                    if (beamWithCouch != null || (plan.StructureSet != null && plan.StructureSet.Structures.Any(s => s.Id.ToUpper().Contains("COUCH"))))
                    {
                        hasCouch = true;
                        couchInfo = "Couch detected - verify correct couch for treatment machine";
                    }
                }
                sb.AppendLine("🛏️ Couch Check: " + (hasCouch ? "⚠️ " + couchInfo : "✅ " + couchInfo));
                
                // CT Slice Check
                int ctSlices = 0;
                if (plan.StructureSet != null && plan.StructureSet.Image != null)
                {
                    ctSlices = plan.StructureSet.Image.ZSize;
                }
                sb.AppendLine("🖼️ CT Slice Check: " + (ctSlices > 399 ? "🚨 " + ctSlices + " slices - EXCESSIVE (>399)" : "✅ " + ctSlices + " slices - Acceptable"));
                
                // Patient Orientation Check
                string patientOrientation = plan.TreatmentOrientation.ToString();
                bool isStandardOrientation = patientOrientation.Contains("HeadFirstSupine") || patientOrientation.Contains("HFS");
                sb.AppendLine("🧑‍⚕️ Patient Orientation: " + (isStandardOrientation ? "✅ " + patientOrientation : "⚠️ " + patientOrientation + " - Verify coordinate interpretation"));
                
                // SiB Detection
                var structures = plan.StructureSet != null ? plan.StructureSet.Structures.ToList() : new List<VMS.TPS.Common.Model.API.Structure>();
                var ptvs = structures.Where(s => s.DicomType == "PTV" || s.Id.ToUpper().Contains("PTV")).ToList();
                bool isSiB = ptvs.Count > 1;
                sb.AppendLine("💊 SiB Check: " + (isSiB ? "🚨 SiB DETECTED (" + ptvs.Count + " PTVs) - Verify multiple dose levels in Mosaiq" : "✅ Single PTV plan"));
                
                // Prescription Check
                sb.AppendLine("💊 Prescription Dose: " + (plan.TotalDose != null ? "✅ " + (plan.TotalDose.Dose / 100.0).ToString("F1") + " Gy" : "❌ Not defined"));
                sb.AppendLine("💊 Fractionation: " + (plan.NumberOfFractions != null ? "✅ " + plan.NumberOfFractions + " fractions" : "❌ Not defined"));
                
                // Plan Normalization Information
                if (plan.PlanNormalizationValue != 0)
                {
                    sb.AppendLine("💊 Plan Normalization: ✅ " + plan.PlanNormalizationValue.ToString("F1") + "%");
                }
                else
                {
                    sb.AppendLine("💊 Plan Normalization: ⚠️ Not set (0%)");
                }
                
                if (plan.PlanNormalizationMethod != null)
                {
                    sb.AppendLine("💊 Normalization Method: ✅ " + plan.PlanNormalizationMethod.ToString());
                }
                else
                {
                    sb.AppendLine("💊 Normalization Method: ⚠️ Not defined");
                }
                
                // PTV Coverage Check (simplified)
                string ptvCoverage = "✅ Refer to Dose/Structures tabs for detailed coverage";
                if (ptvs.Any() && plan.Dose != null)
                {
                    try
                    {
                        var firstPTV = ptvs.First();
                        if (!firstPTV.IsEmpty)
                        {
                            var prescDose = plan.TotalDose;
                            var v95Dose = new DoseValue(prescDose.Dose * 0.95, prescDose.Unit);
                            var coverage = plan.GetVolumeAtDose(firstPTV, v95Dose, VolumePresentation.Relative);
                            ptvCoverage = coverage >= 95.0 ? "✅ Primary PTV V95%: " + coverage.ToString("F1") + "%" : 
                                         coverage >= 90.0 ? "⚠️ Primary PTV V95%: " + coverage.ToString("F1") + "%" : 
                                         "🚨 Primary PTV V95%: " + coverage.ToString("F1") + "% - REVIEW REQUIRED";
                        }
                    }
                    catch
                    {
                        ptvCoverage = "⚠️ Unable to calculate - check Dose/Structures tabs";
                    }
                }
                sb.AppendLine("🎯 PTV Coverage: " + ptvCoverage);
                
                // Treatment Machine Check
                var machines = plan.Beams != null ? plan.Beams.Where(b => !b.IsSetupField).Select(b => b.TreatmentUnit.Id).Distinct().ToList() : new List<string>();
                sb.AppendLine("🏭 Treatment Machine: " + (machines.Any() ? "✅ " + string.Join(", ", machines) : "❌ No treatment beams"));
                
                // PTV Length vs Linac Compatibility Check
                bool hasLinac2Compatibility = true;
                string ptvLengthSummary = "No PTVs to analyze";
                
                if (ptvs.Any())
                {
                    double maxPtvLength = 0.0;
                    string longestPtvName = "";
                    
                    foreach (var ptv in ptvs)
                    {
                        try
                        {
                            if (!ptv.IsEmpty)
                            {
                                var bounds = ptv.MeshGeometry.Bounds;
                                double ptvLength = Math.Abs(bounds.SizeZ) / 10.0; // Convert mm to cm
                                
                                if (ptvLength > maxPtvLength)
                                {
                                    maxPtvLength = ptvLength;
                                    longestPtvName = ptv.Id;
                                }
                                
                                if (ptvLength > 20.0)
                                {
                                    hasLinac2Compatibility = false;
                                }
                            }
                        }
                        catch
                        {
                            // Continue checking other PTVs
                        }
                    }
                    
                    if (maxPtvLength > 0)
                    {
                        if (hasLinac2Compatibility)
                        {
                            ptvLengthSummary = "✅ Max PTV length: " + maxPtvLength.ToString("F1") + " cm (All linacs compatible)";
                        }
                        else
                        {
                            ptvLengthSummary = "🚨 Max PTV length: " + maxPtvLength.ToString("F1") + " cm - LINAC2 INCOMPATIBLE (>20cm)";
                        }
                    }
                }
                
                sb.AppendLine("📏 PTV Length Check: " + ptvLengthSummary);
                
                // Beam Checks
                var treatmentBeams = plan.Beams != null ? plan.Beams.Where(b => !b.IsSetupField).ToList() : new List<VMS.TPS.Common.Model.API.Beam>();
                var setupBeams = plan.Beams != null ? plan.Beams.Where(b => b.IsSetupField).ToList() : new List<VMS.TPS.Common.Model.API.Beam>();
                
                if (treatmentBeams.Any())
                {
                    var energies = treatmentBeams.Select(b => b.EnergyModeDisplayName).Distinct();
                    var energyModes = treatmentBeams.Select(b => b.EnergyModeDisplayName.ToUpper()).ToList();
                    
                    // Check for special beam types first
                    bool hasElectronBeams = energyModes.Any(e => e.Contains("E") && (e.Contains("MEV") || char.IsDigit(e[e.Length-1])));
                    bool hasProtonBeams = energyModes.Any(e => e.Contains("PROTON") || e.Contains("P"));
                    
                    var techniques = new List<string>();
                    var mlcTypes = new List<string>();
                    
                    if (hasElectronBeams || hasProtonBeams)
                    {
                        // Handle electron and proton beams
                        if (hasElectronBeams && hasProtonBeams)
                        {
                            techniques.Add("Electron + Proton");
                        }
                        else if (hasElectronBeams)
                        {
                            techniques.Add("Electron");
                        }
                        else if (hasProtonBeams)
                        {
                            techniques.Add("Proton");
                        }
                        
                        // For electron/proton beams, show energy instead of MLC
                        sb.AppendLine("⚡ Beam Energy: ✅ " + string.Join(", ", energies));
                        sb.AppendLine("⚡ Beam Technique: ✅ " + string.Join(", ", techniques) + " therapy");
                    }
                    else
                    {
                        // Photon beams - determine technique based on MLC plan type
                        mlcTypes = treatmentBeams.Select(b => b.MLCPlanType.ToString()).Distinct().ToList();
                        
                        foreach (var mlcType in mlcTypes)
                        {
                            string technique = "";
                            if (mlcType.ToUpper().Contains("VMAT"))
                            {
                                technique = "VMAT";
                            }
                            else if (mlcType.ToUpper().Contains("IMRT"))
                            {
                                technique = "IMRT";
                            }
                            else if (mlcType.ToUpper().Contains("STATIC") || 
                                     mlcType.ToUpper().Contains("ARC") || 
                                     mlcType.ToUpper().Contains("DYNAMIC") ||
                                     mlcType.ToUpper().Contains("CONFORMALSTATICANGLE") ||
                                     mlcType.ToUpper().Contains("CONFORMALARC"))
                            {
                                technique = "3D-CRT";
                            }
                            else
                            {
                                technique = "3D-CRT"; // Default for all other MLC types
                            }
                            
                            if (!techniques.Contains(technique))
                            {
                                techniques.Add(technique);
                            }
                        }
                        
                        sb.AppendLine("⚡ Beam Energy: ✅ " + string.Join(", ", energies));
                        sb.AppendLine("⚡ Beam Technique: ✅ " + string.Join(", ", techniques) + " (MLC: " + string.Join(", ", mlcTypes) + ")");
                    }
                }
                
                // DRR Check
                int beamsWithDRR = treatmentBeams.Count(b => b.ReferenceImage != null) + setupBeams.Count(b => b.ReferenceImage != null);
                int totalBeams = treatmentBeams.Count + setupBeams.Count;
                sb.AppendLine("📷 DRR Check: " + (beamsWithDRR == totalBeams ? "✅ All beams have DRRs" : 
                    "⚠️ " + beamsWithDRR + "/" + totalBeams + " beams have DRRs - Complete in Mosaiq"));
                
                // CT Scan Type Check (simplified)
                string ctType = "Standard CT";
                if (plan.StructureSet != null && plan.StructureSet.Image != null)
                {
                    string imageId = plan.StructureSet.Image.Id.ToUpper();
                    if (imageId.Contains("4DCT") || imageId.Contains("4D")) ctType = "4DCT";
                    else if (imageId.Contains("ABC") || imageId.Contains("IABC")) ctType = "iABC/eABC";
                    else if (imageId.Contains("FB") || imageId.Contains("FREE")) ctType = "Free Breathing";
                    else if (imageId.Contains("AVE") || imageId.Contains("MEAN")) ctType = "Average/Mean";
                }
                sb.AppendLine("🫁 CT Scan Type: ✅ " + ctType + (ctType != "Standard CT" ? " - Add note to Rx & site setup" : ""));
                
                // Setup Structures Check
                var setupStructures = structures.Where(s => s.Id.ToUpper().Contains("BB") || s.Id.ToUpper().Contains("ZBB")).ToList();
                sb.AppendLine("📍 Setup Structures: " + (setupStructures.Any() ? "✅ " + setupStructures.Count + " BB structures found" : "⚠️ No BB structures detected"));
                
                // User Origin & Isocenter Checks
                bool userOriginInBody = false;
                bool isocenterInPTV = false;
                
                if (plan.StructureSet != null && plan.StructureSet.Image != null)
                {
                    var bodyStructure = structures.FirstOrDefault(s => s.DicomType == "EXTERNAL" || s.Id.ToUpper().Contains("BODY"));
                    if (bodyStructure != null && !bodyStructure.IsEmpty)
                    {
                        try
                        {
                            userOriginInBody = bodyStructure.IsPointInsideSegment(plan.StructureSet.Image.UserOrigin);
                        }
                        catch { }
                        
                        if (plan.Beams != null && plan.Beams.Any())
                        {
                            var firstBeam = plan.Beams.First();
                            var isocenter = firstBeam.IsocenterPosition;
                            var targetStructure = ptvs.FirstOrDefault();
                            if (targetStructure != null && !targetStructure.IsEmpty)
                            {
                                try
                                {
                                    isocenterInPTV = targetStructure.IsPointInsideSegment(isocenter);
                                }
                                catch { }
                            }
                        }
                    }
                }
                
                sb.AppendLine("📍 User Origin in Body: " + (userOriginInBody ? "✅ Yes" : "🚨 NO - CRITICAL CHECK"));
                sb.AppendLine("📍 Isocenter in PTV: " + (isocenterInPTV ? "✅ Yes" : "⚠️ Check isocenter location"));
                
                // Shift Check
                if (plan.StructureSet != null && plan.StructureSet.Image != null && plan.Beams != null && plan.Beams.Any())
                {
                    var userOrigin = plan.StructureSet.Image.UserOrigin;
                    var isocenter = plan.Beams.First().IsocenterPosition;
                    
                    double shiftX = Math.Abs(isocenter.x - userOrigin.x) / 10.0; // Convert to cm
                    double shiftY = Math.Abs(isocenter.y - userOrigin.y) / 10.0;
                    double shiftZ = Math.Abs(isocenter.z - userOrigin.z) / 10.0;
                    
                    bool largeShift = shiftX > 20 || shiftY > 20 || shiftZ > 20;
                    sb.AppendLine("📏 Shift Check: " + (largeShift ? "🚨 LARGE SHIFT (>20cm) - Verify coordinates" : "✅ Standard shifts (<20cm)"));
                }
                
                // Empty Structures Check
                var emptyStructures = structures.Where(s => s.IsEmpty).ToList();
                sb.AppendLine("📦 Empty Structures: " + (emptyStructures.Any() ? "⚠️ " + emptyStructures.Count + " empty structures found" : "✅ No empty structures"));
                
                // Mosaiq Checklist
                sb.AppendLine();
                sb.AppendLine("📋 MOSAIQ COMMON CHECKLIST ITEMS:");
                sb.AppendLine("=================================");
                sb.AppendLine("□ Complete QCLs in Mosaiq");
                sb.AppendLine("□ Attach DRRs in Mosaiq");
                sb.AppendLine("□ Check Dosimetry adds up in Mosaiq");
                sb.AppendLine("□ Check Prescription is filled out");
                sb.AppendLine("□ Check Prescription imaging notes added");
                sb.AppendLine("□ Check High Dose Mode is used for SRS mode only (Not just SBRT cases)");
                sb.AppendLine("□ Check Approve Site Setup in Mosaiq");
                sb.AppendLine("□ Check Plan Documents are Planner approved in Mosaiq");
                sb.AppendLine("□ Check Shift transferred correctly in Mosaiq");
                sb.AppendLine("□ Check Patient Setup image and description is consistent");
                sb.AppendLine("□ Check SiB in Rx if found in plan");
                sb.AppendLine("□ Check ClearCheck template used and in plan report");
                sb.AppendLine("□ Check ISO or Calc Point or Reference point exist inside PTV");
                sb.AppendLine("□ Check Hotspots are reasonable");
                sb.AppendLine("□ Check Dose Fall Off are reasonable");
                sb.AppendLine("□ Check BEV for flash and margins");
                
                if (hasBolus)
                {
                    sb.AppendLine();
                    sb.AppendLine("🛡️ BOLUS SPECIFIC REMINDERS:");
                    sb.AppendLine("□ Add custom bolus to fields in Mosaiq");
                    sb.AppendLine("□ Add bolus notes to site setup");
                    sb.AppendLine("□ Verify bolus thickness and material");
                }
                
                if (hasCouch)
                {
                    sb.AppendLine();
                    sb.AppendLine("🛏️ COUCH SPECIFIC REMINDERS:");
                    sb.AppendLine("□ Verify correct couch model for treatment machine");
                    sb.AppendLine("□ Linac1: BrainLAB/iBeam couch");
                    sb.AppendLine("□ Linac2: Exact IGRT Couch, thin");
                }
                
                if (isSiB)
                {
                    sb.AppendLine();
                    sb.AppendLine("💊 SiB SPECIFIC REMINDERS:");
                    sb.AppendLine("□ Verify multiple dose levels in Mosaiq prescription");
                    sb.AppendLine("□ Check dose gradients between PTVs are acceptable");
                    sb.AppendLine("□ Confirm OAR constraints met for highest dose level");
                    sb.AppendLine("□ Document clinical rationale for simultaneous boost");
                }
                
                if (ctType != "Standard CT")
                {
                    sb.AppendLine();
                    sb.AppendLine("🫁 MOTION MANAGEMENT REMINDERS:");
                    sb.AppendLine("□ Add " + ctType + " note to plan documents");
                    sb.AppendLine("□ Add " + ctType + " note to Rx");
                    sb.AppendLine("□ Add " + ctType + " note to site setup");
                    sb.AppendLine("□ Verify motion management protocol followed");
                }
                
            }
            catch (Exception ex)
            {
                sb.AppendLine("❌ Error retrieving plan verification status: " + ex.Message);
            }
        }

        #endregion

        // End of PlanCheckWindow class
        // 
        // This comprehensive verification tool provides automated quality assurance
        // for Eclipse treatment plans across all major clinical domains:
        // 
        // 1. Plan Information    - Prescription, technique, positioning
        // 2. Dose Information    - Calculation, normalization, algorithms  
        // 3. Beam Information    - Parameters, delivery, safety limits
        // 4. Structure Info      - Contours, artifacts, density overrides
        // 5. Isocenter Info      - Positioning, coordinates, geometry
        // 6. Plan Status         - Approval workflow, readiness
        //
        // Critical safety features include coordinate system validation,
        // large shift detection, body containment verification, and
        // comprehensive clinical parameter checking for Mosaiq integration.
    }
}
