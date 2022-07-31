namespace Judge.Docker.ImagesContent
{
    public interface IImageContentBuidersFactory
    {
        IImageContentBuider BuildImageContentBuider(string lang);
    }
}