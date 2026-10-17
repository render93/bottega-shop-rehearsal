namespace BottegaAurora.Shop.Checkout;

public sealed record CheckoutResult(decimal Total, decimal Discount, string? PromoCode);

public sealed class CheckoutService(PromoCatalog promos)
{
    public CheckoutResult Complete(Cart cart)
    {
        var subtotal = cart.Subtotal;
        // Promo is optional: most carts have no code (regression in 2.4.0, issue #1).
        var rule = cart.Promo is null ? null : promos.Find(cart.Promo.Code);
        var discount = rule?.Apply(subtotal) ?? 0m;
        return new CheckoutResult(subtotal - discount, discount, rule?.Code);
    }
}
