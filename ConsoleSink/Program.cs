using System;
using System.Collections.Generic;
using System.Linq;
using Common;

namespace ConsoleSink
{
    class Program
    {
        class Tick
        {
            public string Key { get; }
            public Dictionary<string, string> Pairs { get; }

            public Tick(string key)
            {
                Key = key;
                Pairs = new Dictionary<string, string>();
            }

            public void Collect(string key, string value)
            {
                if (Pairs.ContainsKey(key))
                {
                    Pairs[key] = value;
                }
                else
                {
                    Pairs.Add(key, value);
                }
            }
        }
        
        private static WebsocketConnection _webSocketConnection;
        
        static void Main(string[] args)
        {
            if (args.Length < 3)
            {
                Console.WriteLine(
                    @"Usage:
ConsoleSink.exe 221 ""ws://schuecobe5hackdays.azurewebsites.net/WebSocketServer.ashx?"" ""userdefined_string_1,wind_direction,wind_speed,sun_state,userdefined_double_1,userdefined_double_2,userdefined_double_3""

Where ..
..the first argument is the 'propertyId' (supply 0 for a new one)
..the second argument is the 'endpointUrl'
..the third argument is a list of value_names that should be printed");
                return;
            }
            
            var propertyId = int.Parse(args[0]);
            var endpointUrl = args[1];
            var filter = args[2].Split(',').Select(o => o.Trim()).ToHashSet();

            string currentTickTime = null;
            Tick currentTick = null;
            
            Console.WriteLine($"tick,sensor,value");
            
            _webSocketConnection = new WebsocketConnection(propertyId, endpointUrl, true);
            _webSocketConnection.OnMessage += (s, e) =>
            {
                var valueName = e.Message["value_name"];
                if (valueName == null)
                {
                    return;
                }
                
                var value = e.Message["value"].ToString();

                if (valueName.ToString() == "userdefined_string_1")
                {
                    if (value != currentTickTime)
                    {
                        if (currentTick != null && currentTick.Pairs.Count > 1)
                        {
                            foreach (var pair in currentTick.Pairs)
                            {
                                Console.WriteLine($"{currentTick.Key},{pair.Key},{pair.Value}");
                            }
                        }
                        currentTick = new Tick(value);
                        currentTickTime = value;
                    }
                }

                if (!filter.Contains(valueName.ToString()))
                {
                    return;
                }

                currentTick?.Collect(valueName.ToString(), value);
            };

            Console.ReadLine();
        }
    }
}