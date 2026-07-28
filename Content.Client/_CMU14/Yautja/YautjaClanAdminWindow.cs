using System;
using System.Linq;
using System.Numerics;
using Content.Client._RMC14.UserInterface;
using Content.Shared._CMU14.Yautja;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;

namespace Content.Client._CMU14.Yautja;

public sealed class YautjaClanAdminWindow : DefaultWindow
{
    private readonly LineEdit _clanName;
    private readonly LineEdit _clanDescription;
    private readonly LineEdit _clanColor;
    private readonly Label _clanFormHeader;
    private readonly Button _submitClan;
    private readonly Button _cancelClan;
    private readonly LineEdit _player;
    private readonly LineEdit _clanId;
    private readonly OptionButton _membershipRank;
    private readonly OptionButton _rank;
    private readonly OptionButton _whitelist;
    private readonly BoxContainer _clans;
    private readonly Label _inspection;
    private readonly Label _status;
    private readonly YautjaClanAdminEditorState _editor = new();
    private ConfirmationWindow? _deleteConfirmation;

    private static readonly YautjaRank[] PersistentRanks =
        YautjaClanPolicy.GetNormalAssignableRanks().Append(YautjaRank.Ancient).ToArray();

    public event Action? OnRefresh;
    public event Action<string, string, string>? OnCreateClan;
    public event Action<int, string, string, string>? OnUpdateClan;
    public event Action<int>? OnDeleteClan;
    public event Action<string, string, YautjaRank>? OnSetMembership;
    public event Action<string, YautjaRank>? OnSetRank;
    public event Action<string, YautjaWhitelistFlags>? OnSetWhitelist;
    public event Action<string>? OnInspect;

    public YautjaClanAdminWindow()
    {
        Title = Loc.GetString("cmu-yautja-clan-admin-title");
        SetSize = MinSize = new Vector2(820, 700);

        var root = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            SeparationOverride = 8,
            Margin = new Thickness(8),
            HorizontalExpand = true,
            VerticalExpand = true,
        };
        Contents.AddChild(root);

        _clanFormHeader = CreateHeader("cmu-yautja-clan-admin-create-header");
        root.AddChild(_clanFormHeader);
        _clanName = CreateLineEdit("cmu-yautja-clan-admin-name");
        root.AddChild(_clanName);
        _clanDescription = CreateLineEdit("cmu-yautja-clan-admin-description");
        root.AddChild(_clanDescription);
        _clanColor = CreateLineEdit("cmu-yautja-clan-admin-color");
        root.AddChild(_clanColor);
        _submitClan = new Button();
        _submitClan.OnPressed += _ => SubmitClan();
        root.AddChild(_submitClan);
        _cancelClan = new Button { Text = Loc.GetString("cmu-yautja-clan-admin-cancel") };
        _cancelClan.OnPressed += _ =>
        {
            _editor.Cancel();
            SyncEditorControls();
        };
        root.AddChild(_cancelClan);
        SyncEditorControls();

        root.AddChild(CreateHeader("cmu-yautja-clan-admin-player-header"));
        _player = CreateLineEdit("cmu-yautja-clan-admin-player");
        root.AddChild(_player);

