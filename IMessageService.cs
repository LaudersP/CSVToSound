namespace CSVToSound
{
    public interface IMessageService
    {
        Task DisplayAlert(string title, string message, string cancel);
    }
}