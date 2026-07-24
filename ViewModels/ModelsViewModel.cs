using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading;
using LunaChat.Models;
using LunaChat.Services;

namespace LunaChat.ViewModels;

/// <summary>One field row in a provider's key form.</summary>
public class ProviderFieldViewModel : ViewModelBase
{
    private readonly Action _onChanged;

    public ProviderFieldViewModel(ProviderField field, Action onChanged)
    {
        Field = field;
        _onChanged = onChanged;
    }

    public ProviderField Field { get; }
    public string Key => Field.Key;
    public string Label => Field.Label;
    public bool IsSecret => Field.Secret;
    public char PasswordChar => Field.Secret ? '●' : '\0';
    public string Placeholder => Field.Placeholder;
    public string Help => Field.Help;
    public bool HasHelp => !string.IsNullOrWhiteSpace(Field.Help);

    private string _value = "";
    public string Value
    {
        get => _value;
        set { if (SetField(ref _value, value)) _onChanged(); }
    }
}

/// <summary>One provider card in the gallery.</summary>
public class ProviderCardViewModel : ViewModelBase
{
    public ProviderCardViewModel(ModelProviderDef def, ProviderStore store)
    {
        Def = def;
        Refresh(store);
    }

    public ModelProviderDef Def { get; }
    public string Id => Def.Id;
    public string Title => Def.Title;
    public string Blurb => Def.Blurb;
    public string Initial => string.IsNullOrEmpty(Def.Title) ? "?" : Def.Title[..1];

    private bool _isConnected;
    public bool IsConnected { get => _isConnected; private set => SetField(ref _isConnected, value); }

    private string _status = "Not set up";
    public string Status { get => _status; private set => SetField(ref _status, value); }

    public void Refresh(ProviderStore store)
    {
        var configured = store.IsConfigured(Def.Id);
        IsConnected = configured;
        Status = configured
            ? (Def.NeedsKey ? "✓ Connected" : "✓ Running")
            : (Def.NeedsKey ? "Not set up" : "No key needed");
        OnPropertyChanged(nameof(IsConnected));
        OnPropertyChanged(nameof(Status));
    }
}

/// <summary>A curated model the user can activate from within the provider form.</summary>
public class CuratedModelViewModel : ViewModelBase
{
    public CuratedModelViewModel(string providerId, CuratedModel model, bool isActive)
    {
        ProviderId = providerId;
        Model = model;
        _isActive = isActive;
    }

    public string ProviderId { get; }
    public CuratedModel Model { get; }
    public string Id => Model.Id;
    public string Label => Model.Label;

    private bool _isActive;
    public bool IsActive { get => _isActive; set => SetField(ref _isActive, value); }
}

/// <summary>
/// Settings ▸ Models: a provider gallery ⇄ key form (test-to-save) ⇄ model
/// activation. Mirrors OpenWorker's ProviderSetup. Keys go to the OS vault via
/// <see cref="ProviderStore"/>; only the active model id is kept in settings.
/// </summary>
public class ModelsViewModel : ViewModelBase
{
    private readonly AppState _app;
    private readonly IDialogService _dialogs;

    public ModelsViewModel(AppState app, IDialogService dialogs)
    {
        _app = app;
        _dialogs = dialogs;

        foreach (var def in ModelProviderRegistry.All)
            Providers.Add(new ProviderCardViewModel(def, app.ProviderStore));

        OpenProviderCommand = new RelayCommand(p => OpenProvider(p as ProviderCardViewModel));
        BackCommand = new RelayCommand(_ => BackToGallery());
        TestCommand = new AsyncRelayCommand(_ => TestAndSaveAsync());
        RemoveKeyCommand = new AsyncRelayCommand(_ => RemoveKeyAsync());
        OpenKeyHelpCommand = new RelayCommand(_ => OpenKeyHelp());
        UseModelCommand = new RelayCommand(p => UseModel(p as CuratedModelViewModel));
    }

