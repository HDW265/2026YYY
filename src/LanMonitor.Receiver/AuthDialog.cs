namespace LanMonitor.Receiver;

/// <summary>通用密码验证对话框（启动登录 / 托盘打开 / 退出）。</summary>
internal sealed class AuthDialog : Form
{
    private readonly TextBox _password = new();
    private readonly Func<string, bool> _verify;

    public AuthDialog(string title, string prompt, Func<string, bool> verify)
    {
        _verify = verify;
        Text = title;
        AutoScaleMode = AutoScaleMode.Dpi;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        ShowInTaskbar = true;
        Font = new Font("Microsoft YaHei UI", 9.75F);
        BackColor = Color.FromArgb(32, 32, 36);
        ForeColor = Color.Gainsboro;
        MinimumSize = new Size(380, 200);
        ClientSize = new Size(380, 200);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(20, 16, 20, 16)
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));

        var label = new Label
        {
            Text = prompt,
            AutoSize = true,
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 0, 10)
        };

        _password.Dock = DockStyle.Fill;
        _password.UseSystemPasswordChar = true;
        _password.BackColor = Color.FromArgb(58, 58, 64);
        _password.ForeColor = Color.Gainsboro;
        _password.BorderStyle = BorderStyle.FixedSingle;
        _password.Margin = new Padding(0, 0, 0, 8);

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false
        };
        var cancel = MakeButton("取消");
        cancel.DialogResult = DialogResult.Cancel;
        var ok = MakeButton("确定");
        ok.Click += (_, _) => OnOk();
        buttons.Controls.Add(cancel);
        buttons.Controls.Add(ok);

        root.Controls.Add(label, 0, 0);
        root.Controls.Add(_password, 0, 1);
        root.Controls.Add(buttons, 0, 2);
        Controls.Add(root);
        AcceptButton = ok;
        CancelButton = cancel;
    }

    private void OnOk()
    {
        if (_verify(_password.Text))
        {
            DialogResult = DialogResult.OK;
            Close();
            return;
        }

        MessageBox.Show(this, "密码错误。", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        _password.SelectAll();
        _password.Focus();
    }

    private static Button MakeButton(string text) => new()
    {
        Text = text,
        Width = 88,
        Height = 34,
        Margin = new Padding(8, 4, 0, 4),
        FlatStyle = FlatStyle.Flat,
        BackColor = Color.FromArgb(70, 70, 78),
        ForeColor = Color.Gainsboro,
        FlatAppearance = { BorderSize = 0 }
    };
}
