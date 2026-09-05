ghenv.Component.Message = """FlahaGrow 0.0
Load Spectral Data"""

import csv, os
import scriptcontext as sc

import System
from System import EventHandler
import System.Windows.Forms as WinForms
from System.Windows.Forms import (
    Form, Panel, Button, Label, TextBox, OpenFileDialog, SaveFileDialog,
    DataGridView, DataGridViewAutoSizeColumnsMode, DataGridViewSelectionMode,
    DockStyle, AnchorStyles, DataGridViewCellBorderStyle, TableLayoutPanel,
    ColumnStyle, SizeType, Padding
)
from System.Drawing import Size, Point, Font, FontStyle, Color

def _push_update():
    try:
        ghenv.Component.ExpireSolution(True)
    except:
        pass


H  = 6.62607015e-34
C  = 2.99792458e8
NA = 6.02214076e23


WL_MIN, WL_MAX = 380, 780
try:
    WL_STEP = int(_wavelength_interval)
except:
    WL_STEP = 1
if WL_STEP <= 0:
    WL_STEP = 1


WL_LIST = []
w = WL_MIN
while w <= WL_MAX:
    WL_LIST.append(w)
    w += WL_STEP
if WL_LIST[-1] != WL_MAX and WL_MAX - WL_LIST[-1] < WL_STEP:

    pass


