using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Xml.Linq;
using XData.Data.Services;

namespace XData.Web.Http.Models
{
    internal class ODataModel
    {
        private static readonly string XSI = "http://www.w3.org/2001/XMLSchema-instance";
        private static readonly XNamespace XSINamespace = XSI;

        public HttpResponseMessage GetNow(string name, HttpRequestMessage request)
        {
            const string FORMAT = "yyyy-MM-ddTHH:mm:ss.FFFFFFFK";

            DateTime now = ODataService.GetNow(name);

            string mediaType = request.GetResponseMediaType();
            switch (mediaType)
            {
                case "application/json":
                    string json = string.Format("{{\"now\": {0}}}", now.ToString(FORMAT));
                    return CreateHttpResponseMessage(json, request);
                case "application/xml":
                    XElement element = new XElement("now", now.ToString(FORMAT));
                    return CreateHttpResponseMessage(element, request);
                default:
                    throw new NotSupportedException(mediaType.ToString());
            }
        }

        public HttpResponseMessage GetUtcNow(string name, HttpRequestMessage request)
        {
            const string FORMAT = "yyyy-MM-ddTHH:mm:ss.FFFFFFFZ";

            DateTime utcNow = ODataService.GetUtcNow(name);

            string mediaType = request.GetResponseMediaType();
            switch (mediaType)
            {
                case "application/json":
                    string json = string.Format("{{\"utcnow\": {0}}}", utcNow.ToString(FORMAT));
                    return CreateHttpResponseMessage(json, request);
                case "application/xml":
                    XElement element = new XElement("utcnow", utcNow.ToString(FORMAT));
                    return CreateHttpResponseMessage(element, request);
                default:
                    throw new NotSupportedException(mediaType.ToString());
            }
        }

        public HttpResponseMessage Count(string name, string collection, HttpRequestMessage request)
        {
            int count = ODataService.Count(name, collection, request.GetQueryNameValuePairs());

            string mediaType = request.GetResponseMediaType();
            switch (mediaType)
            {
                case "application/json":
                    string json = string.Format("{{\"Count\": {0}}}", count);
                    return CreateHttpResponseMessage(json, request);
                case "application/xml":
                    XElement element = new XElement("Count", count);
                    return CreateHttpResponseMessage(element, request);
                default:
                    throw new NotSupportedException(mediaType.ToString());
            }
        }

        public HttpResponseMessage GetDefault(string name, string entity, HttpRequestMessage request)
        {
            string mediaType = request.GetResponseMediaType();
            switch (mediaType)
            {
                case "application/json":
                    {
                        string json = new ODataService<string>(name, request.GetQueryNameValuePairs()).GetDefault(entity);
                        return CreateHttpResponseMessage(json, request);
                    }
                case "application/xml":
                    {
                        XElement element = new ODataService<XElement>(name, request.GetQueryNameValuePairs()).GetDefault(entity, out XElement xsd);
                        element.SetAttributeValue(XNamespace.Xmlns + "i", XSI);

                        return CreateHttpResponseMessage(Pack(element, null, xsd), request);
                    }
                default:
                    throw new NotSupportedException(mediaType.ToString());
            }
        }

        public HttpResponseMessage GetCollection(string name, string collection, HttpRequestMessage request)
        {
            int? count = null;
            if (IsPagingQuery(request))
            {
                count = ODataService.Count(name, collection, request.GetQueryNameValuePairs());
            }

            string mediaType = request.GetResponseMediaType();
            switch (mediaType)
            {
                case "application/json":
                    {
                        ODataService<string> service = new ODataService<string>(name, request.GetQueryNameValuePairs());
                        IEnumerable<string> jsonCollection = service.GetCollection(collection);
                        string json = string.Format("[{0}]", string.Join(",", jsonCollection));
                        if (count != null)
                        {
                            json = string.Format("{{\"@count\":{0},\"value\":{1}}}", count, json);
                        }
                        return CreateHttpResponseMessage(json, request);
                    }
                case "application/xml":
                    {
                        ODataService<XElement> service = new ODataService<XElement>(name, request.GetQueryNameValuePairs());
                        IEnumerable<XElement> xCollection = service.GetCollection(collection, out XElement xsd);
                        XElement element = new XElement(collection, xCollection);
                        element.SetAttributeValue(XNamespace.Xmlns + "i", XSI);

                        return CreateHttpResponseMessage(Pack(element, count, xsd), request);
                    }
                default:
                    throw new NotSupportedException(mediaType.ToString());
            }
        }

        public HttpResponseMessage Find(string name, string collectionKey, HttpRequestMessage request)
        {
            string collection = SplitCollectionKey(collectionKey, out string[] key);

            string mediaType = request.GetResponseMediaType();
            switch (mediaType)
            {
                case "application/json":
                    {
                        ODataService<string> service = new ODataService<string>(name, request.GetQueryNameValuePairs());
                        string json = service.Find(collection, key);
                        return CreateHttpResponseMessage(json, request);
                    }
                case "application/xml":
                    {
                        ODataService<XElement> service = new ODataService<XElement>(name, request.GetQueryNameValuePairs());
                        XElement element = service.Find(collection, key, out XElement xsd);
                        if (element == null)
                        {
                            string entity = service.Schema.Elements("entity").First(x => x.Attribute("collection").Value == collection).Attribute("name").Value;
                            element = new XElement(entity);
                            element.SetAttributeValue(XNamespace.Xmlns + "i", XSI);
                            element.SetAttributeValue(XSINamespace + "nil", "true");
                            return CreateHttpResponseMessage(element, request);
                        }

                        element.SetAttributeValue(XNamespace.Xmlns + "i", XSI);
                        return CreateHttpResponseMessage(Pack(element, null, xsd), request);
                    }
                default:
                    throw new NotSupportedException(mediaType.ToString());
            }
        }

