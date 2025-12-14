namespace PostApiService.Interfaces
{
    public interface ISnippetGeneratorService
    {
        string CreateSnippet(string content, string searchKeyword, int contextLength);
    }
}
