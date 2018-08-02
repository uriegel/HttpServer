using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HttpServer.Enums;

namespace HttpServer.Interfaces
{
    public class IExtension
    {
        public Task InitializeAsync(IServer server) => throw new NotImplementedException();
        public Task ServiceRequestAsync(IService service) => throw new NotImplementedException();
        public Task RequestAsync(ISession service, Method method, string path, UrlQueryComponents query) 
            => throw new NotImplementedException();
        public Task ShutdownAsync() => throw new NotImplementedException();
        public Task<bool> OnErrorAsync(Exception e, IService service) => throw new NotImplementedException();
    }
}
