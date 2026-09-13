using System.Drawing;
using System.Windows.Forms;
using Grasshopper.Kernel;
using GH_IO.Serialization;
using FlahaGrow.Core.Operations;
using FlahaGrow.Core.PlantLight;
using FlahaGrow.Grasshopper.Parameters;

namespace FlahaGrow.Grasshopper.Components;

public sealed class SelectHourIndexComponent : FlahaGrowComponent
{
    private int? selectedIndex;
    private readonly ActionLatch picker = new();
    public SelectHourIndexComponent() : base("Select Date and Hour", "Hour Index", "Selects a date and AM/PM hour and returns its annual 0-based hour index (0–8759).", "FlahaGrow", "03 Annual") { }
    public override Guid ComponentGuid => new("a9c4973b-acb7-45be-96ee-a6d8a35fa418");
    protected override void RegisterInputParams(GH_InputParamManager p)
    {
        p.AddBooleanParameter("Run", "Run", "Connect a Button. Opens once on False → True; selected clock hour is the interval start.", GH_ParamAccess.item, false);
        p.AddParameter(new AnnualResultParameter(), "Annual Result", "Result", "Connect Load Annual Result → Result. Location and UTC come from the verified run EPW automatically.", GH_ParamAccess.item); p[1].Optional = true;
    }
    protected override void RegisterOutputParams(GH_OutputParamManager p)
    {
        p.AddIntegerParameter("Selected hour index", "Hour", "Connect to PPFD at Hour.Hour or an illuminance Hour input. 0–8759; interval start, local standard time.", GH_ParamAccess.item);
        p.AddTextParameter("Selected date and hour", "Date", "Display only: connect to a Panel. NOT Plant Light Context.Alignment.", GH_ParamAccess.item);
        p.AddIntegerParameter("Selected day index", "Day", "Connect to DLI for Day.Day. 0–364; Jan 1 = 0. Equals floor(Hour / 24).", GH_ParamAccess.item);
        p.AddTextParameter("Annual weather alignment", "Alignment", "Inherited from Result weather. Normally Context inherits it directly from Result too. Does not certify unrelated electric schedules.", GH_ParamAccess.item);
    }
    protected override void SolveInstance(IGH_DataAccess da)
    {
        var run = false; da.GetData(0, ref run);
        var result = new AnnualResultGoo();
        if (da.GetData(1, ref result) && result.IsValid)
        {
            if (result.Value.Weather is { } weather) { da.SetData(3, weather.Alignment); SetRevisionMessage(weather.Location); }
            else AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, result.Value.WeatherStatus);
        }
        else AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "Connect Load Annual Result → Result for automatic location/UTC. Unconnected selection is an index-only 365-day calendar.");
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
