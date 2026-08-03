namespace UniversalProjectManager
{
    internal interface IProjectAgent
    {
        string Name { get; }

        Task ExecuteAsync(ProjectState state);
    }
}
