using DocumentFormat.OpenXml.Wordprocessing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace StaircaseDetails
{
    public partial class MainWindow : Window
    {
        public class FlightData
        {
            public int FlightNumber;
            public TextBox NumberOfSteps;
            public TextBox LowerLandingWidth;
            public TextBox LowerLandingThickness;
            public System.Windows.Controls.CheckBox BeamAtStart;
            public TextBox BeamStartDepth;
            public TextBox BeamStartWidth;
            public System.Windows.Controls.CheckBox BeamAtEnd;
            public TextBox BeamEndDepth;
            public TextBox BeamEndWidth;
            public TextBox UpperLandingWidth;
            public TextBox UpperLandingThickness;
            public System.Windows.Controls.CheckBox WallAtEnd;
            public TextBox WallWidth;
            public System.Windows.Controls.CheckBox Solid;
            public System.Windows.Controls.CheckBox Maxpan;
            public TextBox RibWidth;
            public TextBox BarsPerRib;
        }

        public MainWindow()
        {
            InitializeComponent();

            // ================== HARDCODED DEFAULT VALUES ==================
            RiserTextBox.Text = "150";
            TreadTextBox.Text = "300";
            WaistThicknessTextBox.Text = "150";
            StairWidthTextBox.Text = "1500";
            BottomFloorNameTextBox.Text = "GF";
            TopFloorNameTextBox.Text = "FF";
            FloorHeightTextBox.Text = "1650";
            StaircaseNumberTextBox.Text = "01";
            ClearDistanceTextBox.Text= "1000";

            // Default selections
            FlightsComboBox.SelectedIndex = 0;           // 2 flights
            OrientationComboBox.SelectedIndex = 0;       // Left to Right

            LongitudinalBarSizeComboBox.SelectedIndex = 2;   // 12mm
            LongitudinalBarSpacingComboBox.SelectedIndex = 3; // 150mm
            TransverseBarSizeComboBox.SelectedIndex = 2;     // 12mm
            TransverseBarSpacingComboBox.SelectedIndex = 3;  // 150mm

            // Default checkboxes
            HalfTurnCheckBox.IsChecked = true;
            //GridsCheckBox.IsChecked = true;

            FlightsComboBox_SelectionChanged(FlightsComboBox, null);
            // ============================================================
        }

        public List<FlightData> Flights = new List<FlightData>();

        public bool WasGenerated { get; private set; } = false;

        public bool IsBeamSupport { get; private set; }
        public bool IsSlabThickening { get; private set; }
        public double GroundBeamDepth { get; private set; }
        public double GroundBeamWidth { get; private set; }

        public string StaircaseNumber { get; private set; }
        public double Riser { get; private set; }
        public double Tread { get; private set; }
        public double WaistThickness { get; private set; }
        public double StairWidth { get; private set; }
        public string BottomFloorName { get; private set; }
        public string TopFloorName { get; private set; }
        public double FloorHeight { get; private set; }
        public bool IsGroundFloor { get; private set; }
        public bool IsHalfTurn { get; private set; }
        public bool IsQuarterTurn { get; private set; }
        public int NumberOfFlights { get; private set; }
        public string Orientation { get; private set; }

        public int LongitudinalBarSize { get; private set; }
        public int LongitudinalBarSpacing { get; private set; }
        public int TransverseBarSize { get; private set; }
        public int TransverseBarSpacing { get; private set; }

        public double ClearPlanDistance { get; private set; }
        public bool HasGrids { get; private set; }
        public string GridLabel { get; private set; }
        public double GridDistance { get; private set; }

        private void IntegerOnlyInput(object sender, TextCompositionEventArgs e)
        {
            // Allow only integer input
            e.Handled = !int.TryParse(e.Text, out _);
        }

        private void FlightsComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Clear existing flight sections
            FlightsPanel.Children.Clear();
            Flights.Clear();

            // Get the selected number of flights
            if (FlightsComboBox.SelectedItem is ComboBoxItem selectedItem && int.TryParse(selectedItem.Content.ToString(), out int numberOfFlights))
            {
                for (int i = 1; i <= numberOfFlights; i++)
                {
                    FlightData fd = new FlightData { FlightNumber = i };

                    // Create a section for each flight
                    //StackPanel flightPanel = new StackPanel { Orientation = System.Windows.Controls.Orientation.Vertical, Margin = new Thickness(10) };
                    //flightPanel.Children.Add(new TextBlock { Text = $"Flight {i} Details:" });

                    StackPanel flightPanel = new StackPanel { Orientation = System.Windows.Controls.Orientation.Vertical, Margin = new Thickness(10) };
                    flightPanel.Children.Add(new TextBlock { Text = $"Flight {i} Details:", FontWeight = FontWeights.Bold });

                    // Number of Steps
                    //StackPanel stepsPanel = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal, Margin = new Thickness(0, 2, 0, 2) };
                    //stepsPanel.Children.Add(new TextBlock { Text = "Number of Steps:", Width = 150, VerticalAlignment = VerticalAlignment.Center });
                    //TextBox numStepsBox = new TextBox { Width = 50 };
                    //numStepsBox.PreviewTextInput += IntegerOnlyInput;
                    //stepsPanel.Children.Add(numStepsBox);
                    //fd.NumberOfSteps = numStepsBox;
                    //flightPanel.Children.Add(stepsPanel);

                    StackPanel stepsPanel = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal, Margin = new Thickness(0, 2, 0, 2) };
                    stepsPanel.Children.Add(new TextBlock { Text = "Number of Steps:", Width = 150, VerticalAlignment = VerticalAlignment.Center });
                    TextBox numStepsBox = new TextBox { Width = 50, Text = "11" };   // ← Default
                    numStepsBox.PreviewTextInput += IntegerOnlyInput;
                    stepsPanel.Children.Add(numStepsBox);
                    fd.NumberOfSteps = numStepsBox;
                    flightPanel.Children.Add(stepsPanel);

                    // Lower Slab / Landing Details
                    flightPanel.Children.Add(new TextBlock { Text = "Lower Slab / Landing Details:" });
                    StackPanel lowerLandingPanel = new StackPanel { Orientation = System.Windows.Controls.Orientation.Vertical, Margin = new Thickness(0, 2, 0, 2) };


                    // Slab / Landing Width
                    //StackPanel landingWidthPanel = new StackPanel
                    //{
                    //    Orientation = System.Windows.Controls.Orientation.Horizontal,
                    //    Margin = new Thickness(0, 2, 0, 2)
                    //};
                    //landingWidthPanel.Children.Add(new TextBlock { Text = "Slab / Landing Width:", Width = 150, VerticalAlignment = VerticalAlignment.Center });
                    //TextBox lowerWidthBox = new TextBox { Width = 50 };
                    //landingWidthPanel.Children.Add(lowerWidthBox);
                    //fd.LowerLandingWidth = lowerWidthBox;
                    //lowerLandingPanel.Children.Add(landingWidthPanel);

                    StackPanel landingWidthPanel = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal, Margin = new Thickness(0, 2, 0, 2) };
                    landingWidthPanel.Children.Add(new TextBlock { Text = "Slab / Landing Width:", Width = 150, VerticalAlignment = VerticalAlignment.Center });
                    TextBox lowerWidthBox = new TextBox { Width = 50, Text = "1200" };   // ← Default
                    landingWidthPanel.Children.Add(lowerWidthBox);
                    fd.LowerLandingWidth = lowerWidthBox;
                    lowerLandingPanel.Children.Add(landingWidthPanel);

                    // Slab/Landing Thickness
                    //StackPanel lowerSlabThicknessPanel = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal, Margin = new Thickness(0, 2, 0, 2) };
                    //lowerSlabThicknessPanel.Children.Add(new TextBlock { Text = "Slab/Landing Thickness:", Width = 150, VerticalAlignment = VerticalAlignment.Center });
                    //TextBox lowerThicknessBox = new TextBox { Width = 50 };
                    //lowerSlabThicknessPanel.Children.Add(lowerThicknessBox);
                    //fd.LowerLandingThickness = lowerThicknessBox;
                    //lowerLandingPanel.Children.Add(lowerSlabThicknessPanel);

                    StackPanel lowerSlabThicknessPanel = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal, Margin = new Thickness(0, 2, 0, 2) };
                    lowerSlabThicknessPanel.Children.Add(new TextBlock { Text = "Slab/Landing Thickness:", Width = 150, VerticalAlignment = VerticalAlignment.Center });
                    TextBox lowerThicknessBox = new TextBox { Width = 50, Text = "150" };   // ← Default
                    lowerSlabThicknessPanel.Children.Add(lowerThicknessBox);
                    fd.LowerLandingThickness = lowerThicknessBox;
                    lowerLandingPanel.Children.Add(lowerSlabThicknessPanel);

                    // Link lower landing to previous flight's upper landing (mirrors, disabled)
                    if (i > 1)
                    {
                        FlightData prevFlight = Flights[i - 2];

                        lowerWidthBox.IsEnabled = false;
                        lowerThicknessBox.IsEnabled = false;

                        lowerWidthBox.Text = prevFlight.UpperLandingWidth.Text;
                        lowerThicknessBox.Text = prevFlight.UpperLandingThickness.Text;

                        prevFlight.UpperLandingWidth.TextChanged += (s, args) => lowerWidthBox.Text = prevFlight.UpperLandingWidth.Text;
                        prevFlight.UpperLandingThickness.TextChanged += (s, args) => lowerThicknessBox.Text = prevFlight.UpperLandingThickness.Text;
                    }

                    // Beam at Start
                    StackPanel beamStartPanel = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal, Margin = new Thickness(0, 2, 0, 2) };
                    System.Windows.Controls.CheckBox beamStartCheckBox = new System.Windows.Controls.CheckBox { Content = "Beam at Start" };
                    if (i == 1) beamStartCheckBox.Tag = "Flight1";
                    beamStartPanel.Children.Add(beamStartCheckBox);
                    fd.BeamAtStart = beamStartCheckBox;
                    lowerLandingPanel.Children.Add(beamStartPanel);

                    StackPanel beamStartDetails = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal, Visibility = Visibility.Collapsed, Margin = new Thickness(0, 2, 0, 2), Tag = "BeamStartDetails" };
                    beamStartDetails.Children.Add(new TextBlock { Text = "Depth (mm):", Width = 100, VerticalAlignment = VerticalAlignment.Center });
                    TextBox beamStartDepthBox = new TextBox { Width = 50 };
                    beamStartDetails.Children.Add(beamStartDepthBox);
                    fd.BeamStartDepth = beamStartDepthBox;
                    beamStartDetails.Children.Add(new TextBlock { Text = "Width (mm):", Width = 100, VerticalAlignment = VerticalAlignment.Center });
                    TextBox beamStartWidthBox = new TextBox { Width = 50 };
                    beamStartDetails.Children.Add(beamStartWidthBox);
                    fd.BeamStartWidth = beamStartWidthBox;
                    beamStartCheckBox.Checked += (s, args) => beamStartDetails.Visibility = Visibility.Visible;
                    beamStartCheckBox.Unchecked += (s, args) => beamStartDetails.Visibility = Visibility.Collapsed;
                    lowerLandingPanel.Children.Add(beamStartDetails);

                    // Beam at End
                    StackPanel beamEndPanel = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal, Margin = new Thickness(0, 2, 0, 2) };
                    System.Windows.Controls.CheckBox beamEndCheckBox = new System.Windows.Controls.CheckBox { Content = "Beam at End" };
                    if (i == 1) beamEndCheckBox.Tag = "Flight1";
                    beamEndPanel.Children.Add(beamEndCheckBox);
                    fd.BeamAtEnd = beamEndCheckBox;
                    lowerLandingPanel.Children.Add(beamEndPanel);

                    StackPanel beamEndDetails = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal, Visibility = Visibility.Collapsed, Margin = new Thickness(0, 2, 0, 2), Tag = "BeamEndDetails" };
                    beamEndDetails.Children.Add(new TextBlock { Text = "Depth (mm):", Width = 100, VerticalAlignment = VerticalAlignment.Center });
                    TextBox beamEndDepthBox = new TextBox { Width = 50 };
                    beamEndDetails.Children.Add(beamEndDepthBox);
                    fd.BeamEndDepth = beamEndDepthBox;
                    beamEndDetails.Children.Add(new TextBlock { Text = "Width (mm):", Width = 100, VerticalAlignment = VerticalAlignment.Center });
                    TextBox beamEndWidthBox = new TextBox { Width = 50 };
                    beamEndDetails.Children.Add(beamEndWidthBox);
                    fd.BeamEndWidth = beamEndWidthBox;
                    beamEndCheckBox.Checked += (s, args) => beamEndDetails.Visibility = Visibility.Visible;
                    beamEndCheckBox.Unchecked += (s, args) => beamEndDetails.Visibility = Visibility.Collapsed;
                    lowerLandingPanel.Children.Add(beamEndDetails);

                    // Disable beam checkboxes for flights after Flight 1 (mirrored/inherited, not editable)
                    if (i > 1)
                    {
                        beamStartCheckBox.IsEnabled = false;
                        beamEndCheckBox.IsEnabled = false;
                    }

                    // Toggle visibility based on GroundFloorCheckBox state (Flight 1 only)
                    if (i == 1)
                    {
                        if (GroundFloorCheckBox.IsChecked == true)
                        {
                            beamStartPanel.Visibility = Visibility.Collapsed;
                            beamEndPanel.Visibility = Visibility.Collapsed;
                        }
                        else
                        {
                            beamStartPanel.Visibility = Visibility.Visible;
                            beamEndPanel.Visibility = Visibility.Visible;
                        }
                    }

                    flightPanel.Children.Add(lowerLandingPanel);

                    // Upper Slab / Landing Details
                    flightPanel.Children.Add(new TextBlock { Text = "Upper Slab / Landing Details:" });
                    StackPanel upperLandingPanel = new StackPanel { Orientation = System.Windows.Controls.Orientation.Vertical, Margin = new Thickness(0, 2, 0, 2) };

                    StackPanel upperLandingWidthPanel = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal, Margin = new Thickness(0, 2, 0, 2) };
                    upperLandingWidthPanel.Children.Add(new TextBlock { Text = "Slab / Landing Width:", Width = 150, VerticalAlignment = VerticalAlignment.Center });
                    //TextBox upperWidthBox = new TextBox { Width = 50 };
                    TextBox upperWidthBox = new TextBox { Width = 50, Text = "1200" };
                    upperLandingWidthPanel.Children.Add(upperWidthBox);
                    fd.UpperLandingWidth = upperWidthBox;
                    upperLandingPanel.Children.Add(upperLandingWidthPanel);

                    StackPanel upperSlabThicknessPanel = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal, Margin = new Thickness(0, 2, 0, 2) };
                    upperSlabThicknessPanel.Children.Add(new TextBlock { Text = "Slab/Landing Thickness:", Width = 150, VerticalAlignment = VerticalAlignment.Center });
                    //TextBox upperThicknessBox = new TextBox { Width = 50 };
                    TextBox upperThicknessBox = new TextBox { Width = 50, Text = "150" };
                    upperSlabThicknessPanel.Children.Add(upperThicknessBox);
                    fd.UpperLandingThickness = upperThicknessBox;
                    upperLandingPanel.Children.Add(upperSlabThicknessPanel);

                    // Wall at End
                    System.Windows.Controls.CheckBox wallAtEndCheckBox = new System.Windows.Controls.CheckBox { Content = "Wall at End" };
                    fd.WallAtEnd = wallAtEndCheckBox;
                    upperLandingPanel.Children.Add(wallAtEndCheckBox);
                    StackPanel wallAtEndDetails = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal, Visibility = Visibility.Collapsed, Margin = new Thickness(0, 2, 0, 2) };
                    wallAtEndDetails.Children.Add(new TextBlock { Text = "Width:", Width = 100, VerticalAlignment = VerticalAlignment.Center });
                    TextBox wallWidthBox = new TextBox { Width = 50 };
                    wallAtEndDetails.Children.Add(wallWidthBox);
                    fd.WallWidth = wallWidthBox;
                    wallAtEndCheckBox.Checked += (s, args) => wallAtEndDetails.Visibility = Visibility.Visible;
                    wallAtEndCheckBox.Unchecked += (s, args) => wallAtEndDetails.Visibility = Visibility.Collapsed;
                    upperLandingPanel.Children.Add(wallAtEndDetails);

                    // Slab / Slab / Landing Type
                    upperLandingPanel.Children.Add(new TextBlock { Text = "Slab / Slab / Landing Type:" });
                    System.Windows.Controls.CheckBox solidCheckBox = new System.Windows.Controls.CheckBox { Content = "Solid" };
                    System.Windows.Controls.CheckBox maxpanCheckBox = new System.Windows.Controls.CheckBox { Content = "Maxpan" };
                    fd.Solid = solidCheckBox;
                    fd.Maxpan = maxpanCheckBox;
                    solidCheckBox.Checked += (s, args) => maxpanCheckBox.IsEnabled = false;
                    solidCheckBox.Unchecked += (s, args) => maxpanCheckBox.IsEnabled = true;
                    maxpanCheckBox.Checked += (s, args) => solidCheckBox.IsEnabled = false;
                    maxpanCheckBox.Unchecked += (s, args) => solidCheckBox.IsEnabled = true;
                    upperLandingPanel.Children.Add(solidCheckBox);
                    upperLandingPanel.Children.Add(maxpanCheckBox);

                    // Maxpan Details
                    StackPanel maxpanDetails = new StackPanel { Orientation = System.Windows.Controls.Orientation.Vertical, Visibility = Visibility.Collapsed };
                    maxpanCheckBox.Checked += (s, args) => maxpanDetails.Visibility = Visibility.Visible;
                    maxpanCheckBox.Unchecked += (s, args) => maxpanDetails.Visibility = Visibility.Collapsed;
                    StackPanel sectionVisiblePanel = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal };
                    sectionVisiblePanel.Children.Add(new TextBlock { Text = "Section Visible:", Width = 150 });
                    System.Windows.Controls.CheckBox longitudinalCheckBox = new System.Windows.Controls.CheckBox { Content = "Longitudinal" };
                    System.Windows.Controls.CheckBox transverseCheckBox = new System.Windows.Controls.CheckBox { Content = "Transverse" };
                    longitudinalCheckBox.Checked += (s, args) => transverseCheckBox.IsEnabled = false;
                    longitudinalCheckBox.Unchecked += (s, args) => transverseCheckBox.IsEnabled = true;
                    transverseCheckBox.Checked += (s, args) => longitudinalCheckBox.IsEnabled = false;
                    transverseCheckBox.Unchecked += (s, args) => longitudinalCheckBox.IsEnabled = true;
                    sectionVisiblePanel.Children.Add(longitudinalCheckBox);
                    sectionVisiblePanel.Children.Add(transverseCheckBox);
                    maxpanDetails.Children.Add(sectionVisiblePanel);

                    StackPanel ribDetailsPanel = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal, Visibility = Visibility.Collapsed };
                    transverseCheckBox.Checked += (s, args) => ribDetailsPanel.Visibility = Visibility.Visible;
                    transverseCheckBox.Unchecked += (s, args) => ribDetailsPanel.Visibility = Visibility.Collapsed;
                    ribDetailsPanel.Children.Add(new TextBlock { Text = "Rib Width:", Width = 100 });
                    TextBox ribWidthBox = new TextBox { Width = 50 };
                    ribDetailsPanel.Children.Add(ribWidthBox);
                    fd.RibWidth = ribWidthBox;

                    ribDetailsPanel.Children.Add(new TextBlock { Text = "Bars per Rib:", Width = 100 });
                    TextBox barsPerRibBox = new TextBox { Width = 50 };
                    ribDetailsPanel.Children.Add(barsPerRibBox);
                    fd.BarsPerRib = barsPerRibBox;
                    maxpanDetails.Children.Add(ribDetailsPanel);
                    upperLandingPanel.Children.Add(maxpanDetails);

                    flightPanel.Children.Add(upperLandingPanel);

                    FlightsPanel.Children.Add(flightPanel);
                    Flights.Add(fd);
                }
            }
        }

        private void GroundFloorCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            // Show the support options panel when "Ground Floor?" is checked
            SupportOptionsPanel.Visibility = Visibility.Visible;

            // Iterate through FlightsPanel to find Flight 1
            foreach (var child in FlightsPanel.Children)
            {
                if (child is StackPanel flightPanel)
                {
                    var flightLabel = flightPanel.Children.OfType<TextBlock>().FirstOrDefault(tb => tb.Text == "Flight 1 Details:");
                    if (flightLabel != null)
                    {
                        var beamStartDetails = FindStackPanelByTag(flightPanel, "BeamStartDetails");
                        var beamEndDetails = FindStackPanelByTag(flightPanel, "BeamEndDetails");

                        if (beamStartDetails != null) beamStartDetails.Visibility = Visibility.Collapsed;
                        if (beamEndDetails != null) beamEndDetails.Visibility = Visibility.Collapsed;
                    }
                }
            }
        }

        private StackPanel FindStackPanelByTag(Panel parent, string tag)
        {
            foreach (var child in parent.Children)
            {
                if (child is StackPanel stackPanel && stackPanel.Tag?.ToString() == tag)
                {
                    return stackPanel;
                }
                else if (child is Panel nestedPanel)
                {
                    var result = FindStackPanelByTag(nestedPanel, tag);
                    if (result != null)
                    {
                        return result;
                    }
                }
            }
            return null;
        }

        private void GroundFloorCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            // Hide the support options panel when "Ground Floor?" is unchecked
            SupportOptionsPanel.Visibility = Visibility.Collapsed;

            foreach (var child in FlightsPanel.Children)
            {
                if (child is StackPanel flightPanel)
                {
                    var flightLabel = flightPanel.Children.OfType<TextBlock>().FirstOrDefault(tb => tb.Text == "Flight 1 Details:");
                    if (flightLabel != null)
                    {
                        var beamStartDetails = FindStackPanelByTag(flightPanel, "BeamStartDetails");
                        var beamEndDetails = FindStackPanelByTag(flightPanel, "BeamEndDetails");

                        if (beamStartDetails != null) beamStartDetails.Visibility = Visibility.Visible;
                        if (beamEndDetails != null) beamEndDetails.Visibility = Visibility.Visible;
                    }
                }
            }
        }

        private void SupportType_Checked(object sender, RoutedEventArgs e)
        {
            // Ensure mutual exclusivity between "Beam Support" and "Slab Thickening"
            if (sender == BeamSupportCheckBox && BeamSupportCheckBox.IsChecked == true)
            {
                SlabThickeningCheckBox.IsEnabled = false;

                if (BeamSupportCheckBox.Parent is StackPanel parentPanel)
                {
                    StackPanel beamSupportDetails = new StackPanel
                    {
                        Orientation = System.Windows.Controls.Orientation.Horizontal,
                        Margin = new Thickness(10, 0, 0, 0),
                        Name = "BeamSupportDetails"
                    };

                    beamSupportDetails.Children.Add(new TextBlock { Text = "Depth (mm):", Width = 80, VerticalAlignment = VerticalAlignment.Center });
                    beamSupportDetails.Children.Add(new TextBox { Width = 50, Name = "BeamDepthTextBox" });
                    beamSupportDetails.Children.Add(new TextBlock { Text = "Width (mm):", Width = 80, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(10, 0, 0, 0) });
                    beamSupportDetails.Children.Add(new TextBox { Width = 50, Name = "BeamWidthTextBox" });

                    parentPanel.Children.Insert(parentPanel.Children.IndexOf(BeamSupportCheckBox) + 1, beamSupportDetails);
                }

                SetFlight1BeamCheckboxState(false);
            }
            else if (sender == SlabThickeningCheckBox && SlabThickeningCheckBox.IsChecked == true)
            {
                BeamSupportCheckBox.IsEnabled = false;
            }
        }

        private void SupportType_Unchecked(object sender, RoutedEventArgs e)
        {
            BeamSupportCheckBox.IsEnabled = true;
            SlabThickeningCheckBox.IsEnabled = true;

            if (sender == BeamSupportCheckBox && BeamSupportCheckBox.Parent is StackPanel parentPanel)
            {
                var beamSupportDetails = parentPanel.Children.OfType<StackPanel>().FirstOrDefault(sp => sp.Name == "BeamSupportDetails");
                if (beamSupportDetails != null)
                {
                    parentPanel.Children.Remove(beamSupportDetails);
                }
            }

            SetFlight1BeamCheckboxState(true);
        }

        private void SetFlight1BeamCheckboxState(bool isEnabled)
        {
            foreach (var child in FlightsPanel.Children)
            {
                if (child is StackPanel flightPanel)
                {
                    foreach (var element in flightPanel.Children)
                    {
                        if (element is System.Windows.Controls.CheckBox checkBox && checkBox.Tag?.ToString() == "Flight1")
                        {
                            checkBox.IsEnabled = isEnabled;
                        }
                    }
                }
            }
        }

        private void StaircaseType_Checked(object sender, RoutedEventArgs e)
        {
            if (sender == HalfTurnCheckBox && HalfTurnCheckBox.IsChecked == true)
            {
                QuarterTurnCheckBox.IsEnabled = false;
                ClearDistanceLabel.Visibility = Visibility.Visible;
                ClearDistanceTextBox.Visibility = Visibility.Visible;
            }
            else if (sender == QuarterTurnCheckBox && QuarterTurnCheckBox.IsChecked == true)
            {
                HalfTurnCheckBox.IsEnabled = false;
            }
        }

        private void StaircaseType_Unchecked(object sender, RoutedEventArgs e)
        {
            HalfTurnCheckBox.IsEnabled = true;
            QuarterTurnCheckBox.IsEnabled = true;

            if (sender == HalfTurnCheckBox)
            {
                ClearDistanceLabel.Visibility = Visibility.Collapsed;
                ClearDistanceTextBox.Visibility = Visibility.Collapsed;
            }
        }

        private void GridsCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            GridsPanel.Visibility = Visibility.Visible;
        }

        private void GridsCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            GridsPanel.Visibility = Visibility.Collapsed;
        }

        private IEnumerable<TextBox> FindAllTextBoxes(DependencyObject parent)
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is TextBox tb) yield return tb;
                foreach (var nested in FindAllTextBoxes(child))
                    yield return nested;
            }
        }

        private void GenerateButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(StaircaseNumberTextBox.Text))
                {
                    MessageBox.Show("Please enter a Staircase Number.", "Input Error",
                                     MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!double.TryParse(RiserTextBox.Text, out double riser) ||
                    !double.TryParse(TreadTextBox.Text, out double tread) ||
                    !double.TryParse(WaistThicknessTextBox.Text, out double waist) ||
                    !double.TryParse(StairWidthTextBox.Text, out double width) ||
                    !double.TryParse(FloorHeightTextBox.Text, out double floorHeight))
                {
                    MessageBox.Show("Please enter valid numeric values for Riser, Tread, " +
                                     "Waist Thickness, Stair Width, and Floor Height.",
                                     "Input Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (FlightsComboBox.SelectedItem == null ||
                    LongitudinalBarSizeComboBox.SelectedItem == null ||
                    LongitudinalBarSpacingComboBox.SelectedItem == null ||
                    TransverseBarSizeComboBox.SelectedItem == null ||
                    TransverseBarSpacingComboBox.SelectedItem == null)
                {
                    MessageBox.Show("Please select all reinforcement and flight options.",
                                     "Input Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                StaircaseNumber = StaircaseNumberTextBox.Text.Trim();
                Riser = riser;
                Tread = tread;
                WaistThickness = waist;
                StairWidth = width;
                BottomFloorName = BottomFloorNameTextBox.Text.Trim();
                TopFloorName = TopFloorNameTextBox.Text.Trim();
                FloorHeight = floorHeight;
                IsGroundFloor = GroundFloorCheckBox.IsChecked == true;
                IsHalfTurn = HalfTurnCheckBox.IsChecked == true;
                IsQuarterTurn = QuarterTurnCheckBox.IsChecked == true;

                NumberOfFlights = int.Parse(((ComboBoxItem)FlightsComboBox.SelectedItem).Content.ToString());
                Orientation = ((ComboBoxItem)OrientationComboBox.SelectedItem)?.Content.ToString() ?? "Left to Right";
                LongitudinalBarSize = int.Parse(((ComboBoxItem)LongitudinalBarSizeComboBox.SelectedItem).Content.ToString());
                LongitudinalBarSpacing = int.Parse(((ComboBoxItem)LongitudinalBarSpacingComboBox.SelectedItem).Content.ToString());
                TransverseBarSize = int.Parse(((ComboBoxItem)TransverseBarSizeComboBox.SelectedItem).Content.ToString());
                TransverseBarSpacing = int.Parse(((ComboBoxItem)TransverseBarSpacingComboBox.SelectedItem).Content.ToString());

                if (IsHalfTurn)
                {
                    if (!double.TryParse(ClearDistanceTextBox.Text, out double clearDist))
                    {
                        MessageBox.Show("Please enter a valid Clear Plan Distance between flights.",
                                         "Input Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                    ClearPlanDistance = clearDist;
                }

                HasGrids = GridsCheckBox.IsChecked == true;
                if (HasGrids)
                {
                    GridLabel = GridLabelTextBox.Text.Trim();
                    if (!double.TryParse(GridDistanceTextBox.Text, out double gridDist))
                    {
                        MessageBox.Show("Please enter a valid grid distance.",
                                         "Input Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                    GridDistance = gridDist;
                }

                if (IsGroundFloor)
                {
                    IsBeamSupport = BeamSupportCheckBox.IsChecked == true;
                    IsSlabThickening = SlabThickeningCheckBox.IsChecked == true;

                    if (IsBeamSupport)
                    {
                        var depthBox = SupportOptionsPanel.FindName("BeamDepthTextBox") as TextBox;
                        var widthBox = SupportOptionsPanel.FindName("BeamWidthTextBox") as TextBox;
                        // Fallback: search visual tree since these are added dynamically without RegisterName
                        if (depthBox == null || widthBox == null)
                        {
                            foreach (var child in FindAllTextBoxes(SupportOptionsPanel))
                            {
                                if (child.Name == "BeamDepthTextBox") depthBox = child;
                                if (child.Name == "BeamWidthTextBox") widthBox = child;
                            }
                        }
                        double.TryParse(depthBox?.Text, out double gd);
                        double.TryParse(widthBox?.Text, out double gw);
                        GroundBeamDepth = gd;
                        GroundBeamWidth = gw;
                    }
                }


                WasGenerated = true;
                this.Close();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Error while collecting inputs:\n{ex.Message}\n\n{ex.StackTrace}",
                                 "Generate Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}