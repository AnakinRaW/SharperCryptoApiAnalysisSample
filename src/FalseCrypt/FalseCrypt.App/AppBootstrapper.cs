using Caliburn.Micro;

namespace FalseCrypt.App
{
    public sealed class AppBootstrapper : BootstrapperBase
    {
        public AppBootstrapper() : base(false)
        {
            Initialize();
        }
    }
}
