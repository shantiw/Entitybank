using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Xml.Linq;
using XData.Data.Services;

namespace XData.Web.Http.Controllers
{
    [RoutePrefix("mgmt")]
    public class MgmtController : ApiController
    {
        private ConfigService ConfigService = new ConfigService();

        [HttpPut]
        [Route("database/{name}")]
        public void UpdateDatabase(string name)
        {
            ConfigService.UpdateDatabase(name);
        }

        [HttpPut]
        [Route("dateFormatters")]
        public void UpdateDateFormatters()
        {
            ConfigService.UpdateDateFormatters();
        }

        [HttpPut]
        [Route("dataConverters")]
        public void UpdateDataConverters()
        {
            ConfigService.UpdateDataConverters();
        }

        [Route("config/{*path}")]
        public XElement Get(string path)
        {
            string mapPath = System.Web.HttpContext.Current.Server.MapPath("/" + path);
            XElement element = XElement.Load(mapPath + ".config");
            return element;
        }


    }
}
