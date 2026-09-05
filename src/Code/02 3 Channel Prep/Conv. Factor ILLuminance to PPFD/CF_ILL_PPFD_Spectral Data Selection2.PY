ghenv.Component.Message = """FlahaGrow 0.0
Spectral Data Selection"""




import os, csv

from System import EventHandler
from System.Windows.Forms import (
    Form, Button, Label, TextBox, OpenFileDialog,
    FlowLayoutPanel, DockStyle, DialogResult, Padding, FlowDirection,
    FormStartPosition, AutoScaleMode, LinkLabel, LinkArea
)
from System.Drawing import Size, Color, SizeF


try:
    _LAST_FACTOR
    _LAST_LABEL
except NameError:
    _LAST_FACTOR = None
    _LAST_LABEL  = None


H  = 6.62607015e-34
C  = 2.99792458e8
NA = 6.02214076e23

WL_MIN, WL_MAX, WL_STEP = 380, 780, _wavelength_interval
WL_LIST = list(range(WL_MIN, WL_MAX + _wavelength_interval, WL_STEP))



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
    return OPN1_1NM[wl - 380]

def to_float(x, default=0.0):
    if x is None: return default
    if isinstance(x, (int, float)): return float(x)
    s = str(x).strip().replace(",", "")
    if not s: return default
    try: return float(s)
    except: return default

def best_header_match(headers, words):
    ws = [w.lower() for w in words]
    for h in headers or []:
        hl = h.lower()
        if all(w in hl for w in ws):
            return h
    return None

def compute_cf_from_csv(path):
    """Read CSV (wavelength + spectral power), forward-fill onto WL_LIST, compute cf = s_lx/s_par."""
    with open(path, "r", encoding="utf-8-sig", newline="") as f:
        rdr = csv.DictReader(f)
        headers = list(rdr.fieldnames or [])
        wl_col = best_header_match(headers, ["wavelength"]) or best_header_match(headers, ["nm"]) or (headers[0] if headers else None)
        sp_col = (best_header_match(headers, ["spectral","power"]) or
                  best_header_match(headers, ["power"]) or
                  best_header_match(headers, ["spectral_power"]) or
                  best_header_match(headers, ["w","m2","nm"]) or
                  (headers[1] if headers and len(headers) > 1 else wl_col))

        raw = {}
        for row in rdr:
            wl = int(round(to_float(row.get(wl_col, None), 0.0)))
            sp = to_float(row.get(sp_col, None), 0.0)
            if WL_MIN <= wl <= WL_MAX:
                raw[wl] = sp


    spectral_power = {}
    last = None
    for wl in WL_LIST:
        if wl in raw:
            last = raw[wl]
        spectral_power[wl] = last if last is not None else 0.0



    # sums
    s_par = 0.0
    s_lx  = 0.0
    for wl in WL_LIST:
        equal_par = 1.0 if 400 <= wl <= 700 else 0.0
        par_spec  = (wl * equal_par * 1e-3) / (H * C * NA)
        opn1      = opn1_at_nm(wl)
        sp        = float(spectral_power.get(wl, 0.0))
        s_par += par_spec * sp
        s_lx  += opn1 * sp

    cf = (s_par / (s_lx*683)) if s_par != 0.0 else 0.0
    return s_par, s_lx, cf



STANDARD_SOURCES = [
    ("CIE-D55",     0.017833),
    ("CIE-D65",     0.018043),
    ("CIE-D75",     0.018345),
    ("CIE-HPS-1",   0.011640),
    ("CIE-HPS-5",   0.016180),
    ("CIE-LED-BH1", 0.013633),
    ("CIE-LED-V2",  0.017691)
]
DEFAULT_FACTOR = 0.018043