OPN1_1NM = [
0.0000390000,0.0000428264,0.0000469146,0.0000515896,0.0000571764,0.0000640000,0.0000723442,0.0000822122,0.0000935082,0.0001061361,
0.0001200000,0.0001349840,0.0001514920,0.0001702080,0.0001918160,0.0002170000,0.0002469067,0.0002812400,0.0003185200,0.0003572667,
0.0003960000,0.0004337147,0.0004730240,0.0005178760,0.0005722187,0.0006400000,0.0007245600,0.0008255000,0.0009411600,0.0010698800,
0.0012100000,0.0013620910,0.0015307520,0.0017203680,0.0019353230,0.0021800000,0.0024548000,0.0027640000,0.0031178000,0.0035264000,
0.0040000000,0.0045462400,0.0051593200,0.0058292800,0.0065461600,0.0073000000,0.0080865070,0.0089087200,0.0097676800,0.0106644300,
0.0116000000,0.0125731700,0.0135827200,0.0146296800,0.0157150900,0.0168400000,0.0180073600,0.0192144800,0.0204539200,0.0217182400,
0.0230000000,0.0242946100,0.0256102400,0.0269585700,0.0283512500,0.0298000000,0.0313108300,0.0328836800,0.0345211200,0.0362257100,
0.0380000000,0.0398466700,0.0417680000,0.0437660000,0.0458426700,0.0480000000,0.0502436800,0.0525730400,0.0549805600,0.0574587200,
0.0600000000,0.0626019700,0.0652775200,0.0680420800,0.0709110900,0.0739000000,0.0770160000,0.0802664000,0.0836668000,0.0872328000,
0.0909800000,0.0949175500,0.0990458400,0.1033674000,0.1078846000,0.1126000000,0.1175320000,0.1226744000,0.1279928000,0.1334528000,
0.1390200000,0.1446764000,0.1504693000,0.1564619000,0.1627177000,0.1693000000,0.1762431000,0.1835581000,0.1912735000,0.1994180000,
0.2080200000,0.2171199000,0.2267345000,0.2368571000,0.2474812000,0.2586000000,0.2701849000,0.2822939000,0.2950505000,0.3085780000,
0.3230000000,0.3384021000,0.3546858000,0.3716986000,0.3892875000,0.4073000000,0.4256299000,0.4443096000,0.4633944000,0.4829395000,
0.5030000000,0.5235693000,0.5445120000,0.5656900000,0.5869653000,0.6082000000,0.6293456000,0.6503068000,0.6708752000,0.6908424000,
0.7100000000,0.7281852000,0.7454636000,0.7619694000,0.7778368000,0.7932000000,0.8081104000,0.8224962000,0.8363068000,0.8494916000,
0.8620000000,0.8738108000,0.8849624000,0.8954936000,0.9054432000,0.9148501000,0.9237348000,0.9320924000,0.9399226000,0.9472252000,
0.9540000000,0.9602561000,0.9660074000,0.9712606000,0.9760225000,0.9803000000,0.9840924000,0.9874182000,0.9903128000,0.9928116000,
0.9949501000,0.9967108000,0.9980983000,0.9991120000,0.9997482000,1.0000000000,0.9998567000,0.9993046000,0.9983255000,0.9968987000,
0.9950000000,0.9926005000,0.9897426000,0.9864444000,0.9827241000,0.9786000000,0.9740837000,0.9691712000,0.9638568000,0.9581349000,
0.9520000000,0.9454504000,0.9384992000,0.9311628000,0.9234576000,0.9154000000,0.9070064000,0.8982772000,0.8892048000,0.8797816000,
0.8700000000,0.8598613000,0.8493920000,0.8386220000,0.8275813000,0.8163000000,0.8047947000,0.7930820000,0.7811920000,0.7691547000,
0.7570000000,0.7447541000,0.7324224000,0.7200036000,0.7074965000,0.6949000000,0.6822192000,0.6694716000,0.6566744000,0.6438448000,
0.6310000000,0.6181555000,0.6053144000,0.5924756000,0.5796379000,0.5668000000,0.5539611000,0.5411372000,0.5283528000,0.5156323000,
0.5030000000,0.4904688000,0.4780304000,0.4656776000,0.4534032000,0.4412000000,0.4290800000,0.4170360000,0.4050320000,0.3930320000,
0.3810000000,0.3689184000,0.3568272000,0.3447768000,0.3328176000,0.3210000000,0.3093381000,0.2978504000,0.2865936000,0.2756245000,
0.2650000000,0.2547632000,0.2448896000,0.2353344000,0.2260528000,0.2170000000,0.2081616000,0.1995488000,0.1911552000,0.1829744000,
0.1750000000,0.1672235000,0.1596464000,0.1522776000,0.1451259000,0.1382000000,0.1315003000,0.1250248000,0.1187792000,0.1127691000,
0.1070000000,0.1014762000,0.0961886400,0.0911229600,0.0862648500,0.0816000000,0.0771206400,0.0728255200,0.0687100800,0.0647697600,
0.0610000000,0.0573962100,0.0539550400,0.0506737600,0.0475496500,0.0445800000,0.0417587200,0.0390849600,0.0365638400,0.0342004800,
0.0320000000,0.0299626100,0.0280766400,0.0263293600,0.0247080500,0.0232000000,0.0218007700,0.0205011200,0.0192810800,0.0181206900,
0.0170000000,0.0159037900,0.0148371800,0.0138106800,0.0128347800,0.0119200000,0.0110683100,0.0102733900,0.0095333110,0.0088461570,
0.0082100000,0.0076237810,0.0070854240,0.0065914760,0.0061384850,0.0057230000,0.0053430590,0.0049957960,0.0046764040,0.0043800750,
0.0041020000,0.0038384530,0.0035890990,0.0033542190,0.0031340930,0.0029290000,0.0027381390,0.0025598760,0.0023932440,0.0022372750,
0.0020910000,0.0019535870,0.0018245800,0.0017035800,0.0015901870,0.0014840000,0.0013844960,0.0012912680,0.0012040920,0.0011227440,
0.0010470000,0.0009765896,0.0009111088,0.0008501332,0.0007932384,0.0007400000,0.0006900827,0.0006433100,0.0005994960,0.0005584547,
0.0005200000,0.0004839136,0.0004500528,0.0004183452,0.0003887184,0.0003611000,0.0003353835,0.0003114404,0.0002891656,0.0002684539,
0.0002492000,0.0002313019,0.0002146856,0.0001992884,0.0001850475,0.0001719000,0.0001597781,0.0001486044,0.0001383016,0.0001287925,
0.0001200000,0.0001118595,0.0001043224,0.0000973356,0.0000908459,0.0000848000,0.0000791467,0.0000738580,0.0000689160,0.0000643027,
0.0000600000,0.0000559819,0.0000522256,0.0000487184,0.0000454475,0.0000424000,0.0000395610,0.0000369151,0.0000344487,0.0000321482,
0.0000300000,0.0000279913,0.0000261136,0.0000243602,0.0000227246,0.0000212000,0.0000197786,0.0000184529,0.0000172169,0.0000160646,
0.0000149900
]

