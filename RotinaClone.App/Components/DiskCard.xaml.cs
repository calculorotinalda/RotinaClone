using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using RotinaClone.Domain.Models;

namespace RotinaClone.App.Components
{
    public partial class DiskCard : UserControl
    {
        public static readonly DependencyProperty DiskProperty =
            DependencyProperty.Register("Disk", typeof(DiskInfo), typeof(DiskCard),
                new PropertyMetadata(null, OnDiskChanged));

        public DiskInfo Disk
        {
            get => (DiskInfo)GetValue(DiskProperty);
            set => SetValue(DiskProperty, value);
        }

        public DiskCard()
        {
            InitializeComponent();
        }

        private static void OnDiskChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DiskCard card && e.NewValue is DiskInfo disk)
            {
                card.UpdateDiskUI(disk);
            }
        }

        private void UpdateDiskUI(DiskInfo disk)
        {
            DataContext = disk;

            // Configure health indicators
            if (disk.HealthStatus.Equals("Critical", System.StringComparison.OrdinalIgnoreCase))
            {
                HealthIndicatorDot.Fill = FindResource("ErrorColor") as Brush;
                HealthText.Foreground = FindResource("ErrorColor") as Brush;
            }
            else if (disk.HealthStatus.Equals("Warning", System.StringComparison.OrdinalIgnoreCase))
            {
                HealthIndicatorDot.Fill = FindResource("PrimaryAccent") as Brush;
                HealthText.Foreground = FindResource("PrimaryAccent") as Brush;
            }
            else
            {
                HealthIndicatorDot.Fill = FindResource("SuccessColor") as Brush;
                HealthText.Foreground = FindResource("TextPrimary") as Brush;
            }

            // Build partitions grid
            PartitionGrid.ColumnDefinitions.Clear();
            PartitionGrid.Children.Clear();

            if (disk.Partitions == null || disk.Partitions.Count == 0)
            {
                // Unallocated space block
                PartitionGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                var border = new Border
                {
                    Background = new SolidColorBrush(Color.FromArgb(20, 255, 255, 255)),
                    BorderBrush = FindResource("CardBorder") as Brush,
                    BorderThickness = new Thickness(0),
                    Margin = new Thickness(1),
                    CornerRadius = new CornerRadius(4)
                };
                var text = new TextBlock
                {
                    Text = "Espaço Não Alocado",
                    Foreground = FindResource("TextSecondary") as Brush,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    FontSize = 12,
                    FontStyle = FontStyles.Italic
                };
                border.Child = text;
                PartitionGrid.Children.Add(border);
                Grid.SetColumn(border, 0);
                return;
            }

            for (int i = 0; i < disk.Partitions.Count; i++)
            {
                var part = disk.Partitions[i];
                
                // Add proportional column
                PartitionGrid.ColumnDefinitions.Add(new ColumnDefinition 
                { 
                    Width = new GridLength(System.Math.Max(part.TotalSize, 100 * 1024 * 1024), GridUnitType.Star) 
                });

                // Styling based on type
                Brush partBg;
                Brush textBrush = FindResource("TextPrimary") as Brush;
                CornerRadius corner = new CornerRadius(0);

                if (i == 0) corner.TopLeft = corner.BottomLeft = 4;
                if (i == disk.Partitions.Count - 1) corner.TopRight = corner.BottomRight = 4;

                if (part.IsSystem)
                {
                    partBg = new SolidColorBrush(Color.FromArgb(40, 0, 191, 255)); // EFI/System (Info blue-ish)
                }
                else if (part.FileSystem.Equals("NTFS", System.StringComparison.OrdinalIgnoreCase))
                {
                    partBg = new SolidColorBrush(Color.FromArgb(25, 255, 140, 0)); // Data partition (Primary Accent soft orange)
                }
                else
                {
                    partBg = new SolidColorBrush(Color.FromArgb(20, 255, 255, 255)); // Recovery/RAW/MSR
                }

                var border = new Border
                {
                    Background = partBg,
                    BorderBrush = FindResource("CardBorder") as Brush,
                    BorderThickness = new Thickness(i == 0 ? 0 : 1, 0, 0, 0),
                    CornerRadius = corner,
                    ToolTip = $"Tipo: {part.PartitionType}\nSist. Ficheiros: {part.FileSystem}\nOffset: {part.StartOffset} bytes"
                };

                // Add grid inner contents (Name, FileSystem, Space info)
                var container = new Grid { Margin = new Thickness(6, 4, 6, 4) };
                
                // For data partitions, add a mini progress indicator bar inside the block representing used space!
                if (part.TotalSize > 0 && part.UsedSize > 0 && !part.IsSystem && part.FileSystem.Equals("NTFS"))
                {
                    var progressGrid = new Grid { VerticalAlignment = VerticalAlignment.Bottom, Height = 4, Margin = new Thickness(0, 0, 0, 2) };
                    progressGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(part.UsedPercent, GridUnitType.Star) });
                    progressGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100 - part.UsedPercent, GridUnitType.Star) });

                    var fill = new Border { Background = FindResource("PrimaryAccent") as Brush, CornerRadius = new CornerRadius(2) };
                    progressGrid.Children.Add(fill);
                    Grid.SetColumn(fill, 0);
                    container.Children.Add(progressGrid);
                }

                var labelStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
                
                string labelText = string.IsNullOrEmpty(part.DriveLetter) ? part.Name : $"({part.DriveLetter}) {part.Name}";
                if (string.IsNullOrEmpty(labelText)) labelText = $"Partição {part.Index}";

                labelStack.Children.Add(new TextBlock
                {
                    Text = labelText,
                    Foreground = textBrush,
                    FontSize = 11,
                    FontWeight = FontWeights.Bold,
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    HorizontalAlignment = HorizontalAlignment.Center
                });

                string sizeText = $"{part.TotalSizeGB:F1} GB";
                if (part.FileSystem.Equals("NTFS") && !part.IsSystem)
                {
                    sizeText = $"{part.UsedSizeGB:F1} GB / {part.TotalSizeGB:F1} GB";
                }

                labelStack.Children.Add(new TextBlock
                {
                    Text = $"{sizeText} ({part.FileSystem})",
                    Foreground = FindResource("TextSecondary") as Brush,
                    FontSize = 9,
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    HorizontalAlignment = HorizontalAlignment.Center
                });

                container.Children.Add(labelStack);
                border.Child = container;

                PartitionGrid.Children.Add(border);
                Grid.SetColumn(border, i);
            }
        }
    }
}
