using System;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.IO;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;

[assembly: AssemblyVersion("1.0.0.1")]

namespace VMS.TPS
{
    public class Script
    {
        public Script()
        {
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Execute(ScriptContext context)
        {
            try
            {
                // Validate required plan components
                if (context.PlanSetup == null)
                {
                    MessageBox.Show("No plan is currently loaded. Please load a plan and try again.", "Plan Check Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Launch the plan check application (non-blocking)
                var planCheckWindow = new PlanCheckWindow(context);
                planCheckWindow.Show(); // Use Show() instead of ShowDialog() to allow Eclipse to remain accessible
                
                // Script exits here, but window stays open independently
            }
            catch (Exception ex)
            {
                MessageBox.Show("Script execution error: " + ex.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    public class PlanCheckWindow : Window
    {
        private ScriptContext scriptContext;
        private string patientId;
        private string courseId;
        private string planId;
        private TabControl tabControl;
        private Dictionary<string, TextBox> tabTextBoxes = new Dictionary<string, TextBox>();

        public PlanCheckWindow(ScriptContext scriptContext)
        {
            this.scriptContext = scriptContext;
            this.patientId = scriptContext.Patient.Id;
            this.courseId = scriptContext.Course.Id;
            this.planId = scriptContext.PlanSetup.Id;
            InitializeComponents();
            RunPlanCheck();
        }

        private void InitializeComponents()
        {
            // Window setup
            this.Title = "Plan Check - " + planId;
            this.Width = 900;
            this.Height = 700;
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            this.Background = Brushes.WhiteSmoke;

            // Main grid
            var mainGrid = new Grid();
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Header
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Tabs
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Buttons
            this.Content = mainGrid;

            // Header
            CreateHeader(mainGrid);

            // Tabbed content
            CreateTabbedContent(mainGrid);

            // Action buttons
            CreateActionButtons(mainGrid);
        }

        private void CreateHeader(Grid mainGrid)
        {
            var headerPanel = new StackPanel
            {
                Background = Brushes.LightBlue,
                Margin = new Thickness(0)
            };

            var titleLabel = new Label
            {
                Content = "Plan Check Results - " + planId,
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(10)
            };

            var instructionLabel = new Label
            {
                Content = "✓ Review results below. Make corrections in Eclipse, then rerun script for updated analysis.",
                FontSize = 11,
                FontWeight = FontWeights.Normal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(5),
                Foreground = Brushes.DarkGreen
            };

            headerPanel.Children.Add(titleLabel);
            headerPanel.Children.Add(instructionLabel);
            Grid.SetRow(headerPanel, 0);
            mainGrid.Children.Add(headerPanel);
        }

        private void CreateTabbedContent(Grid mainGrid)
        {
            tabControl = new TabControl
            {
                Margin = new Thickness(10)
            };

            // Create tabs for each section
            string[] tabs = { "Plan Info", "Dose", "Beams", "Structures", "Status" };

            foreach (var tabName in tabs)
            {
                var tabItem = new TabItem
                {
                    Header = tabName
                };

                var scrollViewer = new ScrollViewer
                {
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                    HorizontalScrollBarVisibility = ScrollBarVisibility.Auto
                };

                var textBox = new TextBox
                {
                    IsReadOnly = true,
                    VerticalAlignment = VerticalAlignment.Stretch,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    FontFamily = new FontFamily("Segoe UI"),
                    FontSize = 12,
                    Background = Brushes.White,
                    Foreground = Brushes.Black,
                    BorderThickness = new Thickness(0),
                    Padding = new Thickness(10),
                    TextWrapping = TextWrapping.Wrap,
                    AcceptsReturn = true,
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
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
                Width = 150,
                Height = 30,
                Margin = new Thickness(5),
                Background = Brushes.LightYellow,
                IsEnabled = false
            };
            // Note: Refresh disabled - ScriptContext is disposed after script ends
            // To get updated data, close this window and rerun the script

            var closeButton = new Button
            {
                Content = "Close",
                Width = 80,
                Height = 30,
                Margin = new Thickness(5),
                Background = Brushes.LightGray
            };
            closeButton.Click += (s, e) => this.Close();

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
                if (plan == null)
                {
                    MessageBox.Show("Plan not found.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Update each tab
                UpdateTab("Plan Info", plan);
                UpdateTab("Dose", plan);
                UpdateTab("Beams", plan);
                UpdateTab("Structures", plan);
                UpdateTab("Status", plan);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateTab(string tabName, PlanSetup plan)
        {
            TextBox textBox = null;
            if (tabTextBoxes.TryGetValue(tabName, out textBox))
            {
                var sb = new StringBuilder();
                
                sb.AppendLine("*** " + tabName + " ***");
                sb.AppendLine("Last Updated: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                sb.AppendLine();

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
                    case "Status":
                        CheckPlanStatus(plan, sb);
                        break;
                }

                sb.AppendLine();
                sb.AppendLine("For additional help, contact your medical physicist.");
                
                textBox.Text = sb.ToString();
            }
        }

        private void CheckPlanInformation(PlanSetup plan, StringBuilder sb)
        {
            sb.AppendLine("1. PLAN INFORMATION:");
            sb.AppendLine("====================================");
            
            try
            {
                // Basic Plan Info
                sb.AppendLine("✓ Plan ID: " + plan.Id);
                sb.AppendLine("✓ Plan Name: " + plan.Name);
                sb.AppendLine("✓ Plan Status: " + plan.ApprovalStatus.ToString());
                sb.AppendLine("✓ Treatment Orientation: " + plan.TreatmentOrientation.ToString());
                sb.AppendLine("✓ Plan Type: " + plan.PlanType.ToString());
                
                // Course and Patient Info
                if (plan.Course != null)
                {
                    sb.AppendLine("✓ Course ID: " + plan.Course.Id);
                    
                    if (plan.Course.Patient != null)
                    {
                        sb.AppendLine("✓ Patient ID: " + plan.Course.Patient.Id);
                        sb.AppendLine("✓ Patient Name: " + plan.Course.Patient.LastName + ", " + plan.Course.Patient.FirstName);
                        sb.AppendLine("✓ Date of Birth: " + plan.Course.Patient.DateOfBirth.ToString());
                    }
                }
                
                // Plan Creation Info - removed CreationDate as it's not available in this ESAPI version
                // sb.AppendLine("✓ Creation Date: " + plan.CreationDate.ToString("yyyy-MM-dd HH:mm"));
                if (plan.PlanningApprovalDate != null)
                {
                    sb.AppendLine("✓ Planning Approval Date: " + plan.PlanningApprovalDate.ToString());
                }
                
                // Structure Set Info
                if (plan.StructureSet != null)
                {
                    sb.AppendLine("✓ Structure Set ID: " + plan.StructureSet.Id);
                    // sb.AppendLine("✓ Structure Set Date: " + plan.StructureSet.HistoryDate.ToString("yyyy-MM-dd HH:mm"));
                }
                else
                {
                    sb.AppendLine("⚠ WARNING: No structure set associated with plan");
                }
                
                // Image Info
                if (plan.StructureSet != null && plan.StructureSet.Image != null)
                {
                    var image = plan.StructureSet.Image;
                    sb.AppendLine("✓ Image ID: " + image.Id);
                    // sb.AppendLine("✓ Image Date: " + image.CreationDate.ToString("yyyy-MM-dd HH:mm"));
                    sb.AppendLine("✓ Image Resolution: " + image.XRes.ToString("F1") + " x " + image.YRes.ToString("F1") + " x " + image.ZRes.ToString("F1") + " mm");
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
                else
                {
                    sb.AppendLine("⚠ WARNING: No dose per fraction specified");
                }
                
                sb.AppendLine("✓ Number of Fractions: " + (plan.NumberOfFractions ?? 0).ToString());
                
                // Calculate total dose
                if (plan.DosePerFraction != null && plan.NumberOfFractions.HasValue)
                {
                    double totalDose = plan.DosePerFraction.Dose * plan.NumberOfFractions.Value;
                    sb.AppendLine("✓ Total Prescribed Dose: " + totalDose.ToString("F1") + " cGy (" + (totalDose / 100).ToString("F1") + " Gy)");
                }
                else
                {
                    sb.AppendLine("⚠ WARNING: Cannot calculate total dose (missing prescription information)");
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
                
            }
            catch (Exception ex)
            {
                sb.AppendLine("❌ Error retrieving dose information: " + ex.Message);
            }
        }

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
                sb.AppendLine();
                
                // Treatment Beam Details
                if (treatmentBeams.Count > 0)
                {
                    sb.AppendLine("TREATMENT BEAMS:");
                    sb.AppendLine("----------------");
                    
                    foreach (var beam in treatmentBeams)
                    {
                        sb.AppendLine("Beam: " + beam.Id);
                        sb.AppendLine("  ✓ Energy: " + beam.EnergyModeDisplayName);
                        sb.AppendLine("  ✓ Dose Rate: " + beam.DoseRate + " MU/min");
                        sb.AppendLine("  ✓ Monitor Units: " + beam.Meterset.Value.ToString("F1") + " MU");
                        sb.AppendLine("  ✓ Gantry Angle: " + beam.ControlPoints.First().GantryAngle.ToString("F1") + "°");
                        sb.AppendLine("  ✓ Collimator Angle: " + beam.ControlPoints.First().CollimatorAngle.ToString("F1") + "°");
                        sb.AppendLine("  ✓ Couch Angle: " + beam.ControlPoints.First().PatientSupportAngle.ToString("F1") + "°");
                        
                        // Technique
                        if (beam.Technique != null)
                        {
                            sb.AppendLine("  ✓ Technique: " + beam.Technique.Id);
                        }
                        
                        // Field Size
                        var firstCP = beam.ControlPoints.First();
                        if (firstCP.JawPositions != null)
                        {
                            var jawX = Math.Abs(firstCP.JawPositions.X2 - firstCP.JawPositions.X1);
                            var jawY = Math.Abs(firstCP.JawPositions.Y2 - firstCP.JawPositions.Y1);
                            sb.AppendLine("  ✓ Field Size: " + jawX.ToString("F1") + " x " + jawY.ToString("F1") + " cm");
                        }
                        
                        // SSD if available - check if SSD property exists and has a value
                        try
                        {
                            sb.AppendLine("  ✓ SSD: " + beam.SSD.ToString("F1") + " cm");
                        }
                        catch
                        {
                            // SSD property might not be available in this ESAPI version
                        }
                        
                        sb.AppendLine();
                    }
                    
                    // Total MU
                    double totalMU = treatmentBeams.Sum(b => b.Meterset.Value);
                    sb.AppendLine("✓ Total Monitor Units: " + totalMU.ToString("F1") + " MU");
                }
                
                // Setup Beam Details
                if (setupBeams.Count > 0)
                {
                    sb.AppendLine();
                    sb.AppendLine("SETUP BEAMS:");
                    sb.AppendLine("------------");
                    
                    foreach (var beam in setupBeams)
                    {
                        sb.AppendLine("Setup Field: " + beam.Id);
                        sb.AppendLine("  ✓ Energy: " + beam.EnergyModeDisplayName);
                        sb.AppendLine("  ✓ Gantry Angle: " + beam.ControlPoints.First().GantryAngle.ToString("F1") + "°");
                    }
                }
                
                // Basic Safety Checks
                sb.AppendLine();
                sb.AppendLine("BASIC CHECKS:");
                sb.AppendLine("-------------");
                
                if (treatmentBeams.Count == 0)
                {
                    sb.AppendLine("❌ ERROR: No treatment beams found");
                }
                
                // Check for reasonable MU values
                foreach (var beam in treatmentBeams)
                {
                    if (beam.Meterset.Value > 1000)
                    {
                        sb.AppendLine("⚠ WARNING: High MU value for beam " + beam.Id + ": " + beam.Meterset.Value.ToString("F1") + " MU");
                    }
                    if (beam.Meterset.Value < 5)
                    {
                        sb.AppendLine("⚠ WARNING: Low MU value for beam " + beam.Id + ": " + beam.Meterset.Value.ToString("F1") + " MU");
                    }
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
                
                // Categorize structures
                var targets = structures.Where(s => s.DicomType == "PTV" || s.Id.ToUpper().Contains("PTV") || s.Id.ToUpper().Contains("GTV") || s.Id.ToUpper().Contains("CTV")).ToList();
                var oars = structures.Where(s => s.DicomType == "ORGAN" || (s.DicomType != "PTV" && !s.Id.ToUpper().Contains("PTV") && !s.Id.ToUpper().Contains("GTV") && !s.Id.ToUpper().Contains("CTV") && s.DicomType != "EXTERNAL")).ToList();
                var body = structures.Where(s => s.DicomType == "EXTERNAL" || s.Id.ToUpper().Contains("BODY") || s.Id.ToUpper().Contains("EXTERNAL")).ToList();
                var supports = structures.Where(s => s.DicomType == "SUPPORT").ToList();
                
                sb.AppendLine("✓ Target Structures: " + targets.Count);
                sb.AppendLine("✓ Organ at Risk Structures: " + oars.Count);
                sb.AppendLine("✓ Body/External Structures: " + body.Count);
                sb.AppendLine("✓ Support Structures: " + supports.Count);
                sb.AppendLine();
                
                // Target Volume Details
                if (targets.Count > 0)
                {
                    sb.AppendLine("TARGET VOLUMES:");
                    sb.AppendLine("---------------");
                    
                    foreach (var target in targets)
                    {
                        sb.AppendLine("Target: " + target.Id);
                        sb.AppendLine("  ✓ DICOM Type: " + target.DicomType);
                        if (target.Volume > 0)
                        {
                            sb.AppendLine("  ✓ Volume: " + target.Volume.ToString("F1") + " cm³");
                        }
                        else
                        {
                            sb.AppendLine("  ⚠ WARNING: Volume is 0 or not calculated");
                        }
                        sb.AppendLine();
                    }
                }
                else
                {
                    sb.AppendLine("⚠ WARNING: No target volumes (PTV/CTV/GTV) identified");
                    sb.AppendLine();
                }
                
                // Critical Organ Summary
                if (oars.Count > 0)
                {
                    sb.AppendLine("ORGANS AT RISK:");
                    sb.AppendLine("---------------");
                    
                    foreach (var oar in oars.Take(15)) // Limit to first 15 to avoid clutter
                    {
                        sb.AppendLine("OAR: " + oar.Id);
                        if (oar.Volume > 0)
                        {
                            sb.AppendLine("  ✓ Volume: " + oar.Volume.ToString("F1") + " cm³");
                        }
                        else
                        {
                            sb.AppendLine("  ⚠ WARNING: Volume is 0 or not calculated");
                        }
                    }
                    
                    if (oars.Count > 15)
                    {
                        sb.AppendLine("... and " + (oars.Count - 15) + " more OAR structures");
                    }
                    sb.AppendLine();
                }
                
                // Body Structure Check
                if (body.Count > 0)
                {
                    sb.AppendLine("BODY/EXTERNAL STRUCTURES:");
                    sb.AppendLine("------------------------");
                    
                    foreach (var bodyStruct in body)
                    {
                        sb.AppendLine("Body: " + bodyStruct.Id);
                        if (bodyStruct.Volume > 0)
                        {
                            sb.AppendLine("  ✓ Volume: " + bodyStruct.Volume.ToString("F1") + " cm³");
                        }
                    }
                }
                else
                {
                    sb.AppendLine("⚠ WARNING: No body/external structure found");
                }
                
                // Basic Structure Checks
                sb.AppendLine();
                sb.AppendLine("STRUCTURE CHECKS:");
                sb.AppendLine("----------------");
                
                // Check for empty structures
                var emptyStructures = structures.Where(s => s.Volume <= 0).ToList();
                if (emptyStructures.Count > 0)
                {
                    sb.AppendLine("⚠ WARNING: " + emptyStructures.Count + " structure(s) have no volume:");
                    foreach (var empty in emptyStructures.Take(5))
                    {
                        sb.AppendLine("  - " + empty.Id);
                    }
                    if (emptyStructures.Count > 5)
                    {
                        sb.AppendLine("  ... and " + (emptyStructures.Count - 5) + " more");
                    }
                }
                else
                {
                    sb.AppendLine("✓ All structures have calculated volumes");
                }
                
            }
            catch (Exception ex)
            {
                sb.AppendLine("❌ Error retrieving structure information: " + ex.Message);
            }
        }

        private void CheckPlanStatus(PlanSetup plan, StringBuilder sb)
        {
            sb.AppendLine("5. PLAN STATUS & SAFETY CHECKS:");
            sb.AppendLine("====================================");
            
            try
            {
                // Approval Status
                sb.AppendLine("APPROVAL STATUS:");
                sb.AppendLine("---------------");
                sb.AppendLine("✓ Plan Approval Status: " + plan.ApprovalStatus.ToString());
                
                if (plan.PlanningApprovalDate != null)
                {
                    sb.AppendLine("✓ Planning Approval Date: " + plan.PlanningApprovalDate.ToString());
                }
                
                if (plan.TreatmentApprovalDate != null)
                {
                    sb.AppendLine("✓ Treatment Approval Date: " + plan.TreatmentApprovalDate.ToString());
                }
                
                sb.AppendLine();
                
                // Safety Checks
                sb.AppendLine("SAFETY CHECKS:");
                sb.AppendLine("-------------");
                
                // Check if plan is approved for treatment
                if (plan.ApprovalStatus == PlanSetupApprovalStatus.TreatmentApproved)
                {
                    sb.AppendLine("✓ Plan is approved for treatment");
                }
                else if (plan.ApprovalStatus == PlanSetupApprovalStatus.PlanningApproved)
                {
                    sb.AppendLine("⚠ WARNING: Plan is only planning approved - not ready for treatment");
                }
                else
                {
                    sb.AppendLine("❌ ERROR: Plan is not approved - approval required before treatment");
                }
                
                // Check for dose calculation
                if (plan.Dose == null)
                {
                    sb.AppendLine("❌ ERROR: No dose calculation - dose must be calculated before treatment");
                }
                else
                {
                    sb.AppendLine("✓ Dose calculation present");
                }
                
                // Check for treatment beams
                var treatmentBeams = plan.Beams.Where(b => !b.IsSetupField).ToList();
                if (treatmentBeams.Count == 0)
                {
                    sb.AppendLine("❌ ERROR: No treatment beams found");
                }
                else
                {
                    sb.AppendLine("✓ Treatment beams present (" + treatmentBeams.Count + " beams)");
                }
                
                // Check prescription
                if (plan.DosePerFraction == null || !plan.NumberOfFractions.HasValue)
                {
                    sb.AppendLine("⚠ WARNING: Prescription information incomplete");
                }
                else
                {
                    sb.AppendLine("✓ Prescription information complete");
                }
                
                // Check structure set
                if (plan.StructureSet == null)
                {
                    sb.AppendLine("❌ ERROR: No structure set associated with plan");
                }
                else
                {
                    sb.AppendLine("✓ Structure set present");
                }
                
                sb.AppendLine();
                
                // Overall Readiness Assessment
                sb.AppendLine("TREATMENT READINESS:");
                sb.AppendLine("-------------------");
                
                bool isReady = true;
                var issues = new List<string>();
                
                if (plan.ApprovalStatus != PlanSetupApprovalStatus.TreatmentApproved)
                {
                    isReady = false;
                    issues.Add("Plan not treatment approved");
                }
                
                if (plan.Dose == null)
                {
                    isReady = false;
                    issues.Add("No dose calculation");
                }
                
                if (treatmentBeams.Count == 0)
                {
                    isReady = false;
                    issues.Add("No treatment beams");
                }
                
                if (plan.StructureSet == null)
                {
                    isReady = false;
                    issues.Add("No structure set");
                }
                
                if (isReady)
                {
                    sb.AppendLine("✓ PLAN IS READY FOR TREATMENT");
                }
                else
                {
                    sb.AppendLine("❌ PLAN IS NOT READY FOR TREATMENT");
                    sb.AppendLine("Issues to resolve:");
                    foreach (var issue in issues)
                    {
                        sb.AppendLine("  - " + issue);
                    }
                }
                
            }
            catch (Exception ex)
            {
                sb.AppendLine("❌ Error retrieving plan status: " + ex.Message);
            }
        }
    }
}
