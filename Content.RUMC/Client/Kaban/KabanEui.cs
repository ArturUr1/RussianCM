using System;
using System.Linq;
using Content.Client.Eui;
using Content.Client.Lobby.UI;
using Content.Client.Options.UI;
using Content.Client.Stylesheets;
using Content.Shared._RuCM.Kaban;
using Content.Shared.Eui;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Localization;
using Robust.Shared.Utility;

namespace Content.Client._RuCM.Kaban;

// CMU14: private server-authoritative account UI.
[UsedImplicitly]
public sealed class KabanEui : BaseEui
{
    private readonly KabanWindow _window;
    public KabanEui()
    {
        _window = new(request => SendMessage(request));
        _window.OnClose += () => SendMessage(new CloseEuiMessage());
    }
    public override void Opened() => _window.OpenCentered();
    public override void Closed() => _window.Dispose();
    public override void HandleState(EuiStateBase state)
    {
        if (state is KabanState view)
            _window.Update(view);
    }
}

public sealed class KabanWindow : DefaultWindow
{
    private readonly Action<KabanRequest> _send;
    private readonly BoxContainer _root = Column();
    private readonly BoxContainer _page = Column();
    private readonly BoxContainer _nav = Column();
    private readonly RichTextLabel _feedback = new();
    private KabanState _view = new();
    private string _section = "wallet";
    private Guid _token;
    private KabanRequest? _pending;

    private static string L(string key) => Loc.GetString("cmu-kaban-" + key);
    private static BoxContainer Column() => new() { Orientation = BoxContainer.LayoutOrientation.Vertical, SeparationOverride = 8, HorizontalExpand = true };
    private static BoxContainer Row() => new() { SeparationOverride = 8, HorizontalExpand = true };

    public KabanWindow(Action<KabanRequest> send)
    {
        _send = send;
        Title = L("title");
        MinSize = new(760, 530);
        SetSize = new(1000, 700);
        _root.Margin = new(12);
        _root.VerticalExpand = true;
        var screen = new PanelContainer { HorizontalExpand = true, VerticalExpand = true,
            StyleClasses = { StyleNano.StyleClassCrtScreenPanel } };
        screen.AddChild(_root);
        Contents.AddChild(screen);
        _root.AddChild(_feedback);
        var body = Row();
        body.VerticalExpand = true;
        _root.AddChild(body);
        _nav.MinWidth = 200;
        body.AddChild(_nav);
        var scroll = new ScrollContainer { VerticalExpand = true, HorizontalExpand = true, HScrollEnabled = false };
        scroll.AddChild(_page);
        body.AddChild(scroll);
        Render();
    }

    private static void Text(BoxContainer box, string value)
    {
        var label = new RichTextLabel { HorizontalExpand = true };
        label.SetMessage(FormattedMessage.FromUnformatted(value), StyleNano.CrtGreenSoft);
        box.AddChild(label);
    }
    private static Button Button(BoxContainer box, string text, Action action, bool disabled = false)
    {
        var button = new Button { Text = text, HorizontalExpand = true, MinHeight = 34, Disabled = disabled };
        button.AddStyleClass(StyleNano.StyleClassCrtButton);
        button.OnPressed += _ => action();
        box.AddChild(button);
        return button;
    }
    private static LineEdit Input(BoxContainer box, string placeholder)
    {
        var input = new LineEdit { PlaceHolder = placeholder, HorizontalExpand = true };
        box.AddChild(input);
        return input;
    }
    private static string Date(long ticks) => ticks == 0 ? L("permanent") : new DateTime(ticks, DateTimeKind.Utc).ToLocalTime().ToString("g");
    private string Account(Guid id) => _view.Accounts.GetValueOrDefault(id, id.ToString());

    private LineEdit AccountInput(BoxContainer box)
    {
        var input = Input(box, L("account-id"));
        var accounts = _view.Accounts.OrderBy(a => a.Value).ToArray();
        var select = new OptionButton { HorizontalExpand = true };
        select.AddItem(L("select-account"), 0);
        for (var i = 0; i < accounts.Length; i++)
            select.AddItem(accounts[i].Value, i + 1);
        select.OnItemSelected += e =>
        {
            select.SelectId(e.Id);
            if (e.Id > 0)
                input.Text = accounts[e.Id - 1].Key.ToString();
        };
        box.AddChild(select);
        return input;
    }

    private string ItemText(KabanItemView item)
        => item.Kind != "token" ? item.Description : (item.Flexible ? L("flexible") + " " : "")
            + $"×{item.Multiplier} · " + string.Join(", ", (item.Jobs ?? []).Select(j => _view.Jobs.GetValueOrDefault(j, j)));

