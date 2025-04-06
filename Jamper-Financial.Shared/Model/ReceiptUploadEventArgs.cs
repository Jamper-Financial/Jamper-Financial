namespace Jamper_Financial.Shared.Models
{
public class ReceiptUploadEventArgs : EventArgs
{
    public List<byte[]> UploadedImages { get; set; }
    public List<string> FileNames { get; set; }
}
}
