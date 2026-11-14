# CbData.OrderAggregator

REST API, které přijímá objednávky, agreguje množství podle produktu
a každých 20 sekund odesílá agregovanou dávku (aktuálně na výstup konzole).

## Požadavky

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)

## Spuštění

```bash
dotnet run --project CbData.OrderAggregator.Api
```

API poběží na `http://localhost:5170`.

Alternativně otevřete solution ve Visual Studiu a spusťte projekt `CbData.OrderAggregator.Api` (profil `http`).

## Odeslání objednávek

`POST /api/orders` – tělo je pole objednávek:

```bash
curl -i -X POST http://localhost:5170/api/orders \
  -H "Content-Type: application/json" \
  -d '[{"productId":"456","quantity":5},{"productId":"789","quantity":42}]'
```

PowerShell:

```powershell
Invoke-RestMethod -Method Post -Uri http://localhost:5170/api/orders `
  -ContentType "application/json" `
  -Body '[{"productId":"456","quantity":5},{"productId":"789","quantity":42}]'
```

Úspěšný request vrací `202 Accepted`, nevalidní `400` s detailem chyby (`application/problem+json`).

Připravené requesty (validní, agregace, nevalidní vstupy) jsou také v souboru
`CbData.OrderAggregator.Api/CbData.OrderAggregator.Api/Http/debug.http`, který lze spustit
přímo ve Visual Studiu nebo Rideru.

Do 20 sekund se v konzoli aplikace objeví odeslaná dávka, například:

```json
{"batchId":"…","orders":[{"productId":"789","quantity":42},{"productId":"456","quantity":5}]}
```

## Testy

```bash
dotnet test
```

## Konfigurace

V `appsettings.json`:

| Klíč | Výchozí | Popis |
|---|---|---|
| `OrderSenderOptions:SendInterval` | `00:00:20` | Interval odesílání dávek (20 s – 5 min) |
| `OrderSenderOptions:ShutdownTokenTimeout` | `00:00:10` | Časový limit pro odeslání zbylých objednávek při ukončení (1–25 s) |
| `DataAccessOptions:Provider` | `InMemory` | Úložiště objednávek |