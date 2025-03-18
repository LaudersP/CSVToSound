namespace CSVToSound
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            MainPage = new AppShell();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var window = base.CreateWindow(activationState);

            // Set window size
            window.Width = 350;
            window.Height = 430;
            window.MinimumWidth = 350;
            window.MinimumHeight = 430;

            return window;
        }
    }
}
