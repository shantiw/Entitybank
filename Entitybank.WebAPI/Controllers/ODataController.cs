using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Xml.Linq;
using XData.Web.Http.Models;

namespace XData.Web.Http.Controllers
{
    [RoutePrefix("OData")]
    public class ODataController : ApiController
    {
        private ODataModel ODataModel = new ODataModel();

        [HttpGet]
        [Route("{name}/$now")]
        public HttpResponseMessage Now(string name)
        {
            return ODataModel.GetNow(name, Request);
        }

        [HttpGet]
        [Route("{name}/$utcnow")]
        public HttpResponseMessage UtcNow(string name)
        {
            return ODataModel.GetUtcNow(name, Request);
        }

        [HttpGet]
        [Route("{name}/{collection}/$count")]
        public HttpResponseMessage Count(string name, string collection)
        {
            return ODataModel.Count(name, collection, Request);
        }

        [HttpGet]
        [Route("{name}/{entity}/$default")]
        public HttpResponseMessage Default(string name, string entity)
        {
            return ODataModel.GetDefault(name, entity, Request);
        }

        [Route("{name}/{collection}")]
        public HttpResponseMessage Get(string name, string collection)
        {
            if (collection.IndexOf('(') == -1)
            {
                return ODataModel.GetCollection(name, collection, Request);
            }
            else
            {
                // {collection}({key})
                return ODataModel.Find(name, collection, Request);
            }
        }

        [Route("{name}/{entity}")]
        public HttpResponseMessage Post(string name, string entity, [FromBody]object value)
        {
            return ODataModel.Create(name, entity, value, Request);
        }

        [Route("{name}/{entity}")]
        public void Delete(string name, string entity, [FromBody]object value)
        {
            ODataModel.Delete(name, entity, value, Request);
        }

        [Route("{name}/{entity}")]
        public void Put(string name, string entity, [FromBody]object value)
        {
            ODataModel.Update(name, entity, value, Request);
        }

        [HttpPost]
        [Route("{name}")]
        public HttpResponseMessage PostElement(string name, [FromBody]XElement value)
        {
            return ODataModel.Create(name, value, Request);
        }

        [HttpDelete]
        [Route("{name}")]
        public void DeleteElement(string name, [FromBody]XElement value)
        {
            ODataModel.Delete(name, value, Request);
        }

        [HttpPut]
        [Route("{name}")]
        public void PutElement(string name, [FromBody]XElement value)
        {
            ODataModel.Update(name, value, Request);
        }


    }
}
