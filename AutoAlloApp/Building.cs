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

        /// <summary>
        /// Gives the nearest parking spot to the building. Returns true if found. False if not found
        /// </summary>
        /// <param name="spotDict"></param>
        /// <returns></returns>
        public bool AquireNearestSpot(Dictionary<string, bool> spotDict) {
            string spot = FindNearest(Location, new Point(9999,9999), spotDict, 0).Item1;

            if (spot == "U1")
                return false;

            spots.Add(spot);
            spotDict[spot] = true;
            return true;
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

            if (bestSpot.Item1 != null && bestSpot.Item1 != "" && IsInLegalParkingZone(bestSpot.Item1)) {
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

        private bool IsInLegalParkingZone(string cell) {

            string[] manualSelection = new string[] {
            "1; G 001; G 002; G 003; G 004; G 005; G 006; G 007; G 008; G 009; G 010; G 011; G 012; G 013; G 014; G 015; G 016; G 017; G 018; G 019; G 020; G 021; G 022; G 023; G 024; G 025; G 026; G 027; G 028; G 029; G 030; G 031; G 032; G 033; G 034; G 035; G 036; G 037; G 038; G 039; G 040; G 041; G 042; G 043; G 044; G 045; G 046; G 047; G 048; G 049; G 050; G 051; G 052; G 053; G 054; G 055; G 056; G 057; G 058; G 059; G 060; G 061; G 062; G 063; G 064; G 065; G 066; G 067; G 068; G 069; G 070; G 071; G 072; G 073; G 074; G 075; G 076; G 077; G 078; G 079",
            "3; G 080; G 081; G 082; G 083; G 084; G 085; G 086; G 087; G 088; G 089; G 090; G 091; G 092; G 093; G 094; G 095; G 096; G 097; G 098; G 099; G 100; G 101; G 102; G 103; G 104; G 105; G 106; G 107; G 108; G 109; G 110; G 111; G 112; G 113; G 114; G 115; G 116; G 117; G 118; G 119; G 120; G 121; G 122; G 123; G 124; G 125; G 126; G 127; G 128; G 129; G 130; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ;",
            "5; G 131; G 132; G 133; G 134; G 135; G 136; G 137; G 138; G 139; G 140; G 141; G 142; G 143; G 144; G 145; G 146; G 147; G 148; G 149; G 150; G 151; G 152; G 153; G 154; G 155; G 156; G 157; G 158; G 159; G 160; G 161; G 162; G 163; G 164; G 165; G 166; G 167; G 168; G 169; G 170; G 171; G 172; G 173; G 174; G 175; G 176; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ;",
            "7; G 178; G 179; G 180; G 181; G 182; G 183; G 184; G 185; G 186; G 187; G 188; G 189; E 004; E 005; E 006; E 007; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ;",
            "18; E 008; E 010; E 012; E 014; E 016; E 018; E 020; E 022; E 024; E 026; E 028; E 030; E 032; E 034; E 036; E 038; E 040; E 042; E 044; E 046; E 048; E 050; D 071; D 069; D 067; D 065; D 063; D 061; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ;",
            "24; F 044; F 042; F 040; F 038; F 036; F 034; F 032; F 030; F 028; F 026; F 024; F 022; F 020; F 018; F 016; F 014; F 012; F 010; F 006; F 004; F 065; F 066; F 067; F 068; F 069; F 070; F 071; F 072; F 073; F 074; F 075; F 076; F 077; F 078; F 079; F 080; F 081; F 082; F 083; F 084; F 085; F 086; F 087; F 088; F 089; F 090; F 091; F 092; F 093; F 094; F 095; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ;",
            "22; F 043; F 041; F 039; F 037; F 035; F 033; F 031; F 029; F 027; F 025; F 023; F 021; F 019; F 017; F 011; F 009; F 007; F 005; F 003; F 001; F 002; E 001; E 003; F 045; F 046; F 047; F 048; F 049; F 050; F 051; F 052; F 053; F 054; F 055; F 056; F 057; F 058; F 059; F 060; F 061; F 062; F 063; F 064; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ;",
            "20; E 009; E 011; E 013; E 015; E 017; E 019; E 021; E 023; E 025; E 027; E 029; E 031; E 033; E 035; E 037; E 039; E 041; E 043; E 045; E 047; E 049; D 080; D 078; D 076; D 074; D 072; D 070; D 068; D 066; D 064; D 062; D 060; D 058; D 056; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ;",
            "4; A 002; A 003; A 004; A 005; A 006; A 007; A 008; A 009; A 010; A 011; A 012; A 013; A 014; A 015; A 016; A 017; A 018; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ;",
            "2; B 023; B 024; B 025; B 026; B 027; B 028; B 029; B 030; B 031; B 032; B 033; B 034; B 035; B 036; B 037; B 038; B 039; B 040; B 041; B 042; B 043; B 044; B 045; B 046; B 047; B 048; B 049; B 050; B 051; B 052; B 053; B 054; B 055; B 056; B 057; B 058; B 059; B 060; B 061; B 062; B 063; B 064; B 065; B 066; B 067; B 068; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ;",
            "10; B 001; B 002; B 003; B 004; B 005; B 006; B 007; B 008; B 009; B 010; B 011; B 012; B 013; B 014; B 015; B 016; B 017; B 018; B 019; B 020; B 021; B 022; C 028; C 029; C 030; C 031; C 032; C 033; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ;",
            "12; A 019; A 020; A 021; A 022; A 023; A 024; A 025; A 026; A 027; A 028; A 029; A 030; A 031; A 032; A 033; A 034; A 035; A 036; A 037; A 038; A 039; A 040; A 041; A 042; A 043; A 044; A 045; A 046; A 047; A 048; A 049; A 050; A 051; A 052; A 053; A 054; A 055; A 056; A 057; A 058; A 059; A 060; A 061; C 011; C 012; C 013; C 014; C 015; C 016; C 017; C 018; C 019; C 020; C 021; C 022; C 023; C 024; C 025; C 026; C 027; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ;",
            "16; C 001; C 002; C 003; C 004; C 005; C 006; C 007; C 008; C 009; C 010; D 059; D 057; D 055; D 053; D 051; D 049; D 047; D 045; D 043; D 041; D 031; D 029; D 027; D 025; D 023; D 021; D 019; D 017; D 015; D 011; D 009; D 007; D 005; D 001; D 003; D 014; D 012; D 010; D 008; D 006; D 004; D 002; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ;",
            "14; D 054; D 052; D 050; D 048; D 046; D 044; D 042; D 040; D 038; D 036; D 024; D 022; D 020; D 018; D 016; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ; ;"
            };

            return Program.allocationAlorithm != AllocateAlgorithm.ManualDividing || manualSelection.First(x => x.Split(";")[0] == BuildingNumber.ToString()).Split(";").Contains(" " + cell);
        }

        public Building(string name, Point Location) {
            this.Name = name;
            this.Location = Location;
        }
    }
}
