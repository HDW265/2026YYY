namespace LanMonitor.Sender;

internal sealed class PasswordDialog : Form
{
    private readonly TextBox _password = new();
    private readonly SenderSettings _settings;

    public PasswordDialog(SenderSettings settings)
    {
        _settings = settings;
        Text = "验证密码";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        ShowInTaskbar = true;
        ClientSize = new Size(360, 140);
        Font = new Font("Microsoft YaHei UI", 9.75F);
        BackColor = Color.FromArgb(32, 32, 36);
        ForeColor = Color.Gainsboro;

        var label = new Label
        {
            Text = "输入管理密码以打开设置",
            AutoSize = true,
            Location = new Point(20, 18)
        };
        _password.Location = new Point(20, 48);
        _password.Width = 310;
        _password.UseSystemPasswordChar = true;
        _password.BackColor = Color.FromArgb(58, 58, 64);
        _password.ForeColor = Color.Gainsboro;
        _password.BorderStyle = BorderStyle.FixedSingle;

        var ok = new Button
        {
            Text = "确定",
            DialogResult = DialogResult.None,
            Location = new Point(160, 90),
            Width = 80
        };
        ok.Click += (_, _) =>
        {
            if (PasswordHasher.Verify(_settings, _password.Text))
            {
                DialogResult = DialogResult.OK;
                Close();
            }
            else
            {
                MessageBox.Show(this, "密码错误。", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _password.SelectAll();
                _password.Focus();
            }
        };

        var cancel = new Button
        {
            Text = "取消",
            DialogResult = DialogResult.Cancel,
            Location = new Point(250, 90),
            Width = 80
        };

        AcceptButton = ok;
        CancelButton = cancel;
        Controls.Add(label);
        Controls.Add(_password);
        Controls.Add(ok);
        Controls.Add(cancel);
    }
}
