using Microsoft.AspNetCore.Mvc;
using RhManagementApi.Data;

namespace RhManagementApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PeopleController : ControllerBase
    {
        private readonly AdventureWorksContext db;
        public PeopleController(AdventureWorksContext db)
        {
            this.db = db;
        }
    }
}

