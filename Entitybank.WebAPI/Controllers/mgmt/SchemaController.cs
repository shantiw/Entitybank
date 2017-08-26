using XData.Data.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Xml.Linq;

namespace XData.Web.Http.Controllers
{
    [RoutePrefix("mgmt/schema")]
    public class SchemaController : ApiController
    {
        protected SchemaService SchemaService = new SchemaService();

        [Route("{name}")]
        public XElement Get(string name)
        {
            IEnumerable<KeyValuePair<string, string>> deltaKey = Request.GetQueryNameValuePairs();
            return SchemaService.GetSchema(name, deltaKey);
        }

        [Route("{name}/{level}")]
        public XElement Get(string name, int level)
        {
            return SchemaService.GetSchema(name, level);
        }

        [Route("{name}/{level}")]
        public void Put(string name, int level)
        {
            SchemaService.Update(name, level);
        }


    }
}
