using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoAlloApp
{
    class Building
    {
        //Spots affiliated with this building. This is automatically sorted. best first
        public List<string> spots = new();

        //temporarily hold the parking spots once.
        public List<string> temp = new();

        public int neededNumberOfSpots;
        public string Name { get; set; }

        public Point Location { get; set; }

        public int BuildingNumber {

            get {
                return Int16.Parse(Name.Trim().Split(" ")[1]);      
            }
        }

        /// <summary>
        /// Badness scroe. The higher, the worse the placements of the parking spots are (predicted)
        /// Change the scale variable for different weighted results
        /// </summary>
        public float Badness
        {
            get {
                return AvgWalkDistance + (((1 - PercentageAllocated) * 100) * Program.scale);
            }
        }

        /// <summary>
        /// Returns the percentage (0 - 1) of spots that have been allocated
        /// </summary>
        public float PercentageAllocated {
            get {
                return (float)spots.Count / (float)neededNumberOfSpots;
            }
        }

        /// <summary>
        /// Returns the average walk distance for all the spots affiliated with this building
        /// </summary>
        public float AvgWalkDistance {
            get {
                if (spots.Count == 0)
                    return 0;

                float avg = 0;
                foreach (string spot in spots) {
                    avg += FindWalkDistance(spot);
                }
                return avg / spots.Count;
            }
        }

        private string[,] matrix { get => Program.matrix; }

        public void AquireNearestSpot(Dictionary<string, bool> spotDict) {
            string spot = FindNearest(Location, new Point(9999,9999), spotDict, 0).Item1;
            spots.Add(spot);
            spotDict[spot] = true;
        }

        private (string, int) FindNearest(Point currentPos, Point lastPos, Dictionary<string, bool> spotDict, int stepsTaken)
        {
            (string, Point)[] directions = new (string, Point)[4] {
                (matrix[currentPos.X + 1, currentPos.Y], new Point(currentPos.X + 1, currentPos.Y)),
                (matrix[currentPos.X - 1, currentPos.Y], new Point(currentPos.X - 1, currentPos.Y)),
                (matrix[currentPos.X, currentPos.Y + 1], new Point(currentPos.X, currentPos.Y + 1)),
                (matrix[currentPos.X, currentPos.Y - 1], new Point(currentPos.X, currentPos.Y - 1))
            };

            (string, int) bestSpot = (directions.FirstOrDefault(x => spotDict.ContainsKey(x.Item1) && x.Item1.IsSpot() && spotDict[x.Item1] == false).Item1 , stepsTaken +1);

            if (bestSpot.Item1 != null && bestSpot.Item1 != "") {
                return bestSpot;
            }

            List<(string, int)> candidateSpots = new();

            foreach ((string, Point) d in directions)
            {
                if (d.Item1 == "&" && d.Item2 != lastPos)
                {
                    candidateSpots.Add(FindNearest(d.Item2, currentPos, spotDict, stepsTaken + 1));
                }
            }

            if (candidateSpots.Count == 0)
            {
                return ("U1", 99999);
            }
            else 
            {
                return candidateSpots.OrderByDescending(x => x.Item2).Last();    
            }

        }

        /// <summary>
        /// Finds the walk distance in number of steps to a given spot from this building
        /// </summary>
        /// <param name="spot"></param>
        /// <returns></returns>
        public int FindWalkDistance(string spot) {
            return FindDistance(Location, new Point(9999, 9999), spot, 0);
        }

        /// <summary>
        /// Recursive method for locating and counting steps from this building
        /// </summary>
        /// <param name="currentPos"></param>
        /// <param name="lastPos"></param>
        /// <param name="goal"></param>
        /// <param name="stepsTaken"></param>
        /// <returns></returns>
        private int FindDistance(Point currentPos, Point lastPos, string goal, int stepsTaken) {

            (string, Point)[] directions = new (string, Point)[4] {
                (matrix[currentPos.X + 1, currentPos.Y], new Point(currentPos.X + 1, currentPos.Y)),
                (matrix[currentPos.X - 1, currentPos.Y], new Point(currentPos.X - 1, currentPos.Y)),
                (matrix[currentPos.X, currentPos.Y + 1], new Point(currentPos.X, currentPos.Y + 1)),
                (matrix[currentPos.X, currentPos.Y - 1], new Point(currentPos.X, currentPos.Y - 1))
            };

            if (directions.Any(x => x.Item1 == goal)) {
                return stepsTaken + 1;
            }

            foreach ((string, Point) d in directions)
            {
                if (d.Item1 == "&" && d.Item2 != lastPos)
                {
                    int placeHolder = FindDistance(d.Item2, currentPos, goal, stepsTaken + 1);
                    if (placeHolder != -1)
                    {
                        return placeHolder;
                    }
                }
            }

            return -1;
        }

        public Building(string name, Point Location) {
            this.Name = name;
            this.Location = Location;
        }
    }
}
