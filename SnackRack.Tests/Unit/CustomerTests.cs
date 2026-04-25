using SnackRack.Pages.Features.Customers;

namespace SnackRack.Tests.Unit;

public class CustomerTests
{
    [Fact]
    public void Customer_DefaultName_IsGuest()
    {
        var customer = new Customer();
        Assert.Equal("Guest", customer.Name);
    }

    [Fact]
    public void Customer_DefaultCustomerTypeId_IsGuest()
    {
        var customer = new Customer();
        Assert.Equal((int)CustomerType.Guest, customer.CustomerTypeId);
    }

    [Fact]
    public void Customer_Id_IsGeneratedAutomatically()
    {
        var a = new Customer();
        var b = new Customer();
        Assert.NotEqual(a.Id, b.Id);
    }

    [Fact]
    public void CustomerRequest_AllPropertiesNullable()
    {
        var request = new CustomerRequest();
        Assert.Null(request.Email);
        Assert.Null(request.PhoneNumber);
    }

    [Fact]
    public void CustomerType_GuestValue_IsOne()
    {
        Assert.Equal(1, (int)CustomerType.Guest);
    }

    [Fact]
    public void CustomerType_RegisteredValue_IsTwo()
    {
        Assert.Equal(2, (int)CustomerType.Registered);
    }
}