    public ObservableCollection<ProviderCardViewModel> Providers { get; } = new();
    public ObservableCollection<ProviderFieldViewModel> Fields { get; } = new();
    public ObservableCollection<CuratedModelViewModel> CuratedModels { get; } = new();

    public string BackendName => _app.ProviderStore.BackendName;

    private ModelProviderDef? _selected;

    public bool IsGallery => _selected == null;
    public bool IsForm => _selected != null;

    public string FormTitle => _selected?.Title ?? "";
    public string FormBlurb => _selected?.Blurb ?? "";
    public string Initial => string.IsNullOrEmpty(FormTitle) ? "?" : FormTitle[..1];
    public bool NeedsKey => _selected?.NeedsKey ?? true;
    public string TestButtonText => NeedsKey ? "Test" : "Detect";

    private bool _dirty;
    private bool _connectedNow; // provider is configured (stored or just verified)
    public bool IsConnected => _connectedNow;

    // Verify state
    private bool _testing;
    public bool Testing { get => _testing; private set { if (SetField(ref _testing, value)) OnPropertyChanged(nameof(CanTest)); } }
    public bool CanTest => !Testing;

    private string _errorText = "";
    public string ErrorText
    {
        get => _errorText;
        private set { if (SetField(ref _errorText, value)) OnPropertyChanged(nameof(HasError)); }
    }
    public bool HasError => !string.IsNullOrEmpty(_errorText);

    private string _okText = "";
    public string OkText
    {
        get => _okText;
        private set { if (SetField(ref _okText, value)) OnPropertyChanged(nameof(HasOk)); }
    }
    public bool HasOk => !string.IsNullOrEmpty(_okText);

    // Key help
    public string KeyHelpText => _selected == null ? "" : $"Create one at {_selected.KeyHelpLabel} ↗";
    public bool HasKeyHelp => _selected is { NeedsKey: true } && !string.IsNullOrWhiteSpace(_selected.KeyHelpUrl);
    public bool ShowModelList => _connectedNow && CuratedModels.Count > 0;

    public RelayCommand OpenProviderCommand { get; }
    public RelayCommand BackCommand { get; }
    public AsyncRelayCommand TestCommand { get; }
    public AsyncRelayCommand RemoveKeyCommand { get; }
    public RelayCommand OpenKeyHelpCommand { get; }
    public RelayCommand UseModelCommand { get; }

    private void OpenProvider(ProviderCardViewModel? card)
    {
        if (card == null) return;
        _selected = card.Def;
        _dirty = false;
        _connectedNow = _app.ProviderStore.IsConfigured(card.Def.Id);
        ErrorText = "";
        OkText = _connectedNow ? (NeedsKey ? "Connected" : "Running") : "";

        Fields.Clear();
        foreach (var f in card.Def.Fields)
        {
            var vm = new ProviderFieldViewModel(f, OnFieldChanged);
            // Prefill non-secret values (e.g. custom base_url); never the secret.
            if (!f.Secret)
                vm.Value = _app.ProviderStore.ValueOr(card.Def.Id, f.Key, f.Default);
            Fields.Add(vm);
        }

        RebuildCuratedModels();
        RaiseFormProps();
    }

    private void OnFieldChanged()
    {
        _dirty = true;
        ErrorText = "";
        OkText = "";
        OnPropertyChanged(nameof(HasError));
        OnPropertyChanged(nameof(HasOk));
    }

    private void BackToGallery()
    {
        _selected = null;
        Fields.Clear();
        CuratedModels.Clear();
        RaiseFormProps();
    }

