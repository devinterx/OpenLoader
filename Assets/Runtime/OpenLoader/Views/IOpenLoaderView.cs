namespace OpenUniverse.Runtime.OpenLoader.Views
{
    public interface IOpenLoaderView
    {
        string Version { get; set; }
        float Progress { get; set; }
        string ProgressStatus { get; set; }

        bool IsShowScreenUi { get; }
        void ShowLoaderUi();
        void HideLoaderUi();

        bool IsShowScreenLoader { get; }
        void ShowScreenLoader();
        void HideScreenLoader();
    }
}
