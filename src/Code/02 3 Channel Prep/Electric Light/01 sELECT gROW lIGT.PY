ghenv.Component.Message = """FlahaGrow 0.0
IES Luminaries"""

import os, re
import System
import System.Windows.Forms as WinForms

from System.Windows.Forms import (
    TableLayoutPanel, ColumnStyle, RowStyle, SizeType, DockStyle,
    Padding, DataGridViewSelectionMode, DataGridViewColumnHeadersHeightSizeMode
)
from System.Drawing import (
    Size, Point, Color, Font, FontStyle, Bitmap, Imaging, ContentAlignment
)


ies_folder = r"C:\RadIES"



def name_case_display(s):
    return s.replace("_", " ").title()



def as_int(x, default=None):
    try: return int(float(x))
    except: return default



def as_float(x, default=None):
    try: return float(x)
    except: return default



def fmt_mm(v_mm):
    try:
        return str(int(round(v_mm)))
    except:
        return "—"



def extract_first(patterns, text, cast=None):
    """
    Try multiple regex patterns; return first captured group.
    cast can be int/float/str.
    """
    for pat in patterns:
        m = re.search(pat, text, flags=re.I)
        if m:
            val = m.group(1)
            if cast:
                try:
                    return cast(val)
                except:
                    return val
            return val
    return None



