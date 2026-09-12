using System.Drawing;
using System.Windows.Forms;
using Grasshopper.Kernel;
using GH_IO.Serialization;
using FlahaGrow.Core.Operations;
using FlahaGrow.Core.PlantLight;

namespace FlahaGrow.Grasshopper.Components;

public abstract class AnnualHourSelectorComponent : FlahaGrowComponent
{
    private int? selectedIndex;
    private readonly ActionLatch picker = new();
    protected AnnualHourSelectorComponent(string name, string nick, string description, Guid guid) : base(name, nick, description, "FlahaGrow", "03 Annual") => Id = guid;
    private Guid Id { get; }
    public override Guid ComponentGuid => Id;
    protected override void RegisterInputParams(GH_InputParamManager p)
    {
        p.AddBooleanParameter("Run", "Run", "Connect a Button. Opens once on False → True; selected clock hour is the interval start.", GH_ParamAccess.item, false);
        p.AddIntegerParameter("Simulation UTC offset (minutes)", "UTC min", "Optional: actual simulation/weather-file standard-time UTC offset in minutes, e.g. +180 for UTC+03:00. Enables Alignment output. Not inferred from the computer/date. Verify mixed-source schedules share the same 365-day Jan–Dec axis, without DST.", GH_ParamAccess.item);
        p[1].Optional = true;
    }
    protected override void RegisterOutputParams(GH_OutputParamManager p)
    {
        p.AddIntegerParameter("Selected hour index", "Hour", "Connect to PPFD at Hour.Hour or an illuminance Hour input. 0–8759; interval start, local standard time.", GH_ParamAccess.item);
        p.AddTextParameter("Selected date and hour", "Date", "Display only: connect to a Panel. NOT Plant Light Context.Alignment.", GH_ParamAccess.item);
        p.AddIntegerParameter("Selected day index", "Day", "Connect to DLI for Day.Day. 0–364; Jan 1 = 0. Equals floor(Hour / 24).", GH_ParamAccess.item);
        p.AddTextParameter("Annual alignment declaration", "Alignment", "Connect to Plant Light Context.Alignment for each mixed source. Requires explicit simulation UTC offset. Whole annual axis, not chosen date. User declaration, not verified metadata.", GH_ParamAccess.item);
    }
    protected override void SolveInstance(IGH_DataAccess da)
    {
        var run = false; da.GetData(0, ref run);
        int utcMinutes = 0;
        if (da.GetData(1, ref utcMinutes))
        {
            try { da.SetData(3, AnnualTime.Alignment(utcMinutes)); }
            catch (ArgumentException ex) { AddRuntimeMessage(GH_RuntimeMessageLevel.Error, ex.Message); return; }
        }
        if (picker.Observe(run))
        {
            using var dialog = new AnnualHourSelectorDialog();
            if (dialog.ShowDialog() == DialogResult.OK) { RecordUndoEvent("Select annual interval"); selectedIndex = dialog.HourIndex; }
        }
        if (selectedIndex.HasValue)
        {
            da.SetData(0, selectedIndex.Value); da.SetData(1, AnnualTime.Label(selectedIndex.Value)); da.SetData(2, selectedIndex.Value / 24);
        }
        else da.SetData(1, "Click Button → Run to select a 365-day date and interval start hour.");
    }
    public override bool Write(GH_IWriter writer)
    {
        if (selectedIndex.HasValue) writer.SetInt32("IntervalStartIndex", selectedIndex.Value);
        return base.Write(writer);
    }
    public override bool Read(GH_IReader reader)
    {
        selectedIndex = reader.ItemExists("IntervalStartIndex") ? reader.GetInt32("IntervalStartIndex") : null;
        if (selectedIndex is < 0 or > 8759) selectedIndex = null;
        picker.Disarm(); return base.Read(reader);
    }
}

public sealed class SelectHourIndexComponent : AnnualHourSelectorComponent
{
    public SelectHourIndexComponent() : base("Select Date and Hour", "Hour Index", "Selects a date and AM/PM hour and returns its annual 0-based hour index (0–8759).", new Guid("31f97f51-692b-43b1-9b45-47f1d4ef2d48")) { }
}

public sealed class SelectPpfdHourComponent : AnnualHourSelectorComponent
{
    public SelectPpfdHourComponent() : base("Select PIT to PPFD", "PPFD Hour", "Selects the annual 0-based hour index for point-in-time PPFD.", new Guid("66090f6d-e92c-4dde-b72f-d85d033ae1f6")) { }
}

internal sealed class AnnualHourSelectorDialog : Form
{
    private readonly MonthCalendar calendar = new() { Location = new Point(10, 10), MaxSelectionCount = 1, ShowToday = false, ShowTodayCircle = false };
    private readonly ComboBox hour = new() { Location = new Point(120, 190), Size = new Size(190, 24), DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly RadioButton am = new() { Text = "AM", Location = new Point(120, 225), Checked = true };
    private readonly RadioButton pm = new() { Text = "PM", Location = new Point(225, 225) };
    internal DateTime SelectedDateTime { get; private set; }
    internal int HourIndex { get; private set; }
    internal AnnualHourSelectorDialog()
    {
        Text = "Select interval START — local standard time"; ClientSize = new Size(400, 315); BackColor = Color.White; StartPosition = FormStartPosition.CenterScreen; FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false; MinimizeBox = false;
        calendar.DateChanged += (_, _) => GuardLeapDay(); Controls.Add(calendar);
        Controls.Add(new Label { Text = "Hour:", Location = new Point(20, 194), AutoSize = true });
        for (var value = 1; value <= 12; value++) hour.Items.Add(value.ToString()); hour.SelectedIndex = 0; Controls.Add(hour); Controls.Add(am); Controls.Add(pm);
        var ok = new Button { Text = "OK", Size = new Size(300, 30), Location = new Point(10, 265) }; ok.Click += (_, _) => Confirm(); Controls.Add(ok);
    }
    private void GuardLeapDay()
    {
        if (calendar.SelectionStart.Month != 2 || calendar.SelectionStart.Day != 29) return;
        MessageBox.Show("February 29 is not supported in this workflow.", "Invalid date"); calendar.SetDate(new DateTime(calendar.SelectionStart.Year, 2, 28));
    }
    private void Confirm()
    {
        var date = calendar.SelectionStart; if (date.Month == 2 && date.Day == 29) { GuardLeapDay(); return; }
        var hourValue = int.Parse(hour.SelectedItem!.ToString()!); if (pm.Checked && hourValue != 12) hourValue += 12; if (am.Checked && hourValue == 12) hourValue = 0;
        SelectedDateTime = new DateTime(2001, date.Month, date.Day, hourValue, 0, 0); HourIndex = AnnualTime.HourIndex(date.Month, date.Day, hourValue); DialogResult = DialogResult.OK;
    }
}
