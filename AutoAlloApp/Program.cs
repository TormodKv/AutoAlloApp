using System.Collections.Generic;
using System.Drawing;
using System.IO;

namespace AutoAlloApp
{
    static class Program
    {

        private static string MAPLOCATION = (AppDomain.CurrentDomain.BaseDirectory + "Map.csv").Replace("AutoAlloApp\\bin\\Debug\\net5.0\\", "");
        private static string EXPORTLOCATION = (AppDomain.CurrentDomain.BaseDirectory + "Export.csv").Replace("AutoAlloApp\\bin\\Debug\\net5.0\\", "");
        private static string RESULTLOCATION = (AppDomain.CurrentDomain.BaseDirectory).Replace("AutoAlloApp\\bin\\Debug\\net5.0\\", "");

        static string[,] matrix;
        static Dictionary<string, Point> buildingLocations;
        
        //normal parking spots
        static Dictionary<string, Point> spotLocations;

        static List<Reservation> reservations;
        
        static void Main(string[] args)
        {
            buildingLocations = new();
            reservations = new();

            FillReservations();

            string[] lines = File.ReadAllLines(MAPLOCATION);
            int rowCount = lines.Length; //Y values
            int columnCount = lines[0].Split(';').Length; //X values

            matrix = new string[columnCount , rowCount];

            for (int y = 0; y < rowCount; y++) {

                string[] splitLine = lines[y].Split(';');

                for (int x = 0; x < columnCount; x++)
                {
                    string cell = splitLine[x].Trim();
                    matrix[x, y] = cell;

                    //Indexing for good measure
                    //           V

                    //Cell is a comment
                    if (cell.Contains("#"))
                        return;

                    //Cell i a building
                    if (cell.IsBuilding())
                        buildingLocations.Add(cell, new Point(x, y));

                    //Cell is a normal parking spot
                    else if (cell.IsSpot())
                        spotLocations.Add(cell, new Point(x, y));

                }
            }

        }

        /// <summary>
        /// Return true if the cell is a normal parking spot
        /// </summary>
        /// <param name="cell"></param>
        /// <returns></returns>
        private static bool IsSpot(this string cell)
        {
            if (cell.Trim().Split(" ").Length == 2)
            {
                string[] cellParts = cell.Split(" ");
                if ((cellParts[0] is "A" or "B" or "C" or "D" or "E" or "F" or "G" or "U2") && cellParts[1].Length == 3)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Return true if the cell is a handicap parking spot
        /// </summary>
        /// <param name="cell"></param>
        /// <returns></returns>
        private static bool IsHCSpot(this string cell)
        {
            if (cell.Contains("HC") && !cell.Contains("#"))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Return true if the cell is a building
        /// </summary>
        /// <param name="cell"></param>
        /// <returns></returns>
        private static bool IsBuilding(this string cell)
        {
            if (cell.Contains("X ") && !cell.Contains("#"))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Fills up the reservation list with reservation export
        /// </summary>
        private static void FillReservations()
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
