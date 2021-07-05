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

        public string ParkingAgreementCode { get; private set; }

        public string RoomKey { get; private set; }

        public string ContractType { get; private set; }

        public int HouseNumber {

            get { 
                return RoomKey.ToUpper() == "NULL" ? 99999 : Int16.Parse(RoomKey.Split("_")[1]); 
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

        public int PriorityNumber {
            get {
                return Priority switch
                {
                    "gold" => 1,
                    "silver" => 2,
                    "bronze" => 3,
                    _ => 2,
                };
            }
        }

        public Reservation(string personKey, string parkingAgreementCode, string roomKey, string contractType) {

            this.PersonKey = personKey;
            this.ParkingAgreementCode = parkingAgreementCode;
            this.RoomKey = roomKey;
            this.ContractType = contractType;

        }
    }
}
