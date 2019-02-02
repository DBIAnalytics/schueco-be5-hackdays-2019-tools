using System;
using System.CodeDom;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WebSocketSharp;

namespace Common
{
    public class WebsocketConnection
    {
        private readonly WebSocket _webSocket;

        public event EventHandler<MessageEventArgs> OnMessage;
        public event EventHandler Listening;
        
        public WebsocketConnection(int propertyId, string endpoint, bool silent)
        {
            _webSocket = new WebSocket(endpoint);

            _webSocket.OnOpen += (s, e) =>
            {
                // Send init message
                var initialRequest = new
                {
                    type = "connection_request",
                    request_type = "connect_to_prop",
                    prop_id = propertyId
                };
                var json = JsonConvert.SerializeObject(initialRequest);
                _webSocket.Send(json);
            };

            _webSocket.OnMessage += (s, e) =>
            {
                try
                {
                    var message = (JObject) JsonConvert.DeserializeObject(e.Data);

                    var t = message["type"];
                    var tv = t.ToString();

                    var pn = message["value_name"];
                    var pnVv = pn.ToString();

                    if (tv == "property_update" &&
                        pnVv == "prop_number")
                    {
                        // Init reply arrived
                        var propertyId1 = int.Parse(message["value"].Value<string>());
                        if (!silent)
                        {
                            Console.WriteLine($"PropertyId is: \n{propertyId1}");
                        }

                        Listening?.Invoke(this, EventArgs.Empty);
                    }

                    OnMessage?.Invoke(this, new MessageEventArgs(message));
                }
                catch (Exception ex)
                {
                    if (!silent)
                    {
                        Console.WriteLine($"{ex.Message}\n{ex.StackTrace}");
                    }
                }
            };

            _webSocket.OnClose += (s, e) => { };
            _webSocket.OnError += (s, e) =>
            {
                // Fuck up
                if (!silent)
                {
                    Console.WriteLine(
                        $"Fuckup: {e.Message}, Ex: {e.Exception.Message + "\n" + e.Exception.StackTrace}");
                }

                throw e.Exception;
            };

            _webSocket.Connect();
        }
        
        public void Send(object message)
        {
            _webSocket.Send(JsonConvert.SerializeObject(message));
        }
        
        public class MessageEventArgs : EventArgs
        {
            public JObject Message { get; }

            public MessageEventArgs(JObject message)
            {
                Message = message;
            }
        }
    }
}