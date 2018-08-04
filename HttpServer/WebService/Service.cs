using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using HttpServer.Enums;
using HttpServer.Interfaces;
using HttpServer.Sessions;

namespace HttpServer.WebService
{
    class Service : IService
    {
        #region Properties

        public string Method { get; protected set; }

        #endregion

        #region Methods

        public static async Task<Service> CreateAsync(RequestSession session)
        {
            var stream = new MemoryStream();
            await session.ReadStreamAsync(stream);
            stream.Position = 0;
            // TODO:
            //    string jason = UrlUtility.GetJsonFromUrlParameters(Parameters);
            //    body = Encoding.UTF8.GetBytes(jason);
            //    ms.Write(body, 0, body.Length);
            //    ms.Position = 0;
            var method = session.Headers.Url.Substring(session.Headers.Url.LastIndexOf('/') + 1);
            return new Service(session, stream, method);
        }

        public T GetInput<T>()
        {
            var type = typeof(T);
            var deser = new DataContractJsonSerializer(type);
            return (T)deser.ReadObject(stream);
        }

        public async Task SendResultAsync(object result)
        {
            var type = result.GetType();
            var jason = new DataContractJsonSerializer(type);
            var memStm = new MemoryStream();

            Stream streamToDeserialize;
            switch (session.Headers.ContentEncoding)
            {
                case ContentEncoding.Deflate:
                    streamToDeserialize = new DeflateStream(memStm, System.IO.Compression.CompressionMode.Compress, true);
                    break;
                case ContentEncoding.GZip:
                    streamToDeserialize = new GZipStream(memStm, System.IO.Compression.CompressionMode.Compress, true);
                    break;
                default:
                    streamToDeserialize = memStm;
                    break;
            }
            jason.WriteObject(streamToDeserialize, result);
            if (session.Headers.ContentEncoding != ContentEncoding.None)
                streamToDeserialize.Close();

            memStm.Capacity = (int)memStm.Length;
            await session.SendJsonBytesAsync(memStm.GetBuffer());
        }

        #endregion

        #region Constructor	

        Service(RequestSession session, Stream stream, string method) 
        {
            this.session = session;
            this.stream = stream;
            Method = method;
        }

        #endregion

        #region Fields	

        Stream stream;
        RequestSession session;

        #endregion
    }
}
