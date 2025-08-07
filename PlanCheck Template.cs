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

                if (context.StructureSet == null)
                {
                    MessageBox.Show("Loaded plan has no structure set.", "Plan Check Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                }

                if (context.PlanSetup.Dose == null)
                {
                    MessageBox.Show("Loaded plan has no dose calculation.", "Plan Check Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                }

                // Launch the plan check application (non-blocking)
                var planCheckWindow = new PlanCheckWindow(context);
                planCheckWindow.Show(); // Use Show() instead of ShowDialog() to allow Eclipse to remain accessible

                // Add dispatcher frame to keep script running
                System.Windows.Threading.DispatcherFrame frame = new System.Windows.Threading.DispatcherFrame();
                planCheckWindow.Closed += (sender, e) => 
                {
                    frame.Continue = false;
                };
                System.Windows.Threading.Dispatcher.PushFrame(frame);
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
        private Button refreshButton;
        private Button exportButton;
        private Button closeButton;
        private Button topmostButton;

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
            this.ResizeMode = ResizeMode.CanResize;
            this.Background = Brushes.WhiteSmoke;
            
            // Set maximum size to prevent extending beyond screen
            this.MaxHeight = System.Windows.SystemParameters.WorkArea.Height * 0.9;
            this.MaxWidth = System.Windows.SystemParameters.WorkArea.Width * 0.9;
            
            // Set minimum size for usability
            this.MinHeight = 500;
            this.MinWidth = 700;

            // Position window to not interfere with Eclipse
            this.Left = System.Windows.SystemParameters.WorkArea.Width * 0.1;
            this.Top = System.Windows.SystemParameters.WorkArea.Height * 0.1;

            // Keep window on top initially (user can toggle this)
            this.Topmost = true;

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
                Content = "Plan Check Results - " + planId, // Use planId for title
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(10)
            };

            var subtitleLabel = new Label
            {
                Content = "Comprehensive Plan Analysis and Verification - Use alongside Eclipse",
                FontSize = 12,
                FontWeight = FontWeights.Normal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(5)
            };

            var instructionLabel = new Label
            {
                Content = "✓ Check issues below, then return to Eclipse to make corrections. Use 'Refresh' to update after changes.",
                FontSize = 10,
                FontWeight = FontWeights.Normal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(5),
                Foreground = Brushes.DarkGreen
            };

            headerPanel.Children.Add(titleLabel);
            headerPanel.Children.Add(subtitleLabel);
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
                    HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                    FlowDirection = FlowDirection.LeftToRight,
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
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(10)
            };

            refreshButton = new Button
            {
                Content = "Refresh",
                Width = 120,
                Height = 30,
                Margin = new Thickness(5),
                Background = Brushes.LightGreen,
                BorderBrush = Brushes.Gray
            };
            refreshButton.Click += (s, e) => RunPlanCheck();

            topmostButton = new Button
            {
                Content = "Stay on Top: ON",
                Width = 120,
                Height = 30,
                Margin = new Thickness(5),
                Background = Brushes.LightYellow,
                BorderBrush = Brushes.Gray
            };
            topmostButton.Click += (s, e) => ToggleTopmost();

            exportButton = new Button
            {
                Content = "Export Results",
                Width = 120,
                Height = 30,
                Margin = new Thickness(5),
                Background = Brushes.LightBlue,
                BorderBrush = Brushes.Gray
            };
            exportButton.Click += (s, e) => ExportResults();

            closeButton = new Button
            {
                Content = "Close",
                Width = 80,
                Height = 30,
                Margin = new Thickness(5),
                Background = Brushes.LightGray,
                BorderBrush = Brushes.Gray
            };
            closeButton.Click += (s, e) => this.Close();

            buttonPanel.Children.Add(refreshButton);
            buttonPanel.Children.Add(topmostButton);
            buttonPanel.Children.Add(exportButton);
            buttonPanel.Children.Add(closeButton);

            Grid.SetRow(buttonPanel, 2);
            mainGrid.Children.Add(buttonPanel);
        }

        private void ToggleTopmost()
        {
            this.Topmost = !this.Topmost;
            topmostButton.Content = "Stay on Top: " + (this.Topmost ? "ON" : "OFF");
            topmostButton.Background = this.Topmost ? Brushes.LightYellow : Brushes.LightGray;
        }

        private void RunPlanCheck()
        {
            try
            {
                var plan = scriptContext.PlanSetup;
                if (plan == null)
                {
                    MessageBox.Show("Plan not found. Please ensure the original plan is still open.", "Refresh Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Clear and update each tab
                UpdateTab("Plan Info", plan);
                UpdateTab("Dose", plan);
                UpdateTab("Beams", plan);
                UpdateTab("Structures", plan);
                UpdateTab("Status", plan);
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("Error refreshing plan data: {0}", ex.Message), "Refresh Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateTab(string tabName, PlanSetup plan)
        {
            TextBox textBox = null;
            if (tabTextBoxes.TryGetValue(tabName, out textBox))
            {
                var sb = new StringBuilder();
                
                // Add tab-specific title with timestamp
                sb.AppendLine("*** " + tabName + " ***");
                sb.AppendLine("Last Updated: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                sb.AppendLine();

                // Call the check
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

                // Add help note
                sb.AppendLine();
                sb.AppendLine("For additional help, contact your medical physicist.");
                
                textBox.Text = sb.ToString();
            }
        }

        private void CheckPlanInformation(PlanSetup plan, StringBuilder sb)
        {
            sb.AppendLine("TEST: This should display horizontally as normal text");
            sb.AppendLine();
            
            sb.AppendLine("1. PLAN INFORMATION:");
            
            try
            {
                sb.AppendLine("✓ Plan ID: " + plan.Id);
                sb.AppendLine("✓ Plan Name: " + plan.Name);
                sb.AppendLine("✓ Plan Status: " + plan.ApprovalStatus.ToString());
                sb.AppendLine("✓ Treatment Orientation: " + plan.TreatmentOrientation.ToString());
                sb.AppendLine("✓ Plan Type: " + plan.PlanType.ToString());
                
                if (plan.Course != null)
                {
                    sb.AppendLine("✓ Course ID: " + plan.Course.Id);
                    
                    if (plan.Course.Patient != null)
                    {
                        sb.AppendLine("✓ Patient ID: " + plan.Course.Patient.Id);
                    }
                }
            }
            catch (Exception ex)
            {
                sb.AppendLine("❌ Error retrieving plan information: " + ex.Message);
            }
            
            sb.AppendLine();
        }

        private void CheckDoseInformation(PlanSetup plan, StringBuilder sb)
        {
            sb.AppendLine("2. DOSE INFORMATION:");
            sb.AppendLine("✓ Dose checks will be implemented");
            sb.AppendLine();
        }

        private void CheckBeamInformation(PlanSetup plan, StringBuilder sb)
        {
            AddSectionTitle(paragraph, "3. BEAM INFORMATION:");
            
            try
            {
                var beams = plan.Beams.ToList();
                AddInfoItem(paragraph, string.Format("Total Beams: {0}", beams.Count));
                
                var setupFields = beams.Where(b => b.IsSetupField).ToList();
                var treatmentBeams = beams.Where(b => !b.IsSetupField).ToList();
                
                AddInfoItem(paragraph, string.Format("Setup Fields: {0}", setupFields.Count));
                AddInfoItem(paragraph, string.Format("Treatment Beams: {0}", treatmentBeams.Count));
                
                paragraph.Inlines.Add(new LineBreak());
                
                // Treatment beams details
                if (treatmentBeams.Count > 0)
                {
                    AddSubSectionTitle(paragraph, "Treatment Beams:");
                    
                    foreach (var beam in treatmentBeams)
                    {
                        AddInfoItem(paragraph, string.Format("  Beam ID: {0}", beam.Id));
                        AddInfoItem(paragraph, string.Format("    Energy: {0}", beam.EnergyModeDisplayName));
                        AddInfoItem(paragraph, string.Format("    Technique: {0}", beam.Technique));
                        AddInfoItem(paragraph, string.Format("    MLC Plan Type: {0}", beam.MLCPlanType));
                        
                        // Try to get gantry rotation information
                        try
                        {
                            if (beam.ControlPoints != null && beam.ControlPoints.Count > 0)
                            {
                                var firstCP = beam.ControlPoints.First();
                                var lastCP = beam.ControlPoints.Last();
                                
                                if (firstCP.GantryAngle != lastCP.GantryAngle)
                                {
                                    AddInfoItem(paragraph, string.Format("    Gantry Rotation: {0:F1}° to {1:F1}°", firstCP.GantryAngle, lastCP.GantryAngle));
                                }
                                else
                                {
                                    AddInfoItem(paragraph, string.Format("    Gantry Angle: {0:F1}°", firstCP.GantryAngle));
                                }
                            }
                            else
                            {
                                AddInfoItem(paragraph, "    Gantry Angle: Manual verification required");
                            }
                        }
                        catch
                        {
                            AddInfoItem(paragraph, "    Gantry Angle: Manual verification required");
                        }
                        
                        AddInfoItem(paragraph, string.Format("    MU: {0:F1}", beam.Meterset.Value));
                        paragraph.Inlines.Add(new LineBreak());
                    }
                }
                
                // Setup fields details
                if (setupFields.Count > 0)
                {
                    AddSubSectionTitle(paragraph, "Setup Fields:");
                    
                    foreach (var beam in setupFields)
                    {
                        AddInfoItem(paragraph, string.Format("  Setup Field: {0}", beam.Id));
                        AddInfoItem(paragraph, string.Format("    Energy: {0}", beam.EnergyModeDisplayName));
                        AddInfoItem(paragraph, string.Format("    Technique: {0}", beam.Technique));
                        paragraph.Inlines.Add(new LineBreak());
                    }
                }
            }
            catch (Exception ex)
            {
                AddErrorItem(paragraph, string.Format("Error retrieving beam information: {0}", ex.Message));
            }
            
            paragraph.Inlines.Add(new LineBreak());
        }

        private void CheckStructureInformation(PlanSetup plan, Paragraph paragraph)
        {
            AddSectionTitle(paragraph, "4. STRUCTURE INFORMATION:");
            
            try
            {
                if (plan.StructureSet != null)
                {
                    var structures = plan.StructureSet.Structures.ToList();
                    AddInfoItem(paragraph, string.Format("Total Structures: {0}", structures.Count));
                    
                    var bodyStructure = structures.FirstOrDefault(s => s.Id.ToLower().Contains("body"));
                    if (bodyStructure != null)
                    {
                        AddInfoItem(paragraph, string.Format("Body Contour: {0} (Found)", bodyStructure.Id));
                    }
                    else
                    {
                        AddWarningItem(paragraph, "Body Contour: Not found");
                    }
                    
                    // List key structures
                    var keyStructures = structures.Where(s => 
                        s.Id.ToLower().Contains("ptv") || 
                        s.Id.ToLower().Contains("ctv") || 
                        s.Id.ToLower().Contains("gtv") ||
                        s.Id.ToLower().Contains("rectum") ||
                        s.Id.ToLower().Contains("bladder") ||
                        s.Id.ToLower().Contains("bowel") ||
                        s.Id.ToLower().Contains("sigmoid") ||
                        s.Id.ToLower().Contains("urethra")
                    ).ToList();
                    
                    if (keyStructures.Count > 0)
                    {
                        AddSubSectionTitle(paragraph, "Key Structures:");
                        foreach (var structure in keyStructures.Take(10)) // Limit to first 10
                        {
                            AddInfoItem(paragraph, string.Format("  {0}", structure.Id));
                        }
                        
                        if (keyStructures.Count > 10)
                        {
                            AddInfoItem(paragraph, string.Format("  ... and {0} more structures", keyStructures.Count - 10));
                        }
                    }
                }
                else
                {
                    AddWarningItem(paragraph, "No structure set available");
                }
            }
            catch (Exception ex)
            {
                AddErrorItem(paragraph, string.Format("Error retrieving structure information: {0}", ex.Message));
            }
            
            paragraph.Inlines.Add(new LineBreak());
        }

        private void CheckPlanStatus(PlanSetup plan, Paragraph paragraph)
        {
            AddSectionTitle(paragraph, "5. PLAN STATUS AND VALIDATION:");
            
            try
            {
                AddInfoItem(paragraph, string.Format("Approval Status: {0}", plan.ApprovalStatus));
                
                if (plan.ApprovalStatus.ToString().ToLower().Contains("planning"))
                {
                    AddWarningItem(paragraph, "Plan is still in planning status - requires approval");
                }
                
                // Check for common issues
                var beams = plan.Beams.ToList();
                var treatmentBeams = beams.Where(b => !b.IsSetupField).ToList();
                
                if (treatmentBeams.Count == 0)
                {
                    AddWarningItem(paragraph, "No treatment beams found");
                }
                
                if (plan.Dose == null)
                {
                    AddWarningItem(paragraph, "No dose calculation available");
                }
                
                if (plan.StructureSet == null)
                {
                    AddWarningItem(paragraph, "No structure set available");
                }
                
                // Check for IMRT/VMAT specific requirements
                var hasDynamicMLC = treatmentBeams.Any(b => b.MLCPlanType.ToString().ToLower().Contains("dose"));
                if (hasDynamicMLC)
                {
                    AddInfoItem(paragraph, "Dynamic MLC detected - IMRT QA required");
                }
            }
            catch (Exception ex)
            {
                AddErrorItem(paragraph, string.Format("Error checking plan status: {0}", ex.Message));
            }
            
            paragraph.Inlines.Add(new LineBreak());
        }

        private void AddSectionTitle(Paragraph paragraph, string title)
        {
            var titleRun = new Run(title)
            {
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.DarkBlue
            };
            paragraph.Inlines.Add(titleRun);
            paragraph.Inlines.Add(new LineBreak());
        }

        private void AddSubSectionTitle(Paragraph paragraph, string title)
        {
            var titleRun = new Run(title)
            {
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.DarkGreen
            };
            paragraph.Inlines.Add(titleRun);
            paragraph.Inlines.Add(new LineBreak());
        }

        private void AddInfoItem(Paragraph paragraph, string text)
        {
            var infoRun = new Run("✓ " + text)
            {
                FontSize = 12,
                FontWeight = FontWeights.Normal,
                Foreground = Brushes.Black
            };
            paragraph.Inlines.Add(infoRun);
            paragraph.Inlines.Add(new LineBreak());
        }

        private void AddWarningItem(Paragraph paragraph, string text)
        {
            var warningRun = new Run("⚠ " + text)
            {
                FontSize = 12,
                FontWeight = FontWeights.Normal,
                Foreground = Brushes.Orange
            };
            paragraph.Inlines.Add(warningRun);
            paragraph.Inlines.Add(new LineBreak());
        }

        private void AddErrorItem(Paragraph paragraph, string text)
        {
            var errorRun = new Run("❌ " + text)
            {
                FontSize = 12,
                FontWeight = FontWeights.Normal,
                Foreground = Brushes.Red
            };
            paragraph.Inlines.Add(errorRun);
            paragraph.Inlines.Add(new LineBreak());
        }

        private void ExportResults()
        {
            try
            {
                // Use concatenation instead of string.Format to avoid syntax issues in old compiler
                string timestampForFile = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string fileName = "PlanCheck_" + planId + "_" + timestampForFile + ".txt";
                string filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), fileName);
                
                var sb = new StringBuilder();

                foreach (TabItem tabItem in tabControl.Items)
                {
                    RichTextBox rtb = null;
                    if (tabRichTextBoxes.TryGetValue(tabItem.Header.ToString(), out rtb))
                    {
                        // Use concatenation for header
                        string headerText = "*** " + tabItem.Header + " ***";
                        sb.AppendLine(headerText);
                        sb.AppendLine("Last Updated: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                        sb.AppendLine();

                        var textRange = new TextRange(rtb.Document.ContentStart, rtb.Document.ContentEnd);
                        sb.AppendLine(textRange.Text);

                        sb.AppendLine();
                        sb.AppendLine("---");
                        sb.AppendLine();
                    }
                }

                string content = sb.ToString();

                // Replace Unicode symbols with ASCII
                content = content.Replace("✓", "[OK]");
                content = content.Replace("⚠", "[WARNING]");
                content = content.Replace("❌", "[ERROR]");

                File.WriteAllText(filePath, content);
                
                MessageBox.Show(string.Format("Results exported to: {0}", filePath), "Export Successful", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("Error exporting results: {0}", ex.Message), "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
