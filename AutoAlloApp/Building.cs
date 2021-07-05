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
        //Spots affiliated with this building
        public List<string> spots = new();

        public int neededNumberOfSpots;
        public string Name { get; set; }

        public Point Location { get; set; }

        public int BuildingNumber {

            get {
                return Int16.Parse(Name.Trim().Split(" ")[1]);      
            }
        }

        public float percentageAllocated {
            get {
                return (float)spots.Count / (float)neededNumberOfSpots;
            }
        }

        public float avgWalkDistance {
            get {
                throw new NotImplementedException();
            }
        }

        private int findWalkDistance(string spot) {
            throw new NotImplementedException();
        }

        public Building(string name, Point Location) {
            this.Name = name;
            this.Location = Location;
        }
    }
}
