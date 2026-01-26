using Zafiro.UI.Commands;

namespace Zafiro.UI.Wizards.Slim;

/// <summary>
/// Non-generic Slim wizard contract.
/// </summary>
public interface ISlimWizard
{
    /// <summary>Gets the command to navigate forward.</summary>
    IEnhancedCommand Next { get; }

    /// <summary>Gets the command to navigate backward.</summary>
    IEnhancedCommand Back { get; }



    /// <summary>Gets the current page.</summary>
    IPage CurrentPage { get; }

    /// <summary>Gets the footer of the current page, if any.</summary>
    IObservable<object?> PageFooter { get; }

    /// <summary>Gets the total number of pages.</summary>
    int TotalPages { get; }
}

/// <summary>
/// Generic Slim wizard contract exposing the completion result.
/// </summary>
public interface ISlimWizard<out T> : ISlimWizard
{
    /// <summary>Observable that emits the final result once and completes.</summary>
    IObservable<T> Finished { get; }
}
