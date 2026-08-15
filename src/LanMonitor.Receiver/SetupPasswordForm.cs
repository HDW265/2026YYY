namespace LanMonitor.Receiver;

/// <summary>首次运行设置管理密码。</summary>
internal sealed class SetupPasswordForm : Form
{
    private readonly TextBox _password = new();
    private readonly TextBox _password2 = new();

    public ReceiverSettings Result { get; private set; } = new();

    public SetupPasswordForm()
    {
        Text = "SF_view 首次设置";
        Icon = AppIcon.Resolve();
        AutoScaleMode = AutoScaleMode.Dpi;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Microsoft YaHei UI", 9.75F);
        BackColor = Color.FromArgb(32, 32, 36);
        ForeColor = Color.Gainsboro;
        ClientSize = new Size(420, 260);
        MinimumSize = new Size(420, 260);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 5,
            Padding = new Padding(20, 16, 20, 16)
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));

        var hint = new Label
        {
            Text = "设置管理密码。之后启动、从托盘打开、退出均需验证。",
            AutoSize = true,
            Dock = DockStyle.Fill,
            ForeColor = Color.FromArgb(180, 180, 190)
        };
        root.Controls.Add(hint, 0, 0);
        root.SetColumnSpan(hint, 2);

        AddLabeled(root, 1, "管理密码", StyleBox(_password));
        AddLabeled(root, 2, "确认密码", StyleBox(_password2));

        var tip = new Label
        {
            Text = "配置：%AppData%\\SF_view · 关窗隐藏到托盘",
            Dock = DockStyle.Fill,
            ForeColor = Color.FromArgb(140, 140, 150)
        };
        root.Controls.Add(tip, 0, 3);
        root.SetColumnSpan(tip, 2);

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false
        };
        var cancel = MakeButton("退出");
        cancel.DialogResult = DialogResult.Cancel;
        var save = MakeButton("保存并启动");
        save.Width = 120;
        save.Click += (_, _) => OnSave();
        buttons.Controls.Add(cancel);
        buttons.Controls.Add(save);
        root.Controls.Add(buttons, 0, 4);
        root.SetColumnSpan(buttons, 2);

        Controls.Add(root);
        AcceptButton = save;
        CancelButton = cancel;
    }

    private void OnSave()
    {
        if (string.IsNullOrEmpty(_password.Text))
        {
            MessageBox.Show(this, "必须设置管理密码。", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (_password.Text != _password2.Text)
        {
            MessageBox.Show(this, "两次密码不一致。", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var settings = new ReceiverSettings();
        settings.SetPassword(_password.Text);
        settings.Save();
        Result = settings;
        DialogResult = DialogResult.OK;
        Close();
    }

    private static void AddLabeled(TableLayoutPanel root, int row, string caption, Control input)
    {
        var label = new Label
        {
            Text = caption,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft
        };
        input.Dock = DockStyle.Fill;
        input.Margin = new Padding(8, 4, 0, 4);
        root.Controls.Add(label, 0, row);
        root.Controls.Add(input, 1, row);
    }

    private static TextBox StyleBox(TextBox box)
    {
        box.UseSystemPasswordChar = true;
        box.BackColor = Color.FromArgb(58, 58, 64);
        box.ForeColor = Color.Gainsboro;
        box.BorderStyle = BorderStyle.FixedSingle;
        return box;
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
