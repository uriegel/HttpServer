using System.IO;
using System.Threading.Tasks;
using Http2;

namespace HttpServer.Sessions
{
    class RequestSession2 : RequestSession
    {
        public RequestSession2(Server server, SocketSession socketSession, Stream networkStream)
            : base(server, socketSession, networkStream)
        {
        }

        public override async Task<bool> StartAsync()
        {
            var http2Session = new Session(networkStream);
            return await http2Session.StartAsync();
        }
    }
}
