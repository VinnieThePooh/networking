namespace Sockets.Common.Models;

public class ItemQuote
{
    public long? ItemNumber { get; set; }

    public ItemQuote(long? itemNumber, string? description, int unitPrice, int quantity, bool discounted, bool inStock)
    {
        ItemNumber = itemNumber;
        Description = description;
        UnitPrice = unitPrice;
        Quantity = quantity;
        Discounted = discounted;
        InStock = inStock;
    }

    public string? Description { get; set; }

    public int UnitPrice { get; set; }

    public int Quantity { get; set; }

    public bool Discounted  { get; set; }

    public bool InStock { get; set; }

    public override string ToString()
    {
        string EOL = "\n";
        string value = "Item# = " + ItemNumber + EOL +
        "Description = " + Description + EOL +
        "Quantity = " + Quantity + EOL +
        "Price (each) = " + UnitPrice + EOL +
        "Total Price = " + (Quantity * UnitPrice);

        if (Discounted)
            value += " (discounted)";
        if (InStock)
            value += EOL + "In Stock" + EOL;
        else
            value += EOL + "Out of Stock" + EOL;

        return value;
    }
}
