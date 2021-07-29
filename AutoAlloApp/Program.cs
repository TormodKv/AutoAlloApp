using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;

namespace AutoAlloApp
{

    static class Program
    {
        //Don't ask
        public static float scale = 1.5f;

        private static string ReservationDatabaseConnection = "Database Connection Here";
        private static string ParkingAllocationDatabaseConnection = "Database Connection Here";

        //File locations
        private static string EXCLUDEDSPOTSSQL = (AppDomain.CurrentDomain.BaseDirectory + "ExcludedSpots.sql").Replace("AutoAlloApp\\bin\\Debug\\net5.0\\", "");
        private static string EXCLUDEDSPOTSCSV = (AppDomain.CurrentDomain.BaseDirectory + "ExcludedSpots.csv").Replace("AutoAlloApp\\bin\\Debug\\net5.0\\", "");
        private static string EXCLUDEDPERSONSCSV = (AppDomain.CurrentDomain.BaseDirectory + "ExcludedPersons.csv").Replace("AutoAlloApp\\bin\\Debug\\net5.0\\", "");
        private static string MAPLOCATION = (AppDomain.CurrentDomain.BaseDirectory + "Map.csv").Replace("AutoAlloApp\\bin\\Debug\\net5.0\\", "");
        private static string EXPORTSQL = (AppDomain.CurrentDomain.BaseDirectory + "ConRes.sql").Replace("AutoAlloApp\\bin\\Debug\\net5.0\\", "");
        private static string ROOTLOCATION = AppDomain.CurrentDomain.BaseDirectory.Replace("AutoAlloApp\\bin\\Debug\\net5.0\\", "");

        //The main grid of the parking garage
        public static string[,] matrix;

        static List<Reservation> reservations;
        static List<Building> buildings;

        //Key: Spot name. Value: occupied
        static Dictionary<string, bool> spots;

        static List<string> excludedSpotsListSQL;

        static List<string> excludedSpotsListCSV;

        static List<string> excludedPersonsListCSV;

        static void Main(string[] args)
        {

            reservations = new();
            buildings = new();
            spots = new();

            try
            {
                excludedPersonsListCSV = File.ReadAllLines(EXCLUDEDPERSONSCSV).ToList<string>();
            }
            catch (FileNotFoundException) {
                excludedPersonsListCSV = new();
            }

            try
            {
                excludedSpotsListSQL = GetDataFromDatebase(EXCLUDEDSPOTSSQL, ParkingAllocationDatabaseConnection);
            }
            catch (FileNotFoundException)
            {
                excludedSpotsListSQL = new();
            }

            try
            {
                excludedSpotsListCSV = File.ReadAllLines(EXCLUDEDSPOTSCSV).ToList<string>();
            }
            catch (FileNotFoundException) {
                excludedSpotsListCSV = new();
            }

            List<string> rawReservations = RemoveDuplicatesFromExport(GetDataFromDatebase(EXPORTSQL, ReservationDatabaseConnection));

            FillReservations(rawReservations);

            FillMatrix();

            //Allocate spots for 1. prio
            UpdateBuildingsNumberOfSpots(new int[] { 1 });
            AssignSpotsToBuildings();

            //Allocate spots for 2. prio
            UpdateBuildingsNumberOfSpots(new int[] { 1, 2 });
            AssignSpotsToBuildings();

            //Allocate spots for 3. prio
            UpdateBuildingsNumberOfSpots(new int[] { 1, 2, 3 });
            AssignSpotsToBuildings();

            AssignSpotsToReservations();

            //Not neccessary
            PrintStatistics();

            CreateResultCSV();

            //Not neccessary
            CreateHeatMap();

        }

        /// <summary>
        /// Returns an emulated CSV file from the database.
        /// </summary>
        /// <param name="sqlLocation"></param>
        /// <returns></returns>
        private static List<string> GetDataFromDatebase(string sqlLocation, string sqlConnection)
        {
            List<string> content = new();

            //Create querystring from sql file
            string queryString = "";
            string[] lines = File.ReadAllLines(sqlLocation);
            foreach (string line in lines) {
                queryString += " " + line;
            }


            using (SqlConnection connection = new SqlConnection(sqlConnection))
            {
                SqlCommand command = new SqlCommand(queryString, connection);
                connection.Open();
                SqlDataReader reader = command.ExecuteReader();
                try
                {
                    while (reader.Read())
                    {
                        string row = ReadSingleRow((IDataRecord)reader);

                        bool excluded = false;
                        foreach (string personKey in excludedPersonsListCSV) {
                            if (row.Contains(personKey)) 
                            {
                                excluded = true;
                                break;
                            }
                        }

                        if (!excluded)
                            content.Add(row);
                    }
                }
                finally
                {
                    reader.Close();
                }
            }

            return content;
        }

