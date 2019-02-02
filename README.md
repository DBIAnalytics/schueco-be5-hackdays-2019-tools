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
