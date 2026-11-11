---
title: Try It
order: 2
---

# Try It

Run the snippet below to use the sample's own types from the page. The code runs in
a copy of this app's executable, which `MokaDocsReplWorker.TryRunAsync()` at the top
of `Program.cs` turns into a REPL worker.

```csharp-repl
using Moka.Docs.Samples.AspNetCore.Models;

var mug = new Product { Name = "Moka Mug", Price = 12.50m, StockQuantity = 3 };
Money total = new Money(mug.Price) + new Money(4.99m);

Console.WriteLine($"{mug.Name} in stock: {mug.IsAvailable}");
Console.WriteLine($"With shipping: {total.Amount} {total.Currency}");
```

The Run button works in the Development environment, from the machine the app runs
on. Everywhere else the endpoint isn't mapped unless `AllowRemoteRepl` is set,
because it runs any code it receives on the server.