        private static string ReadSingleRow(IDataRecord record)
        {
 
            string cell = "";
            for (int i = 0; i <= 6; i++) {

                //This try catch is very inneffective, but it works
                try
                {
                    //If cell is a date. Convert it from (dd.mm.yyyy hh:mm:ss) to (yyyy-mm-dd)
                    if (record[i].ToString().Contains(" 00:00:00"))
                    {
                        string[] date = record[i].ToString().Replace(" 00:00:00", "").Split(".");
                        cell += $"{date[2]}-{date[1]}-{date[0]};";
                    }
                    else { 
                        cell += record[i] + ";";
                    }

                }
                catch {
                    break;
                }
            }

            //Remove last semicolon
            return cell.Remove(cell.Length - 1);
        }

        /// <summary>
        /// Removes all duplicate personkeys with the same date from export list. only keeps the best contracts
        /// </summary>
        private static List<string> RemoveDuplicatesFromExport(List<string> lines)
        {
            List<string> Copy = lines.ToList();
            List<string> personKeys = new();

            Copy = Copy.OrderBy(x => Int32.Parse(x.Split(";")[0].Replace("-",""))).ToList();

            foreach (string line in Copy) {
                if (personKeys.Contains(line.Split(";")[2]))
                {
                    lines.Remove(line);
                }
                else 
                { 
                    personKeys.Add(line.Split(";")[2]);
                }
            }
            return lines;
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
                b.temp.Clear();
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
                }
            }
        }

        private static bool IsExcluded(this string cell) {

            //remove this clearing if you want to take into consideration old parkingspots 
            // and don't want to override old spots
            excludedSpotsListSQL.Clear();

            return excludedSpotsListSQL.Contains(cell) || excludedSpotsListCSV.Contains(cell);
        }

        private static void PrintStatistics()
        {

            Console.WriteLine($"Total Avareage distance: {buildings.Sum(x => x.AvgWalkDistance) / buildings.Count} Biggest difference: {buildings.OrderBy(x => x.AvgWalkDistance).Last().AvgWalkDistance - buildings.OrderBy(x => x.AvgWalkDistance).First().AvgWalkDistance}");

            Console.WriteLine($"Total in U2: {reservations.Where(x => x.ParkingSpot.Contains("U2")).Count()}");
            Console.WriteLine($"Total in A-G: {reservations.Where(x => !x.ParkingSpot.Contains("U2")).Count()}");
            Console.WriteLine($"SOMMER in U2: {reservations.Where(x => x.PriorityNumber == 3 && x.ParkingSpot.Contains("U2")).Count()}");
            Console.WriteLine($"SOMMER in A-G: {reservations.Where(x => x.PriorityNumber == 3 && !x.ParkingSpot.Contains("U2")).Count()}");
            Console.WriteLine($"SLT in U2: {reservations.Where(x => x.PriorityNumber == 2 && x.ParkingSpot.Contains("U2")).Count()}");
            Console.WriteLine($"SLT in A-G: {reservations.Where(x => x.PriorityNumber == 2 && !x.ParkingSpot.Contains("U2")).Count()}");
            Console.WriteLine($"RES in U2: {reservations.Where(x => x.PriorityNumber == 1 && x.ParkingSpot.Contains("U2")).Count()}");
            Console.WriteLine($"RES in A-G: {reservations.Where(x => x.PriorityNumber == 1 && !x.ParkingSpot.Contains("U2")).Count()}");
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

            int reservationsCount = reservations.Where(x => x.BuildingNumber == building.BuildingNumber && includedPriorities.Contains(x.PriorityNumber)).Count();
            return reservationsCount;
        }

        /// <summary>
        /// Fills up the reservation list with reservation export from database
        /// </summary>
        private static void FillReservations(List<string> lines)
        {
            foreach (string line in lines) {

                //Headers is as following:
                //DateArrival [0], DateDeparture [1], ContactExternalId [2], RoomKey [3], ContractType [4], ParkingAgreementCode[5], ReservationID[6]

                string[] splitLine = line.Split(";");

                Reservation res = new Reservation(splitLine[2], splitLine[5], splitLine[3], splitLine[4], splitLine[0], splitLine[1], splitLine[6]);

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