    private static string HistoryText(string reason)
    {
        if (reason is "voice-special-grant" or "force-refund" or "privilege-refund")
            return L("history-" + reason);
        if (reason.StartsWith("purchase:", StringComparison.Ordinal))
            return L("product-" + reason[9..]);
        if (reason.StartsWith("award:", StringComparison.Ordinal))
        {
            var parts = reason.Split(':', 3);
            return L("category-" + parts[1]) + ": " + parts[2];
        }
        return reason;
    }

    public void Update(KabanState view)
    {
        _view = view;
        if (_pending != null && view.Response == _pending.Operation && view.Result != "cmu-kaban-storage-unavailable")
            _pending = null;
        _feedback.SetMessage(FormattedMessage.FromUnformatted(view.Result.Length > 0 ? Loc.GetString(view.Result) : ""));
        Render();
    }

    private void Submit(KabanRequest request)
    {
        if (_pending != null)
            return;
        _pending = request;
        _send(request);
        Render();
    }

    private void Render()
    {
        _nav.RemoveAllChildren();
        foreach (var section in new[] { "wallet", "shop", "inventory", "priorities", "forcing", "history", "awards" })
            Button(_nav, L(section), () => { _section = section; Render(); });
        if (_view.Manager)
            Button(_nav, L("admin"), () => { _section = "admin"; Render(); });
        Button(_nav, L("refresh"), () => _send(new KabanRequest { Action = KabanAction.Refresh }));
        _page.RemoveAllChildren();
        if (_pending != null)
        {
            Text(_page, L("pending"));
            Button(_page, L("retry"), () => _send(_pending));
            Text(_page, L("retry-help"));
            return;
        }
        if (_view.Products.Count == 0)
        {
            Text(_page, L("unavailable"));
            return;
        }
        switch (_section)
        {
            case "wallet":
                Text(_page, Loc.GetString("cmu-kaban-balance", ("balance", _view.Balance), ("earned", _view.Earned)));
                Text(_page, L("wallet-rules"));
                break;
            case "shop":
                foreach (var product in _view.Products)
                {
                    var card = Column();
                    var panel = new PanelContainer { Margin = new(0, 2), StyleClasses = { StyleNano.StyleClassCrtScreenPanel } };
                    panel.AddChild(card);
                    _page.AddChild(panel);
                    Text(card, L("product-" + product.Id) + $" — {product.Price} " + L("currency"));
                    Text(card, L("description-" + product.Id));
                    if (product.Blocker.Length > 0)
                        Text(card, Loc.GetString(product.Blocker));
                    Button(card, L("buy"), () => ConfirmPurchase(product), !product.Available || _view.Balance < product.Price);
                }
                break;
            case "inventory":
                foreach (var item in _view.Items)
                {
                    Text(_page, $"{L("kind-" + item.Kind)}: {ItemText(item)}\n{L("status-" + item.Status)} · {Date(item.Expires)}");
                    if (item.Kind == "token")
                        Button(_page, L("manage-token"), () => { _token = item.Id; _section = "priorities"; Render(); });
                }
                break;
            case "priorities":
                Text(_page, L("priority-rules"));
                foreach (var item in _view.Items.Where(i => i.Kind == "token"))
                    Button(_page, ItemText(item) + " · " + Date(item.Expires) + " · " + L("status-" + item.Status),
                        () => { _token = item.Id; Render(); });
                var selected = _view.Items.FirstOrDefault(i => i.Id == _token && i.Kind == "token");
                if (selected != null)
                {
                    Text(_page, ItemText(selected));
                    if (selected.Status == "reserved")
                        Button(_page, L("disarm"), () => Submit(new KabanRequest { Action = KabanAction.Disarm, Item = _token }));
                    else
                    {
                        var choices = new System.Collections.Generic.List<string>();
                        var search = Input(_page, L("search-job"));
                        var jobBox = Column();
                        _page.AddChild(jobBox);
                        void Jobs()
                        {
                            jobBox.RemoveAllChildren();
                            foreach (var (id, name) in _view.Jobs.OrderBy(j => j.Value)
                                         .Where(j => j.Value.Contains(search.Text, StringComparison.OrdinalIgnoreCase)))
                            {
                                var check = new CheckBox { Text = name, Pressed = choices.Contains(id) };
                                check.OnToggled += e => { if (e.Pressed) { if (!choices.Contains(id)) choices.Add(id); } else choices.Remove(id); };
                                jobBox.AddChild(check);
                            }
                        }
                        search.OnTextChanged += _ => Jobs();
                        Button(_page, L("arm"), () => Submit(new KabanRequest { Action = KabanAction.Arm, Item = _token, Jobs = choices.ToArray() }));
                        Jobs();
                    }
                }
                break;
            case "forcing":
                Text(_page, L("force-blocked"));
                foreach (var item in _view.Items.Where(i => i.Kind == "force"))
                {
                    Text(_page, item.Description + " · " + L("status-" + item.Status));
                    Button(_page, L("cancel"), () => Submit(new KabanRequest { Action = KabanAction.CancelRequest, Item = item.Id }), item.Status != "queued");
                }
                break;
            case "history":
                Text(_page, L("recent-history"));
                foreach (var entry in _view.History)
                    Text(_page, $"{Date(entry.At)} · {entry.Amount:+#;-#;0} {L("currency")}\n{HistoryText(entry.Description)}\n{entry.Id}");
                break;
            case "awards":
                AwardForm();
                foreach (var award in _view.Awards)
                {
                    Text(_page, $"{Account(award.Recipient)} · {award.Amount} {L("currency")} · {L("status-" + award.Status)}\n"
                        + $"{L("category-" + award.Category)}: {award.Reason}\n{L("initiator")}: {Account(award.Initiator)}\n"
                        + $"{L("approvals")}: {string.Join(", ", award.Approvals.Select(Account))} · {Date(award.At)}\n{award.Id}");
                    if (_view.Manager && award.Status == "pending")
                    {
                        Button(_page, L("approve"), () => Submit(new KabanRequest { Action = KabanAction.Approve, Item = award.Id }),
                            award.Recipient == _view.Viewer || award.Approvals.Contains(_view.Viewer));
                        Button(_page, L("reject"), () => Submit(new KabanRequest { Action = KabanAction.Reject, Item = award.Id }));
                    }
                }
                break;
            case "admin":
                Admin();
                break;
        }
    }

