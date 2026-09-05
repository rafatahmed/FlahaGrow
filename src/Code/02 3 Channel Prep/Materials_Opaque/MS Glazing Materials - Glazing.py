ghenv.Component.Message = """FlahaGrow 0.0 
Glazing Materials"""


import os, math
import System
import System.Windows.Forms as WinForms
from System.Collections.Generic import List
from System.Windows.Forms import (
    TableLayoutPanel, ColumnStyle, RowStyle, SizeType, DockStyle,
    Padding, FlowDirection, AnchorStyles, DataGridViewSelectionMode
)
from System.Drawing import (
    Size, Point, Color, Font, FontStyle, Bitmap, Imaging, ContentAlignment
)
from System.Windows.Forms import DataGridViewColumnHeadersHeightSizeMode, Padding



glazing_folder = str(globals().get("_rad_glazing_folder") or os.environ.get("FLAHAGROW_RAD_GLAZING_DIR", "")).strip()
if not os.path.isdir(glazing_folder):
    raise ValueError("Set _rad_glazing_folder or FLAHAGROW_RAD_GLAZING_DIR to a valid RadGlazing folder.")



def name_case_display(material_id):
    return material_id.replace("_", " ").title()



def safe_float(token):
    try:
        return float(token)
    except:
        return None



def fresnel_vlr_percent(n_idx):
    """
    Normal-incidence Fresnel reflectance for uncoated glass (per surface):
        R = ((n-1)/(n+1))^2
    Report as % (one surface/front reflectance).
    """
    R = ((n_idx - 1.0) / (n_idx + 1.0)) ** 2
    return round(100.0 * R, 1)



def parse_glazing_rad(filepath):
    """
    Returns: (row_list, original_name, fullpath)
             row_list = [name, R, G, B, VLT, VLR%, Specularity, Roughness]
    """
    name_from_file = os.path.splitext(os.path.basename(filepath))[0]
    original_name = name_from_file
    mat_type = None

    kept = []
    with open(filepath, 'r') as f:
        for line in f:
            s = line.strip()
            if not s or s.startswith('#'):
                continue
            kept.append(s)



    for line in kept:
        if line.startswith("void"):
            parts = line.split()
            if len(parts) >= 3:
                mat_type = parts[1].lower()
                original_name = parts[2]
            break




    floats = []
    for line in kept:
        for tok in line.split():
            val = safe_float(tok)
            if val is not None:
                floats.append(val)



    r = g = b = 0.0
    n_idx = 1.52
    specularity = 0.00
    roughness = 0.00

    if len(floats) >= 3:
        r, g, b = floats[0], floats[1], floats[2]

    if mat_type == 'glass' and len(floats) >= 4 and 1.2 <= floats[3] <= 2.2:
        n_idx = floats[3]

    if mat_type == 'trans':
        if len(floats) >= 4:
            specularity = max(0.0, min(1.0, floats[3]))
        if len(floats) >= 5:
            roughness = max(0.0, min(1.0, floats[4]))

    vlt = round((0.265 * r) + (0.670 * g) + (0.065 * b), 1)
    vlr_pct = fresnel_vlr_percent(n_idx)

    row = [
        original_name,
        f"{r:.3f}", f"{g:.3f}", f"{b:.3f}",
        f"{vlt}", f"{vlr_pct}",
        f"{specularity:.2f}", f"{roughness:.2f}"
    ]
    return row, original_name, filepath


def load_glazing_rows():
    rows = []
    for filename in os.listdir(glazing_folder):
        if filename.lower().endswith(".rad"):
            fp = os.path.join(glazing_folder, filename)
            rows.append(parse_glazing_rad(fp))
    rows.sort(key=lambda t: (t[0][0][0].isdigit(), t[0][0].lower()))
    return rows




columns = ["Glazing", "R", "G", "B", "VLT", "VLR%", "Specularity", "Roughness"]

