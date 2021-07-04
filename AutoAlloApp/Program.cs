using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

namespace AutoAlloApp
{
    class Program
    {

        private static string MAPLOCATION = (AppDomain.CurrentDomain.BaseDirectory + "Map.csv").Replace("AutoAlloApp\\bin\\Debug\\net5.0\\", "");
        private static string EXPORTLOCATION = (AppDomain.CurrentDomain.BaseDirectory + "Export.csv").Replace("AutoAlloApp\\bin\\Debug\\net5.0\\", "");
        private static string RESULTLOCATION = AppDomain.CurrentDomain.BaseDirectory.Replace("AutoAlloApp\\bin\\Debug\\net5.0\\", "");

        static string[,] matrix;
        static Dictionary<string, Point> buildingLocations;
        static List<Reservation> reservations;

        //Key: Spot name. Value: Building affiliation
        static Dictionary<string, string> spots;
        
        static void Main(string[] args)
        {
            buildingLocations = new();
            reservations = new();

            fillReservations();

            string[] lines = File.ReadAllLines(MAPLOCATION);
            int rowsCount = lines.Length; //Y values
            int columnCount = lines[0].Split(';').Length; //X values

            matrix = new string[columnCount , rowsCount];

            for (int y = 0; y < rowsCount; y++) {

                string[] splitLine = lines[y].Split(';');

                for (int x = 0; x < columnCount; x++)
                {
                    string cell = splitLine[x];
                    matrix[x, y] = cell;

                    //Indexing for good measure
                    //           V

                    if (cell.Contains("X "))
                        buildingLocations.Add(cell, new Point(x, y));

                    if (cell.)
                }
            }

        }

        /// <summary>
        /// Fills up the reservation list with reservation export
        /// </summary>
        private static void fillReservations()
        {
            //Eventually replace this with a query to a database
            string[] lines = File.ReadAllLines(EXPORTLOCATION);

            foreach (string line in lines) {

                //Headers is as following:
                //DateArrival [0], DateDeparture [1], ContactExternalId [2], RoomKey [3], ContractType [4], ParkingAgreementCode[5]

                // We are working with literal "NULL" instead of "" for null values.

                string[] splitLine = line.Split(";");
                reservations.Add(new Reservation(splitLine[2], splitLine[5], splitLine[3], splitLine[4]));
            }
        }
    }
        
}
