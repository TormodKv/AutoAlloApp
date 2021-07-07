using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;

namespace AutoAlloApp
{
    enum AllocateAlgorithm { 
        FirstAndBest,
        FirstAndBestFair,
        CustomOrder,
        CustomOrderSinglePercent
    }

    static class Program
    {
        private static AllocateAlgorithm AlloAlgo = AllocateAlgorithm.CustomOrderSinglePercent;
        static int[] customOrder = new int[] { 1, 3, 5, 22, 24, 7, 18, 20, 16, 2, 10, 14 ,12, 4};
        static float singlePercent = 0.7f;

        private static string MAPLOCATION = (AppDomain.CurrentDomain.BaseDirectory + "Map.csv").Replace("AutoAlloApp\\bin\\Debug\\net5.0\\", "");
        private static string EXPORTLOCATION = (AppDomain.CurrentDomain.BaseDirectory + "Export.csv").Replace("AutoAlloApp\\bin\\Debug\\net5.0\\", "");
        private static string OLDEXPORTLOCATION = (AppDomain.CurrentDomain.BaseDirectory + "OldExport.csv").Replace("AutoAlloApp\\bin\\Debug\\net5.0\\", "");
        private static string RESULTLOCATION = AppDomain.CurrentDomain.BaseDirectory.Replace("AutoAlloApp\\bin\\Debug\\net5.0\\", "");

        public static string[,] matrix;

        static List<Reservation> oldReservations;
        static List<Reservation> reservations;
        static List<Building> buildings;
        public static float scale = 1.5f;

        //Key: Spot name. Value: occupied
        static Dictionary<string, bool> spots;
        
        static void Main(string[] args)
        {
            if (AlloAlgo is AllocateAlgorithm.FirstAndBest or AllocateAlgorithm.CustomOrder)
                scale = 1000;

            oldReservations = new();
            reservations = new();
            buildings = new();
            spots = new();

            FillReservations();

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
                    else if (cell.IsSpot())
                        spots.Add(cell, false);

                }
            }

            FillOldReservations();

            //Sets the number of reservations (with parking) per building
            foreach (Building building in buildings)
            {
                building.neededNumberOfSpots = building.NumberOfReservations();
            }

            int customOrderIndex = 0;
            float phPercent = singlePercent;
            //Combine buildings and spots
            while (buildings.Any(x => x.PercentageAllocated < 1))
            {

                switch (AlloAlgo)
                {
                    case AllocateAlgorithm.CustomOrder:
                        Building building1 = buildings.First(x => x.BuildingNumber == customOrder[customOrderIndex]);
                        while (building1.PercentageAllocated < 1)
                        {
                            building1.AquireNearestSpot(spots);
                        }
                        customOrderIndex++;
                        break;
                    case AllocateAlgorithm.CustomOrderSinglePercent:
                        if (customOrderIndex >= customOrder.Length)
                        {
                            phPercent = 1;
                            customOrderIndex = 0;
                        }

                        Building building2 = buildings.First(x => x.BuildingNumber == customOrder[customOrderIndex]);
                        while (building2.PercentageAllocated < phPercent)
                        {
                            building2.AquireNearestSpot(spots);
                        }
                        customOrderIndex++;

                        break;
                    case AllocateAlgorithm.FirstAndBest:
                    case AllocateAlgorithm.FirstAndBestFair:
                        //the worst building that isn't already filled with spots - give it a spot
                        buildings.Where(x => x.PercentageAllocated < 1).OrderByDescending(x => x.Badness).First().AquireNearestSpot(spots);
                        break;

                }

            }

            //combine reservations and spots
            foreach (Reservation res in reservations)
            {

                if (res.ParkingSpot.Length > 0)
                {
                    buildings.First(x => x.Name == res.Building).spots.Remove(res.ParkingSpot);
                    buildings.First(x => x.Name == res.Building).spots.Add(res.ParkingSpot);
                    continue;
                }

                string bestSpot = buildings.First(x => x.Name == res.Building).spots.First();
                res.ParkingSpot = bestSpot;
                buildings.First(x => x.Name == res.Building).spots.Remove(bestSpot);
                buildings.First(x => x.Name == res.Building).spots.Add(bestSpot);

            }

            PrintData();


            CreateFile();
            //Optional. Just for visualization
            CreateHeatMap();

        }

        /// <summary>
        /// Gives the already handed out parking spot to an old customer
        /// </summary>
        private static void FillOldReservations()
        {
            if (!File.Exists(OLDEXPORTLOCATION)) {
                return;
            }

            //Eventually replace this with a query to a database
            string[] lines = File.ReadAllLines(OLDEXPORTLOCATION);

            foreach (string line in lines)
            {

                //Headers is as following:
                //Parking Spot Number, Customer, Cell Phone, Email, Person Key, Room Key, Contract Type, Arrival Date, Departure Date, Status

                // We are working with literal "NULL" instead of "" for null values.

                string[] splitLine = line.Split(";");

                string arrival = splitLine[7].Split(" ")[0];
                //swapping
                string[] arrivalArray = arrival.Split(".");
                string year = arrivalArray[2];
                string month = arrivalArray[1];
                string day = arrivalArray[0];

                arrival = $"{year}-{month}-{day}";
            

                string parkingSpot = splitLine[0];

                Reservation mirrorReservation = reservations.FirstOrDefault(x => x.PersonKey == splitLine[4] && x.RoomKey == splitLine[5] && arrival == x.ArrivalDate);

                mirrorReservation.ParkingSpot = parkingSpot;

                buildings.First(x => x.Name == mirrorReservation.Building).spots.Add(parkingSpot);

                foreach (string spot in buildings.First(x => x.Name == mirrorReservation.Building).spots) {
                    spots[spot] = true;
                }

            }
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

                    if (cell.IsSpot() && spots.ContainsKey(cell)) {

                        //This query is kinda time consuming
                        Building? building = buildings.FirstOrDefault(x => x.spots.Contains(cell));

                        if (building != null) { 
                            splitLine[x] = cell + " Z " + building.BuildingNumber.ToString();
                        }
                        
                    }

                }

                lines[y] = String.Join(";", splitLine);
            }

            File.WriteAllLines(RESULTLOCATION + "HeatMap.csv", lines);
        }

        private static void CreateFile()
        {
            string[] lines = new string[reservations.Count + 1];

            lines[0] = "SpotNumber;Priority;PersonKey;RoomKey;ArrivalDate";

            for (int i = 1; i < reservations.Count; i++) {
                Reservation res = reservations[i];
                lines[i] = res.ParkingSpot + ";" + res.PriorityNumber + ";" + res.PersonKey + ";" + res.RoomKey + ";" + res.ArrivalDate;
            }

            File.WriteAllLines(RESULTLOCATION + "Result.csv", lines);
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


        private static int NumberOfReservations(this Building building) {

            int newRes = reservations.Where(x => x.BuildingNumber == building.BuildingNumber).Count();
            int oldRes = oldReservations.Where(x => x.BuildingNumber == building.BuildingNumber).Count();
            return oldRes + newRes;
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

                //If roomkey doesn't exist. Skip to the next
                if (splitLine[3] == "NULL" || splitLine[3] == "")
                    continue;

                reservations.Add(new Reservation(splitLine[2], splitLine[5], splitLine[3], splitLine[4], splitLine[0], splitLine[1]));
            }

            //how the reservations should be ordered.
            //Take history into consideration in the future
            reservations = reservations.OrderByDescending(x => x.InvertedPriorityNumber).ToList();
        }
    }
        
}
