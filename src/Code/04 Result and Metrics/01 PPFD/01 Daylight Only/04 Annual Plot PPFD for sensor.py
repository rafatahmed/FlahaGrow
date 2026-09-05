ghenv.Component.Message = 'FlahaGrow 1.0.0'


import System.Windows.Forms as WinForms
import System.Drawing as Drawing
from System.Drawing import Point, Color
from System.Drawing.Drawing2D import SmoothingMode, PixelOffsetMode

# Inputs
if not _run:
    ghenv.Component.Message = """FlahaGrow 0.1 Beta
Annual Plot"""
else:
    try:
        flat_data = [float(x) for x in _result_hourly if x is not None]
        if len(flat_data) != 8760:
            raise ValueError("Expected 8760 values.")
    except:
        WinForms.MessageBox.Show("PAR_8760 must contain exactly 8760 float values.", "Error")
        raise

    thresholds = [
        float(_range1) if _range1 is not None else 0,
        float(_range2) if _range2 is not None else 10,
        float(_range3) if _range3 is not None else 20,
        float(_range4) if _range4 is not None else 50
    ]

    # Ascending thresholds
    if not (thresholds[0] <= thresholds[1] <= thresholds[2] <= thresholds[3]):
        WinForms.MessageBox.Show("Ranges must be ascending (r1 ≤ r2 ≤ r3 ≤ r4).", "Invalid Ranges")
        raise ValueError("Non-ascending thresholds")

    # Color Setup
    def to_color(val):
        if val is None:
            return Color.LightGray
        if isinstance(val, Color):
            return val
        if isinstance(val, (tuple, list)) and len(val) == 3:
            return Color.FromArgb(int(val[0]), int(val[1]), int(val[2]))
        if isinstance(val, str):
            s = val.strip()
            if ',' in s or ' ' in s:
                s = s.replace('(', '').replace(')', '')
                parts = [p for p in s.replace(',', ' ').split(' ') if p]
                if len(parts) == 3:
                    r, g, b = [int(float(p)) for p in parts]
                    return Color.FromArgb(r, g, b)
            s = s.lstrip('#')
            if len(s) == 6:
                r = int(s[0:2], 16); g = int(s[2:4], 16); b = int(s[4:6], 16)
                return Color.FromArgb(r, g, b)
        raise ValueError("Unsupported color format: use '#rrggbb', (r,g,b), 'r,g,b', or a GH Color.")

    def hex_to_color(hex_str):
        s = hex_str.lstrip('#')
        return Drawing.Color.FromArgb(int(s[0:2], 16), int(s[2:4], 16), int(s[4:6], 16))

    # Grid / inset mode
    mode = int(round(float(_grid_mode))) if _grid_mode is not None else 0
    mode = max(0, min(3, mode))  # clamp to 0..3

    INSET = 0.5
    inset_px     = INSET if mode == 1 else 0.0
    grid_enabled = (mode in (2, 3))
    grid_color   = to_color("#636363") if mode == 3 else \
                   (to_color(_grid_color) if _grid_color else Drawing.Color.LightGray)

    # Bucket 0 color
    first_color = hex_to_color("#808080") if mode == 3 else hex_to_color("#ffffff")

    colors = [
        first_color,                 # imperceptible
        hex_to_color("#f9ebab"),     # perceptible
        hex_to_color("#f0be39"),     # disturbing
        hex_to_color("#e46828"),     # intolerable
        hex_to_color("#d70e17")      # excessive
    ]

    # Classification
    def classify(value):
        if value <= thresholds[0]: return 0
        elif value <= thresholds[1]: return 1
        elif value <= thresholds[2]: return 2
        elif value <= thresholds[3]: return 3
        else: return 4

    # Custom range names
    def _safe_name(val, default):
        return val if (val is not None and str(val).strip() != "") else default

    n1 = _safe_name(globals().get("_name_range1"), "Imperceptible")
    n2 = _safe_name(globals().get("_name_range2"), "Perceptible")
    n3 = _safe_name(globals().get("_name_range3"), "Disturbing")
    n4 = _safe_name(globals().get("_name_range4"), "Intolerable")
    n5 = _safe_name(globals().get("_name_range5"), "Excessive")
    range_names = [n1, n2, n3, n4, n5]


    # Heatmap Form Class
    class PARHeatmapForm(WinForms.Form):
        def __init__(self, data):
            super(PARHeatmapForm, self).__init__()
            self.Text = "Hourly Heat Map"
            self.StartPosition = WinForms.FormStartPosition.CenterScreen
            self.FormBorderStyle = WinForms.FormBorderStyle.Sizable
            self.DoubleBuffered = True
            self.BackColor = Drawing.Color.Gray if mode == 3 else Drawing.Color.White


            self.mode = mode
            self.inset_px = inset_px
            self.grid_enabled = grid_enabled
            self.grid_color = grid_color
            self.colors = colors
            self.range_names = range_names

            self.cell_w = 4
            self.cell_h = 20
            self.cols = 365
            self.rows = 24
            self.title_height = 25
            self.margin_top = 40
            self.margin_right = 130
            self.margin_left = 30
            self.margin_bottom = 200
            self.data = data

            self.ClientSize = Drawing.Size(
                self.margin_left + self.cols * self.cell_w + self.margin_right,
                self.rows * self.cell_h + self.margin_bottom + self.margin_top
            )

            self.Paint += self.on_paint
            self.MouseMove += self.on_mouse_move

            self.save_btn = WinForms.Button()
            self.save_btn.Text = "Export PNG"
            self.save_btn.Height = 30
            self.save_btn.Dock = WinForms.DockStyle.Bottom
            self.save_btn.Margin = WinForms.Padding(0, 10, 0, 10)
            self.save_btn.Click += self.on_export_click
            self.Controls.Add(self.save_btn)

            self.hover_label = WinForms.Label()
            self.hover_label.AutoSize = True
            self.hover_label.Location = Point(120, self.rows * self.cell_h + self.margin_top + 70)
            self.Controls.Add(self.hover_label)

        def on_paint(self, sender, e):
            g = e.Graphics
            g.SmoothingMode = SmoothingMode.Default
            g.PixelOffsetMode = PixelOffsetMode.HighSpeed

            # Shift title
            g.TranslateTransform(self.margin_left, self.margin_top + self.title_height)

            # GDI objects
            bucket_brushes = [Drawing.SolidBrush(c) for c in self.colors]
            grid_pen = Drawing.Pen(self.grid_color) if self.grid_enabled else None

            # Heatmap cells
            inset = float(self.inset_px)
            for i in range(self.rows):
                for j in range(self.cols):
                    idx = j * 24 + i
                    value = self.data[idx]
                    cat = classify(value)
                    brush = bucket_brushes[cat]

                    x = j * self.cell_w
                    y = i * self.cell_h

                    # symmetric inset
                    ix = x + inset
                    iy = y + inset
                    w  = self.cell_w  - 2 * inset
                    h  = self.cell_h  - 2 * inset

                    g.FillRectangle(brush, ix, iy, w, h)

                    if self.grid_enabled:
                        g.DrawRectangle(grid_pen, x, y, self.cell_w, self.cell_h)

            # Month dividers and labels
            month_starts = [0, 31, 59, 90, 120, 151, 181, 212, 243, 273, 304, 334]
            month_names = ["Jan", "Feb", "Mar", "Apr", "May", "Jun",
                           "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"]

            if self.mode == 2:
                month_pen = Drawing.Pens.Gray
            elif self.mode == 3:
                month_pen = Drawing.Pens.Black
            else:
                month_pen = Drawing.Pens.LightGray

            for idx, day in enumerate(month_starts):
                x = day * self.cell_w
                g.DrawLine(month_pen, x, 0, x, self.rows * self.cell_h)
                g.DrawString(month_names[idx], self.Font, Drawing.Brushes.Black,
                             x + 50, self.rows * self.cell_h + 5)

            # Hour labels
            for i in [0, 6, 12, 18, 24]:
                if i <= 24:
                    label = str(i).zfill(2) + ":00"
                    y = i * self.cell_h - 20
                    g.DrawString(label, self.Font, Drawing.Brushes.Black,
                                 self.cols * self.cell_w + 5, y)


            # Legend (counts + labels)
            legend_counts = [0] * 5
            for val in self.data:
                legend_counts[classify(val)] += 1

            total = float(len(self.data))
            percentages = [int(round(c / total * 100)) for c in legend_counts]

            legend_labels = [
                "{}% {}".format(percentages[0], self.range_names[0]),
                "{}% {}".format(percentages[1], self.range_names[1]),
                "{}% {}".format(percentages[2], self.range_names[2]),
                "{}% {}".format(percentages[3], self.range_names[3]),
                "{}% {}".format(percentages[4], self.range_names[4])
            ]

            legend_spacing = 55
            legend_elements = []
            total_width = 0
            for i in range(5):
                label = legend_labels[i]
                label_size = g.MeasureString(label, self.Font)
                element_width = 20 + 5 + int(label_size.Width) + legend_spacing
                legend_elements.append((label, element_width))
                total_width += element_width

            legend_x = (self.cols * self.cell_w - total_width) // 2
            legend_y = self.rows * self.cell_h + self.margin_top + 45

            for i, (label, element_width) in enumerate(legend_elements):
                g.FillRectangle(bucket_brushes[i], legend_x, legend_y, 20, 15)
                g.DrawString(label, self.Font, Drawing.Brushes.Black, legend_x + 25, legend_y)
                legend_x += element_width

            # Reset transform
            g.ResetTransform()

            # Title
            title_font = Drawing.Font(self.Font.FontFamily, 10) # Drawing.FontStyle.Bold
            title = _graph_title or ""
            sz = g.MeasureString(title, title_font)
            g.DrawString(_graph_title, title_font, Drawing.Brushes.Black,
                        self.margin_left + (self.cols * self.cell_w - sz.Width) / 2.0, 10)

            # X-Axis Label
            g.DrawString("Day of Year", self.Font, Drawing.Brushes.Black,
                        self.margin_left + self.cols * self.cell_w // 2 - 30,
                        self.rows * self.cell_h + self.margin_top + 70)

            # Y-Axis Label
            g.TranslateTransform(self.cols * self.cell_w + 90,
                                 self.margin_top + self.rows * self.cell_h // 2 + 30)
            g.RotateTransform(-90)
            g.DrawString("Time of Day", self.Font, Drawing.Brushes.Black, -60, 30)
            g.ResetTransform()

            # GDI objects
            if grid_pen: grid_pen.Dispose()
            for b in bucket_brushes: b.Dispose()

        def on_mouse_move(self, sender, e):
            x, y = e.X, e.Y
            col = (x - self.margin_left) // self.cell_w
            row = (y - self.margin_top - self.title_height) // self.cell_h
            if 0 <= col < 365 and 0 <= row < 24:
                index = col * 24 + row
                val = round(self.data[index], 2)
                self.hover_label.Text = f"Day {col+1}, Hour {row}: {val}"

        def on_export_click(self, sender, args):
            dialog = WinForms.SaveFileDialog()
            dialog.Filter = "PNG Image|*.png"
            dialog.FileName = _graph_title
            if dialog.ShowDialog() == WinForms.DialogResult.OK:
                try:
                    bmp = Drawing.Bitmap(self.ClientSize.Width, self.ClientSize.Height)
                    g = Drawing.Graphics.FromImage(bmp)
                    g.Clear(self.BackColor)
                    paint_args = WinForms.PaintEventArgs(g, self.ClientRectangle)
                    self.on_paint(self, paint_args)
                    bmp.Save(dialog.FileName)
                    WinForms.MessageBox.Show("Saved successfully to:\n" + dialog.FileName)
                except Exception as e:
                    WinForms.MessageBox.Show("Export failed:\n" + str(e), "Error")

    # Show Form
    form = PARHeatmapForm(flat_data)
    form.Show()
