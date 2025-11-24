namespace Zafiro.UI.Wizards.Classic.Builder;

/// <summary>
/// Base contract for a Classic wizard step.
/// </summary>
public interface IStep : IValidatable, IBusy
{
    /// <summary>Indicates whether the wizard should automatically advance when valid.</summary>
    bool AutoAdvance { get; }
    /// <summary>Optional title for the step.</summary>
    Maybe<string> Title => Maybe<string>.None;
}