class ConversionFactorUI(Form):
    def __init__(self, last_factor=None, last_label=None):
        Form.__init__(self)

        self.SuspendLayout()
        self.AutoScaleMode = AutoScaleMode.Dpi
        self.AutoScaleDimensions = SizeF(96.0, 96.0)
        self.ClientSize = Size(560, 680)
        self.MinimumSize = Size(560, 420)
        self.StartPosition = FormStartPosition.CenterScreen
        self.Text = "Select Conversion Factor"
        self.BackColor = Color.White

        self.selected_label  = last_label
        self.selected_factor = last_factor if last_factor is not None else DEFAULT_FACTOR

        # Header
        self.header = LinkLabel()
        self.header.Text = "Choose a source for the illuminance to PAR conversion factor"
        self.header.AutoSize = True
        self.header.Dock = DockStyle.Top
        self.header.Padding = Padding(10, 10, 10, 0)
        self.header.LinkArea = LinkArea(0, 0)
        self.Controls.Add(self.header)

        # Main wrapper
        self.wrapper = FlowLayoutPanel()
        self.wrapper.Dock = DockStyle.Fill
        self.wrapper.FlowDirection = FlowDirection.TopDown
        self.wrapper.WrapContents = False
        self.wrapper.AutoScroll = True
        self.wrapper.Padding = Padding(10, 5, 10, 10)
        self.Controls.Add(self.wrapper)

        # Standard Sources
        self.add_group_title("Standard Sources")
        self.std_panel = FlowLayoutPanel()
        self.std_panel.FlowDirection = FlowDirection.TopDown
        self.std_panel.WrapContents = False
        self.std_panel.AutoSize = True
        self.wrapper.Controls.Add(self.std_panel)

        for name, val in STANDARD_SOURCES:
            btn = Button()
            btn.Text = f"{name}   =   {val:.6f}"
            btn.Height = 44
            btn.Margin = Padding(5, 4, 5, 4)
            btn.Tag = (name, val)
            btn.Click += EventHandler(self.pick_standard)
            self.std_panel.Controls.Add(btn)

        # Custom Spectra (CSV)
        self.add_group_title("Custom Spectra (CSV)")
        self.custom_panel = FlowLayoutPanel()
        self.custom_panel.FlowDirection = FlowDirection.TopDown
        self.custom_panel.WrapContents = False
        self.custom_panel.AutoSize = True
        self.wrapper.Controls.Add(self.custom_panel)

        self.custom_hint = Label()
        self.custom_hint.Text = (
            "Load a CSV or enter the conversion factor.\n"
            "Tip: if the CSV contains the pre calculated factor, open it to view and then paste the value here."
        )
        self.custom_hint.AutoSize = True
        self.custom_hint.Margin = Padding(5, 0, 5, 6)
        self.custom_panel.Controls.Add(self.custom_hint)

        self.open_btn = Button()
        self.open_btn.Text = "Open CSV…"
        self.open_btn.Height = 40
        self.open_btn.Click += EventHandler(self.open_csv_and_compute)
        self.custom_panel.Controls.Add(self.open_btn)

        self.factor_row = FlowLayoutPanel()
        self.factor_row.FlowDirection = FlowDirection.LeftToRight
        self.factor_row.AutoSize = True
        self.factor_row.Margin = Padding(5, 6, 5, 6)
        self.custom_panel.Controls.Add(self.factor_row)

        self.factor_lbl = Label()
        self.factor_lbl.Text = "Factor:"
        self.factor_lbl.Width = 60
        self.factor_lbl.AutoSize = False
        self.factor_row.Controls.Add(self.factor_lbl)

        self.factor_box = TextBox()
        self.factor_box.Width = 140
        self.factor_box.Text = f"{self.selected_factor:.6f}"
        self.factor_row.Controls.Add(self.factor_box)

        self.use_custom_btn = Button()
        self.use_custom_btn.Text = "Use this factor"
        self.use_custom_btn.Height = 22
        self.use_custom_btn.Width = 150
        self.use_custom_btn.Click += EventHandler(self.use_custom)
        self.factor_row.Controls.Add(self.use_custom_btn)

        # Footer
        self.selection_lbl = Label()
        self.selection_lbl.AutoSize = True
        self.selection_lbl.Padding = Padding(10, 6, 10, 6)
        self.update_selection_label()
        self.wrapper.Controls.Add(self.selection_lbl)

        self.ok_btn = Button()
        self.ok_btn.Text = "Set Factor and Close"
        self.ok_btn.Height = 44
        self.ok_btn.Margin = Padding(5, 8, 5, 10)
        self.ok_btn.Click += EventHandler(self.confirm_and_close)
        self.wrapper.Controls.Add(self.ok_btn)

        # Resize
        self.Resize += EventHandler(self.resize_fullwidth)
        self.Resize += EventHandler(self.fit_header_width)
        self.wrapper.SizeChanged += EventHandler(self.resize_fullwidth)

        # layout
        self.fit_header_width(None, None)
        self.resize_fullwidth(None, None)
        self.ResumeLayout(False)
        self.PerformLayout()
        self.Shown += lambda *_: (self.CenterToScreen(), self.Invalidate(True))

    # sizing
    def usable_wrapper_width(self):
        return max(
            100,
            self.wrapper.ClientSize.Width - (self.wrapper.Padding.Left + self.wrapper.Padding.Right)
        )

    def fit_header_width(self, sender, e):
        self.header.MaximumSize = Size(self.usable_wrapper_width(), 0)

    def add_group_title(self, text):
        lbl = Label()
        lbl.Text = text
        lbl.AutoSize = True
        lbl.Padding = Padding(5, 10, 5, 6)
        self.wrapper.Controls.Add(lbl)

    def resize_fullwidth(self, sender, e):
        usable = self.usable_wrapper_width()
        for pnl in (self.std_panel, self.custom_panel):
            pnl.Width = usable
            for c in pnl.Controls:
                if isinstance(c, Button) and c is not self.use_custom_btn:
                    c.Width = max(10, usable - 10)
        self.open_btn.Width = max(10, usable - 10)
        self.ok_btn.Width   = usable
        self.fit_header_width(None, None)

    def update_selection_label(self):
        name = self.selected_label if self.selected_label else "Selected"
        self.selection_lbl.Text = f"{name}:  {self.selected_factor:.6f}"

    # events
    def pick_standard(self, sender, e):
        name, val = sender.Tag
        self.selected_label = name
        self.selected_factor = float(val)
        self.factor_box.Text = f"{self.selected_factor:.6f}"
        self.update_selection_label()

    def open_csv_and_compute(self, sender, e):
        dlg = OpenFileDialog()
        dlg.Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*"
        if dlg.ShowDialog() == DialogResult.OK:
            try:
                s_par, s_lx, cf = compute_cf_from_csv(dlg.FileName)
                self.selected_label  = os.path.basename(dlg.FileName)
                self.selected_factor = float(cf)
                self.factor_box.Text = f"{self.selected_factor:.6f}"
                self.update_selection_label()
            except Exception as ex:
                print("CSV parse/compute error:", ex)

    def use_custom(self, sender, e):
        try:
            val = float(self.factor_box.Text)
            self.selected_factor = val
            self.selected_label = "Custom (CSV)"
            self.update_selection_label()
        except:
            pass

    def confirm_and_close(self, sender, e):
        self.DialogResult = DialogResult.OK
        self.Close()


_conversion_factor = _LAST_FACTOR if _LAST_FACTOR is not None else None

if _run:
    ui  = ConversionFactorUI(last_factor=_LAST_FACTOR, last_label=_LAST_LABEL)
    res = ui.ShowDialog()
    if res == DialogResult.OK:
        _conversion_factor = ui.selected_factor
        _LAST_FACTOR = ui.selected_factor
        _LAST_LABEL  = ui.selected_label
else:
    _conversion_factor = _LAST_FACTOR
