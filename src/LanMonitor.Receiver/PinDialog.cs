namespace LanMonitor.Receiver;

internal sealed class PinDialog : Form
{
    private readonly TextBox _pin = new();
    private readonly ReceiverSettings _settings;
    private int _failCount;

    public PinDialog(ReceiverSettings settings, string title, string prompt)
    {
        _settings = settings;
        Text = title;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        ShowInTaskbar = true;
        TopMost = true;
        ClientSize = new Size(360, 150);
        Font = new Font("Microsoft YaHei UI", 9.75F);
        BackColor = Color.FromArgb(32, 32, 36);
        ForeColor = Color.Gainsboro;

        var label = new Label
        {
            Text = prompt,
            AutoSize = true,
            Location = new Point(20, 18),
            ForeColor = ForeColor
        };
        _pin.Location = new Point(20, 48);
        _pin.Width = 310;
        _pin.UseSystemPasswordChar = true;
        _pin.MaxLength = 6;
        _pin.BackColor = Color.FromArgb(58, 58, 64);
        _pin.ForeColor = Color.Gainsboro;
        _pin.BorderStyle = BorderStyle.FixedSingle;

        var ok = new Button
        {
            Text = "确定",
            Location = new Point(160, 100),
            Width = 80,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(70, 70, 78),
            ForeColor = Color.Gainsboro
        };
        ok.Click += (_, _) => TryAccept();

        var cancel = new Button
        {
            Text = "取消",
            DialogResult = DialogResult.Cancel,
            Location = new Point(250, 100),
            Width = 80,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(70, 70, 78),
            ForeColor = Color.Gainsboro
        };

        AcceptButton = ok;
        CancelButton = cancel;
        Controls.Add(label);
        Controls.Add(_pin);
        Controls.Add(ok);
        Controls.Add(cancel);
    }

    private void TryAccept()
    {
        if (PinHasher.Verify(_settings, _pin.Text))
        {
            DialogResult = DialogResult.OK;
            Close();
            return;
        }

        _failCount++;
        MessageBox.Show(this, "验证码错误。", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        _pin.SelectAll();
        _pin.Focus();
        if (_failCount >= 3)
        {
            Enabled = false;
            Task.Delay(1500).ContinueWith(_ =>
            {
                try
                {
                    BeginInvoke(() =>
                    {
                        Enabled = true;
                        _pin.Focus();
                    });
                }
                catch
                {
                    /* ignore */
                }
            });
        }
    }
}
