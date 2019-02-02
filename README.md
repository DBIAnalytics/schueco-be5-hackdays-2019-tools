# schueco-be5-hackdays-2019-tools
Utility tools for the schueco api

## WeatherDataReplay.exe
***Purpose:***  
Replays the 'CR1000_Weather.dat.txt' dataset against the schueco api, acting as simulated sensors.
Data Source: https://www.ed.ac.uk/geosciences/weather-station/weather-station-data

***Usage:***  
WeatherDataReplay.exe 221 "ws://schuecobe5hackdays.azurewebsites.net/WebSocketServer.ashx?" "C:\path\to\file.csv"

Where ..  
..the first argument is the 'propertyId' (supply 0 for a new one)  
..the second argument is the 'endpointUrl'  
..the third argument is the 'inputFile  

You can use the binaries in https://github.com/DBIAnalytics/schueco-be5-hackdays-2019-tools/tree/master/WeatherDataReplay/Binaries or compile the tool from source.  
Build prerequisites are either MonoDevelop, Rider or Visual Studio.  

***Effects:***  
The running application sends one row of the above mentioned file per second to the api.
The following fields are used:  
* userdefined_string_1 -> The timestamp of the current row
* wind_direction -> The wind direction in degrees
* wind_speed -> The wind speed in m/s
* ambient_temperature -> The temparature in °C
* sun_state -> If it's currently 'Dark', 'Sunny', 'Cloudy' or 'Rainy' - all estimated from each row's 'timestamp'
* userdefined_double_1 -> The rain intensity in mm/h
* userdefined_double_2 -> The relative humidity in %
* userdefined_double_3 -> The airpressure in pascal

The application stops when the "Enter" key is pressed.
