using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;

namespace AutoAlloApp
{

    static class Program
    {

        public static float scale = 1.5f;

        //File locations
        private static string EXCLUDEDSPOTSLOCATION = (AppDomain.CurrentDomain.BaseDirectory + "Excluded.csv").Replace("AutoAlloApp\\bin\\Debug\\net5.0\\", "");
        private static string MAPLOCATION = (AppDomain.CurrentDomain.BaseDirectory + "Map.csv").Replace("AutoAlloApp\\bin\\Debug\\net5.0\\", "");
        private static string EXPORTLOCATION = (AppDomain.CurrentDomain.BaseDirectory + "Export.csv").Replace("AutoAlloApp\\bin\\Debug\\net5.0\\", "");
        private static string OLDEXPORTLOCATION = (AppDomain.CurrentDomain.BaseDirectory + "OldExport.csv").Replace("AutoAlloApp\\bin\\Debug\\net5.0\\", "");
        private static string ROOTLOCATION = AppDomain.CurrentDomain.BaseDirectory.Replace("AutoAlloApp\\bin\\Debug\\net5.0\\", "");

        //The main grid of the parking garage
        public static string[,] matrix;

        static List<Reservation> reservations;
        static List<Building> buildings;

        //Used only to allocate employees. They need a building to use the pathfinding alorithm ):
        static Building entrance;

        //Key: Spot name. Value: occupied
        static Dictionary<string, bool> spots;

        //Key: Spot name. Value: occupied. Spots that are never in use by 
        static Dictionary<string, bool> employeeSpots;

        static List<string> employeeList;

        static void Main(string[] args)
        {
            employeeList = new();
            employeeSpots = new();
            reservations = new();
            buildings = new();
            spots = new();

            RemoveDuplicatesFromExport();

            FillReservations();

            FillMatrix();

            //Allocate spots for 1. and 2. prio
            UpdateBuildingsNumberOfSpots(new int[] { 1, 2 });
            AssignSpotsToBuildings();
            AssignSpotsToReservations();

            //Allocate spots for 3. prio
            UpdateBuildingsNumberOfSpots(new int[] { 1, 2, 3 });
            AssignSpotsToBuildings();
            AssignSpotsToReservations();

            PrintData();

            CreateResultCSV();

            //Not neccessary in production
            CreateHeatMap();

        }

        /// <summary>
        /// Removes all duplicate personkeys with the same date from export list. only keeps the best contracts
        /// </summary>
        private static void RemoveDuplicatesFromExport()
        {
            string[] lines = File.ReadAllLines(EXPORTLOCATION);
            List<string> newLines = new();
            List<(string, string)> personKeys = new();
            foreach (string line in lines)
            {
                if (personKeys.Contains((line.Split(";")[2], line.Split(";")[0])))
                {
                    continue;
                }
                else
                {
                    personKeys.Add((line.Split(";")[2], line.Split(";")[0]));
                    newLines.Add(line);
                }
            }

            File.WriteAllLines(EXPORTLOCATION, newLines, Encoding.UTF8);
        }

        private static void UpdateBuildingsNumberOfSpots(int[] includedPriorities)
        {
            //Sets the number of reservations (with parking) per building
            foreach (Building building in buildings)
            {
                building.neededNumberOfSpots = building.NumberOfReservations(includedPriorities);
            }
        }

        private static void AssignSpotsToBuildings()
        {
            //Combine buildings and spots
            while (buildings.Any(x => x.PercentageAllocated < 1))
            {
                buildings.Where(x => x.PercentageAllocated < 1).OrderByDescending(x => x.Badness).First().AquireNearestSpot(spots);
            }
        }

        /// <summary>
        /// Gives each reservation a spot. Cannot give more spots than there are in Building.spots lists.
        /// </summary>
        private static void AssignSpotsToReservations()
        {
            //combine reservations and spots
            foreach (Reservation res in reservations)
            {

                Building building = buildings.First(x => x.Name == res.Building);

                if (building.spots.Count() == 0)
                    continue;

                if (res.ParkingSpot.Length > 0)
                {
                    //reservations already have parkingspot
                    building.spots.Remove(res.ParkingSpot);
                    building.temp.Add(res.ParkingSpot);
                }
                else
                {
                    //reservations do not have a parkingspot
                    string bestSpot = building.spots.First();

                    res.ParkingSpot = bestSpot;
                    building.spots.Remove(bestSpot);
                    building.temp.Add(bestSpot);
                }

            }

            //Add the spots back to the spot array
            foreach (Building b in buildings)
            {
                b.spots.AddRange(b.temp);
            }
        }

        private static void FillMatrix()
        {
            string[] lines = File.ReadAllLines(MAPLOCATION);
            int rowCount = lines.Length; //Y values
            int columnCount = lines[0].Split(';').Length; //X values

            matrix = new string[columnCount, rowCount];

            for (int y = 0; y < rowCount; y++)
            {

                string[] splitLine = lines[y].Split(';');

                for (int x = 0; x < columnCount; x++)
                {
                    string cell = splitLine[x].Trim();
                    matrix[x, y] = cell;

                    if (spots.ContainsKey(cell))
                    {
                        throw new InvalidDataException();
                    }

                    //Indexing
                    //   V

                    //Cell is a comment
                    if (cell.Contains("#"))
                        continue;
                    
                    //Cell i a building
                    if (cell.IsBuilding())
                        buildings.Add(new Building(cell, new Point(x, y)));
                    
                    //Cell is a normal parking spot
                    else if (cell.IsSpot() && !cell.IsExcluded())
                        spots.Add(cell, false);
                    
                    else if (cell.Equals("Enter"))
                        entrance = new Building("X 25", new Point(x, y));
                }
            }
        }

