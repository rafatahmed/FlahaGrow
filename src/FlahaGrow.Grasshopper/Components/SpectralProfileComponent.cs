using System.Windows.Forms;
using GH_IO.Serialization;
using FlahaGrow.Core.Operations;
using FlahaGrow.Core.PlantLight;
using FlahaGrow.Grasshopper.Parameters;
using Grasshopper.Kernel;

namespace FlahaGrow.Grasshopper.Components;

public sealed class SpectralProfileComponent : FlahaGrowComponent
{
    private readonly ActionLatch picker = new();
    private string selectedId = "";
    public SpectralProfileComponent() : base("Spectral Profile", "Profile", "Choose a bundled, referenced spectrum. No CSV required. Selection acknowledges the displayed research assumptions.", "FlahaGrow", "02 Spectral") { }
    public override Guid ComponentGuid => new("a9c4973b-acb7-45be-96ee-a6d8a35fa417");
    protected override void RegisterInputParams(GH_InputParamManager p) =>
        p.AddBooleanParameter("Select profile", "Select", "Connect a Grasshopper Button. Click to open the reference table; choose a row and accept its assumptions.", GH_ParamAccess.item, false);
    protected override void RegisterOutputParams(GH_OutputParamManager p)
    {
        p.AddParameter(new SpectralProfileParameter(), "Spectral Profile", "Profile", "Connect to Plant Light Context → Profile.", GH_ParamAccess.item);
        p.AddNumberParameter("Conversion factor", "Factor", "µmol/m²/s per lux. For numeric conversion inputs; the typed Profile already includes this value.", GH_ParamAccess.item);
        p.AddTextParameter("Status", "Status", "Selected reference, source DOI, method and limitations. Connect to a Panel to inspect.", GH_ParamAccess.item);
    }
    protected override void SolveInstance(IGH_DataAccess da)
    {
        bool select = false; da.GetData(0, ref select);
        try
        {
            if (picker.Observe(select)) ShowPicker();
            if (selectedId.Length == 0)
            {
                da.SetData(2, "Connect Button → Select; choose a profile. Then connect Profile → Plant Light Context.Profile.");
                SetRevisionMessage("Choose profile"); return;
            }
            var profile = SpectralProfileLibrary.Get(selectedId).Profile;
            da.SetData(0, new SpectralProfileGoo(profile)); da.SetData(1, profile.Factor);
            da.SetData(2, $"{profile.Label}; {profile.Provenance}; {profile.Warning}");
            SetRevisionMessage(profile.Label);
            // The user acknowledged these limitations in the picker; retain them in Status.
        }
        catch (Exception ex) { da.SetData(2, ex.Message); AddRuntimeMessage(GH_RuntimeMessageLevel.Error, ex.Message); }
    }
    private void ShowPicker()
    {
        using var form = new Form { Text = "FlahaGrow — Spectral Profile Library", Width = 1050, Height = 640, StartPosition = FormStartPosition.CenterScreen };
        var table = new DataGridView { Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false, MultiSelect = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect, RowHeadersVisible = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill };
        table.Columns.Add("Category", "Category"); table.Columns.Add("Reference", "Reference (not fixture certification)"); table.Columns.Add("Factor", "µmol/m²/s per lux");
        var entries = SpectralProfileLibrary.Profiles;
        foreach (var item in entries) table.Rows.Add(item.Category, item.Profile.Label, item.Profile.Factor.ToString("G10", System.Globalization.CultureInfo.InvariantCulture));
        var detail = new TextBox { Dock = DockStyle.Bottom, Height = 150, Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical };
        var accept = new CheckBox { Dock = DockStyle.Bottom, Height = 40, Text = "I accept the selected reference's limitations, including zero unmeasured tails where stated." };
        var use = new Button { Dock = DockStyle.Bottom, Height = 36, Text = "Use selected profile", Enabled = false };
        void UpdateDetails()
        {
            accept.Checked = false;
            if (table.CurrentRow is null) return;
            var p = entries[table.CurrentRow.Index].Profile;
            detail.Text = p.Provenance + Environment.NewLine + Environment.NewLine + p.Warning;
        }
        table.SelectionChanged += (_, _) => UpdateDetails();
        accept.CheckedChanged += (_, _) => use.Enabled = accept.Checked && table.CurrentRow is not null;
        use.Click += (_, _) => { if (table.CurrentRow is null || !accept.Checked) return; RecordUndoEvent("Select spectral profile"); selectedId = entries[table.CurrentRow.Index].Id; form.DialogResult = DialogResult.OK; };
        form.Controls.Add(table); form.Controls.Add(detail); form.Controls.Add(accept); form.Controls.Add(use);
        var index = entries.ToList().FindIndex(p => p.Id == selectedId);
        table.CurrentCell = table.Rows[Math.Max(0, index)].Cells[0]; UpdateDetails();
        form.ShowDialog();
    }
    public override bool Write(GH_IWriter writer)
    {
        writer.SetString("LibraryProfileId", selectedId);
        writer.SetString("LibraryRevision", SpectralProfileLibrary.Revision);
        return base.Write(writer);
    }
    public override bool Read(GH_IReader reader)
    {
        selectedId = reader.ItemExists("LibraryProfileId") ? reader.GetString("LibraryProfileId") : "";
        if (reader.ItemExists("LibraryRevision") && reader.GetString("LibraryRevision") != SpectralProfileLibrary.Revision) selectedId = "";
        picker.Disarm(); return base.Read(reader);
    }
}