def parse_ies(filepath):
    """
    Returns:
      row_list: [Name, Lumens, Size, CCT, CRI, Vert Range, Horiz Range]
      original_name: best ID for the row (we use Name again)
      fullpath: filepath
    Parsing is LM-63 tolerant (86/95/02).
    """


    with open(filepath, 'r', errors='ignore') as f:
        lines = [ln.rstrip("\r\n") for ln in f]
    text = "\n".join(lines)



    basename = os.path.splitext(os.path.basename(filepath))[0]
    name = extract_first([
        r'^\s*\[LUMINAIRE\]\s*(.+)$',
        r'^\s*\[LUMCAT\]\s*(.+)$',
        r'^\s*\[LABEL\]\s*(.+)$',
        r'^\s*\[TEST\]\s*(.+)$',
    ], text, cast=str)
    if not name:
        name = extract_first([
            r'^\s*LUMINAIRE\s*:?[\t ]*(.+)$',
            r'^\s*LUMCAT\s*:?[\t ]*(.+)$',
            r'^\s*LABEL\s*:?[\t ]*(.+)$',
            r'^\s*TEST\s*:?[\t ]*(.+)$',
        ], text, cast=str)
    if not name:
        name = basename
    name = name.strip()


    tilt_idx = None
    for i, ln in enumerate(lines):
        if re.match(r'^\s*TILT\s*=\s*', ln, flags=re.I):
            tilt_idx = i
            break
    if tilt_idx is None:


        lumens_total = "—"
        size_str = "—"
        cct_str = extract_first([r'(\d{3,5})\s*K', r'CCT[^0-9]*?(\d{3,5})'], text, cast=str) or "—"
        cri_str = extract_first([r'CRI[^0-9]*?(\d{2,3})', r'Ra[^0-9]*?(\d{2,3})'], text, cast=str) or "—"
        v_range = "—"
        h_range = "—"
        row = [name, str(lumens_total), size_str, str(cct_str), str(cri_str), v_range, h_range]
        return row, name, filepath


    hdr_tokens = []
    j = tilt_idx + 1
    while j < len(lines):
        toks = [t for t in re.split(r'[\s,]+', lines[j].strip()) if t]
        numeric = [as_float(t) for t in toks]
        if len([n for n in numeric if n is not None]) >= 10:
            hdr_tokens = toks
            break
        j += 1


    lamps = lum_per_lamp = 1.0
    mult = 1.0
    n_vert = n_horz = 0
    photometric_type = 3
    units_type = 2
    width = length = height = None
    input_watts = None

    if hdr_tokens:
        if len(hdr_tokens) >= 1:  lamps = as_float(hdr_tokens[0], 1.0)
        if len(hdr_tokens) >= 2:  lum_per_lamp = as_float(hdr_tokens[1], 0.0)
        if len(hdr_tokens) >= 3:  mult = as_float(hdr_tokens[2], 1.0)
        if len(hdr_tokens) >= 4:  n_vert = as_int(hdr_tokens[3], 0)
        if len(hdr_tokens) >= 5:  n_horz = as_int(hdr_tokens[4], 0)
        if len(hdr_tokens) >= 6:  photometric_type = as_int(hdr_tokens[5], 3)
        if len(hdr_tokens) >= 7:  units_type = as_int(hdr_tokens[6], 2)
        if len(hdr_tokens) >= 8:  width = as_float(hdr_tokens[7], None)
        if len(hdr_tokens) >= 9:  length = as_float(hdr_tokens[8], None)
        if len(hdr_tokens) >= 10: height = as_float(hdr_tokens[9], None)
        if len(hdr_tokens) >= 13: input_watts = as_float(hdr_tokens[12], None)



    # Vertical angles
    v_angles, k = [], j+1
    while len(v_angles) < max(n_vert, 0) and k < len(lines):
        for tok in re.split(r'[\s,]+', lines[k].strip()):
            val = as_float(tok)
            if val is not None:
                v_angles.append(val)
                if len(v_angles) >= n_vert: break
        k += 1



    # Horizontal angles
    h_angles = []
    while len(h_angles) < max(n_horz, 0) and k < len(lines):
        for tok in re.split(r'[\s,]+', lines[k].strip()):
            val = as_float(tok)
            if val is not None:
                h_angles.append(val)
                if len(h_angles) >= n_horz: break
        k += 1



    # Derived fields
    if lamps is None: lamps = 1.0
    if lum_per_lamp is None: lum_per_lamp = 0.0
    if mult is None: mult = 1.0
    lumens_total = lamps * lum_per_lamp * mult
    lumens_str = str(int(round(lumens_total))) if lumens_total and lumens_total > 0 else "—"



    # Size
    size_str = "—"
    if width is not None and length is not None and height is not None:
        if units_type == 1:
            Wmm = width * 304.8; Lmm = length * 304.8; Hmm = height * 304.8
        else:
            Wmm = width * 1000.0; Lmm = length * 1000.0; Hmm = height * 1000.0
        size_str = "{}×{}×{} mm".format(fmt_mm(Lmm), fmt_mm(Wmm), fmt_mm(Hmm))



    # CCT & CRI
    cct_val = extract_first([
        r'\b(\d{4,5})\s*K\b',
        r'\bCCT[^0-9]*?(\d{3,5})\b',
        r'\b(\d{3,5})K\b',
    ], text, cast=str)
    cct_str = cct_val if cct_val else "—"

    cri_val = extract_first([
        r'\bCRI[^0-9]*?(\d{2,3})\b',
        r'\bRa[^0-9]*?(\d{2,3})\b',
        r'\bCRI\s*=\s*(\d{2,3})\b',
    ], text, cast=str)
    cri_str = cri_val if cri_val else "—"



    # Ranges
    if v_angles:
        v_min, v_max = (min(v_angles), max(v_angles))
        v_range = "{}–{} ({})".format(int(round(v_min)), int(round(v_max)), len(v_angles))
    else:
        v_range = "—"
    if h_angles:
        h_min, h_max = (min(h_angles), max(h_angles))
        h_range = "{}–{} ({})".format(int(round(h_min)), int(round(h_max)), len(h_angles))
    else:
        h_range = "—"

    row = [name, lumens_str, size_str, cct_str, cri_str, v_range, h_range]
    return row, name, filepath



def load_ies_rows():
    rows = []
    if not os.path.isdir(ies_folder):
        return rows
    for filename in os.listdir(ies_folder):
        if filename.lower().endswith(".ies"):
            fp = os.path.join(ies_folder, filename)
            try:
                rows.append(parse_ies(fp))
            except Exception:
                nm = os.path.splitext(filename)[0]
                rows.append(([nm, "—", "—", "—", "—", "—", "—"], nm, fp))
    rows.sort(key=lambda t: (not t[0][0][0].isdigit(), t[0][0].lower()))
    return rows


columns = ["Name", "Lumens", "Size", "CCT", "CRI", "Vert Range", "Horiz Range"]



