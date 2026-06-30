using LunaChat.Models;

namespace LunaChat.ViewModels;

/// <summary>
/// A toggleable skill chip in the Skill Rail.
/// </summary>
public class SkillChipViewModel : ViewModelBase
{
    private readonly Action<SkillChipViewModel> _onToggled;

    public SkillChipViewModel(SkillDefinition skill, bool isActive, Action<SkillChipViewModel> onToggled)
    {
        Skill = skill;
        _isActive = isActive;
        _onToggled = onToggled;
    }

    public SkillDefinition Skill { get; }

    public string Id => Skill.Id;
    public string Name => Skill.Name;
    public string Description => Skill.Description;
    public string SourcePath => Skill.SourcePath;

    public string TooltipText =>
        $"{Name}\n{(string.IsNullOrWhiteSpace(Description) ? "No description" : FirstSentence(Description))}\nloaded from: {SourcePath}";

    private bool _isActive;
    public bool IsActive
    {
        get => _isActive;
        set
        {
            if (SetField(ref _isActive, value))
            {
                OnPropertyChanged(nameof(Glyph));
                _onToggled(this);
            }
        }
    }

    public string Glyph => _isActive ? "◉" : "○";

    private static string FirstSentence(string text)
    {
        var idx = text.IndexOf('.');
        return idx > 0 ? text[..(idx + 1)] : text;
    }
}