    private async Task TestAndSaveAsync()
    {
        if (_selected == null) return;
        var def = _selected;
        var fields = Fields.ToDictionary(f => f.Key, f => f.Value);

        Testing = true;
        ErrorText = "";
        OkText = "";
        OnPropertyChanged(nameof(HasError));
        OnPropertyChanged(nameof(HasOk));

        VerifyResult result;
        using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30)))
        {
            try { result = await _app.ModelChat.VerifyAsync(def, fields, cts.Token); }
            catch (OperationCanceledException) { result = VerifyResult.Fail("timed out"); }
            catch (Exception ex) { result = VerifyResult.Fail(ex.Message); }
        }
        Testing = false;

        if (!result.Ok)
        {
            ErrorText = result.Error ?? "couldn't verify";
            OnPropertyChanged(nameof(HasError));
            return;
        }

        // Persist (secret → vault, rest → json), mark verified.
        if (_dirty || !_app.ProviderStore.IsConfigured(def.Id))
            await _app.ProviderStore.SaveAsync(def, fields);
        await _app.ProviderStore.MarkVerifiedAsync(def.Id);

        _dirty = false;
        _connectedNow = true;
        OkText = NeedsKey ? "Tested & saved" : "Detected";
        OnPropertyChanged(nameof(HasOk));

        RefreshCard(def.Id);
        RebuildCuratedModels();

        // First provider connected with no active model yet → activate its first model.
        if (string.IsNullOrEmpty(_app.Settings.SelectedModel) && def.Models.Count > 0)
            UseModel(new CuratedModelViewModel(def.Id, def.Models[0], false));

        _app.NotifyModelsChanged();
        RaiseFormProps();
    }

    private async Task RemoveKeyAsync()
    {
        if (_selected == null) return;
        var def = _selected;
        await _app.ProviderStore.RemoveAsync(def.Id);

        // If the active model belonged to this provider, clear the selection.
        if (_app.Settings.SelectedProvider == def.Id)
        {
            _app.Settings.SelectedProvider = "";
            _app.Settings.SelectedModel = "";
            await _app.SettingsStore.SaveAsync(_app.Settings);
        }

        RefreshCard(def.Id);
        _app.NotifyModelsChanged();
        BackToGallery();
    }

    private void UseModel(CuratedModelViewModel? pick)
    {
        if (pick == null) return;
        _app.Settings.SelectedProvider = pick.ProviderId;
        _app.Settings.SelectedModel = pick.Id;
        _ = _app.SettingsStore.SaveAsync(_app.Settings);
        _app.NotifyModelsChanged();
        foreach (var m in CuratedModels)
            m.IsActive = m.ProviderId == pick.ProviderId && m.Id == pick.Id;
    }

    private void OpenKeyHelp()
    {
        var url = _selected?.KeyHelpUrl;
        if (string.IsNullOrWhiteSpace(url)) return;
        try { Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true }); }
        catch (Exception ex) { Console.Error.WriteLine(ex.Message); }
    }

    private void RebuildCuratedModels()
    {
        CuratedModels.Clear();
        if (_selected == null) return;
        foreach (var m in _selected.Models)
            CuratedModels.Add(new CuratedModelViewModel(_selected.Id, m,
                _app.Settings.SelectedProvider == _selected.Id && _app.Settings.SelectedModel == m.Id));
        OnPropertyChanged(nameof(ShowModelList));
    }

    private void RefreshCard(string id)
    {
        var card = Providers.FirstOrDefault(c => c.Id == id);
        card?.Refresh(_app.ProviderStore);
    }

    private void RaiseFormProps()
    {
        OnPropertyChanged(nameof(IsGallery));
        OnPropertyChanged(nameof(IsForm));
        OnPropertyChanged(nameof(FormTitle));
        OnPropertyChanged(nameof(FormBlurb));
        OnPropertyChanged(nameof(Initial));
        OnPropertyChanged(nameof(NeedsKey));
        OnPropertyChanged(nameof(TestButtonText));
        OnPropertyChanged(nameof(IsConnected));
        OnPropertyChanged(nameof(HasError));
        OnPropertyChanged(nameof(HasOk));
        OnPropertyChanged(nameof(KeyHelpText));
        OnPropertyChanged(nameof(HasKeyHelp));
        OnPropertyChanged(nameof(ShowModelList));
    }
}