def safe_load_image(png_path):
    """Load image without locking the file; return Bitmap or None."""
    if not os.path.exists(png_path):
        return None
    try:
        from System.IO import FileStream, FileMode, FileAccess, FileShare
        fs = FileStream(png_path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)
        try:
            img = Bitmap(fs)
            return img.Clone()
        finally:
            fs.Close()
    except:
        return None



def show_ies_selector():
    selected_path = [None]
    selected_name = [None]
    reload_flag = [True]

    while reload_flag[0]:
        reload_flag[0] = False

        form = WinForms.Form()
        form.Text = "FlahaGrowIES Selector"
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
        nameLabel.Text = "Select an IES!"
        nameLabel.Dock = WinForms.DockStyle.Fill
        nameLabel.TextAlign = System.Drawing.ContentAlignment.BottomLeft
        nameLabel.Font = System.Drawing.Font("Segoe UI", 20, System.Drawing.FontStyle.Bold)
        topPanel.Controls.Add(nameLabel, 1, 0)

        infoLabel = WinForms.Label()
        infoLabel.Text = "Lumens: — | CCT: — | CRI: —"
        infoLabel.Dock = WinForms.DockStyle.Fill
        infoLabel.TextAlign = System.Drawing.ContentAlignment.TopLeft
        infoLabel.Font = System.Drawing.Font("Segoe UI", 10)
        topPanel.Controls.Add(infoLabel, 1, 1)

        imageBox = WinForms.PictureBox()
        imageBox.SizeMode = WinForms.PictureBoxSizeMode.Zoom
        imageBox.Dock = WinForms.DockStyle.Fill
        imageBox.BackColor = System.Drawing.Color.White
        topPanel.Controls.Add(imageBox, 0, 0)
        topPanel.SetRowSpan(imageBox, 2)

        grid = WinForms.DataGridView()
        grid.Dock = WinForms.DockStyle.Fill
        grid.ReadOnly = True
        grid.MultiSelect = False
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect
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



        # populate
        ies_rows = load_ies_rows()
        for row_list, original_name, fullpath in ies_rows:
            display_row = row_list[:]
            display_row[0] = name_case_display(row_list[0])
            idx = grid.Rows.Add(display_row)
            grid.Rows[idx].Tag = (original_name, fullpath, os.path.splitext(os.path.basename(fullpath))[0])

        grid.Columns[0].Width = 80



        def on_select(sender, args):
            if grid.SelectedRows.Count > 0:
                orig_name, fullpath, _base = grid.SelectedRows[0].Tag
                selected_path[0] = fullpath
                selected_name[0] = orig_name
                form.Close()


        def on_reload(sender, args):
            reload_flag[0] = True
            form.Close()


        def update_image(sender, args):
            if grid.SelectedRows.Count > 0:
                original_name, fullpath, base_no_ext = grid.SelectedRows[0].Tag
                png_path = os.path.join(ies_folder, base_no_ext + ".png")


                if imageBox.Image is not None:
                    try:
                        old = imageBox.Image
                        imageBox.Image = None
                        old.Dispose()
                    except:
                        pass

                imageBox.Image = safe_load_image(png_path)

                display_name = name_case_display(original_name)
                lumens = grid.SelectedRows[0].Cells[1].Value
                cct = grid.SelectedRows[0].Cells[3].Value
                cri = grid.SelectedRows[0].Cells[4].Value
                nameLabel.Text = display_name
                infoLabel.Text = "Lumens: {} | CCT: {} | CRI: {}".format(lumens, cct, cri)

        grid.SelectionChanged += update_image



        # Buttons row
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



        # assemble form
        layout.Controls.Add(topPanel, 0, 0)
        layout.Controls.Add(grid, 0, 1)
        layout.Controls.Add(buttonPanel, 0, 2)
        form.Controls.Add(layout)



        # Preselect first row
        if grid.Rows.Count > 0:
            try:
                grid.ClearSelection()
                grid.Rows[0].Selected = True
            except:
                pass

        form.ShowDialog()

    return selected_path[0], selected_name[0]



if run:
    _ies_path, _ies_name = show_ies_selector()
else:
    _ies_path, _ies_name = None, None
