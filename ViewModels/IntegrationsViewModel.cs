using System.Collections.ObjectModel;
using System.Diagnostics;
using Avalonia.Media;
using LunaChat.Models;
using LunaChat.Services;

namespace LunaChat.ViewModels;

/// <summary>One connector card in the Integrations ▸ Connectors gallery.</summary>
public class ConnectorCardViewModel : ViewModelBase
{
    public ConnectorCardViewModel(ConnectorDef def, ConnectorStore store)
    {
        Def = def;
        BrandBrush = new SolidColorBrush(Color.Parse(def.BrandColor));
        BrandSoftBrush = new SolidColorBrush(Color.Parse(def.BrandSoft));
        Refresh(store);
    }

    public ConnectorDef Def { get; }
    public string Id => Def.Id;
    public string Title => Def.Title;
    public string Description => Def.Description;
    public string Initial => Def.Initial;
    public bool TwoWay => Def.TwoWay;
    public IBrush BrandBrush { get; }
    public IBrush BrandSoftBrush { get; }

    private bool _isConnected;
    public bool IsConnected { get => _isConnected; private set => SetField(ref _isConnected, value); }

    public void Refresh(ConnectorStore store)
    {
        IsConnected = store.IsConnected(Def.Id);
        OnPropertyChanged(nameof(IsConnected));
    }
}

/// <summary>One registered MCP server row.</summary>
public class McpRowViewModel : ViewModelBase
{
    private readonly Action<McpRowViewModel, bool> _onToggle;

    public McpRowViewModel(McpServer server, Action<McpRowViewModel, bool> onToggle)
    {
        Server = server;
        _onToggle = onToggle;
        _enabled = server.Enabled;
    }

    public McpServer Server { get; }
    public string Name => Server.Name;
    public string TransportText => Server.Transport == McpTransport.Stdio ? "stdio" : "sse";
    public string Detail => Server.Transport == McpTransport.Stdio
        ? $"{Server.Command} {Server.Args}".Trim()
        : Server.Url;

    private bool _enabled;
    public bool Enabled
    {
        get => _enabled;
        set { if (SetField(ref _enabled, value)) _onToggle(this, value); }
    }
}

/// <summary>
/// Integrations view: Connectors (manual-credential integrations, stored in the
/// vault) and MCP servers (local tool-server registry). Layout mirrors
/// OpenWorker's redesign.html Integrations screen.
/// </summary>
public class IntegrationsViewModel : ViewModelBase
{
    private readonly AppState _app;
    private readonly IDialogService _dialogs;

    public IntegrationsViewModel(AppState app, IDialogService dialogs)
    {
        _app = app;
        _dialogs = dialogs;

        foreach (var def in ConnectorRegistry.All)
            Connectors.Add(new ConnectorCardViewModel(def, app.ConnectorStore));
        ReloadMcp();

        SelectTabCommand = new RelayCommand(p => SelectedTab = p as string ?? "connectors");
        OpenConnectorCommand = new RelayCommand(p => OpenConnector(p as ConnectorCardViewModel));
        BackCommand = new RelayCommand(_ => BackToGallery());
        ConnectCommand = new AsyncRelayCommand(_ => ConnectAsync());
        DisconnectCommand = new AsyncRelayCommand(_ => DisconnectAsync());
        OpenHelpCommand = new RelayCommand(_ => OpenHelp());

        AddServerCommand = new AsyncRelayCommand(_ => AddServerAsync(), _ => CanAddServer);
        RemoveServerCommand = new AsyncRelayCommand(RemoveServerAsync);
    }

    // ----- Tabs -----
    private string _selectedTab = "connectors";
    public string SelectedTab
    {
        get => _selectedTab;
        set
        {
            if (SetField(ref _selectedTab, value))
            {
                OnPropertyChanged(nameof(IsConnectorsTab));
                OnPropertyChanged(nameof(IsMcpTab));
            }
        }
    }
    public bool IsConnectorsTab => _selectedTab == "connectors";
    public bool IsMcpTab => _selectedTab == "mcp";

    public RelayCommand SelectTabCommand { get; }

    // ===================== CONNECTORS =====================
    public ObservableCollection<ConnectorCardViewModel> Connectors { get; } = new();
    public ObservableCollection<ProviderFieldViewModel> Fields { get; } = new();

    public int ConnectedCount => _app.ConnectorStore.ConnectedCount;
    public string BackendName => _app.ProviderStore.BackendName;

    private ConnectorDef? _selected;
    public bool IsConnectorGallery => _selected == null;
    public bool IsConnectorForm => _selected != null;

    public string FormTitle => _selected?.Title ?? "";
    public string FormDescription => _selected?.Description ?? "";
    public string FormInitial => _selected?.Initial ?? "?";
    public IBrush FormBrandBrush { get; private set; } = Brushes.Gray;
    public IBrush FormBrandSoftBrush { get; private set; } = Brushes.Gainsboro;
    public bool SelectedConnected => _selected != null && _app.ConnectorStore.IsConnected(_selected.Id);
    public string HelpText => _selected == null ? "" : $"Get credentials at {_selected.HelpLabel} ↗";
    public bool HasHelp => _selected != null && !string.IsNullOrWhiteSpace(_selected.HelpUrl);

    public RelayCommand OpenConnectorCommand { get; }
    public RelayCommand BackCommand { get; }
    public AsyncRelayCommand ConnectCommand { get; }
    public AsyncRelayCommand DisconnectCommand { get; }
    public RelayCommand OpenHelpCommand { get; }

