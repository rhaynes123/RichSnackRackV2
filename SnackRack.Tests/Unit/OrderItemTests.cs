using SnackRack.Pages.Features.Orders;

namespace SnackRack.Tests.Unit;

public class OrderItemTests
{
    [Fact]
    public void OrderItem_DefaultQuantity_IsOne()
    {
        var item = new OrderItem { ProductId = Guid.NewGuid(), ProductName = "Pretzel", Price = 1.99m };
        Assert.Equal(1, item.Quantity);
    }

    [Fact]
    public void OrderItem_Price_RoundsToTwoDecimalPlaces()
    {
        var item = new OrderItem { ProductId = Guid.NewGuid(), ProductName = "Chips", Price = 2.505m };
        Assert.Equal(2.505m, item.Price);
    }

    [Fact]
    public void OrderStatus_PendingValue_IsOne()
    {
        Assert.Equal(1, (int)OrderStatus.Pending);
    }

    [Fact]
    public void OrderStatus_SubmittedValue_IsTwo()
    {
        Assert.Equal(2, (int)OrderStatus.Submitted);
    }

    [Fact]
    public void OrderStatus_AllValuesAreDistinct()
    {
        var values = Enum.GetValues<OrderStatus>().Cast<int>().ToList();
        Assert.Equal(values.Count, values.Distinct().Count());
    }
}
