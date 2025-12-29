using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using SnackRack.Pages.Features.Orders;
using SnackRack.Pages.Features.Products;

namespace SnackRack.Data;

public class ApplicationUser: IdentityUser
{
    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
}