from System.Windows.Forms import (
    Form, MonthCalendar, Panel, Label, ComboBox, Button,
    RadioButton, FormBorderStyle, FormStartPosition, ComboBoxStyle,
    MessageBox
)
from System.Drawing import Point, Size, Font, FontStyle, ContentAlignment, Color
from System import DateTime
import scriptcontext as sc
import Rhino

# Sticky key
key = 'selected_hour_index'

if _run:

    class CalendarForm(Form):
        def __init__(self):
            Form.__init__(self)
            
            self.Text = "Select Date and Hour"
            self.Size = Size(353, 420)
            self.BackColor = Color.White
            self.StartPosition = self.StartPosition.CenterScreen
            self.FormBorderStyle = self.FormBorderStyle.FixedDialog
            self.MaximizeBox = False
            self.MinimizeBox = False

            # Calendar
            self.calendar = MonthCalendar()
            self.calendar.Location = Point(10, 10)
            self.calendar.MaxSelectionCount = 1
            self.calendar.ShowToday = False
            self.calendar.ShowTodayCircle = False
            self.Controls.Add(self.calendar)

            # month label
            self.calendar.DateChanged += self.update_month_label
            # Block Feb 29
            self.calendar.DateChanged += self.block_feb_29

            # YEAR
            self.header_cover = Panel()
            self.header_cover.Size = Size(200, 30)
            self.header_cover.Location = Point(70, 35)
            self.header_cover.BackColor = Color.White
            self.Controls.Add(self.header_cover)
            self.header_cover.BringToFront()

            # Custom Label month
            self.month_label = Label()
            self.month_label.Text = self.calendar.SelectionStart.ToString("MMMMMMM")
            self.month_label.Location = Point(90, 32)
            self.month_label.Size = Size(150, 30)
            self.month_label.Font = Font("Segoe UI", 10, FontStyle.Bold)
            self.month_label.BackColor = Color.White
            self.month_label.TextAlign = ContentAlignment.MiddleCenter
            self.Controls.Add(self.month_label)
            self.month_label.BringToFront()

            # Hour Label
            self.hour_label = Label()
            self.hour_label.Text = "Hour:"
            self.hour_label.Location = Point(20, 270)
            self.Controls.Add(self.hour_label)

            # Hour Dropdown
            self.hour_combo = ComboBox()
            self.hour_combo.Location = Point(120, 265)
            self.hour_combo.Size = Size(200, 24)
            self.hour_combo.DropDownStyle = ComboBoxStyle.DropDownList
            for i in range(1, 13):
                self.hour_combo.Items.Add(str(i))
            self.hour_combo.SelectedIndex = 0
            self.Controls.Add(self.hour_combo)

            # AM Radio
            self.am_radio = RadioButton()
            self.am_radio.Text = "AM"
            self.am_radio.Font = Font("Segoe UI", 7)
            self.am_radio.Location = Point(120, 300)
            self.am_radio.Checked = True
            self.Controls.Add(self.am_radio)

            # PM Radio
            self.pm_radio = RadioButton()
            self.pm_radio.Text = "PM"
            self.pm_radio.Font = Font("Segoe UI", 7)
            self.pm_radio.Location = Point(230, 300)
            self.Controls.Add(self.pm_radio)

            # OK Button
            self.ok_button = Button()
            self.ok_button.Text = "OK"
            self.ok_button.Size = Size(310, 30)
            self.ok_button.Location = Point(10, 330)
            self.ok_button.Click += self.on_ok_click
            self.Controls.Add(self.ok_button)

        def update_month_label(self, sender, args):
            self.month_label.Text = self.calendar.SelectionStart.ToString("MMMM")

        # prevent selecting Feb 29
        def block_feb_29(self, sender, args):
            d = self.calendar.SelectionStart
            if d.Month == 2 and d.Day == 29:
                MessageBox.Show("February 29 is not supported in this workflow.", "Invalid date")
                safe = DateTime(d.Year, 2, 28)
                self.calendar.SetDate(safe)

        def on_ok_click(self, sender, args):
            date = self.calendar.SelectionStart

            # Validate
            if date.Month == 2 and date.Day == 29:
                MessageBox.Show("February 29 is not supported in this workflow.", "Invalid date")
                return

            hour = int(self.hour_combo.SelectedItem)
            if self.pm_radio.Checked and hour != 12:
                hour += 12
            if self.am_radio.Checked and hour == 12:
                hour = 0

            dt = DateTime(date.Year, date.Month, date.Day, hour, 0, 0)
            # Map hours
            h = dt.Hour  # 0..23
            offset_hour = (h + 23) % 24
            index = (dt.DayOfYear - 1) * 24 + offset_hour
            sc.sticky[key] = index
            self.Close()

    form = CalendarForm()
    form.ShowDialog()

# Output
hour_index = sc.sticky.get(key, None)
