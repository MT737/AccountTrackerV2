using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace AccountTrackerV2.Models
{
    public class TransactionType
    {
        //Properties
        public int TransactionTypeID { get; set; }

        [Required, StringLength(100)]
        public string Name { get; set; }

        //Navigation Property
        public ICollection<Transaction> Transactions { get; set; }

        //Instantiation of navigation collection.
        public TransactionType()
        {
            Transactions = new List<Transaction>();
        }
    }
}