    private void ConfirmPurchase(KabanProductView product)
    {
        _page.RemoveAllChildren();
        Text(_page, L("product-" + product.Id) + $" — {product.Price} " + L("currency"));
        Text(_page, L("description-" + product.Id));
        LineEdit? text = null;
        LineEdit? category = null;
        if (product.Id == "voice")
        {
            category = Input(_page, L("speaker-id"));
            text = Input(_page, L("consent"));
        }
        if (product.Id is "discord" or "rename")
        {
            text = Input(_page, L("role-name"));
            category = Input(_page, L("role-color"));
        }
        var check = new CheckBox { Text = L("confirm-purchase") };
        _page.AddChild(check);
        var confirm = Button(_page, L("confirm"), () => Submit(new KabanRequest
        {
            Action = KabanAction.Purchase, Product = product.Id, Amount = product.Price,
            Text = text?.Text ?? "", Category = category?.Text ?? "", Confirmed = check.Pressed,
        }), true);
        check.OnToggled += e => confirm.Disabled = !e.Pressed;
        Button(_page, L("cancel"), Render);
    }

    private void AwardForm()
    {
        Text(_page, L("award-form"));
        var target = AccountInput(_page);
        var amount = Input(_page, L("amount"));
        var category = new OptionButton();
        var categories = new[] { "development", "administration", "testing", "localization", "training", "content", "events", "merit" };
        for (var i = 0; i < categories.Length; i++)
            category.AddItem(L("category-" + categories[i]), i);
        category.OnItemSelected += e => category.SelectId(e.Id);
        _page.AddChild(category);
        var reason = Input(_page, L("reason"));
        var exceptional = new CheckBox { Text = L("exceptional") };
        _page.AddChild(exceptional);
        Button(_page, L("nominate"), () =>
        {
            if (Guid.TryParse(target.Text, out var id) && int.TryParse(amount.Text, out var value))
                Submit(new KabanRequest { Action = KabanAction.Nominate, Target = id, Amount = value,
                    Category = categories[category.SelectedId], Text = reason.Text, Exceptional = exceptional.Pressed });
        });
    }