def show_glazing_selector():
    selected_name = [None]
    reload_flag = [True]

    while reload_flag[0]:
        reload_flag[0] = False

        form = WinForms.Form()
        form.Text = "FlahaGrowRadiance Glazing"
        form.Size = System.Drawing.Size(1500, 850)
        form.StartPosition = WinForms.FormStartPosition.CenterScreen

        layout = WinForms.TableLayoutPanel()
        layout.Dock = WinForms.DockStyle.Fill
        layout.RowCount = 3
        layout.ColumnCount = 1
        layout.RowStyles.Add(WinForms.RowStyle(WinForms.SizeType.Absolute, 300))
        layout.RowStyles.Add(WinForms.RowStyle(WinForms.SizeType.Percent, 100))
        layout.RowStyles.Add(WinForms.RowStyle(WinForms.SizeType.Absolute, 40))
        layout.BackColor = System.Drawing.Color.White

        topPanel = WinForms.TableLayoutPanel()
        topPanel.Dock = WinForms.DockStyle.Fill
        topPanel.ColumnCount = 2
        topPanel.RowCount = 2
        topPanel.ColumnStyles.Add(WinForms.ColumnStyle(WinForms.SizeType.Absolute, 300))
        topPanel.ColumnStyles.Add(WinForms.ColumnStyle(WinForms.SizeType.Percent, 100))
        topPanel.RowStyles.Add(WinForms.RowStyle(WinForms.SizeType.Percent, 80))
        topPanel.RowStyles.Add(WinForms.RowStyle(WinForms.SizeType.Percent, 20))
        topPanel.BackColor = System.Drawing.Color.White

        nameLabel = WinForms.Label()
        nameLabel.Text = "Select a glazing!"
        nameLabel.Dock = WinForms.DockStyle.Fill
        nameLabel.TextAlign = System.Drawing.ContentAlignment.BottomLeft
        nameLabel.Font = System.Drawing.Font("Segoe UI", 20, System.Drawing.FontStyle.Bold)
        topPanel.Controls.Add(nameLabel, 1, 0)

        infoLabel = WinForms.Label()
        infoLabel.Text = "RGB(0.00, 0.00, 0.00) | VLT: 0.0% | VLR: 0.0%"
        infoLabel.Dock = WinForms.DockStyle.Fill
        infoLabel.TextAlign = System.Drawing.ContentAlignment.TopLeft
        infoLabel.Font = System.Drawing.Font("Segoe UI", 10)
        topPanel.Controls.Add(infoLabel, 1, 1)

        imageBox = WinForms.PictureBox()
        imageBox.SizeMode = WinForms.PictureBoxSizeMode.Zoom
        imageBox.Dock = WinForms.DockStyle.Fill
        imageBox.BackColor = System.Drawing.Color.White
        topPanel.Controls.Add(imageBox, 0, 0)

        grid = WinForms.DataGridView()
        grid.Dock = WinForms.DockStyle.Fill
        grid.ReadOnly = True
        grid.SelectionMode = WinForms.DataGridViewSelectionMode.FullRowSelect
        grid.AllowUserToAddRows = False
        grid.AllowUserToResizeRows = False
        grid.RowHeadersVisible = False
        grid.AutoSizeColumnsMode = WinForms.DataGridViewAutoSizeColumnsMode.Fill
        grid.BackgroundColor = System.Drawing.Color.White
        grid.ColumnHeadersDefaultCellStyle.Font = System.Drawing.Font("Segoe UI", 10, System.Drawing.FontStyle.Bold)
        grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.EnableResizing
        grid.ColumnHeadersHeight = 40
        grid.CellBorderStyle = WinForms.DataGridViewCellBorderStyle.SingleHorizontal
        grid.GridColor = System.Drawing.Color.LightGray

        for col in columns:
            grid.Columns.Add(col, col)

        glazing_rows = load_glazing_rows()
        for row_list, original_name, fullpath in glazing_rows:
            display_row = row_list[:]
            display_row[0] = name_case_display(row_list[0])
            idx = grid.Rows.Add(display_row)
            grid.Rows[idx].Tag = (original_name, fullpath)

        grid.Columns[0].Width = 80



        def on_select(sender, args):
            if grid.SelectedRows.Count > 0:
                original_name, fullpath = grid.SelectedRows[0].Tag
                selected_name[0] = original_name
                form.Close()

        def on_reload(sender, args):
            reload_flag[0] = True
            form.Close()

        buttonPanel = TableLayoutPanel()
        buttonPanel.Dock = DockStyle.Fill
        buttonPanel.RowCount = 1
        buttonPanel.ColumnCount = 3
        buttonPanel.RowStyles.Add(RowStyle(SizeType.Percent, 100))
        buttonPanel.ColumnStyles.Add(ColumnStyle(SizeType.Percent, 80))
        buttonPanel.ColumnStyles.Add(ColumnStyle(SizeType.Absolute, 1))
        buttonPanel.ColumnStyles.Add(ColumnStyle(SizeType.Percent, 20))
        buttonPanel.Padding = Padding(8, 6, 8, 6)

        selectBtn = WinForms.Button()
        selectBtn.Text = "Select"
        selectBtn.MinimumSize = Size(120, 28)
        selectBtn.AutoSize = False
        selectBtn.Dock = DockStyle.Fill
        selectBtn.Margin = Padding(0, 0, 2, 0)
        selectBtn.Click += on_select
        buttonPanel.Controls.Add(selectBtn, 0, 0)

        reloadBtn = WinForms.Button()
        reloadBtn.Text = "Reload"
        reloadBtn.MinimumSize = Size(120, 28)
        reloadBtn.AutoSize = False
        reloadBtn.Dock = DockStyle.Fill
        reloadBtn.Margin = Padding(2, 0, 0, 0)
        reloadBtn.Click += on_reload
        buttonPanel.Controls.Add(reloadBtn, 2, 0)

        def update_image(sender, args):
            if grid.SelectedRows.Count > 0:
                original_name, fullpath = grid.SelectedRows[0].Tag
                bmp_path = os.path.join(glazing_folder, original_name + "_b.bmp")
                imageBox.Image = System.Drawing.Bitmap.FromFile(bmp_path) if os.path.exists(bmp_path) else None

                display_name = name_case_display(original_name)
                r = grid.SelectedRows[0].Cells[1].Value
                g = grid.SelectedRows[0].Cells[2].Value
                b = grid.SelectedRows[0].Cells[3].Value
                vlt = grid.SelectedRows[0].Cells[4].Value
                vlr = grid.SelectedRows[0].Cells[5].Value
                nameLabel.Text = display_name
                infoLabel.Text = f"RGB({r}, {g}, {b}) | VLT: {vlt}% | VLR: {vlr}%"

        grid.SelectionChanged += update_image

        layout.Controls.Add(topPanel, 0, 0)
        layout.Controls.Add(grid, 0, 1)
        layout.Controls.Add(buttonPanel, 0, 2)
        form.Controls.Add(layout)
        form.ShowDialog()

    return selected_name[0]





if run:
    _glazing_modifier = show_glazing_selector()
else:
    _glazing_modifier = None

