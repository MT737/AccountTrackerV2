using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace AccountTrackerV2.ViewModels
{
    public class EntityViewModel : ListViewModel
    {
        public Entity EntityOfInterest { get; set; }
        public Entity AbsorptionEntity { get; set; }

        public class Entity
        {
            public int EntityID { get; set; }

            [Required]
            public string Name { get; set; }

            [Required]
            [Display(Name = "Is Displayed")]
            public bool IsDisplayed { get; set; }
        }
    }
}