def opn1_at_nm(wl):
    return OPN1_1NM[wl - WL_MIN]

def to_float(x, default=0.0):
    if x is None: return default
    if isinstance(x, (int, float)): return float(x)
    s = str(x).strip().replace(",", "")
    if not s: return default
    try: return float(s)
    except: return default

def best_header_match(headers, words):
    ws = [w.lower() for w in words]
    for h in headers:
        hl = h.lower()
        if all(w in hl for w in ws):
            return h
    return None


class SpectralForm(Form):
    def __init__(self, on_update=None):
        Form.__init__(self)
        self._on_update = on_update
        self.Text = "Spectral Data"
        self.Size = Size(1180, 620)
        self.MinimumSize = Size(900, 620)

        # TOP
        self.top = Panel()
        self.top.Dock = DockStyle.Top
        self.top.BackColor = Color.White
        self.top.Height = 250
        self.top.Padding = Padding(10, 10, 10, 10)

        self.lbl_header = Label()
        self.lbl_header.Text = "SPECTRAL DATA"
        self.lbl_header.Font = Font("Segoe UI", 14, FontStyle.Bold)
        self.lbl_header.AutoSize = True
        self.lbl_header.Location = Point(40, 10)

        self.btn_load = Button()
        self.btn_load.Text = "Load Spectral Data"
        self.btn_load.Location = Point(40, 100)
        self.btn_load.Font = Font("Segoe UI", 9, FontStyle.Bold)
        self.btn_load.Height = 30
        self.btn_load.Dock = DockStyle.Top
        self.btn_load.Margin = Padding(0, 36, 0, 10)
        self.btn_load.Click += EventHandler(self.on_load_spectral)

        self.lbl_calc = Label()
        self.lbl_calc.Text = "Conversion Factor Calculation"
        self.lbl_calc.Font = Font("Segoe UI", 10, FontStyle.Regular)
        self.lbl_calc.AutoSize = True
        self.lbl_calc.Location = Point(10, 60)

        self.txt_cf = TextBox()
        self.txt_cf.ReadOnly = True
        self.txt_cf.Font = Font("Segoe UI", 11, FontStyle.Bold)
        self.txt_cf.Location = Point(10, 85)
        self.txt_cf.Size = Size(220, 28)

        self.btn_update = Button()
        self.btn_update.Text = "Update Conversion Factor"
        self.btn_update.Location = Point(240, 85)
        self.btn_update.Size = Size(220, 30)
        self.btn_update.Click += EventHandler(self.update_conversion)

        # 30/70 split row
        self.splitRow = TableLayoutPanel()
        self.splitRow.ColumnCount = 2
        self.splitRow.RowCount = 1
        self.splitRow.Dock = DockStyle.Bottom
        self.splitRow.AutoSize = False
        self.splitRow.Height = 35
        self.splitRow.Margin = Padding(0, 0, 0, 0)
        self.splitRow.Padding = Padding(0)
        self.splitRow.ColumnStyles.Clear()
        self.splitRow.ColumnStyles.Add(ColumnStyle(SizeType.Percent, 30.0))
        self.splitRow.ColumnStyles.Add(ColumnStyle(SizeType.Percent, 70.0))

        self.btn_save = Button()
        self.btn_save.Text = "Save as CSV File"
        self.btn_save.Dock = DockStyle.Top
        self.btn_save.Height = 30
        self.btn_save.Click += EventHandler(self.save_as_csv)

        self.btn_setclose = Button()
        self.btn_setclose.Text = "Set conversion factor and close"
        self.btn_setclose.Dock = DockStyle.Top
        self.btn_setclose.Height = 30
        self.btn_setclose.Click += EventHandler(self.set_and_close)

        self.splitRow.Controls.Add(self.btn_save, 0, 0)
        self.splitRow.Controls.Add(self.btn_setclose, 1, 0)

        self.lbl_file = Label()
        self.lbl_file.Text = "Calculated: (No file loaded)"
        self.lbl_file.AutoSize = True
        self.lbl_file.Location = Point(10, 125)

        self.lbl_sum_par = Label()
        self.lbl_sum_par.Text = "Sum of Calculated PAR: -"
        self.lbl_sum_par.AutoSize = True
        self.lbl_sum_par.Location = Point(10, 145)

        self.lbl_sum_lx = Label()
        self.lbl_sum_lx.Text = "Sum of Calculated Lx: -"
        self.lbl_sum_lx.AutoSize = True
        self.lbl_sum_lx.Location = Point(10, 165)

        self.top.Controls.Add(self.splitRow)
        self.top.Controls.Add(self.btn_load)
        self.top.Controls.Add(self.lbl_header)
        self.top.Controls.Add(self.lbl_calc)
        self.top.Controls.Add(self.txt_cf)
        self.top.Controls.Add(self.btn_update)
        self.top.Controls.Add(self.lbl_file)
        self.top.Controls.Add(self.lbl_sum_par)
        self.top.Controls.Add(self.lbl_sum_lx)

        # GRID
        self.bottom = Panel()
        self.bottom.Dock = DockStyle.Fill

        self.grid = DataGridView()
        self.grid.Dock = DockStyle.Fill
        self.grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        self.grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect

        grey = Color.Gainsboro
        self.grid.DefaultCellStyle.SelectionBackColor = grey
        self.grid.DefaultCellStyle.SelectionForeColor = Color.Black
        self.grid.AlternatingRowsDefaultCellStyle.SelectionBackColor = grey
        self.grid.AlternatingRowsDefaultCellStyle.SelectionForeColor = Color.Black

        self.grid.EnableHeadersVisualStyles = False
        self.grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = grey
        self.grid.ColumnHeadersDefaultCellStyle.SelectionForeColor = Color.Black

        self.grid.AllowUserToAddRows = False
        self.grid.AllowUserToDeleteRows = False
        self.grid.ReadOnly = True
        self.grid.RowHeadersVisible = False
        self.grid.EnableHeadersVisualStyles = False
        self.grid.ColumnHeadersDefaultCellStyle.Font = Font("Segoe UI", 10, FontStyle.Bold)
        self.grid.GridColor = Color.LightGray
        self.grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal
        self.grid.ColumnHeadersBorderStyle = WinForms.DataGridViewHeaderBorderStyle.Single

        self.bottom.Controls.Add(self.grid)
        self.Controls.Add(self.bottom)
        self.Controls.Add(self.top)

        # data
        self.rows = []
        self.spectral_power = {}
        self.loaded_path = None
        self.init_rows()
        self.refresh_grid()
        self.update_conversion(None, None)

 
    def init_rows(self):
        self.rows = []
        for wl in WL_LIST:
            equal_par = 1.0 if 400 <= wl <= 700 else 0.0
            par_spec = (wl * equal_par * 1e-3) / (H * C * NA)
            self.rows.append({
                "Wavelength (nm)": wl,
                "Equal PAR": equal_par,
                "PAR Spectral": par_spec,
                "OPN1": opn1_at_nm(wl),
                "Spectral Power (W/m2/nm1)": 0.0,
                "Calculated PAR": 0.0,
                "Calculated Lx": 0.0
            })

    def refresh_grid(self):
        for r in self.rows:
            wl = r["Wavelength (nm)"]
            sp = float(self.spectral_power.get(wl, 0.0))
            r["Spectral Power (W/m2/nm1)"] = sp
            r["Calculated PAR"] = r["PAR Spectral"] * sp
            r["Calculated Lx"]  = r["OPN1"] * sp

        from System.Data import DataTable
        dt = DataTable("Grid")
        cols = ["Wavelength (nm)", "Equal PAR", "PAR Spectral", "OPN1",
                "Spectral Power (W/m2/nm1)", "Calculated PAR", "Calculated Lx"]
        for c in cols: dt.Columns.Add(c)
        for r in self.rows:
            row = dt.NewRow()
            for c in cols: row[c] = r[c]
            dt.Rows.Add(row)
        self.grid.DataSource = dt

    def on_load_spectral(self, sender, args):
        dlg = OpenFileDialog()
        dlg.Title = "Select spectral CSV (wavelength + spectral power)"
        dlg.Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*"
        if dlg.ShowDialog() != WinForms.DialogResult.OK:
            return
        try:
            with open(dlg.FileName, "r", encoding="utf-8-sig", newline="") as f:
                rdr = csv.DictReader(f)
                headers = list(rdr.fieldnames or [])
                wl_col = best_header_match(headers, ["wavelength"]) or best_header_match(headers, ["nm"]) or headers[0]
                sp_col = (best_header_match(headers, ["spectral","power"]) or
                          best_header_match(headers, ["power"]) or
                          best_header_match(headers, ["spectral_power"]) or
                          best_header_match(headers, ["w","m2","nm"]) or
                          (headers[1] if len(headers) > 1 else headers[0]))
                raw = {}
                for row in rdr:
                    wl = int(round(to_float(row.get(wl_col, None), 0.0)))
                    sp = to_float(row.get(sp_col, None), 0.0)
                    raw[wl] = sp

                filled, last = {}, None
                for wl in WL_LIST:
                    if wl in raw:
                        last = raw[wl]
                    filled[wl] = last if last is not None else 0.0
                self.spectral_power = filled
                self.loaded_path = dlg.FileName
                self.lbl_file.Text = "Calculated: {}".format(os.path.basename(self.loaded_path))
            self.refresh_grid()
            self.update_conversion(None, None)
        except Exception as e:
            WinForms.MessageBox.Show("Failed to read CSV:\n{}".format(e))

    def sums(self):
        s_par, s_lx = 0.0, 0.0
        for r in self.rows:
            wl = r["Wavelength (nm)"]
            sp = float(self.spectral_power.get(wl, 0.0))
            s_par += r["PAR Spectral"] * sp
            s_lx  += r["OPN1"] * sp
        return s_par, s_lx

    def update_conversion(self, sender, args):
        s_par, s_lx = self.sums()
        cf = (s_par / (s_lx*683)) if s_par != 0.0 else 0.0
        self.txt_cf.Text = "{:.9f}".format(cf)
        self.lbl_sum_par.Text = "Sum of Calculated PAR: {:.6f}".format(s_par)
        self.lbl_sum_lx.Text  = "Sum of Calculated Lx: {:.6f}".format(s_lx)
        sc.sticky["__PARCF_SUMS__"] = (s_par, s_lx, cf)
        self.refresh_grid()
        _push_update()

    def save_as_csv(self, sender, args):
        if not self.loaded_path:
            WinForms.MessageBox.Show("Please load a spectral CSV first.")
            return
        base = os.path.basename(self.loaded_path)
        name, ext = os.path.splitext(base)
        out_path = os.path.join(os.path.dirname(self.loaded_path), name + "_calculated.csv")
        cols = ["Wavelength (nm)", "Equal PAR", "PAR Spectral", "OPN1",
                "Spectral Power (W/m2/nm1)", "Calculated PAR", "Calculated Lx"]
        try:
            with open(out_path, "w", encoding="utf-8", newline="") as f:
                w = csv.writer(f)
                w.writerow(cols)
                for r in self.rows:
                    w.writerow([r[c] for c in cols])
                s_par, s_lx = self.sums()
                cf = (s_lx / s_par) if s_par != 0.0 else 0.0
                w.writerow([])
                w.writerow(["Calculated file", os.path.basename(self.loaded_path)])
                w.writerow(["Sum of Calculated PAR", s_par])
                w.writerow(["Sum of Calculated Lx", s_lx])
                w.writerow(["Conversion Factor (Lx/PAR)", cf])
            WinForms.MessageBox.Show('Saved:\n{}'.format(out_path))
        except Exception as e:
            WinForms.MessageBox.Show("Failed to save CSV:\n{}".format(e))

    def set_and_close(self, sender, args):
        self.update_conversion(None, None)
        _push_update()
        self.Close()


key = "__SPECTRAL_CF_FORM__"
if _load_spectral_data:
    frm = sc.sticky.get(key, None)
    if frm is None or frm.IsDisposed:
        frm = SpectralForm(on_update=_push_update)
        sc.sticky[key] = frm
        frm.Show()
    else:
        frm.Activate()

s_par, s_lx, cf = sc.sticky.get("__PARCF_SUMS__", (0.0, 0.0, 0.0))
_conversion_factor = float(cf)