        private static bool IsPagingQuery(HttpRequestMessage request)
        {
            IEnumerable<KeyValuePair<string, string>> nameValues = request.GetQueryNameValuePairs();
            return nameValues.Any(p => p.Key == "$skip") || nameValues.Any(p => p.Key == "$top");
        }

        private static string SplitCollectionKey(string collectionKey, out string[] key)
        {
            int index = collectionKey.IndexOf('(');
            string collection = collectionKey.Substring(0, index).Trim();
            string sKey = collectionKey.Substring(index).Trim();
            sKey = sKey.TrimStart('(').TrimEnd(')');
            string[] keyValues = sKey.Split(',');
            for (int i = 0; i < keyValues.Length; i++)
            {
                keyValues[i] = keyValues[i].Trim();
            }
            key = keyValues;
            return collection;
        }

        private static XElement Pack(XElement element, int? count, XElement xsd)
        {
            if (count == null && xsd == null) return element;

            XElement xml = new XElement("xml");
            if (xsd != null) xml.Add(new XElement("schema", xsd));
            xml.Add(new XElement("element", element));
            if (count != null) xml.Add(new XElement("count", count));
            return xml;
        }

        private static HttpResponseMessage CreateHttpResponseMessage(string json, HttpRequestMessage request)
        {
            HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json ?? "{}", request.GetResponseEncoding(), "application/json")
            };
            return response;
        }

        private static HttpResponseMessage CreateHttpResponseMessage(XElement element, HttpRequestMessage request)
        {
            HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(element.ToString(), request.GetResponseEncoding(), "application/xml")
            };
            return response;
        }

        public HttpResponseMessage Create(string name, string entity, object value, HttpRequestMessage request)
        {
            dynamic obj = JsonConvert.DeserializeObject<dynamic>(value.ToString());
            DynModificationService modificationService = new DynModificationService(name, request.GetQueryNameValuePairs());

            string mediaType = request.GetResponseMediaType();
            switch (mediaType)
            {
                case "application/json":
                    {
                        modificationService.Create(obj, entity, out string json);
                        return CreateHttpResponseMessage(json, request);
                    }
                case "application/xml":
                    {
                        modificationService.Create(obj, entity, out XElement element);
                        return CreateHttpResponseMessage(element, request);
                    }
                default:
                    throw new NotSupportedException(mediaType.ToString());
            }
        }

        public void Delete(string name, string entity, object value, HttpRequestMessage request)
        {
            dynamic obj = JsonConvert.DeserializeObject<dynamic>(value.ToString());
            new DynModificationService(name, request.GetQueryNameValuePairs()).Delete(obj, entity);
        }

        public void Update(string name, string entity, object value, HttpRequestMessage request)
        {
            dynamic obj = JsonConvert.DeserializeObject<dynamic>(value.ToString());
            dynamic original = GetOriginal(obj);

            var service = new DynModificationService(name, request.GetQueryNameValuePairs());
            if (original == null)
            {
                service.Update(obj, entity);
            }
            else
            {
                service.Update(obj, original, entity);
            }
        }

        // json: "@original":{"property":value, ...}
        private static dynamic GetOriginal(dynamic obj)
        {
            return obj["original"];
        }

        public HttpResponseMessage Create(string name, XElement value, HttpRequestMessage request)
        {
            XmlModificationService modificationService = new XmlModificationService(name, request.GetQueryNameValuePairs());

            string mediaType = request.GetResponseMediaType();
            switch (mediaType)
            {
                case "application/json":
                    {
                        modificationService.Create(value, out string json);
                        return CreateHttpResponseMessage(json, request);
                    }
                case "application/xml":
                    {

                        modificationService.Create(value, out XElement element);
                        return CreateHttpResponseMessage(element, request);
                    }
                default:
                    throw new NotSupportedException(mediaType.ToString());
            }
        }

        public void Delete(string name, XElement value, HttpRequestMessage request)
        {
            new XmlModificationService(name, request.GetQueryNameValuePairs()).Delete(value);
        }

        public void Update(string name, XElement value, HttpRequestMessage request)
        {
            XElement original = GetOriginal(value);

            var service = new XmlModificationService(name, request.GetQueryNameValuePairs());
            if (original == null)
            {
                service.Update(value);
            }
            else
            {
                service.Update(value, original);
            }
        }

        // <element>
        // ...
        // <element.original>
        //  <property>value</property> ...
        // </element.original>
        // </element>
        private static XElement GetOriginal(XElement element)
        {
            return element.Element(string.Format("{0}.original", element.Name.LocalName));
        }


    }
}
