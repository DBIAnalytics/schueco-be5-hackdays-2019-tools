using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WebSocketSharp;

namespace WeatherDataReplay
{
    class Program
    {
        private static WebSocket _webSocket;

        static void Main(string[] args)
        {
            if (args.Length < 3)
            {
                Console.WriteLine(
                    @"Usage:
WeatherDataReplay.exe 221 ""ws://schuecobe5hackdays.azurewebsites.net/WebSocketServer.ashx?"" ""C:\path\to\file.csv""

Where ..
..the first argument is the 'propertyId' (supply 0 for a new one)
..the second argument is the 'endpointUrl'
..the third argument is the 'inputFile'");
                return;
            }

            var propertyId = int.Parse(args[0]);
            var endpointUrl = args[1];
            var inputFile = (File.Exists(args[2]) ? args[2] : null) ??
                            throw new Exception(
                                $"The specified input file doesn't exist or is not accessible: {args[2]}");

            _webSocket = new WebSocket(endpointUrl);

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
                // Wait for init reply
                var message = (JObject) JsonConvert.DeserializeObject(e.Data);
                Console.WriteLine($"Received WS message: \n{e.Data}");

                var t = message["type"];
                var tv = t.ToString();

                var pn = message["value_name"];
                var pnVv = pn.ToString();

                if (tv == "property_update" &&
                    pnVv == "prop_number")
                {
                    // Init reply arrived
                    int.Parse(message["value"].Value<string>());
                    Console.WriteLine($"PropertyId is: \n{propertyId}");
                }
            };

            _webSocket.OnClose += (s, e) => { };
            _webSocket.OnError += (s, e) =>
            {
                // Fuck up
                Console.WriteLine($"Fuckup: {e.Message}, Ex: {e.Exception.Message + "\n" + e.Exception.StackTrace}");
                throw e.Exception;
            };

            _webSocket.Connect();

            var oneSecond = TimeSpan.FromSeconds(1);
            var dateRegex = new Regex(@"\d\d\d\d-\d\d-\d\d \d\d:\d\d:\d\d.*", RegexOptions.Compiled);

            var thread = new Thread(() =>
            {
                /*
                 C1	C2	C3	C4	C5	C6	C7	C8	C9	C10	C11	C12	C13	C14	C15	C16	C17	C18
                 TIMESTAMP	RECORD	year	dayofyear	hours	minutes	BattV_Avg	SWtot_Avg	SWdif_Avg	Wdir_Avg	Wavg_Avg	Wmax_Max	Tair_Avg	RHum_Avg	Pair_Avg	Rain	Rintensity_Avg	Hail
                 TS	RN	""	""	""	""	Volts	W/meter	W/meter	degrees	m/s	m/s	deg C	%	Pa	mm	mm/h	hits/cm2
                 ""	""	Smp	Smp	Smp	Smp	Avg	Avg	Avg	Avg	Avg	Max	Avg	Avg	Avg	Smp	Avg	Smp
                 2015-09-21 22:23:00	45287	2015	264	22	23	13.45949	0.3366436	0.5049654	266.1667	4.4	6.1	10.9	79.45	989.5	59.97	0	0
                 2015-09-21 22:24:00	45288	2015	264	22	24	13.4604	0.3366436	0.5049654	250.1667	3.8	5.5	10.9	79.58333	989.5	59.97	0	0
                 2015-09-21 22:25:00	45289	2015	264	22	25	13.46223	0.3226168	0.5049654	258.6667	4.45	6.8	10.9	79.48333	989.5	59.97	0	0             
                 */
                using (var streamReader = new StreamReader(inputFile))
                {
                    while (!streamReader.EndOfStream)
                    {
                        var line = streamReader.ReadLine();
                        if (line == null)
                        {
                            continue;
                        }

                        if (!dateRegex.Match(line).Success)
                        {
                            continue;
                        }

                        // 0: timestamp
                        // 1: No.
                        // 2: Year
                        // 3: DayOfYear
                        // 4: Hours
                        // 5: Minutes
                        // 6: Battery voltage
                        // 7: SWtot_Avg ???
                        // 8: SWdif_Avg ???
                        // 9: Wind direction (Wdir_Avg - °)
                        // 10: Wind avg. speed (Wavg_Avg - m/s)
                        // 11: Wind max. speed (Wmax_Max - m/s)
                        // 12: Air temperature (Tair_Avg - °C)
                        // 13: Relative humidity (RHum_Avg - %)
                        // 14: Air pressure (Pair_Avg - pascal)
                        // 15: Rain (Rain - mm)
                        // 16: Rain intensity (Rintensity_Avg - mm/h)
                        // 17: Hail (Hail - hits/cm2)
                        var cells = line.Split(',');

                        if (!DateTime.TryParse(cells[0].Replace("\"", ""), out var time))
                        {
                            continue;
                        }

                        if (!decimal.TryParse(cells[15], out var rain))
                        {
                            continue;
                        }

                        var light = "";
                        if (time.TimeOfDay >= new DateTime(2001, 01, 01, 06, 00, 00).TimeOfDay
                            && time.TimeOfDay <= new DateTime(2001, 01, 01, 19, 00, 00).TimeOfDay)
                        {
                            // Could be sunny or cloudy
                            if (rain < 1)
                            {
                                light = "Sunny";
                            }
                            else if (rain >= 1)
                            {
                                light = "Cloudy";
                            }
                            else if (rain > 2)
                            {
                                light = "Rainy";
                            }
                        }
                        else
                        {
                            // Dark
                            light = "Dark";
                        }

                        // Timetamp
                        var changeMessage = new {type = "set_value", value_name = "userdefined_string_1", value = cells[0].Replace("\"", "")};
                        _webSocket.Send(JsonConvert.SerializeObject(changeMessage));

                        changeMessage = new {type = "set_value", value_name = "wind_direction", value = cells[9]};
                        _webSocket.Send(JsonConvert.SerializeObject(changeMessage));

                        changeMessage = new {type = "set_value", value_name = "wind_speed", value = cells[10]};
                        _webSocket.Send(JsonConvert.SerializeObject(changeMessage));

                        changeMessage = new
                            {type = "set_value", value_name = "ambient_temperature", value = cells[12]};
                        _webSocket.Send(JsonConvert.SerializeObject(changeMessage));

                        changeMessage = new {type = "set_value", value_name = "sun_state", value = light};
                        _webSocket.Send(JsonConvert.SerializeObject(changeMessage));

                        // Custom field: Rain
                        changeMessage = new
                            {type = "set_value", value_name = "userdefined_double_1", value = cells[15]};
                        _webSocket.Send(JsonConvert.SerializeObject(changeMessage));

                        // Custom field: Relative humidity
                        changeMessage = new
                            {type = "set_value", value_name = "userdefined_double_2", value = cells[13]};
                        _webSocket.Send(JsonConvert.SerializeObject(changeMessage));

                        // Custom field: Air pressure
                        changeMessage = new
                            {type = "set_value", value_name = "userdefined_double_3", value = cells[14]};
                        _webSocket.Send(JsonConvert.SerializeObject(changeMessage));


                        Thread.Sleep(oneSecond);
                    }
                }
            });

            thread.Start();

            Console.WriteLine(">> Press enter to cancel");
            Console.ReadLine();
        }
    }
}