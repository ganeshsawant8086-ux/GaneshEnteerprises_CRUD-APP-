using System.ComponentModel.DataAnnotations;

namespace GaneshEnterprises.Models
{
    public enum BagSize
    {
        [Display(Name = "Small")]
        Small = 1,

        [Display(Name = "Medium")]
        Medium = 2,

        [Display(Name = "Large")]
        Large = 3
    }
}
