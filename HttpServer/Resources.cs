using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HttpServer.Isapi;
using HttpServer.Sessions;
using HttpServer.WebSockets;

namespace HttpServer
{
    class Resources
    {
        public static Resources Current { get; } = new Resources();

        public void Initialize()
        {
        }

        public void RegisterCounter(string name, Func<int[]> getCounter)
        {
            counterDatas = counterDatas.Concat(new[] { new CounterData(name, getCounter) }).ToArray();
        }

        public async Task SendAsync(RequestSession session)
        {
            await session.SendHtmlStringAsync(
$@"<!DOCTYPE html>
<html>
    <head>
        <title>CAESAR Web Server</title>
        <meta charset=""utf-8"" lang=""de"" />
        <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"" />
        <Style> 
            html {{
                font-family: sans-serif;
            }}
            h1, th {{
                font-weight: 100;
            }}
            table {{
                border-collapse: collapse;
            }}
            th, td {{
                padding: 5px;
                border-style: solid;
                border-width: 1px;
                border-color: lightgray;
            }}
            caption {{
                caption-side: bottom;
                padding-top: 5px;
            }}
            .right {{
                text-align: right;
            }}
        </Style>
        <script>
            document.addEventListener('DOMContentLoaded', function () {{
                var gc = document.getElementById('gc')
                gc.onclick = function() {{
                    var xmlhttp = new XMLHttpRequest()
                    xmlhttp.open('POST', '/$$GC', true)
                    xmlhttp.setRequestHeader('Content-Type', 'application/json; charset=utf-8')
                    xmlhttp.send('')
                }}
                var ar = document.getElementById('autorefresh')

                var autorefreshTimer
                ar.onchange = () => {{
                    if (ar.checked) 
                    {{
                        location.hash = ""auto""
                        autorefreshTimer = setTimeout(() => {{
                            location.reload()                            
                        }}, 1000)
                    }}
                    else 
                    {{
                        location.hash = """"
                        clearTimeout(autorefreshTimer)
                    }}
                }}

                if (location.hash = ""auto"") {{
                    ar.checked = true
                    autorefreshTimer = setTimeout(() => {{
                        location.reload()                            
                    }}, 1000)
                }}
            }})
        </script>
    <head>
    <body>
        <h1>Ressourcenverbrauch</h1>
        <p>
            CAESAR WEB Server aktiv seit {started.ToString("f")}
        </p>
        <table> 
            <caption>Anzahl Sessions</caption>
            <tr>
                <th>Art</th>
                <th>Aktuell</th>		
                <th>Aktiv</th>		
                <th>Total</th>		
            </tr>
            <tr>
                <td>Verbindungen:</td>
                <td class=""right"">{SocketSession.Instances.Count}</td>
                <td class=""right"">-</td>
                <td class=""right"">{SocketSession.Instances.TotalCount}</td>
            </tr>
            <tr>
                <td>Anfragen:</td>
                <td class=""right"">{RequestSession.Instances.Count}</td>
                <td class=""right"">{RequestSession.Instances.ActiveCount}</td>
                <td class=""right"">{RequestSession.Instances.TotalCount}</td>
            </tr>
            <tr>
                <td>Erweiterungen:</td>
                <td class=""right"">{Extension.Instances.Count}</td>
                <td class=""right"">{Extension.Instances.ActiveCount}</td>
                <td class=""right"">{Extension.Instances.TotalCount}</td>
            </tr>
            <tr>
                <td>Erweiterungen (Socket-Sessions):</td>
                <td class=""right"">{WebSocketSession.Instances.Count}</td>
                <td class=""right"">{WebSocketSession.Instances.ActiveCount}</td>
                <td class=""right"">{WebSocketSession.Instances.TotalCount}</td>
            </tr>
            <tr>
                <td>Isapi:</td>
                <td class=""right"">{IsapiSession.Instances.Count}</td>
                <td class=""right"">{IsapiSession.Instances.ActiveCount}</td>
                <td class=""right"">{IsapiSession.Instances.TotalCount}</td>
            </tr>
            {string.Join("\r\n", from n in counterDatas
                                 let cs = n.GetCounter()
                                 let c = cs[0]
                                 let t = cs[1]
                                 let a = cs.Length > 2 ? cs[2].ToString() : "-"
                                 select
                   $@"<tr>
    <td>{n.Name}:</td>
    <td class=""right"">{c}</td>
    <td class=""right"">{a}</td>
    <td class=""right"">{t}</td>
</tr>")}
        </table>
        <p>
            <span>Speicheroptimierung anstoßen: </span>
            <button id=""gc"">OK</button>    
        </p>
        <p>
            <input type=""checkbox"" id=""autorefresh"" />
            <span>Autorefresh</span>
        </p>
    </body>
</html>");
        }

        Resources()
        {
        }

        struct CounterData
        {
            public string Name;
            public Func<int[]> GetCounter;

            public CounterData(string name, Func<int[]> getCounter)
            {
                Name = name;
                GetCounter = getCounter;
            }
        }

        DateTime started = DateTime.Now;
        CounterData[] counterDatas = new CounterData[0];
    }
}