    private void Admin()
    {
        Text(_page, L("admin-rules"));
        foreach (var wallet in _view.Wallets)
            Text(_page, Account(wallet.UserId) + " · " + Loc.GetString("cmu-kaban-balance", ("balance", wallet.Balance), ("earned", wallet.Earned)));
        Button(_page, L("awards"), () => { _section = "awards"; Render(); });
        foreach (var item in _view.Items.Where(i => i.Kind is "voice" or "discord" && i.Status == "pending"))
        {
            Text(_page, $"{Account(item.Id)}: {item.Description} · {L("kind-" + item.Kind)}\n{item.Detail}");
            if (item.Kind == "voice")
                Button(_page, L("approve-voice"), () => Submit(new KabanRequest
                    { Action = KabanAction.ApproveVoice, Target = item.Id, Text = item.Description }));
            if (item.Kind == "discord")
                Button(_page, L("approve-discord"), () => Submit(new KabanRequest
                    { Action = KabanAction.ApproveDiscord, Target = item.Id }));
            Button(_page, L("reject-refund"), () => Submit(new KabanRequest
                { Action = KabanAction.RejectPrivilege, Target = item.Id, Product = item.Kind }));
        }
        if (_view.Senior)
        {
            Text(_page, L("special-voice"));
            var voiceTarget = AccountInput(_page);
            var speaker = Input(_page, L("speaker-id"));
            var consent = Input(_page, L("consent"));
            var grantConfirmed = new CheckBox { Text = L("confirm-admin") };
            _page.AddChild(grantConfirmed);
            Button(_page, L("grant-voice"), () =>
            {
                if (grantConfirmed.Pressed && Guid.TryParse(voiceTarget.Text, out var id))
                    Submit(new KabanRequest { Action = KabanAction.GrantVoice, Target = id,
                        Category = speaker.Text, Text = consent.Text, Confirmed = true });
            });
            Text(_page, L("product-settings"));
            var product = new OptionButton { HorizontalExpand = true };
            for (var i = 0; i < _view.Products.Count; i++)
                product.AddItem(L("product-" + _view.Products[i].Id), i);
            product.OnItemSelected += e => product.SelectId(e.Id);
            _page.AddChild(product);
            var price = Input(_page, L("amount"));
            var disable = new CheckBox { Text = L("disable-product") };
            _page.AddChild(disable);
            var confirm = new CheckBox { Text = L("confirm-admin") };
            _page.AddChild(confirm);
            Button(_page, L("save"), () =>
            {
                if (confirm.Pressed && int.TryParse(price.Text, out var value))
                    Submit(new KabanRequest { Action = KabanAction.SetProduct, Product = _view.Products[product.SelectedId].Id,
                        Amount = value, Exceptional = disable.Pressed, Confirmed = true });
            });
            Text(_page, L("correction-help"));
            var target = AccountInput(_page);
            var delta = Input(_page, L("negative-amount"));
            var reason = Input(_page, L("reason"));
            Button(_page, L("correct"), () =>
            {
                if (confirm.Pressed && Guid.TryParse(target.Text, out var id) && int.TryParse(delta.Text, out var value))
                    Submit(new KabanRequest { Action = KabanAction.CorrectBalance, Target = id,
                        Amount = value, Text = reason.Text, Confirmed = true });
            });
        }
        Text(_page, L("audit"));
        foreach (var audit in _view.Audit)
            Text(_page, $"{Date(audit.At)} · {audit.Description}\n{audit.Id}");
    }
}

public sealed partial class KabanEntrySystem : EntitySystem
{
    [Dependency] private readonly IUserInterfaceManager _ui = default!;
    public override void Initialize()
    {
        base.Initialize();
        _ui.OnScreenChanged += ScreenChanged;
        _ui.WindowRoot.OnChildAdded += WindowAdded;
        if (_ui.ActiveScreen is LobbyGui lobby)
            Attach(lobby.ObserveButton.Parent!);
    }
    public override void Shutdown()
    {
        _ui.OnScreenChanged -= ScreenChanged;
        _ui.WindowRoot.OnChildAdded -= WindowAdded;
        base.Shutdown();
    }
    private void ScreenChanged((UIScreen? Old, UIScreen? New) screens)
    {
        if (screens.New is LobbyGui lobby)
            Attach(lobby.ObserveButton.Parent!);
    }
    private void WindowAdded(Control control)
    {
        if (control is EscapeMenu escape)
            Attach(escape.RulesButton.Parent!);
    }
    private void Attach(Control parent)
    {
        if (parent.Children.Any(c => c.Name == "RuCMKabanEntry"))
            return;
        var button = new Button { Name = "RuCMKabanEntry", Text = Loc.GetString("cmu-kaban-title"), HorizontalExpand = true, MinHeight = 34 };
        button.AddStyleClass(StyleNano.StyleClassCrtButton);
        button.OnPressed += _ => RaiseNetworkEvent(new KabanOpenEvent());
        parent.AddChild(button);
    }
}
