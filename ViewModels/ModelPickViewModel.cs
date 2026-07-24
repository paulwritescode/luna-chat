namespace LunaChat.ViewModels;

/// <summary>One selectable model in the composer's model flyout.</summary>
public class ModelPickViewModel : ViewModelBase
{
    public ModelPickViewModel(string providerId, string modelId, string providerTitle, string modelLabel, bool isSelected)
    {
        ProviderId = providerId;
        ModelId = modelId;
        ProviderTitle = providerTitle;
        ModelLabel = modelLabel;
        _isSelected = isSelected;
    }

    public string ProviderId { get; }
    public string ModelId { get; }
    public string ProviderTitle { get; }
    public string ModelLabel { get; }

    public string Display => $"{ModelLabel}";
    public string Subtitle => ProviderTitle;

    private bool _isSelected;
    public bool IsSelected { get => _isSelected; set => SetField(ref _isSelected, value); }
}
