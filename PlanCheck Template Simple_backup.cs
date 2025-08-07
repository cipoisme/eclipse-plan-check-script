using System;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Documents;
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
        private Dictionary<string, RichTextBox> tabRichTextBoxes = new Dictionary<string, RichTextBox>();

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
            string[] tabs = { "Plan Info", "Dose", "Beams", "Structures", "Isocenter", "Status" };

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

                var richTextBox = new RichTextBox
                {
                    IsReadOnly = true,
                    VerticalAlignment = VerticalAlignment.Stretch,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    FontFamily = new FontFamily("Segoe UI"),
                    FontSize = 14,
                    Background = Brushes.White,
                    Foreground = Brushes.Black,
                    BorderThickness = new Thickness(0),
                    Padding = new Thickness(15),
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                    HorizontalScrollBarVisibility = ScrollBarVisibility.Auto
                };

                scrollViewer.Content = richTextBox;
                tabItem.Content = scrollViewer;
                tabControl.Items.Add(tabItem);

                tabRichTextBoxes[tabName] = richTextBox;
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
                UpdateTab("Isocenter", plan);
                UpdateTab("Status", plan);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateTab(string tabName, PlanSetup plan)
        {
            RichTextBox richTextBox = null;
            if (tabRichTextBoxes.TryGetValue(tabName, out richTextBox))
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
                    case "Isocenter":
                        CheckIsocenterInformation(plan, sb);
                        break;
                    case "Status":
                        CheckPlanStatus(plan, sb);
                        break;
                }

                sb.AppendLine();
                sb.AppendLine("For additional help, contact your medical physicist.");
                
                SetFormattedText(richTextBox, sb.ToString());
            }
        }

        private void SetFormattedText(RichTextBox richTextBox, string text)
        {
            richTextBox.Document.Blocks.Clear();
            
            var lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            
            foreach (var line in lines)
            {
                var paragraph = new Paragraph();
                paragraph.Margin = new Thickness(0, 2, 0, 2);
                
                if (IsChecklistTitle(line))
                {
                    // Bold checklist titles
                    var run = new Run(line);
                    run.FontWeight = FontWeights.Bold;
                    run.FontSize = 15;
                    paragraph.Inlines.Add(run);
                }
                else if (line.Contains("✓") || line.Contains("✅"))
                {
                    // Green checkmarks
                    var parts = line.Split(new[] { "✓", "✅" }, StringSplitOptions.None);
                    for (int i = 0; i < parts.Length; i++)
                    {
                        if (i > 0)
                        {
                            var checkRun = new Run("✓");
                            checkRun.Foreground = Brushes.Green;
                            checkRun.FontWeight = FontWeights.Bold;
                            paragraph.Inlines.Add(checkRun);
                        }
                        paragraph.Inlines.Add(new Run(parts[i]));
                    }
                }
                else if (line.Contains("❌") || line.Contains("✗"))
                {
                    // Red X marks
                    var parts = line.Split(new[] { "❌", "✗" }, StringSplitOptions.None);
                    for (int i = 0; i < parts.Length; i++)
                    {
                        if (i > 0)
                        {
                            var xRun = new Run("❌");
                            xRun.Foreground = Brushes.Red;
                            xRun.FontWeight = FontWeights.Bold;
                            paragraph.Inlines.Add(xRun);
                        }
                        paragraph.Inlines.Add(new Run(parts[i]));
                    }
                }
                else if (line.Contains("🚨"))
                {
                    // Red critical alerts
                    var parts = line.Split(new[] { "🚨" }, StringSplitOptions.None);
                    for (int i = 0; i < parts.Length; i++)
                    {
                        if (i > 0)
                        {
                            var alertRun = new Run("🚨");
                            alertRun.Foreground = Brushes.Red;
                            alertRun.FontWeight = FontWeights.Bold;
                            paragraph.Inlines.Add(alertRun);
                        }
                        var textRun = new Run(parts[i]);
                        if (i > 0 && line.Contains("CRITICAL"))
                        {
                            textRun.Foreground = Brushes.DarkRed;
                            textRun.FontWeight = FontWeights.Bold;
                        }
                        paragraph.Inlines.Add(textRun);
                    }
                }
                else if (line.Contains("⚠️"))
                {
                    // Orange warnings
                    var parts = line.Split(new[] { "⚠️" }, StringSplitOptions.None);
                    for (int i = 0; i < parts.Length; i++)
                    {
                        if (i > 0)
                        {
                            var warnRun = new Run("⚠️");
                            warnRun.Foreground = Brushes.Orange;
                            warnRun.FontWeight = FontWeights.Bold;
                            paragraph.Inlines.Add(warnRun);
                        }
                        var textRun = new Run(parts[i]);
                        if (i > 0)
                        {
                            textRun.Foreground = Brushes.DarkOrange;
                        }
                        paragraph.Inlines.Add(textRun);
                    }
                }
                else if (line.Contains("💊") || line.Contains("🎯") || line.Contains("🛡️") || line.Contains("🏭") || line.Contains("📋") || line.Contains("🖼️") || line.Contains("🔍") || line.Contains("📷") || line.Contains("🛏️") || line.Contains("🧠") || line.Contains("📍"))
                {
                    // Colored icons
                    var iconRun = new Run(line);
                    if (line.Contains("💊")) iconRun.Foreground = Brushes.Purple;
                    else if (line.Contains("🎯")) iconRun.Foreground = Brushes.Blue;
                    else if (line.Contains("🛡️")) iconRun.Foreground = Brushes.DarkBlue;
                    else if (line.Contains("🏭")) iconRun.Foreground = Brushes.Gray;
                    else if (line.Contains("📋")) iconRun.Foreground = Brushes.DarkGreen;
                    else if (line.Contains("🖼️")) iconRun.Foreground = Brushes.DarkCyan;
                    else if (line.Contains("🔍")) iconRun.Foreground = Brushes.DarkBlue;
                    else if (line.Contains("📷")) iconRun.Foreground = Brushes.DarkSlateBlue;
                    else if (line.Contains("🛏️")) iconRun.Foreground = Brushes.Brown;
                    else if (line.Contains("🧠")) iconRun.Foreground = Brushes.Magenta;
                    else if (line.Contains("📍")) iconRun.Foreground = Brushes.Red;
                    
                    paragraph.Inlines.Add(iconRun);
                }
                else
                {
                    paragraph.Inlines.Add(new Run(line));
                }
                
                richTextBox.Document.Blocks.Add(paragraph);
            }
        }

        private bool IsChecklistTitle(string line)
        {
            return line.Contains("CHECKLIST") || 
                   line.Contains("REQUIREMENTS") || 
                   line.Contains("VERIFICATION") ||
                   (line.Contains("📋") && line.Contains(":")) ||
                   (line.Contains("🚨") && line.Contains(":")) ||
                   line.StartsWith("===") ||
                   line.StartsWith("---");
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
                
                sb.AppendLine();
                sb.AppendLine("========================================");
                sb.AppendLine("📋 MOSAIQ PRESCRIPTION CHECKLIST");
                sb.AppendLine("========================================");
                sb.AppendLine("Please verify the following information is correctly entered in Mosaiq:");
                sb.AppendLine();
                
                // Extract data from plan for Mosaiq checklist
                sb.AppendLine("PRESCRIPTION DETAILS TO VERIFY IN MOSAIQ:");
                sb.AppendLine("------------------------------------------");
                
                // Rx Site
                if (plan.StructureSet != null)
                {
                    var targets = plan.StructureSet.Structures.Where(s => 
                        s.DicomType == "PTV" || 
                        s.Id.ToUpper().Contains("PTV") || 
                        s.Id.ToUpper().Contains("GTV") || 
                        s.Id.ToUpper().Contains("CTV")).ToList();
                    
                    if (targets.Count > 0)
                    {
                        sb.AppendLine("🎯 Rx Site: Based on target structures found:");
                        foreach (var target in targets)
                        {
                            sb.AppendLine("   - " + target.Id + " (Volume: " + target.Volume.ToString("F1") + " cm³)");
                        }
                        // Suggest site based on target names
                        var firstTarget = targets.First().Id.ToUpper();
                        if (firstTarget.Contains("BRAIN") || firstTarget.Contains("SRS"))
                            sb.AppendLine("   ➤ Suggested Mosaiq Site: Brain/CNS");
                        else if (firstTarget.Contains("LUNG") || firstTarget.Contains("THORAX"))
                            sb.AppendLine("   ➤ Suggested Mosaiq Site: Thorax/Lung");
                        else if (firstTarget.Contains("PROSTATE") || firstTarget.Contains("PELVIS"))
                            sb.AppendLine("   ➤ Suggested Mosaiq Site: Pelvis/Prostate");
                        else if (firstTarget.Contains("HEAD") || firstTarget.Contains("NECK"))
                            sb.AppendLine("   ➤ Suggested Mosaiq Site: Head and Neck");
                        else
                            sb.AppendLine("   ➤ Review target location for appropriate Mosaiq site");
                    }
                    else
                    {
                        sb.AppendLine("🎯 Rx Site: ⚠️ No target structures identified - verify site manually");
                    }
                }
                
                sb.AppendLine();
                
                // Technique
                var beams = plan.Beams.Where(b => !b.IsSetupField).ToList();
                if (beams.Count > 0)
                {
                    var firstBeam = beams.First();
                    sb.AppendLine("⚡ Technique: " + (firstBeam.Technique != null ? firstBeam.Technique.Id : "Review beam technique"));
                    
                    // Analyze technique type
                    bool isVMAT = beams.Any(b => b.Technique != null && b.Technique.Id.ToUpper().Contains("ARC"));
                    bool isIMRT = beams.Any(b => b.Technique != null && b.Technique.Id.ToUpper().Contains("IMRT"));
                    bool isSRS = beams.Count > 5 && beams.All(b => b.Meterset.Value < 200);
                    
                    if (isVMAT)
                        sb.AppendLine("   ➤ Suggested Mosaiq Technique: VMAT");
                    else if (isIMRT)
                        sb.AppendLine("   ➤ Suggested Mosaiq Technique: IMRT");
                    else if (isSRS)
                        sb.AppendLine("   ➤ Suggested Mosaiq Technique: SRS");
                    else
                        sb.AppendLine("   ➤ Suggested Mosaiq Technique: 3D-CRT");
                }
                
                sb.AppendLine();
                
                // Modality
                if (beams.Count > 0)
                {
                    var energies = beams.Select(b => b.EnergyModeDisplayName).Distinct().ToList();
                    sb.AppendLine("🔬 Modality: " + string.Join(", ", energies));
                    if (energies.Any(e => e.Contains("X")))
                        sb.AppendLine("   ➤ Mosaiq Modality: Photons");
                    else if (energies.Any(e => e.Contains("E")))
                        sb.AppendLine("   ➤ Mosaiq Modality: Electrons");
                    else
                        sb.AppendLine("   ➤ Review energy types for modality selection");
                }
                
                sb.AppendLine();
                
                // Dose Specification
                if (plan.DosePerFraction != null && plan.NumberOfFractions.HasValue)
                {
                    double totalDose = plan.DosePerFraction.Dose * plan.NumberOfFractions.Value;
                    sb.AppendLine("💊 Dose Spec:");
                    sb.AppendLine("   - Total Dose: " + totalDose.ToString("F1") + " cGy (" + (totalDose/100).ToString("F1") + " Gy)");
                    sb.AppendLine("   - Dose per Fraction: " + plan.DosePerFraction.Dose.ToString("F1") + " cGy");
                    sb.AppendLine("   - Number of Fractions: " + plan.NumberOfFractions.Value.ToString());
                    
                    // Determine if SiB (Simultaneous Integrated Boost)
                    var targets = plan.StructureSet != null ? plan.StructureSet.Structures.Where(s => 
                        s.DicomType == "PTV" || 
                        s.Id.ToUpper().Contains("PTV")).ToList() : null;
                    
                    bool possibleSiB = targets != null && targets.Count > 1;
                    sb.AppendLine();
                    sb.AppendLine("🎯 SiB (Simultaneous Integrated Boost):");
                    if (possibleSiB)
                    {
                        sb.AppendLine("   ⚠️ POSSIBLE SiB DETECTED - Multiple PTV structures found:");
                        foreach (var target in targets)
                        {
                            sb.AppendLine("     - " + target.Id);
                        }
                        sb.AppendLine("   ➤ Verify if this is a SiB plan in Mosaiq");
                    }
                    else
                    {
                        sb.AppendLine("   ➤ Standard single-dose plan (not SiB)");
                    }
                }
                else
                {
                    sb.AppendLine("💊 Dose Spec: ⚠️ Prescription information incomplete");
                }
                

                
                // Breathing Motion Management Detection
                sb.AppendLine("🫁 BREATHING MOTION MANAGEMENT:");
                sb.AppendLine("------------------------------");
                
                bool breathingCompensationDetected = false;
                string breathingMethod = "";
                var breathingIndicators = new List<string>();
                
                // Check plan ID and name for breathing indicators
                string planName = plan.Name != null ? plan.Name.ToUpper() : "";
                string planId = plan.Id.ToUpper();
                
                // Check for various breathing management techniques
                if (planId.Contains("IABC") || planName.Contains("IABC"))
                {
                    breathingCompensationDetected = true;
                    breathingMethod = "iABC (Internal Active Breathing Coordinator)";
                    breathingIndicators.Add("iABC detected in plan name/ID");
                }
                else if (planId.Contains("EABC") || planName.Contains("EABC"))
                {
                    breathingCompensationDetected = true;
                    breathingMethod = "eABC (External Active Breathing Coordinator)";
                    breathingIndicators.Add("eABC detected in plan name/ID");
                }
                else if (planId.Contains("ABC") || planName.Contains("ABC"))
                {
                    breathingCompensationDetected = true;
                    breathingMethod = "ABC (Active Breathing Coordinator)";
                    breathingIndicators.Add("ABC detected in plan name/ID");
                }
                else if (planId.Contains("BH") || planName.Contains("BH") || 
                         planId.Contains("BREATHHOLD") || planName.Contains("BREATHHOLD") ||
                         planId.Contains("BREATH_HOLD") || planName.Contains("BREATH_HOLD"))
                {
                    breathingCompensationDetected = true;
                    breathingMethod = "Breath Hold";
                    breathingIndicators.Add("Breath Hold detected in plan name/ID");
                }
                else if (planId.Contains("FB") || planName.Contains("FB") ||
                         planId.Contains("FREEBREATH") || planName.Contains("FREEBREATH") ||
                         planId.Contains("FREE_BREATH") || planName.Contains("FREE_BREATH"))
                {
                    breathingCompensationDetected = true;
                    breathingMethod = "Free Breathing";
                    breathingIndicators.Add("Free Breathing detected in plan name/ID");
                }
                else if (planId.Contains("4DCT") || planName.Contains("4DCT") ||
                         planId.Contains("4D") || planName.Contains("4D"))
                {
                    breathingCompensationDetected = true;
                    breathingMethod = "4D CT Based Planning";
                    breathingIndicators.Add("4D CT detected in plan name/ID");
                }
                else if (planId.Contains("GATING") || planName.Contains("GATING") ||
                         planId.Contains("GATE") || planName.Contains("GATE"))
                {
                    breathingCompensationDetected = true;
                    breathingMethod = "Respiratory Gating";
                    breathingIndicators.Add("Gating detected in plan name/ID");
                }
                
                // Check image set for breathing indicators
                if (plan.StructureSet != null && plan.StructureSet.Image != null)
                {
                    string imageId = plan.StructureSet.Image.Id.ToUpper();
                    
                    if (imageId.Contains("4DCT") || imageId.Contains("4D"))
                    {
                        breathingCompensationDetected = true;
                        if (string.IsNullOrEmpty(breathingMethod))
                            breathingMethod = "4D CT Based Planning";
                        breathingIndicators.Add("4D CT image set detected");
                    }
                    else if (imageId.Contains("ABC") || imageId.Contains("BH") || imageId.Contains("FB"))
                    {
                        breathingCompensationDetected = true;
                        breathingIndicators.Add("Breathing management indicator in image ID");
                    }
                }
                
                // Check structures for breathing-related ROIs
                if (plan.StructureSet != null)
                {
                    var structures = plan.StructureSet.Structures.ToList();
                    var breathingStructures = structures.Where(s =>
                        s.Id.ToUpper().Contains("ITV") ||
                        s.Id.ToUpper().Contains("GTV_T") ||
                        s.Id.ToUpper().Contains("CTV_T") ||
                        s.Id.ToUpper().Contains("TIDAL") ||
                        s.Id.ToUpper().Contains("EXHALE") ||
                        s.Id.ToUpper().Contains("INHALE") ||
                        s.Id.ToUpper().Contains("PHASE") ||
                        s.Id.ToUpper().Contains("BREATHING")).ToList();
                    
                    if (breathingStructures.Count > 0)
                    {
                        breathingCompensationDetected = true;
                        if (string.IsNullOrEmpty(breathingMethod))
                            breathingMethod = "Motion Management (based on structures)";
                        sb.AppendLine("⚠️ BREATHING-RELATED STRUCTURES DETECTED:");
                        foreach (var structure in breathingStructures)
                        {
                            sb.AppendLine("   - " + structure.Id);
                            breathingIndicators.Add("Structure: " + structure.Id);
                        }
                    }
                }
                
                // Report findings
                if (breathingCompensationDetected)
                {
                    sb.AppendLine("⚠️ BREATHING MOTION MANAGEMENT DETECTED:");
                    sb.AppendLine("   Method: " + breathingMethod);
                    sb.AppendLine("   Indicators found:");
                    foreach (var indicator in breathingIndicators)
                    {
                        sb.AppendLine("     - " + indicator);
                    }
                    
                    sb.AppendLine();
                    sb.AppendLine("🚨 BREATHING COMPENSATION REQUIREMENTS FOR MOSAIQ:");
                    sb.AppendLine("   ➤ Document breathing technique in prescription");
                    sb.AppendLine("   ➤ Add breathing method to treatment instructions");
                    sb.AppendLine("   ➤ Include setup verification requirements");
                    sb.AppendLine("   ➤ Specify imaging guidance needed");
                    sb.AppendLine("   ➤ Document patient coaching requirements");
                    
                    sb.AppendLine();
                    sb.AppendLine("📋 BREATHING MANAGEMENT DOCUMENTATION:");
                    sb.AppendLine("   □ Breathing technique specified (" + breathingMethod + ")");
                    sb.AppendLine("   □ Patient positioning instructions clear");
                    sb.AppendLine("   □ Breathing coaching protocol documented");
                    sb.AppendLine("   □ Image guidance requirements specified");
                    sb.AppendLine("   □ Setup verification procedures defined");
                    sb.AppendLine("   □ Backup plan if breathing technique fails");
                    
                    if (breathingMethod.Contains("ABC"))
                    {
                        sb.AppendLine("   □ ABC device settings documented");
                        sb.AppendLine("   □ Threshold levels specified");
                    }
                    else if (breathingMethod.Contains("Breath Hold"))
                    {
                        sb.AppendLine("   □ Breath hold duration documented");
                        sb.AppendLine("   □ Patient coaching completed");
                    }
                    else if (breathingMethod.Contains("4D"))
                    {
                        sb.AppendLine("   □ 4D CT phase information available");
                        sb.AppendLine("   □ ITV margins appropriate");
                    }
                }
                else
                {
                    sb.AppendLine("✅ No specific breathing compensation detected");
                    sb.AppendLine("   ➤ Standard free-breathing treatment assumed");
                    sb.AppendLine("   ➤ Verify if motion management is needed");
                }
                
                sb.AppendLine();
                
                // Patient Positioning and Laterality Check
                sb.AppendLine("🏥 PATIENT POSITIONING & LATERALITY:");
                sb.AppendLine("-----------------------------------");
                
                bool positioningAlertsNeeded = false;
                var positioningConcerns = new List<string>();
                string patientOrientation = plan.TreatmentOrientation.ToString();
                
                // Analyze patient orientation
                sb.AppendLine("📍 PATIENT ORIENTATION ANALYSIS:");
                sb.AppendLine("   Treatment Orientation: " + patientOrientation);
                
                // Check for non-standard positioning
                bool isPronePosition = patientOrientation.ToUpper().Contains("PRONE");
                bool isFeetFirst = patientOrientation.ToUpper().Contains("FEET") || patientOrientation.ToUpper().Contains("FF");
                bool isDecubitus = patientOrientation.ToUpper().Contains("DECUB") || 
                                 patientOrientation.ToUpper().Contains("LATERAL") ||
                                 patientOrientation.ToUpper().Contains("LEFT_LATERAL") ||
                                 patientOrientation.ToUpper().Contains("RIGHT_LATERAL");
                
                // Alert for special positioning
                if (isPronePosition)
                {
                    positioningAlertsNeeded = true;
                    positioningConcerns.Add("PRONE positioning detected");
                    sb.AppendLine("⚠️ PRONE POSITIONING DETECTED");
                }
                
                if (isFeetFirst)
                {
                    positioningAlertsNeeded = true;
                    positioningConcerns.Add("FEET FIRST positioning detected");
                    sb.AppendLine("⚠️ FEET FIRST POSITIONING DETECTED");
                }
                
                if (isDecubitus)
                {
                    positioningAlertsNeeded = true;
                    positioningConcerns.Add("DECUBITUS/LATERAL positioning detected");
                    sb.AppendLine("⚠️ DECUBITUS/LATERAL POSITIONING DETECTED");
                }
                
                // Check for laterality indicators in plan name/ID and structures
                sb.AppendLine();
                sb.AppendLine("🎯 LATERALITY ANALYSIS:");
                
                bool lateralityDetected = false;
                var lateralityIndicators = new List<string>();
                string detectedLaterality = "";
                
                // Check plan name and ID for laterality
                string planNameUpper = plan.Name != null ? plan.Name.ToUpper() : "";
                string planIdUpper = plan.Id.ToUpper();
                
                if (planIdUpper.Contains("LEFT") || planNameUpper.Contains("LEFT") ||
                    planIdUpper.Contains("LT") || planNameUpper.Contains("LT"))
                {
                    lateralityDetected = true;
                    detectedLaterality = "LEFT";
                    lateralityIndicators.Add("LEFT indicator in plan name/ID");
                }
                else if (planIdUpper.Contains("RIGHT") || planNameUpper.Contains("RIGHT") ||
                         planIdUpper.Contains("RT") || planNameUpper.Contains("RT"))
                {
                    lateralityDetected = true;
                    detectedLaterality = "RIGHT";
                    lateralityIndicators.Add("RIGHT indicator in plan name/ID");
                }
                else if (planIdUpper.Contains("BILATERAL") || planNameUpper.Contains("BILATERAL") ||
                         planIdUpper.Contains("BILAT") || planNameUpper.Contains("BILAT"))
                {
                    lateralityDetected = true;
                    detectedLaterality = "BILATERAL";
                    lateralityIndicators.Add("BILATERAL indicator in plan name/ID");
                }
                
                // Check structures for laterality
                if (plan.StructureSet != null)
                {
                    var structures = plan.StructureSet.Structures.ToList();
                    var leftStructures = structures.Where(s =>
                        s.Id.ToUpper().Contains("LEFT") ||
                        s.Id.ToUpper().Contains("LT_") ||
                        s.Id.ToUpper().Contains("_LT") ||
                        s.Id.ToUpper().Contains("_L_")).ToList();
                    
                    var rightStructures = structures.Where(s =>
                        s.Id.ToUpper().Contains("RIGHT") ||
                        s.Id.ToUpper().Contains("RT_") ||
                        s.Id.ToUpper().Contains("_RT") ||
                        s.Id.ToUpper().Contains("_R_")).ToList();
                    
                    if (leftStructures.Count > 0 && rightStructures.Count > 0)
                    {
                        lateralityDetected = true;
                        if (string.IsNullOrEmpty(detectedLaterality))
                            detectedLaterality = "BILATERAL";
                        lateralityIndicators.Add("Both LEFT and RIGHT structures present");
                    }
                    else if (leftStructures.Count > 0)
                    {
                        lateralityDetected = true;
                        if (string.IsNullOrEmpty(detectedLaterality))
                            detectedLaterality = "LEFT";
                        lateralityIndicators.Add("LEFT-sided structures detected:");
                        foreach (var structure in leftStructures.Take(3))
                        {
                            lateralityIndicators.Add("  - " + structure.Id);
                        }
                        if (leftStructures.Count > 3)
                            lateralityIndicators.Add("  - ... and " + (leftStructures.Count - 3) + " more");
                    }
                    else if (rightStructures.Count > 0)
                    {
                        lateralityDetected = true;
                        if (string.IsNullOrEmpty(detectedLaterality))
                            detectedLaterality = "RIGHT";
                        lateralityIndicators.Add("RIGHT-sided structures detected:");
                        foreach (var structure in rightStructures.Take(3))
                        {
                            lateralityIndicators.Add("  - " + structure.Id);
                        }
                        if (rightStructures.Count > 3)
                            lateralityIndicators.Add("  - ... and " + (rightStructures.Count - 3) + " more");
                    }
                }
                
                // Check beam names for laterality
                var allBeams = plan.Beams.ToList();
                var beamsWithLaterality = allBeams.Where(b =>
                    b.Id.ToUpper().Contains("LEFT") ||
                    b.Id.ToUpper().Contains("RIGHT") ||
                    b.Id.ToUpper().Contains("LT") ||
                    b.Id.ToUpper().Contains("RT")).ToList();
                
                if (beamsWithLaterality.Count > 0)
                {
                    lateralityDetected = true;
                    lateralityIndicators.Add("Laterality in beam names:");
                    foreach (var beam in beamsWithLaterality)
                    {
                        lateralityIndicators.Add("  - " + beam.Id);
                    }
                }
                
                // Report laterality findings
                if (lateralityDetected)
                {
                    sb.AppendLine("⚠️ LATERALITY DETECTED: " + detectedLaterality);
                    sb.AppendLine("   Indicators found:");
                    foreach (var indicator in lateralityIndicators)
                    {
                        sb.AppendLine("     " + indicator);
                    }
                }
                else
                {
                    sb.AppendLine("✅ No specific laterality indicators detected");
                }
                
                // Generate alerts and recommendations
                if (positioningAlertsNeeded || lateralityDetected)
                {
                    sb.AppendLine();
                    sb.AppendLine("🚨 POSITIONING & LATERALITY ALERTS:");
                    
                    if (positioningAlertsNeeded)
                    {
                        sb.AppendLine("   ⚠️ NON-STANDARD POSITIONING DETECTED:");
                        foreach (var concern in positioningConcerns)
                        {
                            sb.AppendLine("     - " + concern);
                        }
                        sb.AppendLine();
                        sb.AppendLine("   🔍 CRITICAL VERIFICATION REQUIRED:");
                        sb.AppendLine("     ➤ Verify ALL field labels are correct for positioning");
                        sb.AppendLine("     ➤ Check setup field labels match treatment orientation");
                        sb.AppendLine("     ➤ Confirm gantry angles appropriate for position");
                        sb.AppendLine("     ➤ Verify patient setup instructions in Mosaiq");
                        sb.AppendLine("     ➤ Double-check left/right orientation markers");
                    }
                    
                    if (lateralityDetected)
                    {
                        sb.AppendLine("   ⚠️ LATERALITY VERIFICATION REQUIRED:");
                        sb.AppendLine("     ➤ Confirm " + detectedLaterality + " laterality in Mosaiq");
                        sb.AppendLine("     ➤ Verify field names reflect correct side");
                        sb.AppendLine("     ➤ Check beam arrangements match intended laterality");
                        sb.AppendLine("     ➤ Confirm setup instructions specify correct side");
                    }
                    
                    sb.AppendLine();
                    sb.AppendLine("📋 POSITIONING & LATERALITY CHECKLIST:");
                    sb.AppendLine("   □ Patient orientation verified in Mosaiq");
                    sb.AppendLine("   □ All field labels checked and correct");
                    sb.AppendLine("   □ Setup field labels match treatment fields");
                    sb.AppendLine("   □ Gantry angles appropriate for positioning");
                    sb.AppendLine("   □ Left/right markers correctly placed");
                    if (lateralityDetected)
                    {
                        sb.AppendLine("   □ " + detectedLaterality + " laterality confirmed in prescription");
                        sb.AppendLine("   □ Beam names reflect correct laterality");
                    }
                    if (positioningAlertsNeeded)
                    {
                        sb.AppendLine("   □ Special positioning instructions documented");
                        sb.AppendLine("   □ Therapist training for unique position confirmed");
                        sb.AppendLine("   □ Immobilization devices specified for position");
                    }
                }
                else
                {
                    sb.AppendLine();
                    sb.AppendLine("✅ Standard positioning - routine verification recommended");
                }
                
                sb.AppendLine();
                

                

                
                // Couch Verification Check
                sb.AppendLine("🛏️ TREATMENT COUCH VERIFICATION:");
                sb.AppendLine("--------------------------------");
                
                bool couchVerificationNeeded = false;
                bool noCouchNeeded = false;
                var couchRecommendations = new List<string>();
                var couchIssues = new List<string>();
                
                // Analyze treatment units for couch requirements
                if (treatmentMachines.Count > 0)
                {
                    sb.AppendLine("🏭 MACHINE-SPECIFIC COUCH REQUIREMENTS:");
                    
                    foreach (var machine in treatmentMachines)
                    {
                        string machineUpper = machine.ToUpper();
                        sb.AppendLine("   Machine: " + machine);
                        
                        // Check for Linac1 (BrainLAB/iBeam couch)
                        if (machineUpper.Contains("LINAC1") || machineUpper.Contains("LINAC_1") || 
                            machineUpper.Contains("TB1") || machineUpper.Contains("TRUEBEAM1"))
                        {
                            couchVerificationNeeded = true;
                            sb.AppendLine("     ➤ REQUIRED COUCH: BrainLAB/iBeam Couch");
                            couchRecommendations.Add("Linac1: BrainLAB/iBeam Couch");
                        }
                        // Check for Linac2 (Exact IGRT Couch)
                        else if (machineUpper.Contains("LINAC2") || machineUpper.Contains("LINAC_2") || 
                                machineUpper.Contains("TB2") || machineUpper.Contains("TRUEBEAM2"))
                        {
                            couchVerificationNeeded = true;
                            sb.AppendLine("     ➤ REQUIRED COUCH: Exact IGRT Couch (Thin)");
                            couchRecommendations.Add("Linac2: Exact IGRT Couch (Thin)");
                        }
                        else
                        {
                            couchVerificationNeeded = true;
                            sb.AppendLine("     ➤ VERIFY: Machine-specific couch requirement");
                            couchRecommendations.Add(machine + ": Verify machine-specific couch");
                        }
                    }
                }
                
                // Analyze treatment site for couch requirements
                sb.AppendLine();
                sb.AppendLine("🎯 SITE-SPECIFIC COUCH ANALYSIS:");
                
                // Check plan name, target structures, and site indicators
                string planNameUpperCouch = plan.Name != null ? plan.Name.ToUpper() : "";
                string planIdUpperCouch = plan.Id.ToUpper();
                
                // Look for head/brain site indicators
                bool isHeadBrainSite = false;
                var headBrainIndicators = new List<string>();
                
                // Check plan name/ID for head/brain indicators
                if (planIdUpperCouch.Contains("HEAD") || planNameUpperCouch.Contains("HEAD") ||
                    planIdUpperCouch.Contains("BRAIN") || planNameUpperCouch.Contains("BRAIN") ||
                    planIdUpperCouch.Contains("CNS") || planNameUpperCouch.Contains("CNS") ||
                    planIdUpperCouch.Contains("SRS") || planNameUpperCouch.Contains("SRS") ||
                    planIdUpperCouch.Contains("SKULL") || planNameUpperCouch.Contains("SKULL"))
                {
                    isHeadBrainSite = true;
                    headBrainIndicators.Add("Head/Brain indicator in plan name/ID");
                }
                
                // Check structures for head/brain indicators
                if (plan.StructureSet != null)
                {
                    var structures = plan.StructureSet.Structures.ToList();
                    var headBrainStructures = structures.Where(s =>
                        s.Id.ToUpper().Contains("BRAIN") ||
                        s.Id.ToUpper().Contains("SKULL") ||
                        s.Id.ToUpper().Contains("HEAD") ||
                        s.Id.ToUpper().Contains("CRANIUM") ||
                        s.Id.ToUpper().Contains("CEREBR") ||
                        s.Id.ToUpper().Contains("PONS") ||
                        s.Id.ToUpper().Contains("MEDULLA") ||
                        s.Id.ToUpper().Contains("CEREBELLUM")).ToList();
                    
                    if (headBrainStructures.Count > 0)
                    {
                        isHeadBrainSite = true;
                        headBrainIndicators.Add("Brain/head structures detected:");
                        foreach (var structure in headBrainStructures.Take(3))
                        {
                            headBrainIndicators.Add("  - " + structure.Id);
                        }
                        if (headBrainStructures.Count > 3)
                            headBrainIndicators.Add("  - ... and " + (headBrainStructures.Count - 3) + " more");
                    }
                }
                
                // Check patient positioning for head treatments
                string patientOrientationCouch = plan.TreatmentOrientation.ToString().ToUpper();
                if ((patientOrientationCouch.Contains("HEAD") || patientOrientationCouch.Contains("HFS")) && 
                    !patientOrientationCouch.Contains("FEET"))
                {
                    headBrainIndicators.Add("Head-first positioning detected");
                }
                
                // Report head/brain site findings
                if (isHeadBrainSite)
                {
                    noCouchNeeded = true;
                    sb.AppendLine("🧠 HEAD/BRAIN SITE DETECTED:");
                    foreach (var indicator in headBrainIndicators)
                    {
                        sb.AppendLine("   " + indicator);
                    }
                    sb.AppendLine();
                    sb.AppendLine("✅ NO COUCH NEEDED for head/brain treatments");
                    sb.AppendLine("   ➤ Head treatments typically use head-only support systems");
                    sb.AppendLine("   ➤ Couch should be excluded from dose calculation");
                    sb.AppendLine("   ➤ Verify head rest/mask system is appropriate");
                }
                else
                {
                    sb.AppendLine("📍 NON-HEAD SITE: Couch verification required");
                    couchVerificationNeeded = true;
                }
                
                // Generate couch verification requirements
                if (couchVerificationNeeded && !noCouchNeeded)
                {
                    sb.AppendLine();
                    sb.AppendLine("🚨 COUCH VERIFICATION REQUIREMENTS:");
                    sb.AppendLine("   ⚠️ TREATMENT COUCH MUST BE VERIFIED");
                    sb.AppendLine();
                    sb.AppendLine("   🔧 REQUIRED ACTIONS:");
                    sb.AppendLine("     ➤ Verify correct couch model selected in Eclipse");
                    sb.AppendLine("     ➤ Confirm couch is included in dose calculation");
                    sb.AppendLine("     ➤ Check couch attenuation factors are current");
                    sb.AppendLine("     ➤ Verify couch positioning matches treatment setup");
                    sb.AppendLine("     ➤ Ensure couch clearance with gantry rotation");
                    
                    sb.AppendLine();
                    sb.AppendLine("📋 COUCH VERIFICATION CHECKLIST:");
                    
                    foreach (var recommendation in couchRecommendations)
                    {
                        sb.AppendLine("   □ " + recommendation + " selected in Eclipse");
                    }
                    
                    sb.AppendLine("   □ Couch model matches physical treatment unit");
                    sb.AppendLine("   □ Couch included in dose calculation appropriately");
                    sb.AppendLine("   □ Couch attenuation data current and verified");
                    sb.AppendLine("   □ Couch position appropriate for beam arrangement");
                    sb.AppendLine("   □ No couch-gantry collision issues");
                    sb.AppendLine("   □ Couch rails positioned correctly for treatment");
                    sb.AppendLine("   □ Patient clearance verified with couch in position");
                    
                    sb.AppendLine();
                    sb.AppendLine("🔧 MACHINE-SPECIFIC REQUIREMENTS:");
                    sb.AppendLine("   LINAC1 REQUIREMENTS:");
                    sb.AppendLine("     □ BrainLAB/iBeam couch model selected");
                    sb.AppendLine("     □ iBeam couch positioning verified");
                    sb.AppendLine("     □ BrainLAB-specific accessories considered");
                    sb.AppendLine();
                    sb.AppendLine("   LINAC2 REQUIREMENTS:");
                    sb.AppendLine("     □ Exact IGRT Couch (Thin) model selected");
                    sb.AppendLine("     □ Thin couch profile verified for dose calculation");
                    sb.AppendLine("     □ IGRT imaging clearance confirmed");
                }
                else if (noCouchNeeded)
                {
                    sb.AppendLine();
                    sb.AppendLine("📋 HEAD/BRAIN TREATMENT CHECKLIST:");
                    sb.AppendLine("   □ Couch excluded from dose calculation");
                    sb.AppendLine("   □ Head rest/immobilization system specified");
                    sb.AppendLine("   □ Mask or headframe system documented");
                    sb.AppendLine("   □ Patient support adequate without couch");
                    sb.AppendLine("   □ Setup instructions specify head-only support");
                }
                
                // Check for potential couch-related issues
                if (couchVerificationNeeded && treatmentMachines.Count > 1)
                {
                    sb.AppendLine();
                    sb.AppendLine("⚠️ MULTIPLE MACHINE ALERT:");
                    sb.AppendLine("   Different machines may require different couch models");
                    sb.AppendLine("   Verify couch compatibility across all treatment units");
                    couchIssues.Add("Multiple machines with potentially different couch requirements");
                }
                
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
                // Bolus verification - moved to Beams tab
                if (breathingCompensationDetected)
                {
                    sb.AppendLine("□ Breathing technique documented in Mosaiq");
                    sb.AppendLine("□ Motion management instructions clear");
                    sb.AppendLine("□ Patient coaching protocol established");
                    sb.AppendLine("□ Imaging guidance requirements specified");
                }
                if (positioningAlertsNeeded || lateralityDetected)
                {
                    sb.AppendLine("□ Patient positioning verified and documented");
                    sb.AppendLine("□ Field labels checked for positioning accuracy");
                    if (lateralityDetected)
                    {
                        sb.AppendLine("□ " + detectedLaterality + " laterality confirmed");
                    }
                    if (positioningAlertsNeeded)
                    {
                        sb.AppendLine("□ Special positioning instructions documented");
                    }
                }
                // Treatment unit verification - always add this
                // Treatment unit verification - moved to Beams tab
                // Density override verification - moved to Structures tab
                // All additional verifications have been moved to their respective tabs:
                // - Bolus verification: moved to Beams tab
                // - Treatment unit verification: moved to Beams tab  
                // - Density override verification: moved to Structures tab
                // - Couch verification: moved to Isocenter tab
                // - User origin & isocenter verification: moved to Isocenter tab
                
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

                // Additional dose information can be added here
                
            }
            catch (Exception ex)
            {
                sb.AppendLine("❌ Error retrieving dose information: " + ex.Message);
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

                // Additional dose information can be added here
                
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
                
                try
                {
                    var userOrigin = plan.StructureSet.Image.UserOrigin;
                    sb.AppendLine("User Origin: (" + userOrigin.x.ToString("F1") + ", " + userOrigin.y.ToString("F1") + ", " + userOrigin.z.ToString("F1") + ") mm");
                }
                catch
                {
                    sb.AppendLine("⚠️  User origin coordinates unavailable");
                }

                // Analyze beam isocenters and potential shifts
                sb.AppendLine();
                sb.AppendLine("🎯 ISOCENTER ANALYSIS:");
                
                var beamGroups = plan.Beams.Where(b => !b.IsSetupField).GroupBy(b => 
                    "(" + b.IsocenterPosition.x.ToString("F1") + ", " + b.IsocenterPosition.y.ToString("F1") + ", " + b.IsocenterPosition.z.ToString("F1") + ")");

                bool hasLargeShift = false;
                bool hasAnyShift = false;
                var shiftAlerts = new List<string>();

                foreach (var group in beamGroups)
                {
                    var beamsList = group.ToList();
                    var firstBeam = beamsList.First();
                    var isocenter = firstBeam.IsocenterPosition;
                    
                    sb.AppendLine("Isocenter: (" + isocenter.x.ToString("F1") + ", " + isocenter.y.ToString("F1") + ", " + isocenter.z.ToString("F1") + ") mm");
                    sb.AppendLine("  Beams: " + string.Join(", ", beamsList.Select(b => b.Id)));

                    // Calculate distance from user origin to isocenter
                    try
                    {
                        var userOrigin = plan.StructureSet.Image.UserOrigin;
                        double shiftX = Math.Abs(isocenter.x - userOrigin.x);
                        double shiftY = Math.Abs(isocenter.y - userOrigin.y);
                        double shiftZ = Math.Abs(isocenter.z - userOrigin.z);
                        double totalShift = Math.Sqrt(shiftX * shiftX + shiftY * shiftY + shiftZ * shiftZ);

                        sb.AppendLine("  Shift from User Origin: " + totalShift.ToString("F1") + " mm");
                        sb.AppendLine("    ΔX: " + (isocenter.x - userOrigin.x).ToString("F1") + " mm, ΔY: " + (isocenter.y - userOrigin.y).ToString("F1") + " mm, ΔZ: " + (isocenter.z - userOrigin.z).ToString("F1") + " mm");

                        if (totalShift > 100.0) // Greater than 10cm
                        {
                            hasLargeShift = true;
                            shiftAlerts.Add("🚨 LARGE SHIFT DETECTED: " + totalShift.ToString("F1") + " mm shift for isocenter (" + isocenter.x.ToString("F1") + ", " + isocenter.y.ToString("F1") + ", " + isocenter.z.ToString("F1") + ")");
                        }
                        else if (totalShift > 5.0) // Greater than 5mm
                        {
                            hasAnyShift = true;
                        }
                    }
                    catch
                    {
                        sb.AppendLine("  ⚠️  Cannot calculate shift (user origin unavailable)");
                    }
                    
                    sb.AppendLine();
                }

                // Setup field analysis
                var setupFields = plan.Beams.Where(b => b.IsSetupField).ToList();
                if (setupFields.Count > 0)
                {
                    sb.AppendLine("🔧 SETUP FIELDS:");
                    foreach (var setupField in setupFields)
                    {
                        var setupIsocenter = setupField.IsocenterPosition;
                        sb.AppendLine("  " + setupField.Id + ": (" + setupIsocenter.x.ToString("F1") + ", " + setupIsocenter.y.ToString("F1") + ", " + setupIsocenter.z.ToString("F1") + ") mm");
                    }
                    sb.AppendLine();
                }

                // Generate alerts and recommendations
                if (shiftAlerts.Count > 0)
                {
                    sb.AppendLine("🚨 CRITICAL SHIFT ALERTS:");
                    foreach (var alert in shiftAlerts)
                    {
                        sb.AppendLine(alert);
                    }
                    sb.AppendLine();
                }

                sb.AppendLine("📋 USER ORIGIN & ISOCENTER REQUIREMENTS:");
                var originItems = new List<string>();

                if (hasLargeShift)
                {
                    originItems.Add("🚨 CRITICAL: Verify isocenter placement is correct");
                    originItems.Add("🚨 CRITICAL: Confirm user origin is set at correct anatomy");
                    originItems.Add("🚨 CRITICAL: Review patient setup and positioning");
                    originItems.Add("🚨 CRITICAL: Validate treatment planning coordinates");
                }
                else if (hasAnyShift)
                {
                    originItems.Add("⚠️  Verify isocenter positioning is appropriate");
                    originItems.Add("⚠️  Confirm user origin placement");
                }

                if (setupStructures.Count > 0)
                {
                    originItems.Add("✅ Verify setup structure (BB) coordinates match physics setup");
                    originItems.Add("✅ Confirm BB placement aligns with treatment isocenter");
                }
                else
                {
                    originItems.Add("⚠️  Consider adding setup structures (BB) for verification");
                }

                originItems.Add("✅ Verify user origin is placed at appropriate anatomical landmark");
                originItems.Add("✅ Confirm isocenter coordinates are clinically appropriate");
                originItems.Add("✅ Check setup instructions match coordinate system");

                foreach (var item in originItems)
                {
                    sb.AppendLine("  " + item);
                }

                sb.AppendLine();
                sb.AppendLine("📋 USER ORIGIN & ISOCENTER CHECKLIST:");
                var originChecklistItems = new List<string>
                {
                    "User origin placed at correct anatomical reference point",
                    "Isocenter coordinates reviewed and approved",
                    "Setup structure (BB) positions verified if present",
                    "Patient positioning matches coordinate system",
                    "Setup instructions are clear and accurate"
                };

                if (hasLargeShift)
                {
                    originChecklistItems.Insert(0, "🚨 Large shift (>10cm) investigation completed");
                    originChecklistItems.Insert(1, "🚨 Coordinate system verification performed");
                }

                foreach (var item in originChecklistItems)
                {
                    sb.AppendLine("  ☐ " + item);
                }

                // Summary note for critical findings
                if (hasLargeShift)
                {
                    sb.AppendLine();
                    sb.AppendLine("🚨 CRITICAL FOLLOW-UP REQUIRED:");
                    sb.AppendLine("   This plan has large coordinate shifts that require immediate attention!");
                    sb.AppendLine("   Verify with physics and physician before proceeding with treatment.");
                }
                else if (hasAnyShift)
                {
                    sb.AppendLine();
                    sb.AppendLine("ℹ️  NOTE: Coordinate shifts detected - verify positioning is appropriate.");
                }

                sb.AppendLine();
                sb.AppendLine("📝 Note: Always verify these details match between Eclipse and Mosaiq before treatment!");
                
            }
            catch (Exception ex)
            {
                sb.AppendLine("❌ Error retrieving plan information: " + ex.Message);
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

                // BOLUS ASSESSMENT
                sb.AppendLine();
                sb.AppendLine("🛡️ BOLUS ASSESSMENT:");
                sb.AppendLine("------------------");
                
                bool bolusDetected = false;
                var bolusStructures = new List<string>();
                
                // Check for bolus structures
                if (plan.StructureSet != null)
                {
                    var structures = plan.StructureSet.Structures.ToList();
                    var possibleBolus = structures.Where(s => 
                        s.Id.ToUpper().Contains("BOLUS") || 
                        s.Id.ToUpper().Contains("TISSUE") ||
                        s.DicomType == "BOLUS" ||
                        s.Id.ToUpper().Contains("WAX") ||
                        s.Id.ToUpper().Contains("SUPERFLAB")).ToList();
                    
                    if (possibleBolus.Count > 0)
                    {
                        bolusDetected = true;
                        sb.AppendLine("⚠️ BOLUS STRUCTURES DETECTED:");
                        foreach (var bolus in possibleBolus)
                        {
                            sb.AppendLine("   - " + bolus.Id + " (Type: " + bolus.DicomType + ")");
                            if (bolus.Volume > 0)
                            {
                                sb.AppendLine("     Volume: " + bolus.Volume.ToString("F1") + " cm³");
                            }
                            bolusStructures.Add(bolus.Id);
                        }
                    }
                }
                
                // Check beam accessories for bolus
                var beamsWithBolus = new List<string>();
                if (treatmentBeams.Count > 0)
                {
                    foreach (var beam in treatmentBeams)
                    {
                        // Check if beam has any accessories that might indicate bolus
                        // Note: ESAPI may not directly expose bolus accessories, but we can check for common patterns
                        if (beam.Id.ToUpper().Contains("BOLUS") || 
                            beam.Id.ToUpper().Contains("TISSUE"))
                        {
                            beamsWithBolus.Add(beam.Id);
                        }
                    }
                    
                    if (beamsWithBolus.Count > 0)
                    {
                        bolusDetected = true;
                        sb.AppendLine("⚠️ BEAMS WITH BOLUS INDICATION:");
                        foreach (var beamId in beamsWithBolus)
                        {
                            sb.AppendLine("   - " + beamId);
                        }
                    }
                }
                
                if (bolusDetected)
                {
                    sb.AppendLine();
                    sb.AppendLine("🚨 BOLUS REQUIREMENTS FOR MOSAIQ:");
                    sb.AppendLine("   ➤ Add bolus information to Mosaiq prescription");
                    sb.AppendLine("   ➤ Document bolus thickness and material");
                    sb.AppendLine("   ➤ Specify which fields require bolus");
                    sb.AppendLine("   ➤ Include setup instructions for therapists");
                    sb.AppendLine("   ➤ Verify bolus availability in department");
                    sb.AppendLine();
                    sb.AppendLine("📋 BOLUS DOCUMENTATION NEEDED:");
                    sb.AppendLine("   □ Bolus thickness (e.g., 0.5cm, 1.0cm)");
                    sb.AppendLine("   □ Bolus material (e.g., Superflab, tissue equivalent)");
                    sb.AppendLine("   □ Bolus coverage area");
                    sb.AppendLine("   □ Which treatment fields use bolus");
                    sb.AppendLine("   □ Daily setup verification required");
                }
                else
                {
                    sb.AppendLine("✅ No bolus detected - standard setup");
                }

                // TREATMENT UNIT VERIFICATION
                sb.AppendLine();
                sb.AppendLine("🏭 TREATMENT UNIT VERIFICATION:");
                sb.AppendLine("------------------------------");
                
                var treatmentMachines = new List<string>();
                bool machineVerificationNeeded = false;
                
                // Extract treatment unit information from beams
                if (treatmentBeams.Count > 0)
                {
                    var machinesFromBeams = treatmentBeams.Select(b => b.TreatmentUnit.Id).Distinct().ToList();
                    treatmentMachines.AddRange(machinesFromBeams);
                    
                    sb.AppendLine("📍 PLANNED TREATMENT UNIT(S):");
                    foreach (var machine in machinesFromBeams)
                    {
                        sb.AppendLine("   ✓ " + machine);
                        
                        // Count beams per machine
                        var beamsOnMachine = treatmentBeams.Where(b => b.TreatmentUnit.Id == machine).ToList();
                        sb.AppendLine("     - Beams on this unit: " + beamsOnMachine.Count);
                        
                        // List beam names for this machine
                        var beamNames = beamsOnMachine.Select(b => b.Id).ToList();
                        if (beamNames.Count <= 5)
                        {
                            sb.AppendLine("     - Fields: " + string.Join(", ", beamNames));
                        }
                        else
                        {
                            sb.AppendLine("     - Fields: " + string.Join(", ", beamNames.Take(3)) + 
                                         " ... and " + (beamNames.Count - 3) + " more");
                        }
                    }
                    
                    // Check for multiple machines
                    if (machinesFromBeams.Count > 1)
                    {
                        machineVerificationNeeded = true;
                        sb.AppendLine();
                        sb.AppendLine("⚠️ MULTIPLE TREATMENT UNITS DETECTED!");
                        sb.AppendLine("   ➤ Verify this is intentional (e.g., boost plan, machine backup)");
                        sb.AppendLine("   ➤ Ensure all units are properly scheduled in Mosaiq");
                        sb.AppendLine("   ➤ Confirm beam delivery sequence is correct");
                    }
                    
                    sb.AppendLine();
                    sb.AppendLine("🚨 TREATMENT UNIT REQUIREMENTS FOR MOSAIQ:");
                    sb.AppendLine("   ➤ Verify treatment unit matches planned machine");
                    sb.AppendLine("   ➤ Confirm machine availability for treatment dates");
                    sb.AppendLine("   ➤ Check machine-specific beam data is current");
                    sb.AppendLine("   ➤ Verify QA status of planned treatment unit");
                    sb.AppendLine("   ➤ Ensure therapist training on selected unit");
                    
                    sb.AppendLine();
                    sb.AppendLine("📋 TREATMENT UNIT CHECKLIST:");
                    foreach (var machine in machinesFromBeams)
                    {
                        sb.AppendLine("   □ " + machine + " selected in Mosaiq prescription");
                        sb.AppendLine("   □ " + machine + " available for treatment schedule");
                        sb.AppendLine("   □ " + machine + " QA current and complete");
                    }
                    sb.AppendLine("   □ Beam delivery sequence verified");
                    sb.AppendLine("   □ Machine-specific accessories available");
                    if (machinesFromBeams.Count > 1)
                    {
                        sb.AppendLine("   □ Multiple machine plan coordination confirmed");
                        sb.AppendLine("   □ Patient transfer procedures documented");
                    }
                    
                    // Check for common machine naming patterns to suggest verification
                    foreach (var machine in machinesFromBeams)
                    {
                        string machineUpper = machine.ToUpper();
                        if (machineUpper.Contains("IX") || machineUpper.Contains("VERSA") || 
                            machineUpper.Contains("TRILOGY") || machineUpper.Contains("CLINAC"))
                        {
                            sb.AppendLine("   □ Varian-specific beam data verified");
                            break;
                        }
                        else if (machineUpper.Contains("ELEKTA") || machineUpper.Contains("SYNERGY") || 
                                machineUpper.Contains("INFINITY"))
                        {
                            sb.AppendLine("   □ Elekta-specific beam data verified");
                            break;
                        }
                    }
                }
                else
                {
                    sb.AppendLine("❌ ERROR: No treatment beams found - cannot determine treatment unit");
                    machineVerificationNeeded = true;
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

                // CT IMAGING & DENSITY OVERRIDE ASSESSMENT
                sb.AppendLine();
                sb.AppendLine("🖼️ CT IMAGING & DENSITY OVERRIDE ASSESSMENT:");
                sb.AppendLine("--------------------------------------------");
                
                bool densityOverrideNeeded = false;
                var artifactStructures = new List<string>();
                var densityStructures = new List<string>();
                var contrastStructures = new List<string>();
                var prosthesisStructures = new List<string>();
                
                // Analyze CT image information
                if (plan.StructureSet != null && plan.StructureSet.Image != null)
                {
                    var image = plan.StructureSet.Image;
                    sb.AppendLine("📷 CT IMAGE ANALYSIS:");
                    sb.AppendLine("   ✓ Image Series: " + image.Id);
                    sb.AppendLine("   ✓ Image Resolution: " + image.XRes.ToString("F1") + " x " + image.YRes.ToString("F1") + " x " + image.ZRes.ToString("F1") + " mm");
                    
                    // Check image ID for contrast indicators
                    string imageIdUpper = image.Id.ToUpper();
                    if (imageIdUpper.Contains("CONTRAST") || imageIdUpper.Contains("IV") || 
                        imageIdUpper.Contains("PO") || imageIdUpper.Contains("ORAL"))
                    {
                        sb.AppendLine("   ⚠️ CONTRAST INDICATOR detected in image series name");
                        densityOverrideNeeded = true;
                    }
                }
                
                // Check structures for artifacts and density override indicators
                if (plan.StructureSet != null)
                {
                    // Look for artifact structures
                    var possibleArtifacts = structures.Where(s =>
                        s.Id.ToUpper().Contains("ARTIFACT") ||
                        s.Id.ToUpper().Contains("ZARTIFACT") ||
                        s.Id.ToUpper().Contains("STREAKING") ||
                        s.Id.ToUpper().Contains("METAL") ||
                        s.Id.ToUpper().Contains("BEAM_HARDENING")).ToList();
                    
                    // Look for density override structures
                    var possibleDensityOverrides = structures.Where(s =>
                        s.Id.ToUpper().Contains("HD") ||
                        s.Id.ToUpper().Contains("ZHD") ||
                        s.Id.ToUpper().Contains("ZDENSITY") ||
                        s.Id.ToUpper().Contains("DENSITY") ||
                        s.Id.ToUpper().Contains("OVERRIDE") ||
                        s.Id.ToUpper().Contains("HU")).ToList();
                    
                    // Look for contrast structures
                    var possibleContrast = structures.Where(s =>
                        s.Id.ToUpper().Contains("CONTRAST") ||
                        s.Id.ToUpper().Contains("ZCONTRAST") ||
                        s.Id.ToUpper().Contains("IV") ||
                        s.Id.ToUpper().Contains("ORAL") ||
                        s.Id.ToUpper().Contains("BARIUM") ||
                        s.Id.ToUpper().Contains("IODINE")).ToList();
                    
                    // Look for prosthesis/implant structures
                    var possibleProsthesis = structures.Where(s =>
                        s.Id.ToUpper().Contains("PROSTHES") ||
                        s.Id.ToUpper().Contains("IMPLANT") ||
                        s.Id.ToUpper().Contains("TITANIUM") ||
                        s.Id.ToUpper().Contains("STEEL") ||
                        s.Id.ToUpper().Contains("METAL") ||
                        s.Id.ToUpper().Contains("CLIP") ||
                        s.Id.ToUpper().Contains("MARKER") ||
                        s.Id.ToUpper().Contains("FIDUCIAL")).ToList();
                    
                    // Report findings
                    sb.AppendLine();
                    sb.AppendLine("🔍 STRUCTURE ANALYSIS:");
                    
                    if (possibleArtifacts.Count > 0)
                    {
                        densityOverrideNeeded = true;
                        sb.AppendLine("⚠️ ARTIFACT STRUCTURES DETECTED:");
                        foreach (var artifact in possibleArtifacts)
                        {
                            sb.AppendLine("   - " + artifact.Id + " (Volume: " + artifact.Volume.ToString("F1") + " cm³)");
                            artifactStructures.Add(artifact.Id);
                        }
                    }
                    
                    if (possibleDensityOverrides.Count > 0)
                    {
                        densityOverrideNeeded = true;
                        sb.AppendLine("⚠️ DENSITY OVERRIDE STRUCTURES DETECTED:");
                        foreach (var density in possibleDensityOverrides)
                        {
                            sb.AppendLine("   - " + density.Id + " (Volume: " + density.Volume.ToString("F1") + " cm³)");
                            densityStructures.Add(density.Id);
                        }
                    }
                    
                    if (possibleContrast.Count > 0)
                    {
                        densityOverrideNeeded = true;
                        sb.AppendLine("⚠️ CONTRAST-RELATED STRUCTURES DETECTED:");
                        foreach (var contrast in possibleContrast)
                        {
                            sb.AppendLine("   - " + contrast.Id + " (Volume: " + contrast.Volume.ToString("F1") + " cm³)");
                            contrastStructures.Add(contrast.Id);
                        }
                    }
                    
                    if (possibleProsthesis.Count > 0)
                    {
                        densityOverrideNeeded = true;
                        sb.AppendLine("⚠️ PROSTHESIS/IMPLANT STRUCTURES DETECTED:");
                        foreach (var prosthesis in possibleProsthesis)
                        {
                            sb.AppendLine("   - " + prosthesis.Id + " (Volume: " + prosthesis.Volume.ToString("F1") + " cm³)");
                            prosthesisStructures.Add(prosthesis.Id);
                        }
                    }
                    
                    if (!densityOverrideNeeded)
                    {
                        sb.AppendLine("✅ No obvious density override indicators detected");
                    }
                }
                
                // Generate alerts and recommendations
                if (densityOverrideNeeded)
                {
                    sb.AppendLine();
                    sb.AppendLine("🚨 DENSITY OVERRIDE REQUIREMENTS:");
                    sb.AppendLine("   ⚠️ HIGH-Z MATERIALS DETECTED - DENSITY OVERRIDES LIKELY NEEDED");
                    sb.AppendLine();
                    sb.AppendLine("   🔧 REQUIRED ACTIONS:");
                    sb.AppendLine("     ➤ Verify all high-Z materials have appropriate density overrides");
                    sb.AppendLine("     ➤ Check Hounsfield Unit assignments for accuracy");
                    sb.AppendLine("     ➤ Confirm override values match institution protocols");
                    sb.AppendLine("     ➤ Verify dose calculation accuracy in override regions");
                    sb.AppendLine("     ➤ Document override rationale in treatment planning notes");
                    
                    sb.AppendLine();
                    sb.AppendLine("📋 DENSITY OVERRIDE CHECKLIST:");
                    
                    if (artifactStructures.Count > 0)
                    {
                        sb.AppendLine("   ARTIFACT STRUCTURES:");
                        foreach (var artifact in artifactStructures)
                        {
                            sb.AppendLine("     □ " + artifact + " - density override applied");
                            sb.AppendLine("     □ " + artifact + " - HU value appropriate for tissue type");
                        }
                    }
                    
                    if (contrastStructures.Count > 0)
                    {
                        sb.AppendLine("   CONTRAST STRUCTURES:");
                        foreach (var contrast in contrastStructures)
                        {
                            sb.AppendLine("     □ " + contrast + " - contrast density removed/overridden");
                            sb.AppendLine("     □ " + contrast + " - HU set to native tissue value");
                        }
                        sb.AppendLine("     □ IV contrast effects removed from dose calculation");
                        sb.AppendLine("     □ Oral contrast regions appropriately handled");
                    }
                    
                    if (prosthesisStructures.Count > 0)
                    {
                        sb.AppendLine("   PROSTHESIS/IMPLANT STRUCTURES:");
                        foreach (var prosthesis in prosthesisStructures)
                        {
                            sb.AppendLine("     □ " + prosthesis + " - appropriate material density assigned");
                            sb.AppendLine("     □ " + prosthesis + " - HU value matches implant material");
                        }
                        sb.AppendLine("     □ Titanium implants: ~4000 HU or material-specific");
                        sb.AppendLine("     □ Steel implants: ~7000+ HU or material-specific");
                        sb.AppendLine("     □ Fiducial markers: appropriate material density");
                    }
                    
                    if (densityStructures.Count > 0)
                    {
                        sb.AppendLine("   DENSITY OVERRIDE STRUCTURES:");
                        foreach (var density in densityStructures)
                        {
                            sb.AppendLine("     □ " + density + " - override value verified");
                            sb.AppendLine("     □ " + density + " - institutional protocol followed");
                        }
                    }
                    
                    sb.AppendLine();
                    sb.AppendLine("   GENERAL DENSITY VERIFICATION:");
                    sb.AppendLine("     □ All override regions identified and contoured");
                    sb.AppendLine("     □ HU assignments documented in planning notes");
                    sb.AppendLine("     □ Dose calculation recalculated after overrides");
                    sb.AppendLine("     □ Physics review of override appropriateness");
                    sb.AppendLine("     □ CT artifact impact on dose minimized");
                    
                    sb.AppendLine();
                    sb.AppendLine("⚠️ CRITICAL REMINDER:");
                    sb.AppendLine("   High-Z materials (contrast, metal implants, artifacts) can significantly");
                    sb.AppendLine("   affect dose calculation accuracy. Proper density overrides are REQUIRED");
                    sb.AppendLine("   for accurate treatment planning and safe dose delivery.");
                }
                else
                {
                    sb.AppendLine();
                    sb.AppendLine("✅ No density override indicators detected");
                    sb.AppendLine("   ➤ Standard CT tissue assignments appear appropriate");
                    sb.AppendLine("   ➤ Routine verification of image quality recommended");
                }
                
            }
            catch (Exception ex)
            {
                sb.AppendLine("❌ Error retrieving structure information: " + ex.Message);
            }
        }

        private void CheckIsocenterInformation(PlanSetup plan, StringBuilder sb)
        {
            sb.AppendLine("6. ISOCENTER & SETUP VERIFICATION:");
            sb.AppendLine("====================================");
            
            try
            {
                // TREATMENT COUCH VERIFICATION
                sb.AppendLine("🛏️ TREATMENT COUCH VERIFICATION:");
                sb.AppendLine("--------------------------------");
                
                bool couchVerificationNeeded = false;
                bool noCouchNeeded = false;
                var couchRecommendations = new List<string>();
                var couchIssues = new List<string>();
                var treatmentMachines = new List<string>();
                
                // Get treatment machines from beams
                var beams = plan.Beams.ToList();
                if (beams.Count > 0)
                {
                    var machinesFromBeams = beams.Select(b => b.TreatmentUnit.Id).Distinct().ToList();
                    treatmentMachines.AddRange(machinesFromBeams);
                    
                    sb.AppendLine("🏭 MACHINE-SPECIFIC COUCH REQUIREMENTS:");
                    
                    foreach (var machine in machinesFromBeams)
                    {
                        string machineUpper = machine.ToUpper();
                        sb.AppendLine("   Machine: " + machine);
                        
                        // Check for Linac1 (BrainLAB/iBeam couch)
                        if (machineUpper.Contains("LINAC1") || machineUpper.Contains("LINAC_1") || 
                            machineUpper.Contains("TB1") || machineUpper.Contains("TRUEBEAM1"))
                        {
                            couchVerificationNeeded = true;
                            sb.AppendLine("     ➤ REQUIRED COUCH: BrainLAB/iBeam Couch");
                            couchRecommendations.Add("Linac1: BrainLAB/iBeam Couch");
                        }
                        // Check for Linac2 (Exact IGRT Couch)
                        else if (machineUpper.Contains("LINAC2") || machineUpper.Contains("LINAC_2") || 
                                machineUpper.Contains("TB2") || machineUpper.Contains("TRUEBEAM2"))
                        {
                            couchVerificationNeeded = true;
                            sb.AppendLine("     ➤ REQUIRED COUCH: Exact IGRT Couch (Thin)");
                            couchRecommendations.Add("Linac2: Exact IGRT Couch (Thin)");
                        }
                        else
                        {
                            couchVerificationNeeded = true;
                            sb.AppendLine("     ➤ VERIFY COUCH: Check machine-specific requirements");
                            couchRecommendations.Add("Machine-specific couch requirements");
                        }
                    }
                }
                
                sb.AppendLine();
                sb.AppendLine("🎯 SITE-SPECIFIC COUCH ANALYSIS:");
                
                // Check plan name, target structures, and site indicators
                string planNameUpperCouch = plan.Name != null ? plan.Name.ToUpper() : "";
                string planIdUpperCouch = plan.Id.ToUpper();
                
                // Look for head/brain site indicators
                bool isHeadBrainSite = false;
                var headBrainIndicators = new List<string>();
                
                // Check plan name/ID for head/brain indicators
                if (planIdUpperCouch.Contains("HEAD") || planNameUpperCouch.Contains("HEAD") ||
                    planIdUpperCouch.Contains("BRAIN") || planNameUpperCouch.Contains("BRAIN") ||
                    planIdUpperCouch.Contains("CNS") || planNameUpperCouch.Contains("CNS") ||
                    planIdUpperCouch.Contains("SRS") || planNameUpperCouch.Contains("SRS") ||
                    planIdUpperCouch.Contains("SKULL") || planNameUpperCouch.Contains("SKULL"))
                {
                    isHeadBrainSite = true;
                    headBrainIndicators.Add("Head/Brain indicator in plan name/ID");
                }
                
                // Check for head/brain structures
                if (plan.StructureSet != null)
                {
                    var headBrainStructures = plan.StructureSet.Structures.Where(s =>
                        s.Id.ToUpper().Contains("BRAIN") ||
                        s.Id.ToUpper().Contains("BRAINSTEM") ||
                        s.Id.ToUpper().Contains("CEREBR") ||
                        s.Id.ToUpper().Contains("SKULL") ||
                        s.Id.ToUpper().Contains("HEAD") ||
                        s.Id.ToUpper().Contains("CRANIUM")).ToList();
                    
                    if (headBrainStructures.Count > 0)
                    {
                        isHeadBrainSite = true;
                        headBrainIndicators.Add("Brain/head structures detected:");
                        foreach (var structure in headBrainStructures.Take(3))
                        {
                            headBrainIndicators.Add("  - " + structure.Id);
                        }
                        if (headBrainStructures.Count > 3)
                            headBrainIndicators.Add("  - ... and " + (headBrainStructures.Count - 3) + " more");
                    }
                }
                
                // Check patient positioning for head treatments
                string patientOrientationCouch = plan.TreatmentOrientation.ToString().ToUpper();
                if ((patientOrientationCouch.Contains("HEAD") || patientOrientationCouch.Contains("HFS")) && 
                    !patientOrientationCouch.Contains("FEET"))
                {
                    headBrainIndicators.Add("Head-first positioning detected");
                }
                
                // Report head/brain site findings
                if (isHeadBrainSite)
                {
                    noCouchNeeded = true;
                    sb.AppendLine("🧠 HEAD/BRAIN SITE DETECTED:");
                    foreach (var indicator in headBrainIndicators)
                    {
                        sb.AppendLine("   " + indicator);
                    }
                    sb.AppendLine();
                    sb.AppendLine("✅ NO COUCH NEEDED for head/brain treatments");
                    sb.AppendLine("   ➤ Head treatments typically use head-only support systems");
                    sb.AppendLine("   ➤ Couch should be excluded from dose calculation");
                    sb.AppendLine("   ➤ Verify head rest/mask system is appropriate");
                }
                else
                {
                    sb.AppendLine("📍 NON-HEAD SITE: Couch verification required");
                    couchVerificationNeeded = true;
                }
                
                // Generate couch verification requirements
                if (couchVerificationNeeded && !noCouchNeeded)
                {
                    sb.AppendLine();
                    sb.AppendLine("🚨 COUCH VERIFICATION REQUIREMENTS:");
                    sb.AppendLine("   ➤ Verify correct couch model is selected in Eclipse");
                    sb.AppendLine("   ➤ Confirm couch is included in dose calculation");
                    sb.AppendLine("   ➤ Check couch attenuation data is current");
                    sb.AppendLine("   ➤ Verify no couch-gantry collision issues");
                    sb.AppendLine("   ➤ Ensure patient clearance with couch positioned");
                    
                    sb.AppendLine();
                    sb.AppendLine("📋 COUCH VERIFICATION CHECKLIST:");
                    if (couchRecommendations.Count > 0)
                    {
                        foreach (var rec in couchRecommendations)
                        {
                            sb.AppendLine("   □ " + rec + " - correct model selected");
                        }
                    }
                    
                    sb.AppendLine("   □ Couch model matches physical treatment unit");
                    sb.AppendLine("   □ Couch included in dose calculation appropriately");
                    sb.AppendLine("   □ Couch attenuation data current and verified");
                    sb.AppendLine("   □ Couch position appropriate for beam arrangement");
                    sb.AppendLine("   □ No couch-gantry collision issues");
                    sb.AppendLine("   □ Couch rails positioned correctly for treatment");
                    sb.AppendLine("   □ Patient clearance verified with couch in position");
                    
                    sb.AppendLine();
                    sb.AppendLine("   LINAC1 REQUIREMENTS:");
                    sb.AppendLine("     □ BrainLAB/iBeam couch model selected");
                    sb.AppendLine("     □ BrainLAB couch profile verified for dose calculation");
                    sb.AppendLine();
                    sb.AppendLine("   LINAC2 REQUIREMENTS:");
                    sb.AppendLine("     □ Exact IGRT Couch (Thin) model selected");
                    sb.AppendLine("     □ Thin couch profile verified for dose calculation");
                    sb.AppendLine("     □ IGRT imaging clearance confirmed");
                }
                else if (noCouchNeeded)
                {
                    sb.AppendLine();
                    sb.AppendLine("📋 HEAD/BRAIN TREATMENT CHECKLIST:");
                    sb.AppendLine("   □ Couch excluded from dose calculation");
                    sb.AppendLine("   □ Head rest/immobilization system specified");
                    sb.AppendLine("   □ Mask or headframe system documented");
                    sb.AppendLine("   □ Patient support adequate without couch");
                    sb.AppendLine("   □ Setup instructions specify head-only support");
                }

                // USER ORIGIN & ISOCENTER VERIFICATION
                sb.AppendLine();
                sb.AppendLine("🎯 USER ORIGIN & ISOCENTER VERIFICATION");
                sb.AppendLine("═══════════════════════════════════════════════════════════════");
                
                // Check for setup BB structures
                var setupStructures = plan.StructureSet != null ? 
                    plan.StructureSet.Structures.Where(s => s != null && !s.IsEmpty && 
                        (s.Id.ToUpper().Contains("BB") || s.Id.ToUpper().Contains("BALL") || 
                         s.Id.ToUpper().Contains("ZBB") || s.Id.ToUpper().Contains("SETUP"))).ToList() : 
                    new List<VMS.TPS.Common.Model.API.Structure>();

                sb.AppendLine();
                sb.AppendLine("🔍 SETUP STRUCTURE ANALYSIS:");
                
                if (setupStructures.Count > 0)
                {
                    sb.AppendLine("✅ Setup structures found:");
                    foreach (var structure in setupStructures)
                    {
                        try
                        {
                            // Get structure center point as approximation
                            var bounds = structure.MeshGeometry.Bounds;
                            double centerX = (bounds.X + bounds.SizeX / 2.0);
                            double centerY = (bounds.Y + bounds.SizeY / 2.0);
                            double centerZ = (bounds.Z + bounds.SizeZ / 2.0);
                            
                            sb.AppendLine("  • " + structure.Id + ": Center ≈ (" + centerX.ToString("F1") + ", " + centerY.ToString("F1") + ", " + centerZ.ToString("F1") + ") mm");
                        }
                        catch
                        {
                            sb.AppendLine("  • " + structure.Id + ": (coordinates unavailable)");
                        }
                    }
                }
                else
                {
                    sb.AppendLine("⚠️  No setup structures (BB, zBB) found in plan");
                    sb.AppendLine("   Common setup structure naming: BB, zBB, Ball Bearing, Setup");
                }

                // Get user origin information
                sb.AppendLine();
                sb.AppendLine("📍 USER ORIGIN INFORMATION:");
                
                try
                {
                    var userOrigin = plan.StructureSet.Image.UserOrigin;
                    sb.AppendLine("User Origin: (" + userOrigin.x.ToString("F1") + ", " + userOrigin.y.ToString("F1") + ", " + userOrigin.z.ToString("F1") + ") mm");
                }
                catch
                {
                    sb.AppendLine("⚠️  User origin coordinates unavailable");
                }

                // Analyze beam isocenters and potential shifts
                sb.AppendLine();
                sb.AppendLine("🎯 ISOCENTER ANALYSIS:");
                
                var treatmentBeams = beams.Where(b => !b.IsSetupField).ToList();
                var beamGroups = treatmentBeams.GroupBy(b => 
                    "(" + b.IsocenterPosition.x.ToString("F1") + ", " + b.IsocenterPosition.y.ToString("F1") + ", " + b.IsocenterPosition.z.ToString("F1") + ")");

                bool hasLargeShift = false;
                bool hasAnyShift = false;
                var shiftAlerts = new List<string>();

                foreach (var group in beamGroups)
                {
                    var beamsList = group.ToList();
                    var firstBeam = beamsList.First();
                    var isocenter = firstBeam.IsocenterPosition;
                    
                    sb.AppendLine("Isocenter: (" + isocenter.x.ToString("F1") + ", " + isocenter.y.ToString("F1") + ", " + isocenter.z.ToString("F1") + ") mm");
                    sb.AppendLine("  Beams: " + string.Join(", ", beamsList.Select(b => b.Id)));

                    // Calculate distance from user origin to isocenter
                    try
                    {
                        var userOrigin = plan.StructureSet.Image.UserOrigin;
                        double shiftX = Math.Abs(isocenter.x - userOrigin.x);
                        double shiftY = Math.Abs(isocenter.y - userOrigin.y);
                        double shiftZ = Math.Abs(isocenter.z - userOrigin.z);
                        double totalShift = Math.Sqrt(shiftX * shiftX + shiftY * shiftY + shiftZ * shiftZ);

                        sb.AppendLine("  Shift from User Origin: " + totalShift.ToString("F1") + " mm");
                        sb.AppendLine("    ΔX: " + (isocenter.x - userOrigin.x).ToString("F1") + " mm, ΔY: " + (isocenter.y - userOrigin.y).ToString("F1") + " mm, ΔZ: " + (isocenter.z - userOrigin.z).ToString("F1") + " mm");

                        if (totalShift > 100.0) // Greater than 10cm
                        {
                            hasLargeShift = true;
                            shiftAlerts.Add("🚨 LARGE SHIFT DETECTED: " + totalShift.ToString("F1") + " mm shift for isocenter (" + isocenter.x.ToString("F1") + ", " + isocenter.y.ToString("F1") + ", " + isocenter.z.ToString("F1") + ")");
                        }
                        else if (totalShift > 5.0) // Greater than 5mm
                        {
                            hasAnyShift = true;
                        }
                    }
                    catch
                    {
                        sb.AppendLine("  ⚠️  Cannot calculate shift (user origin unavailable)");
                    }
                    
                    sb.AppendLine();
                }

                // Setup field analysis
                var setupFields = beams.Where(b => b.IsSetupField).ToList();
                if (setupFields.Count > 0)
                {
                    sb.AppendLine("🔧 SETUP FIELDS:");
                    foreach (var setupField in setupFields)
                    {
                        var setupIsocenter = setupField.IsocenterPosition;
                        sb.AppendLine("  " + setupField.Id + ": (" + setupIsocenter.x.ToString("F1") + ", " + setupIsocenter.y.ToString("F1") + ", " + setupIsocenter.z.ToString("F1") + ") mm");
                    }
                    sb.AppendLine();
                }

                // Generate alerts and recommendations
                if (shiftAlerts.Count > 0)
                {
                    sb.AppendLine("🚨 CRITICAL SHIFT ALERTS:");
                    foreach (var alert in shiftAlerts)
                    {
                        sb.AppendLine(alert);
                    }
                    sb.AppendLine();
                }

                sb.AppendLine("📋 USER ORIGIN & ISOCENTER REQUIREMENTS:");
                var originItems = new List<string>();

                if (hasLargeShift)
                {
                    originItems.Add("🚨 CRITICAL: Verify isocenter placement is correct");
                    originItems.Add("🚨 CRITICAL: Confirm user origin is set at correct anatomy");
                    originItems.Add("🚨 CRITICAL: Review patient setup and positioning");
                    originItems.Add("🚨 CRITICAL: Validate treatment planning coordinates");
                }
                else if (hasAnyShift)
                {
                    originItems.Add("⚠️  Verify isocenter positioning is appropriate");
                    originItems.Add("⚠️  Confirm user origin placement");
                }

                if (setupStructures.Count > 0)
                {
                    originItems.Add("✅ Verify setup structure (BB) coordinates match physics setup");
                    originItems.Add("✅ Confirm BB placement aligns with treatment isocenter");
                }
                else
                {
                    originItems.Add("⚠️  Consider adding setup structures (BB) for verification");
                }

                originItems.Add("✅ Verify user origin is placed at appropriate anatomical landmark");
                originItems.Add("✅ Confirm isocenter coordinates are clinically appropriate");
                originItems.Add("✅ Check setup instructions match coordinate system");

                foreach (var item in originItems)
                {
                    sb.AppendLine("  " + item);
                }

                sb.AppendLine();
                sb.AppendLine("📋 USER ORIGIN & ISOCENTER CHECKLIST:");
                var originChecklistItems = new List<string>
                {
                    "User origin placed at correct anatomical reference point",
                    "Isocenter coordinates reviewed and approved",
                    "Setup structure (BB) positions verified if present",
                    "Patient positioning matches coordinate system",
                    "Setup instructions are clear and accurate"
                };

                if (hasLargeShift)
                {
                    originChecklistItems.Insert(0, "🚨 Large shift (>10cm) investigation completed");
                    originChecklistItems.Insert(1, "🚨 Coordinate system verification performed");
                }

                foreach (var item in originChecklistItems)
                {
                    sb.AppendLine("  ☐ " + item);
                }

                // Summary note for critical findings
                if (hasLargeShift)
                {
                    sb.AppendLine();
                    sb.AppendLine("🚨 CRITICAL FOLLOW-UP REQUIRED:");
                    sb.AppendLine("   This plan has large coordinate shifts that require immediate attention!");
                    sb.AppendLine("   Verify with physics and physician before proceeding with treatment.");
                }
                else if (hasAnyShift)
                {
                    sb.AppendLine();
                    sb.AppendLine("ℹ️  NOTE: Coordinate shifts detected - verify positioning is appropriate.");
                }

                sb.AppendLine();
                sb.AppendLine("📝 Note: Always verify these details match between Eclipse and Mosaiq before treatment!");
                
            }
            catch (Exception ex)
            {
                sb.AppendLine("❌ Error retrieving isocenter information: " + ex.Message);
            }
        }

        private void CheckPlanStatus(PlanSetup plan, StringBuilder sb)
        {
            sb.AppendLine("7. PLAN STATUS & SAFETY CHECKS:");
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
