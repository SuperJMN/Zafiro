using System.Reactive.Linq;
using Zafiro.UI.Navigation;

namespace Zafiro.UI.Wizards.Slim;

public record DialogWizardHost(ISlimWizard Wizard) : IHaveFooter
{
    public IObservable<object> Footer => Wizard.PageFooter.Select(x => x!);
}
