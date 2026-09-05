using System.Drawing;
using System.Windows.Forms;
using Grasshopper.Kernel;

namespace FlahaGrow.Grasshopper.Components;

/// <summary>Shared non-leap-year hour selection state, matching the legacy sticky key.</summary>
internal static class AnnualHourSelection
{
    internal static int? Index;
    internal static string DateTimeLabel = "No date and hour selected.";
}

public abstract class AnnualHourSelectorComponent : GH_Component
{
    protected AnnualHourSelectorComponent(string name, string nick, string description, Guid guid) : base(name, nick, description, "FlahaGrow", "Annual") => Id = guid;
    private Guid Id { get; }
    public override Guid ComponentGuid => Id;
    protected override void RegisterInputParams(GH_InputParamManager p) => p.AddBooleanParameter("Run", "Run", "Set True to open the date and hour selector.", GH_ParamAccess.item, false);
    protected override void RegisterOutputParams(GH_OutputParamManager p) { p.AddIntegerParameter("Selected hour index", "Hour", "0-based non-leap-year index for annual results.", GH_ParamAccess.item); p.AddTextParameter("Selected date and hour", "Date", "Readable selected date and hour.", GH_ParamAccess.item); }
    protected override void SolveInstance(IGH_DataAccess da)
    {
        var run = false; da.GetData(0, ref run);
        if (run)
        {
            using var dialog = new AnnualHourSelectorDialog();
            if (dialog.ShowDialog() == DialogResult.OK) { AnnualHourSelection.Index = dialog.HourIndex; AnnualHourSelection.DateTimeLabel = dialog.SelectedDateTime.ToString("MMMM d, h tt"); }
        }
        if (AnnualHourSelection.Index.HasValue) da.SetData(0, AnnualHourSelection.Index.Value);
        da.SetData(1, AnnualHourSelection.DateTimeLabel);
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
        Text = "Select Date and Hour"; ClientSize = new Size(330, 315); BackColor = Color.White; StartPosition = FormStartPosition.CenterScreen; FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false; MinimizeBox = false;
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
        SelectedDateTime = new DateTime(date.Year, date.Month, date.Day, hourValue, 0, 0); HourIndex = (date.DayOfYear - 1) * 24 + ((hourValue + 23) % 24); DialogResult = DialogResult.OK;
    }
}
