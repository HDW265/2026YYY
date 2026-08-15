using LanMonitor.Core;

namespace LanMonitor.Receiver;

internal sealed class AllowIpDialog : Form
{
    private readonly AllowIpPolicy _policy;
    private readonly RadioButton _radioAll = new();
    private readonly RadioButton _radioList = new();
    private readonly CheckedListBox _list = new();

    public AllowIpDialog(AllowIpPolicy policy)
    {
        _policy = policy;
        Text = "允许的客户";
        AutoScaleMode = AutoScaleMode.Dpi;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        Font = new Font("Microsoft YaHei UI", 9.75F);
        BackColor = Color.FromArgb(32, 32, 36);
        ForeColor = Color.Gainsboro;
        ClientSize = new Size(420, 440);
        MinimumSize = new Size(420, 440);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            Padding = new Padding(16, 14, 16, 14)
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));

        _radioAll.Text = "全部放行";
        _radioAll.AutoSize = true;
        _radioAll.ForeColor = ForeColor;
        _radioAll.Checked = policy.AllowAll;
        _radioAll.Margin = new Padding(0, 0, 0, 6);
        _radioAll.CheckedChanged += (_, _) => SyncListEnabled();

        _radioList.Text = "仅以下 IP（勾选允许）";
        _radioList.AutoSize = true;
        _radioList.ForeColor = ForeColor;
        _radioList.Checked = !policy.AllowAll;
        _radioList.Margin = new Padding(0, 0, 0, 8);
        _radioList.CheckedChanged += (_, _) => SyncListEnabled();

        _list.Dock = DockStyle.Fill;
        _list.CheckOnClick = true;
        _list.BackColor = Color.FromArgb(58, 58, 64);
        _list.ForeColor = Color.WhiteSmoke;
        _list.BorderStyle = BorderStyle.FixedSingle;
        _list.IntegralHeight = false;

        var known = policy.KnownIps.ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var ip in policy.AllowedIps)
        {
            known.Add(ip);
        }

        foreach (var ip in known.OrderBy(x => x))
        {
            var isChecked = policy.AllowedIps.Any(a => string.Equals(a, ip, StringComparison.OrdinalIgnoreCase));
            if (policy.AllowAll && policy.AllowedIps.Count == 0)
            {
                isChecked = false;
            }

            _list.Items.Add(ip, isChecked);
        }

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false
        };
        var cancel = MakeBtn("取消");
        cancel.DialogResult = DialogResult.Cancel;
        var ok = MakeBtn("确定");
        ok.Click += (_, _) =>
        {
            ApplyToPolicy();
            DialogResult = DialogResult.OK;
            Close();
        };
        buttons.Controls.Add(cancel);
        buttons.Controls.Add(ok);

        root.Controls.Add(_radioAll, 0, 0);
        root.Controls.Add(_radioList, 0, 1);
        root.Controls.Add(_list, 0, 2);
        root.Controls.Add(buttons, 0, 3);
        Controls.Add(root);
        AcceptButton = ok;
        CancelButton = cancel;
        SyncListEnabled();
    }

    private void SyncListEnabled() => _list.Enabled = _radioList.Checked;

    private void ApplyToPolicy()
    {
        _policy.AllowAll = _radioAll.Checked;
        var selected = new List<string>();
        for (var i = 0; i < _list.Items.Count; i++)
        {
            var ip = _list.Items[i]?.ToString() ?? "";
            _policy.RememberIp(ip);
            if (_list.GetItemChecked(i))
            {
                selected.Add(ip);
            }
        }

        _policy.SetAllowed(selected);
    }

    private static Button MakeBtn(string text) => new()
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
