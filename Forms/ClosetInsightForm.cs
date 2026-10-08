using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using InventoryPOS.Models;

namespace InventoryPOS.Forms
{
    public class ClosetInsightForm : Form
    {
        private enum InsightMetric
        {
            Sales,
            Earnings,
            Profit,
            ListingsSold
        }

        private enum InsightPeriod
        {
            CurrentYear,
            LastYear,
            Last12Months,
            AllTime
        }

        private sealed record ChartPoint(string Label, decimal Value);

        private sealed class BufferedChartPanel : Panel
        {
            public BufferedChartPanel()
            {
                DoubleBuffered = true;
                ResizeRedraw = true;
            }
        }

        private readonly List<InventoryItem> _items;
        private readonly Label _salesValue;
        private readonly Label _earningsValue;
        private readonly Label _profitValue;
        private readonly Label _listingsSoldValue;
        private readonly ComboBox _periodSelector;
        private readonly Panel _chartPanel;
        private readonly Button[] _metricButtons;
        private readonly ToolTip _chartToolTip = new();
        private InsightMetric _selectedMetric;
        private int _hoveredPointIndex = -1;

        public ClosetInsightForm(IEnumerable<InventoryItem> items)
        {
            _items = items.ToList();
            Text = "Closet Insight";
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(1120, 700);
            MinimumSize = new Size(920, 520);
            Font = new Font("Segoe UI", 10F);
            BackColor = Color.White;

            var border = new Panel
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(12),
                Padding = new Padding(20),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            Controls.Add(border);

            var header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 92,
                BackColor = Color.White
            };
            border.Controls.Add(header);

            var summaryPanel = new FlowLayoutPanel
            {
                Location = new Point(0, 8),
                Size = new Size(610, 76),
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                BackColor = Color.White
            };
            header.Controls.Add(summaryPanel);

            (_salesValue, var salesCard) = CreateSummaryCard("Sales");
            (_earningsValue, var earningsCard) = CreateSummaryCard("Earnings");
            (_profitValue, var profitCard) = CreateSummaryCard("Profit");
            (_listingsSoldValue, var listingsSoldCard) = CreateSummaryCard("Listings Sold");
            summaryPanel.Controls.AddRange(new Control[] { salesCard, earningsCard, profitCard, listingsSoldCard });

            _periodSelector = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 175,
                Height = 34,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(header.ClientSize.Width - 195, 15),
                Font = new Font("Segoe UI", 10F),
                BackColor = Color.White
            };
            _periodSelector.Items.AddRange(new object[] { "Current Year", "Last Year", "Last 12 Months", "All Time" });
            _periodSelector.SelectedIndex = 0;
            _periodSelector.SelectedIndexChanged += (_, _) => RefreshInsights();
            header.Controls.Add(_periodSelector);
            header.Resize += (_, _) => _periodSelector.Left = header.ClientSize.Width - _periodSelector.Width - 8;

