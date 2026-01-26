using Zafiro.UI.Commands;

namespace Zafiro.UI.Wizards.Slim;

public record WizardFooter(ISlimWizard Wizard, IEnhancedCommand? Cancel = null);
