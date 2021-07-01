using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoAlloApp
{
    class Reservation
    {
        public string personKey { get; private set; }

        public string parkingAgreementCode { get; private set; }

        public string roomKey { get; private set; }

        public string contractType { get; private set; }

        public int houseNumber {
            get { 
                return roomKey.ToUpper() == "NULL" ? 0 : Int16.Parse(roomKey.Split("_")[1]); 
            }
        }

        public int roomNumber
        {
            get
            {
                return roomKey.ToUpper() == "NULL" ? 0 : Int16.Parse(roomKey.Split("_")[2]);
            }
        }

        public string priority {
            get {

                return contractType switch
                {
                    "RES" => "gold",
                    "SOMMER" => "bronze",
                    _ => "silver"
                };
            }
        }

        public int priorityNumber {
            get {
                return priority switch
                {
                    "gold" => 1,
                    "silver" => 2,
                    "bronze" => 3,
                    _ => 2,
                };
            }
        }

        public Reservation(string personKey, string parkingAgreementCode, string roomKey, string contractType) {

            this.personKey = personKey;
            this.parkingAgreementCode = parkingAgreementCode;
            this.roomKey = roomKey;
            this.contractType = contractType;

        }
    }
}