    private void OpenConnector(ConnectorCardViewModel? card)
    {
        if (card == null) return;
        _selected = card.Def;
        FormBrandBrush = card.BrandBrush;
        FormBrandSoftBrush = card.BrandSoftBrush;

        Fields.Clear();
        foreach (var f in card.Def.Fields)
        {
            var vm = new ProviderFieldViewModel(f, () => { });
            if (!f.Secret)
                vm.Value = _app.ConnectorStore.ValueOr(card.Def.Id, f.Key, "");
            Fields.Add(vm);
        }
        RaiseConnectorFormProps();
    }

    private void BackToGallery()
    {
        _selected = null;
        Fields.Clear();
        RaiseConnectorFormProps();
    }

    private async Task ConnectAsync()
    {
        if (_selected == null) return;
        var def = _selected;
        var fields = Fields.ToDictionary(f => f.Key, f => f.Value);

        // Require the secret field to be filled.
        if (def.SecretFieldKey != null &&
            string.IsNullOrWhiteSpace(fields.GetValueOrDefault(def.SecretFieldKey)))
        {
            _dialogs.Toast("Enter the token to connect.");
            return;
        }

        await _app.ConnectorStore.ConnectAsync(def, fields);
        RefreshCard(def.Id);
        RaiseConnectorFormProps();
        OnPropertyChanged(nameof(ConnectedCount));
        _dialogs.Toast($"{def.Title} connected");
        BackToGallery();
    }

    private async Task DisconnectAsync()
    {
        if (_selected == null) return;
        var id = _selected.Id;
        await _app.ConnectorStore.DisconnectAsync(id);
        RefreshCard(id);
        OnPropertyChanged(nameof(ConnectedCount));
        BackToGallery();
    }

    private void OpenHelp()
    {
        var url = _selected?.HelpUrl;
        if (string.IsNullOrWhiteSpace(url)) return;
        try { Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true }); }
        catch (Exception ex) { Console.Error.WriteLine(ex.Message); }
    }

    private void RefreshCard(string id) =>
        Connectors.FirstOrDefault(c => c.Id == id)?.Refresh(_app.ConnectorStore);

    private void RaiseConnectorFormProps()
    {
        OnPropertyChanged(nameof(IsConnectorGallery));
        OnPropertyChanged(nameof(IsConnectorForm));
        OnPropertyChanged(nameof(FormTitle));
        OnPropertyChanged(nameof(FormDescription));
        OnPropertyChanged(nameof(FormInitial));
        OnPropertyChanged(nameof(FormBrandBrush));
        OnPropertyChanged(nameof(FormBrandSoftBrush));
        OnPropertyChanged(nameof(SelectedConnected));
        OnPropertyChanged(nameof(HelpText));
        OnPropertyChanged(nameof(HasHelp));
    }

    // ===================== MCP =====================
    public ObservableCollection<McpRowViewModel> Servers { get; } = new();
    public bool HasServers => Servers.Count > 0;

    private string _newName = "";
    public string NewName { get => _newName; set { if (SetField(ref _newName, value)) RaiseAddState(); } }

    public ObservableCollection<string> TransportOptions { get; } = new() { "stdio", "sse" };

    private string _newTransport = "stdio";
    public string NewTransport
    {
        get => _newTransport;
        set
        {
            if (SetField(ref _newTransport, value))
            {
                OnPropertyChanged(nameof(IsStdio));
                OnPropertyChanged(nameof(IsSse));
                RaiseAddState();
            }
        }
    }
    public bool IsStdio => _newTransport == "stdio";
    public bool IsSse => _newTransport == "sse";

    private string _newCommand = "";
    public string NewCommand { get => _newCommand; set { if (SetField(ref _newCommand, value)) RaiseAddState(); } }

    private string _newArgs = "";
    public string NewArgs { get => _newArgs; set => SetField(ref _newArgs, value); }

    private string _newUrl = "";
    public string NewUrl { get => _newUrl; set { if (SetField(ref _newUrl, value)) RaiseAddState(); } }

    public bool CanAddServer =>
        !string.IsNullOrWhiteSpace(NewName) &&
        (IsStdio ? !string.IsNullOrWhiteSpace(NewCommand) : !string.IsNullOrWhiteSpace(NewUrl));

    public AsyncRelayCommand AddServerCommand { get; }
    public AsyncRelayCommand RemoveServerCommand { get; }

    private void RaiseAddState()
    {
        OnPropertyChanged(nameof(CanAddServer));
        AddServerCommand.RaiseCanExecuteChanged();
    }

    private void ReloadMcp()
    {
        Servers.Clear();
        foreach (var s in _app.McpStore.Servers)
            Servers.Add(new McpRowViewModel(s, OnServerToggled));
        OnPropertyChanged(nameof(HasServers));
    }

    private async void OnServerToggled(McpRowViewModel row, bool enabled)
        => await _app.McpStore.SetEnabledAsync(row.Server.Id, enabled);

    private async Task AddServerAsync()
    {
        if (!CanAddServer) return;
        var server = new McpServer
        {
            Name = NewName.Trim(),
            Transport = IsStdio ? McpTransport.Stdio : McpTransport.Sse,
            Command = NewCommand.Trim(),
            Args = NewArgs.Trim(),
            Url = NewUrl.Trim(),
            Enabled = true
        };
        await _app.McpStore.UpsertAsync(server);
        NewName = NewCommand = NewArgs = NewUrl = "";
        NewTransport = "stdio";
        ReloadMcp();
        _dialogs.Toast($"Added MCP server {server.Name}");
    }

    private async Task RemoveServerAsync(object? param)
    {
        if (param is not McpRowViewModel row) return;
        await _app.McpStore.RemoveAsync(row.Server.Id);
        ReloadMcp();
    }
}
