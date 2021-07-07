using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoAlloApp
{
    class Reservation
    {
        private string parkingSpot = "";
        public string ParkingSpot {
            set => parkingSpot = value;
            get => parkingSpot;
        }
        public string PersonKey { get; private set; }

        // Not really in use
        public string ParkingAgreementCode { get; private set; }

        public string RoomKey { get; private set; }

        public string ContractType { get; private set; }

        public string ArrivalDate { get; private set; }

        public string DepartureDate { get; private set; }

        public int BuildingNumber {

            get { 
                return RoomKey.ToUpper() == "NULL" ? 99999 : Int16.Parse(RoomKey.Split("_")[1]); 
            }
        }

        public string Building {
            get {
                return "X " + BuildingNumber.ToString();
            }
        }

        public int RoomNumber
        {
            get
            {
                return RoomKey.ToUpper() == "NULL" ? 99999 : Int16.Parse(RoomKey.Split("_")[2]);
            }
        }

        public string Priority {
            get {

                return ContractType switch
                {
                    "RES" => "gold",
                    "SOMMER" => "bronze",
                    _ => "silver"
                };
            }
        }

        /// <summary>
        /// Translates the priority to numbers. 3 is the highest priority
        /// </summary>
        public int InvertedPriorityNumber {
            get {
                return Priority switch
                {
                    "gold" => 3,
                    "silver" => 2,
                    "bronze" => 1,
                    _ => 2,
                };
            }
        }

        /// <summary>
        /// Translates the priority to numbers. 1 is the highest priority
        /// </summary>
        public int PriorityNumber
        {
            get
            {
                return Priority switch
                {
                    "gold" => 1,
                    "silver" => 2,
                    "bronze" => 3,
                    _ => 2,
                };
            }
        }

        public Reservation(string personKey, string parkingAgreementCode, string roomKey, string contractType, string ArrivalDate, string DepartureDate) {

            this.PersonKey = personKey;
            this.ParkingAgreementCode = parkingAgreementCode;
            this.RoomKey = roomKey;
            this.ContractType = contractType;
            this.ArrivalDate = ArrivalDate;
            this.DepartureDate = DepartureDate;

        }
    }
}
