namespace UniversalProjectManager
{
    public interface IProjectAgent
    {
        string Name { get; }

        Task ExecuteAsync(ProjectState state);
    }
}
