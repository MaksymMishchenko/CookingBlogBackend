namespace PostApiService.Interfaces
{
    public interface IHtmlSanitizationService
    {
        string SanitizeComment(string html);        
    }
}
