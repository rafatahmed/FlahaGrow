ghenv.Component.Message = """FlahaGrow 0.0 
Opaque Materials"""


import os
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




rad_folder = str(globals().get("_rad_materials_folder") or os.environ.get("FLAHAGROW_RAD_MATERIALS_DIR", "")).strip()
if not os.path.isdir(rad_folder):
    raise ValueError("Set _rad_materials_folder or FLAHAGROW_RAD_MATERIALS_DIR to a valid RadMaterials folder.")

def parse_rad_file(filepath):
    with open(filepath, 'r') as file:
        kept = []
        for line in file:
            s = line.strip()
            if not s:
                continue
            if s.startswith('#'):
                continue
            kept.append(s)
    lines = kept

    name = os.path.splitext(os.path.basename(filepath))[0]
    r = g = b = spec = rough = vlr = 0.0

    for line in lines:
        if line.startswith("void"):
            parts = line.split()
            if len(parts) >= 3:
                name = parts[2]
        elif line.startswith("5"):
            parts = line.split()
            if len(parts) >= 6:
                r = float(parts[1]); g = float(parts[2]); b = float(parts[3])
                spec = float(parts[4]); rough = float(parts[5])
                vlr = round((0.265 * r) + (0.670 * g) + (0.065 * b), 1)
            break
    return [name, f"{r:.3f}", f"{g:.3f}", f"{b:.3f}", f"{spec:.2f}", f"{rough:.2f}", f"{vlr}"]



def load_materials():
    materials = []
    for filename in os.listdir(rad_folder):
        if filename.endswith(".rad"):
            full_path = os.path.join(rad_folder, filename)
            materials.append(parse_rad_file(full_path))
    materials.sort(key=lambda row: (row[0][0].isdigit(), row[0].lower()))
    return materials

columns = ["Material", "R", "G", "B", "Specularity", "Roughness", "VLR"]



def name_case_display(material_id):
    return material_id.replace("_", " ").title()



def show_material_selector():
    selected = [None]
    reload_flag = [True]

    while reload_flag[0]:
        reload_flag[0] = False

        form = WinForms.Form()
        form.Text = "FlahaGrowRadiance Material"
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
        topPanel.RowStyles.Add(WinForms.ColumnStyle(WinForms.SizeType.Percent, 80))
        topPanel.RowStyles.Add(WinForms.ColumnStyle(WinForms.SizeType.Percent, 20))
        topPanel.BackColor = System.Drawing.Color.White

        nameLabel = WinForms.Label()
        nameLabel.Text = "Select a material!"
        nameLabel.Dock = WinForms.DockStyle.Fill
        nameLabel.TextAlign = System.Drawing.ContentAlignment.BottomLeft
        nameLabel.Font = System.Drawing.Font("Segoe UI", 20, System.Drawing.FontStyle.Bold)
        topPanel.Controls.Add(nameLabel, 1, 0)

        infoLabel = WinForms.Label()
        infoLabel.Text = "RGB(0.00, 0.00, 0.00) | VLR: 0.0%"
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
        # grid.ColumnHeadersDefaultCellStyle.Padding = Padding(0, 6, 0, 6)

        grid.CellBorderStyle = WinForms.DataGridViewCellBorderStyle.SingleHorizontal
        grid.GridColor = System.Drawing.Color.LightGray

        for col in columns:
            grid.Columns.Add(col, col)


        materials = load_materials()
        for row in materials:
            display_row = row[:]
            display_row[0] = name_case_display(row[0])
            idx = grid.Rows.Add(display_row)
            grid.Rows[idx].Tag = row[0]

        grid.Columns[0].Width = 80



        def on_select(sender, args):
            if grid.SelectedRows.Count > 0:
                original_name = grid.SelectedRows[0].Tag
                selected[0] = original_name
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

        # Reload
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
                original_name = grid.SelectedRows[0].Tag
                bmp_path = os.path.join(rad_folder, original_name + "_b.bmp")
                imageBox.Image = System.Drawing.Bitmap.FromFile(bmp_path) if os.path.exists(bmp_path) else None

                display_name = name_case_display(original_name)
                r = grid.SelectedRows[0].Cells[1].Value
                g = grid.SelectedRows[0].Cells[2].Value
                b = grid.SelectedRows[0].Cells[3].Value
                vlr = grid.SelectedRows[0].Cells[6].Value
                nameLabel.Text = display_name
                infoLabel.Text = f"RGB({r}, {g}, {b}) | VLR: {vlr}%"

        grid.SelectionChanged += update_image

        layout.Controls.Add(topPanel, 0, 0)
        layout.Controls.Add(grid, 0, 1)
        layout.Controls.Add(buttonPanel, 0, 2)
        form.Controls.Add(layout)
        form.ShowDialog()

    return selected[0]



# Main
if run:
    _modifier = show_material_selector()
else:
    _modifier = None
