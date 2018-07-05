using shared;

namespace API.services
{
    [Service(ServiceScope.Scoped, typeof(IService))]
    public class Service : IService
    {
        public string CallMe()
        {
            return "Im called!";
        }
    }
}