            var tabs = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 48,
                ColumnCount = 4,
                RowCount = 1,
                BackColor = Color.White,
                Margin = Padding.Empty
            };
            for (var i = 0; i < 4; i++)
            {
                tabs.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            }

            var metricNames = new[] { "Sales", "Earnings", "Profit", "Listings Sold" };
            _metricButtons = new Button[metricNames.Length];
            for (var i = 0; i < metricNames.Length; i++)
            {
                var metric = (InsightMetric)i;
                var button = new Button
                {
                    Text = metricNames[i],
                    Dock = DockStyle.Fill,
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Color.White,
                    ForeColor = Color.FromArgb(100, 100, 100),
                    Font = new Font("Segoe UI", 11F),
                    Tag = metric,
                    Cursor = Cursors.Hand
                };
                button.FlatAppearance.BorderSize = 0;
                button.Click += MetricButton_Click;
                _metricButtons[i] = button;
                tabs.Controls.Add(button, i, 0);
            }
            border.Controls.Add(tabs);
            tabs.BringToFront();

            _chartPanel = new BufferedChartPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White
            };
            _chartPanel.Paint += ChartPanel_Paint;
            _chartPanel.Resize += (_, _) => _chartPanel.Invalidate();
            _chartPanel.MouseMove += ChartPanel_MouseMove;
            _chartPanel.MouseLeave += ChartPanel_MouseLeave;
            _chartToolTip.InitialDelay = 0;
            _chartToolTip.ReshowDelay = 0;
            _chartToolTip.AutoPopDelay = 5000;
            FormClosed += (_, _) => _chartToolTip.Dispose();
            border.Controls.Add(_chartPanel);
            _chartPanel.BringToFront();

            _selectedMetric = InsightMetric.Sales;
            UpdateMetricButtons();
            RefreshInsights();
        }

        private static (Label Value, Panel Card) CreateSummaryCard(string caption)
        {
            var card = new Panel
            {
                Width = 140,
                Height = 65,
                Margin = new Padding(0, 0, 8, 0),
                BackColor = Color.White
            };
            var value = new Label
            {
                Text = "$0.00",
                Location = new Point(0, 0),
                AutoSize = true,
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                ForeColor = Color.FromArgb(20, 20, 20)
            };
            var label = new Label
            {
                Text = caption,
                Location = new Point(0, 33),
                AutoSize = true,
                Font = new Font("Segoe UI", 10F),
                ForeColor = Color.FromArgb(105, 105, 105)
            };
            card.Controls.Add(value);
            card.Controls.Add(label);
            return (value, card);
        }

        private void MetricButton_Click(object? sender, EventArgs e)
        {
            if (sender is Button { Tag: InsightMetric metric })
            {
                _selectedMetric = metric;
                _hoveredPointIndex = -1;
                _chartToolTip.Hide(_chartPanel);
                UpdateMetricButtons();
                _chartPanel.Invalidate();
            }
        }

        private void UpdateMetricButtons()
        {
            foreach (var button in _metricButtons)
            {
                var isSelected = (InsightMetric)button.Tag! == _selectedMetric;
                button.ForeColor = isSelected ? Color.FromArgb(20, 20, 20) : Color.FromArgb(100, 100, 100);
                button.Font = new Font("Segoe UI", 11F, isSelected ? FontStyle.Bold : FontStyle.Regular);
                button.FlatAppearance.BorderColor = isSelected ? Color.FromArgb(35, 35, 35) : Color.White;
                button.FlatAppearance.BorderSize = isSelected ? 2 : 0;
            }
        }

        private void RefreshInsights()
        {
            var period = (InsightPeriod)Math.Max(0, _periodSelector.SelectedIndex);
            var (start, end) = GetPeriodRange(period);
            var sales = 0m;
            var earnings = 0m;
            var profit = 0m;
            var soldCount = 0;

            foreach (var item in GetSoldItems(start, end))
            {
                sales += item.SoldPrice;
                earnings += item.Earnings;
                profit += item.Profit;
                soldCount++;
            }

            _salesValue.Text = sales.ToString("C2", CultureInfo.CurrentCulture);
            _earningsValue.Text = earnings.ToString("C2", CultureInfo.CurrentCulture);
            _profitValue.Text = profit.ToString("C2", CultureInfo.CurrentCulture);
            _listingsSoldValue.Text = soldCount.ToString("N0", CultureInfo.CurrentCulture);
            _hoveredPointIndex = -1;
            _chartToolTip.Hide(_chartPanel);
            _chartPanel.Invalidate();
        }

        private IEnumerable<InventoryItem> GetSoldItems(DateTime start, DateTime end)
        {
            return _items.Where(item =>
                string.Equals(item.Status, "Sold", StringComparison.OrdinalIgnoreCase) &&
                item.SoldDate.HasValue &&
                item.SoldDate.Value.Date >= start &&
                item.SoldDate.Value.Date <= end);
        }

        private (DateTime Start, DateTime End) GetPeriodRange(InsightPeriod period)
        {
            var today = DateTime.Today;
            return period switch
            {
                InsightPeriod.CurrentYear => (new DateTime(today.Year, 1, 1), today),
                InsightPeriod.LastYear => (new DateTime(today.Year - 1, 1, 1), new DateTime(today.Year - 1, 12, 31)),
                InsightPeriod.Last12Months => (new DateTime(today.Year, today.Month, 1).AddMonths(-11), today),
                _ => (GetEarliestSoldDate() ?? new DateTime(today.Year, 1, 1), today)
            };
        }

        private DateTime? GetEarliestSoldDate()
        {
            var soldDates = _items
                .Where(item => string.Equals(item.Status, "Sold", StringComparison.OrdinalIgnoreCase) && item.SoldDate.HasValue)
                .Select(item => item.SoldDate!.Value.Date);
            return soldDates.Any() ? soldDates.Min() : null;
        }

        private List<ChartPoint> GetChartPoints()
        {
            var period = (InsightPeriod)Math.Max(0, _periodSelector.SelectedIndex);
            var (start, end) = GetPeriodRange(period);
            var monthly = period != InsightPeriod.AllTime;
            var bucketStart = monthly ? new DateTime(start.Year, start.Month, 1) : new DateTime(start.Year, 1, 1);
            var bucketEnd = monthly ? new DateTime(end.Year, end.Month, 1) : new DateTime(end.Year, 1, 1);
            var points = new List<ChartPoint>();

            for (var bucket = bucketStart; bucket <= bucketEnd; bucket = monthly ? bucket.AddMonths(1) : bucket.AddYears(1))
            {
                var bucketItems = GetSoldItems(bucket, monthly ? bucket.AddMonths(1).AddDays(-1) : bucket.AddYears(1).AddDays(-1));
                var value = _selectedMetric switch
                {
                    InsightMetric.Sales => bucketItems.Sum(item => item.SoldPrice),
                    InsightMetric.Earnings => bucketItems.Sum(item => item.Earnings),
                    InsightMetric.Profit => bucketItems.Sum(item => item.Profit),
                    _ => bucketItems.Count()
                };
                points.Add(new ChartPoint(monthly ? bucket.ToString("MMM", CultureInfo.CurrentCulture) : bucket.ToString("yyyy", CultureInfo.CurrentCulture), value));
            }

            return points;
        }

        private void ChartPanel_Paint(object? sender, PaintEventArgs e)
        {
            var graphics = e.Graphics;
            graphics.SmoothingMode = SmoothingMode.AntiAlias;

            var points = GetChartPoints();
            if (_chartPanel.ClientSize.Width < 200 || _chartPanel.ClientSize.Height < 160)
                return;

            var plot = new Rectangle(66, 48, _chartPanel.ClientSize.Width - 92, _chartPanel.ClientSize.Height - 108);
            var (axisMinimum, axisMaximum, step) = GetAxisBounds(points);

            using var gridPen = new Pen(Color.FromArgb(225, 225, 225), 1) { DashStyle = DashStyle.Dash };
            using var zeroPen = new Pen(Color.FromArgb(185, 185, 185), 1);
            using var textBrush = new SolidBrush(Color.FromArgb(105, 105, 105));
            using var axisFont = new Font("Segoe UI", 9F);

            var tickCount = (int)Math.Round((axisMaximum - axisMinimum) / step);
            for (var i = 0; i <= tickCount; i++)
            {
                var labelValue = axisMinimum + step * i;
                var y = plot.Bottom - (int)Math.Round((labelValue - axisMinimum) / (axisMaximum - axisMinimum) * plot.Height);
                graphics.DrawLine(Math.Abs(labelValue) < step * 0.0001 ? zeroPen : gridPen, plot.Left, y, plot.Right, y);
                var label = _selectedMetric == InsightMetric.ListingsSold
                    ? labelValue.ToString("0.##", CultureInfo.CurrentCulture)
                    : labelValue.ToString("C0", CultureInfo.CurrentCulture);
                var size = graphics.MeasureString(label, axisFont);
                graphics.DrawString(label, axisFont, textBrush, plot.Left - size.Width - 10, y - size.Height / 2);
            }

            var (periodStart, periodEnd) = GetPeriodRange((InsightPeriod)Math.Max(0, _periodSelector.SelectedIndex));
            if (!GetSoldItems(periodStart, periodEnd).Any())
            {
                using var emptyFont = new Font("Segoe UI", 11F);
                const string message = "No sold listings for this time period";
                var size = graphics.MeasureString(message, emptyFont);
                graphics.DrawString(message, emptyFont, textBrush,
                    plot.Left + (plot.Width - size.Width) / 2,
                    plot.Top + (plot.Height - size.Height) / 2);
            }
            else
            {
                var bars = GetBarRectangles(plot, points, axisMinimum, axisMaximum);
                var zeroY = plot.Bottom - (int)Math.Round((0 - axisMinimum) / (axisMaximum - axisMinimum) * plot.Height);
                using var positiveBrush = new SolidBrush(Color.FromArgb(62, 126, 245));
                using var negativeBrush = new SolidBrush(Color.FromArgb(215, 91, 76));
                using var hoverBrush = new SolidBrush(Color.FromArgb(106, 157, 250));
                using var valueFont = new Font("Segoe UI", 8.5F);

                for (var i = 0; i < bars.Count; i++)
                {
                    var bar = bars[i];
                    var isNegative = points[i].Value < 0;
                    var brush = i == _hoveredPointIndex
                        ? hoverBrush
                        : isNegative ? negativeBrush : positiveBrush;
                    graphics.FillRectangle(brush, bar);

                    var label = FormatBarValue(points[i].Value, points.Count > 14);
                    var labelSize = graphics.MeasureString(label, valueFont);
                    var labelX = bar.Left + (bar.Width - labelSize.Width) / 2;
                    var labelY = isNegative
                        ? Math.Min(bar.Bottom + 2, plot.Bottom - labelSize.Height)
                        : Math.Max(plot.Top - 2, bar.Top - labelSize.Height - 3);
                    var labelColor = isNegative && bar.Bottom + 2 > plot.Bottom - labelSize.Height
                        ? Brushes.White
                        : textBrush;
                    graphics.DrawString(label, valueFont, labelColor, labelX, labelY);
                }

                graphics.DrawLine(zeroPen, plot.Left, zeroY, plot.Right, zeroY);
            }

            var labelInterval = Math.Max(1, (int)Math.Ceiling(points.Count / Math.Max(1d, plot.Width / 65d)));
            for (var i = 0; i < points.Count; i += labelInterval)
            {
                var slotWidth = plot.Width / (float)points.Count;
                var x = plot.Left + slotWidth * (i + 0.5F);
                var labelSize = graphics.MeasureString(points[i].Label, axisFont);
                graphics.DrawString(points[i].Label, axisFont, textBrush, x - labelSize.Width / 2, plot.Bottom + 9);
            }

            if (points.Count > 0 && periodIsAllTime())
            {
                var yearLabel = $"{points.First().Label}–{points.Last().Label}";
                var labelSize = graphics.MeasureString(yearLabel, axisFont);
                graphics.DrawString(yearLabel, axisFont, textBrush,
                    plot.Left + (plot.Width - labelSize.Width) / 2, plot.Bottom + 34);
            }
        }

        private void ChartPanel_MouseMove(object? sender, MouseEventArgs e)
        {
            if (_chartPanel.ClientSize.Width < 200 || _chartPanel.ClientSize.Height < 160)
                return;

            var points = GetChartPoints();
            var plot = new Rectangle(66, 48, _chartPanel.ClientSize.Width - 92, _chartPanel.ClientSize.Height - 108);
            var (axisMinimum, axisMaximum, _) = GetAxisBounds(points);
            var bars = GetBarRectangles(plot, points, axisMinimum, axisMaximum);
            var hoveredIndex = -1;

            for (var i = 0; i < bars.Count; i++)
            {
                if (bars[i].Contains(e.Location))
                {
                    hoveredIndex = i;
                    break;
                }
            }

            if (hoveredIndex < 0)
            {
                if (_hoveredPointIndex >= 0)
                {
                    _hoveredPointIndex = -1;
                    _chartToolTip.Hide(_chartPanel);
                    _chartPanel.Invalidate();
                }
                return;
            }

            if (_hoveredPointIndex != hoveredIndex)
            {
                _hoveredPointIndex = hoveredIndex;
                _chartPanel.Invalidate();
                var point = points[hoveredIndex];
                var value = _selectedMetric == InsightMetric.ListingsSold
                    ? point.Value.ToString("N0", CultureInfo.CurrentCulture)
                    : point.Value.ToString("C2", CultureInfo.CurrentCulture);
                _chartToolTip.Show($"{point.Label}: {value}", _chartPanel, e.X + 12, e.Y + 18, 5000);
            }
        }

        private void ChartPanel_MouseLeave(object? sender, EventArgs e)
        {
            _chartToolTip.Hide(_chartPanel);
            if (_hoveredPointIndex >= 0)
            {
                _hoveredPointIndex = -1;
                _chartPanel.Invalidate();
            }
        }

        private static List<Rectangle> GetBarRectangles(Rectangle plot, List<ChartPoint> points, double axisMinimum, double axisMaximum)
        {
            var bars = new List<Rectangle>(points.Count);
            if (points.Count == 0)
                return bars;

            var slotWidth = plot.Width / (float)points.Count;
            var barWidth = Math.Max(2, (int)Math.Floor(slotWidth * 0.62F));
            var zeroY = plot.Bottom - (int)Math.Round((0 - axisMinimum) / (axisMaximum - axisMinimum) * plot.Height);
            for (var i = 0; i < points.Count; i++)
            {
                var valueY = plot.Bottom - (int)Math.Round(((double)points[i].Value - axisMinimum) / (axisMaximum - axisMinimum) * plot.Height);
                var top = Math.Min(valueY, zeroY);
                var height = Math.Max(1, Math.Abs(zeroY - valueY));
                var left = plot.Left + (int)Math.Round(slotWidth * (i + 0.5F) - barWidth / 2F);
                bars.Add(new Rectangle(left, top, barWidth, height));
            }
            return bars;
        }

        private string FormatBarValue(decimal value, bool compact)
        {
            var absoluteValue = Math.Abs(value);
            var numberFormat = compact ? "0" : "0.#";
            if (_selectedMetric == InsightMetric.ListingsSold)
            {
                if (absoluteValue < 10000 || compact && absoluteValue < 1000000)
                    return value.ToString("N0", CultureInfo.CurrentCulture);
                return $"{(value / 1000m).ToString(numberFormat, CultureInfo.CurrentCulture)}K";
            }

            if (absoluteValue < 1000)
                return value.ToString(compact ? "C0" : "C2", CultureInfo.CurrentCulture);

            var sign = value < 0 ? "-" : string.Empty;
            var symbol = CultureInfo.CurrentCulture.NumberFormat.CurrencySymbol;
            if (absoluteValue >= 1000000)
                return $"{sign}{symbol}{(absoluteValue / 1000000m).ToString(numberFormat, CultureInfo.CurrentCulture)}M";
            return $"{sign}{symbol}{(absoluteValue / 1000m).ToString(numberFormat, CultureInfo.CurrentCulture)}K";
        }


        private bool periodIsAllTime() => _periodSelector.SelectedIndex == (int)InsightPeriod.AllTime;

        private static double GetAxisStep(double maximum)
        {
            if (maximum <= 0)
                return 1;

            var rawStep = maximum / 5;
            var magnitude = Math.Pow(10, Math.Floor(Math.Log10(rawStep)));
            var normalized = rawStep / magnitude;
            var niceStep = normalized <= 1 ? 1 : normalized <= 2 ? 2 : normalized <= 5 ? 5 : 10;
            return niceStep * magnitude;
        }

        private static (double Minimum, double Maximum, double Step) GetAxisBounds(List<ChartPoint> points)
        {
            var values = points.Select(point => (double)point.Value).ToArray();
            var dataMinimum = values.Length == 0 ? 0 : Math.Min(0, values.Min());
            var dataMaximum = values.Length == 0 ? 0 : Math.Max(0, values.Max());
            var step = GetAxisStep(Math.Max(Math.Abs(dataMinimum), Math.Abs(dataMaximum)));
            var minimum = Math.Floor(dataMinimum / step) * step;
            var maximum = Math.Ceiling(dataMaximum / step) * step;
            if (minimum == maximum)
                maximum = minimum + step;
            return (minimum, maximum, step);
        }
    }
}
