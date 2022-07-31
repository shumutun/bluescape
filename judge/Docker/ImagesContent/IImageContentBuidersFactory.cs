namespace Judge.Docker.ImageContent
{
    public interface IImageContentBuidersFactory
    {
        IImageContentBuider BuildImageContentBuider(string lang);
    }
}