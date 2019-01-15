using System;
using System.Windows;
using Caliburn.Micro;
using FalseCrypt.App.Shell;
using FalseCrypt.App.ViewModels;
using FalseCrypt.Crypto;

namespace FalseCrypt.App
{
    public partial class App
    {
        private void OnStartUp(object sender, StartupEventArgs e)
        {
            var wm = IoC.Get<IWindowManager>();
            var passwordModel = IoC.Get<EnterPasswordViewModel>();            
            var m = new WindowManager();
            m.ShowWindow(IoC.Get<MainWindowViewModel>());

            wm.ShowDialog(passwordModel);
            var hash = WeakPasswordDerivation.StringToHash(passwordModel.Password);
            if (hash == null || !hash.Equals(WeakCryptoConfig.Password, StringComparison.InvariantCultureIgnoreCase))
                Execute.OnUIThread(() => Current.Shutdown());
        }
    }
}
