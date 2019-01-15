using System.ComponentModel.Composition;
using Caliburn.Micro;
using FalseCrypt.App.ViewModels;

namespace FalseCrypt.App.Shell
{
    [Export(typeof(MainWindowViewModel))]
    public class MainWindowViewModel : Conductor<IScreen>
    {
        protected override void OnViewReady(object view)
        {
            base.OnViewReady(view);
            ActivateItem(IoC.Get<WelcomeViewModel>());
        }
    }
}
