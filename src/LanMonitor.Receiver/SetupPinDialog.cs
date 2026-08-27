namespace LanMonitor.Receiver;

internal sealed class SetupPinDialog : Form
{
    private readonly TextBox _pin = new();
    private readonly TextBox _confirm = new();
    private readonly ReceiverSettings _settings;

    public SetupPinDialog(ReceiverSettings settings)
    {
        _settings = settings;
        Text = "SF_view · 设置验证码";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        ShowInTaskbar = true;
        ClientSize = new Size(380, 210);
        Font = new Font("Microsoft YaHei UI", 9.75F);
        BackColor = Color.FromArgb(32, 32, 36);
        ForeColor = Color.Gainsboro;

        var tip = new Label
        {
            Text = "首次使用请设置 4～6 位数字验证码。\n最小化后按 Ctrl+Shift+Alt+V 唤出并验证。",
            AutoSize = true,
            Location = new Point(20, 16),
            ForeColor = Color.FromArgb(180, 180, 190)
        };

        var l1 = new Label { Text = "验证码", AutoSize = true, Location = new Point(20, 70), ForeColor = ForeColor };
        StylePin(_pin);
        _pin.Location = new Point(100, 66);
        _pin.Width = 240;

        var l2 = new Label { Text = "确认", AutoSize = true, Location = new Point(20, 110), ForeColor = ForeColor };
        StylePin(_confirm);
        _confirm.Location = new Point(100, 106);
        _confirm.Width = 240;

        var ok = new Button
        {
            Text = "确定",
            Location = new Point(180, 160),
            Width = 80,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(70, 70, 78),
            ForeColor = Color.Gainsboro
        };
        ok.Click += (_, _) => Save();

        var cancel = new Button
        {
            Text = "取消",
            DialogResult = DialogResult.Cancel,
            Location = new Point(270, 160),
            Width = 80,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(70, 70, 78),
            ForeColor = Color.Gainsboro
        };

        AcceptButton = ok;
        CancelButton = cancel;
        Controls.Add(tip);
        Controls.Add(l1);
        Controls.Add(_pin);
        Controls.Add(l2);
        Controls.Add(_confirm);
        Controls.Add(ok);
        Controls.Add(cancel);
    }

    private static void StylePin(TextBox box)
    {
        box.UseSystemPasswordChar = true;
        box.MaxLength = 6;
        box.BackColor = Color.FromArgb(58, 58, 64);
        box.ForeColor = Color.Gainsboro;
        box.BorderStyle = BorderStyle.FixedSingle;
    }

    private void Save()
    {
        if (!PinHasher.IsValidPinFormat(_pin.Text))
        {
            MessageBox.Show(this, "验证码须为 4～6 位数字。", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (_pin.Text != _confirm.Text)
        {
            MessageBox.Show(this, "两次输入不一致。", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        PinHasher.SetPin(_settings, _pin.Text);
        _settings.Save();
        DialogResult = DialogResult.OK;
        Close();
    }
}
