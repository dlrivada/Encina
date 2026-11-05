```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6973P-C 4.13GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                        | Job        | IterationCount | LaunchCount | subscriberCount | messageCount | Mean            | Error         | StdDev       | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |----------- |--------------- |------------ |---------------- |------------- |----------------:|--------------:|-------------:|------:|--------:|-------:|-------:|----------:|------------:|
| **PublishAsync_NoSubscriber**     | **Job-YFEFPZ** | **10**             | **Default**     | **?**               | **?**            |        **472.6 ns** |       **1.87 ns** |      **0.98 ns** |  **2.33** |    **0.03** | **0.0019** |      **-** |     **168 B** |        **1.31** |
| SubscribeAndUnsubscribe       | Job-YFEFPZ | 10             | Default     | ?               | ?            |      1,838.0 ns |      54.66 ns |     32.53 ns |  9.08 |    0.20 | 0.0076 | 0.0057 |     776 B |        6.06 |
| PublishAsync_SingleSubscriber | Job-YFEFPZ | 10             | Default     | ?               | ?            |        202.5 ns |       5.01 ns |      2.98 ns |  1.00 |    0.02 | 0.0014 |      - |     128 B |        1.00 |
|                               |            |                |             |                 |              |                 |               |              |       |         |        |        |           |             |
| PublishAsync_NoSubscriber     | ShortRun   | 3              | 1           | ?               | ?            |        468.9 ns |      15.13 ns |      0.83 ns |  2.17 |    0.13 | 0.0019 |      - |     168 B |        1.31 |
| SubscribeAndUnsubscribe       | ShortRun   | 3              | 1           | ?               | ?            |      1,757.1 ns |     151.69 ns |      8.31 ns |  8.12 |    0.48 | 0.0076 | 0.0057 |     776 B |        6.06 |
| PublishAsync_SingleSubscriber | ShortRun   | 3              | 1           | ?               | ?            |        217.0 ns |     265.05 ns |     14.53 ns |  1.00 |    0.08 | 0.0014 |      - |     128 B |        1.00 |
|                               |            |                |             |                 |              |                 |               |              |       |         |        |        |           |             |
| **MultipleSubscribers**           | **Job-YFEFPZ** | **10**             | **Default**     | **5**               | **?**            | **10,208,528.5 ns** |  **28,703.59 ns** | **18,985.65 ns** |     **?** |       **?** |      **-** |      **-** |    **1856 B** |           **?** |
|                               |            |                |             |                 |              |                 |               |              |       |         |        |        |           |             |
| MultipleSubscribers           | ShortRun   | 3              | 1           | 5               | ?            | 10,224,474.2 ns | 232,028.69 ns | 12,718.28 ns |     ? |       ? |      - |      - |    1856 B |           ? |
|                               |            |                |             |                 |              |                 |               |              |       |         |        |        |           |             |
| **PublishBurst**                  | **Job-YFEFPZ** | **10**             | **Default**     | **?**               | **10**           |      **2,416.5 ns** |       **5.43 ns** |      **3.23 ns** |     **?** |       **?** | **0.0191** |      **-** |    **1856 B** |           **?** |
|                               |            |                |             |                 |              |                 |               |              |       |         |        |        |           |             |
| PublishBurst                  | ShortRun   | 3              | 1           | ?               | 10           |      2,392.1 ns |     130.53 ns |      7.15 ns |     ? |       ? | 0.0191 |      - |    1856 B |           ? |
|                               |            |                |             |                 |              |                 |               |              |       |         |        |        |           |             |
| **MultipleSubscribers**           | **Job-YFEFPZ** | **10**             | **Default**     | **10**              | **?**            | **10,206,687.2 ns** |  **30,898.11 ns** | **20,437.19 ns** |     **?** |       **?** |      **-** |      **-** |    **2456 B** |           **?** |
|                               |            |                |             |                 |              |                 |               |              |       |         |        |        |           |             |
| MultipleSubscribers           | ShortRun   | 3              | 1           | 10              | ?            | 10,205,277.7 ns | 695,362.85 ns | 38,115.18 ns |     ? |       ? |      - |      - |    2456 B |           ? |
|                               |            |                |             |                 |              |                 |               |              |       |         |        |        |           |             |
| **MultipleSubscribers**           | **Job-YFEFPZ** | **10**             | **Default**     | **20**              | **?**            | **10,206,365.7 ns** |  **44,725.91 ns** | **29,583.42 ns** |     **?** |       **?** |      **-** |      **-** |    **3656 B** |           **?** |
|                               |            |                |             |                 |              |                 |               |              |       |         |        |        |           |             |
| MultipleSubscribers           | ShortRun   | 3              | 1           | 20              | ?            | 10,197,062.0 ns | 339,925.32 ns | 18,632.45 ns |     ? |       ? |      - |      - |    3656 B |           ? |
|                               |            |                |             |                 |              |                 |               |              |       |         |        |        |           |             |
| **PublishBurst**                  | **Job-YFEFPZ** | **10**             | **Default**     | **?**               | **50**           |     **13,147.9 ns** |   **1,356.23 ns** |    **897.06 ns** |     **?** |       **?** | **0.1068** |      **-** |    **9216 B** |           **?** |
|                               |            |                |             |                 |              |                 |               |              |       |         |        |        |           |             |
| PublishBurst                  | ShortRun   | 3              | 1           | ?               | 50           |     13,423.9 ns |   6,137.62 ns |    336.42 ns |     ? |       ? | 0.1068 |      - |    9216 B |           ? |
|                               |            |                |             |                 |              |                 |               |              |       |         |        |        |           |             |
| **PublishBurst**                  | **Job-YFEFPZ** | **10**             | **Default**     | **?**               | **100**          |     **24,156.8 ns** |      **36.93 ns** |     **19.31 ns** |     **?** |       **?** | **0.2136** |      **-** |   **18416 B** |           **?** |
|                               |            |                |             |                 |              |                 |               |              |       |         |        |        |           |             |
| PublishBurst                  | ShortRun   | 3              | 1           | ?               | 100          |     23,950.6 ns |     352.25 ns |     19.31 ns |     ? |       ? | 0.2136 |      - |   18416 B |           ? |
