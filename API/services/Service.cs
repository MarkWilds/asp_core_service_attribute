using shared;

namespace API.services
{
    [Service(ServiceScope.Scoped, false)]
    public class Service : IService
    {
        public string CallMe()
        {
            return "Im called!";
        }
    }
}