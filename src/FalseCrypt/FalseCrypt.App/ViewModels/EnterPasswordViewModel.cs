using System.ComponentModel.Composition;
using System.Windows.Input;
using Caliburn.Micro;
using ModernApplicationFramework.Input.Command;

namespace FalseCrypt.App.ViewModels
{
    [Export(typeof(EnterPasswordViewModel))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class EnterPasswordViewModel : Screen
    {
        public string Password { get; set; }

        public ICommand EnterPasswordCommand => new UICommand(EnterPassword, CanEnterPassword);

        private void EnterPassword()
        {
            TryClose(true);
        }

        private bool CanEnterPassword()
        {
            if (string.IsNullOrEmpty(Password))
                return false;
            return true;
        }
    }
}