        var membership = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            SeparationOverride = 6,
            HorizontalExpand = true,
        };
        _clanId = CreateLineEdit("cmu-yautja-clan-admin-clan-id");
        membership.AddChild(_clanId);
        _membershipRank = CreateRankOption();
        membership.AddChild(_membershipRank);
        var setMembership = new Button { Text = Loc.GetString("cmu-yautja-clan-admin-set-membership") };
        setMembership.OnPressed += _ => OnSetMembership?.Invoke(
            _player.Text,
            string.IsNullOrWhiteSpace(_clanId.Text) ? "none" : _clanId.Text,
            (YautjaRank) _membershipRank.SelectedId);
        membership.AddChild(setMembership);
        root.AddChild(membership);

        var rankRow = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            SeparationOverride = 6,
            HorizontalExpand = true,
        };
        _rank = CreateRankOption();
        rankRow.AddChild(_rank);
        var setRank = new Button { Text = Loc.GetString("cmu-yautja-clan-admin-set-rank") };
        setRank.OnPressed += _ => OnSetRank?.Invoke(_player.Text, (YautjaRank) _rank.SelectedId);
        rankRow.AddChild(setRank);
        _whitelist = new OptionButton { HorizontalExpand = true };
        AddWhitelistOptions(_whitelist);
        rankRow.AddChild(_whitelist);
        var setWhitelist = new Button { Text = Loc.GetString("cmu-yautja-clan-admin-set-whitelist") };
        setWhitelist.OnPressed += _ => OnSetWhitelist?.Invoke(_player.Text, (YautjaWhitelistFlags) _whitelist.SelectedId);
        rankRow.AddChild(setWhitelist);
        var inspect = new Button { Text = Loc.GetString("cmu-yautja-clan-admin-inspect") };
        inspect.OnPressed += _ => OnInspect?.Invoke(_player.Text);
        rankRow.AddChild(inspect);
        root.AddChild(rankRow);

        _inspection = new Label { HorizontalExpand = true };
        root.AddChild(_inspection);

        root.AddChild(CreateHeader("cmu-yautja-clan-admin-clans-header"));
        var scroll = new ScrollContainer
        {
            VerticalExpand = true,
            HorizontalExpand = true,
            HScrollEnabled = false,
        };
        _clans = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            SeparationOverride = 4,
            HorizontalExpand = true,
        };
        scroll.AddChild(_clans);
        root.AddChild(scroll);

        var bottom = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            SeparationOverride = 6,
            HorizontalExpand = true,
        };
        _status = new Label { HorizontalExpand = true };
        bottom.AddChild(_status);
        var refresh = new Button { Text = Loc.GetString("cmu-yautja-clan-admin-refresh") };
        refresh.OnPressed += _ => OnRefresh?.Invoke();
        bottom.AddChild(refresh);
        root.AddChild(bottom);
    }

    public void UpdateState(YautjaClanAdminEuiState state)
    {
        _editor.CaptureDraft(_clanName.Text, _clanDescription.Text, _clanColor.Text);
        _editor.ApplyState(state);
        SyncEditorControls();

        _inspection.Text = string.IsNullOrWhiteSpace(state.InspectedSummary)
            ? Loc.GetString("cmu-yautja-clan-admin-no-inspection")
            : $"{state.InspectedPlayer}: {state.InspectedSummary}";
        _status.Text = state.StatusMessage;

        _clans.RemoveAllChildren();
        foreach (var clan in state.Clans)
        {
            var row = new BoxContainer
            {
                Orientation = BoxContainer.LayoutOrientation.Horizontal,
                SeparationOverride = 6,
                HorizontalExpand = true,
            };
            row.AddChild(new Label
            {
                Text = Loc.GetString(
                    "cmu-yautja-clan-admin-clan-row",
                    ("id", clan.Id),
                    ("name", clan.Name),
                    ("members", clan.Members),
                    ("honor", clan.Honor),
                    ("color", clan.Color)),
                HorizontalExpand = true,
            });
            var edit = new Button { Text = Loc.GetString("cmu-yautja-clan-admin-edit") };
            edit.OnPressed += _ =>
            {
                _editor.BeginEdit(clan);
                SyncEditorControls();
            };
            row.AddChild(edit);
            var delete = new Button { Text = Loc.GetString("cmu-yautja-clan-admin-delete") };
            delete.OnPressed += _ => OpenDeleteConfirmation(clan);
            row.AddChild(delete);
            _clans.AddChild(row);
        }
    }

    [System.Obsolete]
    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _deleteConfirmation?.Close();

        base.Dispose(disposing);
    }

    private void SubmitClan()
    {
        if (_editor.EditingClanId is { } clanId)
        {
            OnUpdateClan?.Invoke(clanId, _clanName.Text, _clanDescription.Text, _clanColor.Text);
            return;
        }

        OnCreateClan?.Invoke(_clanName.Text, _clanDescription.Text, _clanColor.Text);
    }

    private void SyncEditorControls()
    {
        _clanName.Text = _editor.Name;
        _clanDescription.Text = _editor.Description;
        _clanColor.Text = _editor.Color;
        _clanFormHeader.Text = Loc.GetString(_editor.IsEditing
            ? "cmu-yautja-clan-admin-edit-header"
            : "cmu-yautja-clan-admin-create-header");
        _submitClan.Text = Loc.GetString(_editor.IsEditing
            ? "cmu-yautja-clan-admin-save"
            : "cmu-yautja-clan-admin-create");
        _cancelClan.Visible = _editor.IsEditing;
    }

    private void OpenDeleteConfirmation(YautjaClanAdminClanState clan)
    {
        _deleteConfirmation?.Close();

        var confirmation = new ConfirmationWindow();
        _deleteConfirmation = confirmation;
        confirmation.Setup(
            Loc.GetString("cmu-yautja-clan-admin-delete-title"),
            Loc.GetString("cmu-yautja-clan-admin-delete-text", ("name", clan.Name)),
            Loc.GetString("cmu-yautja-clan-admin-delete-accept"),
            Loc.GetString("cmu-yautja-clan-admin-delete-deny"));
        confirmation.OnClose += () =>
        {
            if (_deleteConfirmation == confirmation)
                _deleteConfirmation = null;
        };
        confirmation.DenyButton.OnPressed += _ => confirmation.Close();
        confirmation.AcceptButton.OnPressed += _ =>
        {
            OnDeleteClan?.Invoke(clan.Id);
            confirmation.Close();
        };
        confirmation.OpenCentered();
    }

    private static Label CreateHeader(string localization)
    {
        return new Label
        {
            Text = Loc.GetString(localization),
            FontColorOverride = Color.LightGray,
            HorizontalExpand = true,
        };
    }

    private static LineEdit CreateLineEdit(string localization)
    {
        return new LineEdit
        {
            PlaceHolder = Loc.GetString(localization),
            HorizontalExpand = true,
        };
    }

    private static OptionButton CreateRankOption()
    {
        var option = new OptionButton { HorizontalExpand = true };
        foreach (var rank in PersistentRanks)
        {
            option.AddItem(Loc.GetString(YautjaRankMetadata.For(rank).LocalizedName), (int) rank);
        }

        option.SelectId((int) YautjaRank.Blooded);
        return option;
    }

    private static void AddWhitelistOptions(OptionButton option)
    {
        option.AddItem(Loc.GetString("cmu-yautja-clan-admin-whitelist-none"), (int) YautjaWhitelistFlags.None);
        option.AddItem(Loc.GetString("cmu-yautja-clan-admin-whitelist-yautja"), (int) YautjaWhitelistFlags.Yautja);
        option.AddItem(Loc.GetString("cmu-yautja-clan-admin-whitelist-council"), (int) YautjaWhitelistFlags.Council);
        option.AddItem(Loc.GetString("cmu-yautja-clan-admin-whitelist-leader"), (int) YautjaWhitelistFlags.Leader);
        option.SelectId((int) YautjaWhitelistFlags.None);
    }
}
