using System.ComponentModel.DataAnnotations;

namespace CbData.OrderAggregator.Api.Models;

public sealed record OrderDto
{
    //vycházím z toho, že ProductId je číslo a to by se mělo ověřit, ale zadání to přímo nespecifikuje,
    //pouze jsem to odhadl z ukázkového jsonu. Takže to by bylo potřeba upřesnit
    [Required]
    [StringLength(50)]
    [RegularExpression("^[0-9]+$", ErrorMessage = "ProductId must contain digits only.")]
    public required string ProductId { get; init; }

    //tady by se mělo ověřit, zda by max neměl být nižší
    [Range(1, int.MaxValue)]
    public required int Quantity { get; init; }
}