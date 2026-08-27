using LanMonitor.Core;

namespace LanMonitor.Receiver;

internal sealed class AllowIpDialog : Form
{
    private readonly AllowIpPolicy _policy;
    private readonly RadioButton _radioAll = new();
    private readonly RadioButton _radioList = new();
    private readonly CheckedListBox _list = new();
    private readonly TextBox _addBox = new();
    private readonly Label _hint = new();

    public AllowIpDialog(AllowIpPolicy policy)
    {
        _policy = policy;
        Text = "允许连接";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(420, 440);
        Font = new Font("Microsoft YaHei UI", 9.75F);
        BackColor = Color.FromArgb(32, 32, 36);
        ForeColor = Color.Gainsboro;

        _radioAll.Text = "全部放行";
        _radioAll.AutoSize = true;
        _radioAll.Location = new Point(20, 16);
        _radioAll.ForeColor = ForeColor;
        _radioAll.Checked = policy.AllowAll;

        _radioList.Text = "仅以下 IP（勾选允许）";
        _radioList.AutoSize = true;
        _radioList.Location = new Point(140, 16);
        _radioList.ForeColor = ForeColor;
        _radioList.Checked = !policy.AllowAll;
        _radioAll.CheckedChanged += (_, _) => SyncListMode();
        _radioList.CheckedChanged += (_, _) => SyncListMode();

        _list.Location = new Point(20, 48);
        _list.Size = new Size(380, 220);
        _list.CheckOnClick = true;
        _list.BackColor = Color.FromArgb(58, 58, 64);
        _list.ForeColor = Color.Gainsboro;
        _list.BorderStyle = BorderStyle.FixedSingle;

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

        _addBox.Location = new Point(20, 280);
        _addBox.Width = 220;
        _addBox.BackColor = Color.FromArgb(58, 58, 64);
        _addBox.ForeColor = Color.Gainsboro;
        _addBox.BorderStyle = BorderStyle.FixedSingle;
        _addBox.PlaceholderText = "例如 192.168.3.10";

        var addBtn = MakeBtn("加入", 250, 278);
        addBtn.Click += (_, _) => AddIp();
        var delBtn = MakeBtn("删除所选", 330, 278);
        delBtn.Click += (_, _) => RemoveSelected();

        _hint.Text = "全部放行时也可手工加入 IP，便于日后切换白名单。";
        _hint.AutoSize = false;
        _hint.Location = new Point(20, 318);
        _hint.Size = new Size(380, 36);
        _hint.ForeColor = Color.FromArgb(150, 150, 160);

        var ok = MakeBtn("确定", 230, 380);
        ok.Width = 80;
        ok.Click += (_, _) =>
        {
            ApplyToPolicy();
            DialogResult = DialogResult.OK;
            Close();
        };
        var cancel = MakeBtn("取消", 320, 380);
        cancel.Width = 80;
        cancel.DialogResult = DialogResult.Cancel;

        Controls.Add(_radioAll);
        Controls.Add(_radioList);
        Controls.Add(_list);
        Controls.Add(_addBox);
        Controls.Add(addBtn);
        Controls.Add(delBtn);
        Controls.Add(_hint);
        Controls.Add(ok);
        Controls.Add(cancel);
        AcceptButton = ok;
        CancelButton = cancel;
        SyncListMode();
    }

    private void SyncListMode()
    {
        var whitelist = _radioList.Checked;
        _hint.Text = whitelist
            ? "白名单模式：仅勾选的 IPv4 可连接。"
            : "全部放行时也可手工加入 IP，便于日后切换白名单。";
    }

    private void AddIp()
    {
        var ip = _addBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(ip))
        {
            return;
        }

        if (!IpFilter.IsValidIpv4(ip))
        {
            MessageBox.Show(this, "请输入有效的 IPv4 地址，例如 192.168.1.10。", "IP 无效", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        for (var i = 0; i < _list.Items.Count; i++)
        {
            if (string.Equals(_list.Items[i]?.ToString(), ip, StringComparison.OrdinalIgnoreCase))
            {
                if (_radioList.Checked)
                {
                    _list.SetItemChecked(i, true);
                }

                _addBox.Clear();
                return;
            }
        }

        _list.Items.Add(ip, _radioList.Checked);
        _addBox.Clear();
    }

    private void RemoveSelected()
    {
        for (var i = _list.CheckedIndices.Count - 1; i >= 0; i--)
        {
            _list.Items.RemoveAt(_list.CheckedIndices[i]);
        }
    }

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

    private static Button MakeBtn(string text, int x, int y) => new()
    {
        Text = text,
        Location = new Point(x, y),
        Width = 70,
        Height = 30,
        FlatStyle = FlatStyle.Flat,
        BackColor = Color.FromArgb(70, 70, 78),
        ForeColor = Color.Gainsboro
    };
}
