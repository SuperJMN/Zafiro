using System.Reactive.Linq;
using Zafiro.UI.Commands;
using Zafiro.UI.Navigation;

namespace Zafiro.UI.Wizards.Slim;

public record NavigationWizardHost(ISlimWizard Wizard, IEnhancedCommand Cancel) : IHaveFooter, IHaveHeader
{
    public IObservable<object> Header => ((IHaveHeader)Wizard).Header;
    public IObservable<object> Footer => Observable.Return(new WizardFooter(Wizard, Cancel)).Select(x => (object)x);
}
