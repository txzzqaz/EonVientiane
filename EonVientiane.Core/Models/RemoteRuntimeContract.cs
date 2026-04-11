namespace EonVientiane.Core.Models;

public sealed class RuntimeCommandResult
{
    public bool Handled { get; set; }
    public bool ShouldExit { get; set; }
    public string Output { get; set; } = string.Empty;
}

public interface IRemoteGameRuntime
{
    string RuntimeId { get; }
    string RuntimeVersion { get; }
    void Initialize(string playerName);
    string GetPrompt();
    RuntimeCommandResult Execute(string commandLine);
}