        private static bool IsExcluded(this string cell) {
            string[] lines = File.ReadAllLines(EXCLUDEDSPOTSLOCATION);
            return lines.Contains(cell);
        }

        private static void PrintData()
        {
            float avereageDistance = 0;
            float highest = 0;
            float lowest = 9999;

            foreach (Building b in buildings) {
                float thisAvg = b.AvgWalkDistance;
                highest = highest > thisAvg ? highest : thisAvg;
                lowest = lowest < thisAvg ? lowest : thisAvg;
                avereageDistance += thisAvg;
            }

            Console.WriteLine($"Avareage distance: {avereageDistance / buildings.Count} Biggest difference: {highest - lowest}");

            Console.WriteLine($"SOMMER in U2: {reservations.Where(x => x.PriorityNumber == 3 && x.ParkingSpot.Contains("U2")).Count()}");
            Console.WriteLine($"SOMMER in A-G: {reservations.Where(x => x.PriorityNumber == 3 && !x.ParkingSpot.Contains("U2")).Count()}");
            Console.WriteLine($"SLT and RES in U2: {reservations.Where(x => x.PriorityNumber is 1 or 2 && x.ParkingSpot.Contains("U2")).Count()}");
            Console.WriteLine($"SLT and RES in A-G: {reservations.Where(x => x.PriorityNumber is 1 or 2 && !x.ParkingSpot.Contains("U2")).Count()}");
        }

        private static void CreateHeatMap()
        {
            string[] lines = File.ReadAllLines(MAPLOCATION);
            int rowCount = lines.Length; //Y values
            int columnCount = lines[0].Split(';').Length; //X values

            for (int y = 0; y < rowCount; y++)
            {

                string[] splitLine = lines[y].Split(';');

                for (int x = 0; x < columnCount; x++)
                {
                    string cell = splitLine[x].Trim();

                    if (cell.IsSpot() && spots.ContainsKey(cell))
                    {

                        //This query is kinda time consuming
                        Reservation? res = reservations.FirstOrDefault(x => x.ParkingSpot == cell);

                        if (res != null)
                        {
                            splitLine[x] = cell + " Z " + res.BuildingNumber.ToString();
                        }

                    }

                }

                lines[y] = String.Join(";", splitLine);
            }

            File.WriteAllLines(ROOTLOCATION + "HeatMap.csv", lines, Encoding.UTF8);
        }

        /// <summary>
        /// Creates the final csv file for importing into HotelAdmin
        /// </summary>
        private static void CreateResultCSV()
        {
            string[] lines = new string[reservations.Count + 1];

            lines[0] = "SpotNumber;Priority;PersonKey;RoomKey;ArrivalDate";

            for (int i = 1; i <= reservations.Count; i++) {
                Reservation res = reservations[i-1];
                lines[i] = res.ParkingSpot + ";" + res.PriorityNumber + ";" + res.PersonKey + ";" + res.RoomKey + ";" + res.ArrivalDate;
            }

            File.WriteAllLines(ROOTLOCATION + "Result.csv", lines, Encoding.UTF8);
        }

        /// <summary>
        /// Return true if the cell is a normal parking spot. Excludes HC
        /// </summary>
        /// <param name="cell"></param>
        /// <returns></returns>
        public static bool IsSpot(this string cell)
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
        /// Returns the number of reservations for a given building
        /// </summary>
        /// <param name="building"></param>
        /// <returns></returns>
        private static int NumberOfReservations(this Building building, int[] includedPriorities) {

            int newReservationsCount = reservations.Where(x => x.BuildingNumber == building.BuildingNumber && includedPriorities.Contains(x.PriorityNumber)).Count();
            return newReservationsCount;
        }

        /// <summary>
        /// Fills up the reservation list with reservation export from database
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

                //If roomkey doesn't exist. Skip to the next
                if (splitLine[3] == "NULL" || splitLine[3] == "")
                    continue;

                Reservation res = new Reservation(splitLine[2], splitLine[5], splitLine[3], splitLine[4], splitLine[0], splitLine[1]);

                //If the personkey already list: prioritize the one with the highest contract type.
                if (reservations.Any(x => x.PersonKey == res.PersonKey))
                {
                    if (reservations.First(x => x.PersonKey == res.PersonKey).PriorityNumber > res.PriorityNumber)
                    {
                        reservations.Remove(reservations.First(x => x.PersonKey == res.PersonKey));
                    }
                    else {
                        continue;
                    }
                }

                reservations.Add(res);
            }

            //how the reservations should be ordered.
            //Take history into consideration in the future
            reservations = reservations.OrderByDescending(x => x.InvertedPriorityNumber).ToList();
        }
    }
        
}
