using API.services;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Route("[controller]")]
    public class ServiceController : Controller
    {
        private IService _service;

        public ServiceController(IService service)
        {
            _service = service;
        }
        
        [HttpGet]
        public string Get()
        {
            return _service.CallMe();
        }
    }
}
