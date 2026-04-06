```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method                        | Job        | IterationCount | LaunchCount | subscriberCount | messageCount | Mean            | Error         | StdDev       | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |----------- |--------------- |------------ |---------------- |------------- |----------------:|--------------:|-------------:|------:|--------:|-------:|-------:|----------:|------------:|
| **PublishAsync_SingleSubscriber** | **Job-YFEFPZ** | **10**             | **Default**     | **?**               | **?**            |        **283.3 ns** |       **2.39 ns** |      **1.58 ns** |  **1.00** |    **0.01** | **0.0076** |      **-** |     **128 B** |        **1.00** |
| PublishAsync_NoSubscriber     | Job-YFEFPZ | 10             | Default     | ?               | ?            |        914.5 ns |       3.61 ns |      2.39 ns |  3.23 |    0.02 | 0.0095 |      - |     168 B |        1.31 |
| SubscribeAndUnsubscribe       | Job-YFEFPZ | 10             | Default     | ?               | ?            |      1,983.4 ns |      76.03 ns |     50.29 ns |  7.00 |    0.17 | 0.0458 | 0.0420 |     776 B |        6.06 |
|                               |            |                |             |                 |              |                 |               |              |       |         |        |        |           |             |
| PublishAsync_SingleSubscriber | ShortRun   | 3              | 1           | ?               | ?            |        291.4 ns |      17.31 ns |      0.95 ns |  1.00 |    0.00 | 0.0076 |      - |     128 B |        1.00 |
| PublishAsync_NoSubscriber     | ShortRun   | 3              | 1           | ?               | ?            |        903.0 ns |      43.47 ns |      2.38 ns |  3.10 |    0.01 | 0.0095 |      - |     168 B |        1.31 |
| SubscribeAndUnsubscribe       | ShortRun   | 3              | 1           | ?               | ?            |      2,000.6 ns |     185.53 ns |     10.17 ns |  6.87 |    0.04 | 0.0458 | 0.0420 |     776 B |        6.06 |
|                               |            |                |             |                 |              |                 |               |              |       |         |        |        |           |             |
| **MultipleSubscribers**           | **Job-YFEFPZ** | **10**             | **Default**     | **5**               | **?**            | **10,270,387.8 ns** |  **17,956.32 ns** | **11,876.99 ns** |     **?** |       **?** |      **-** |      **-** |    **1856 B** |           **?** |
|                               |            |                |             |                 |              |                 |               |              |       |         |        |        |           |             |
| MultipleSubscribers           | ShortRun   | 3              | 1           | 5               | ?            | 10,307,395.9 ns | 537,069.70 ns | 29,438.60 ns |     ? |       ? |      - |      - |    1856 B |           ? |
|                               |            |                |             |                 |              |                 |               |              |       |         |        |        |           |             |
| **PublishBurst**                  | **Job-YFEFPZ** | **10**             | **Default**     | **?**               | **10**           |      **3,622.0 ns** |      **28.41 ns** |     **18.79 ns** |     **?** |       **?** | **0.1106** |      **-** |    **1856 B** |           **?** |
|                               |            |                |             |                 |              |                 |               |              |       |         |        |        |           |             |
| PublishBurst                  | ShortRun   | 3              | 1           | ?               | 10           |      3,541.8 ns |     144.99 ns |      7.95 ns |     ? |       ? | 0.1106 |      - |    1856 B |           ? |
|                               |            |                |             |                 |              |                 |               |              |       |         |        |        |           |             |
| **MultipleSubscribers**           | **Job-YFEFPZ** | **10**             | **Default**     | **10**              | **?**            | **10,275,287.5 ns** |  **24,513.71 ns** | **14,587.71 ns** |     **?** |       **?** |      **-** |      **-** |    **2456 B** |           **?** |
|                               |            |                |             |                 |              |                 |               |              |       |         |        |        |           |             |
| MultipleSubscribers           | ShortRun   | 3              | 1           | 10              | ?            | 10,301,131.9 ns |  26,193.68 ns |  1,435.76 ns |     ? |       ? |      - |      - |    2456 B |           ? |
|                               |            |                |             |                 |              |                 |               |              |       |         |        |        |           |             |
| **MultipleSubscribers**           | **Job-YFEFPZ** | **10**             | **Default**     | **20**              | **?**            | **10,280,286.6 ns** |  **33,027.14 ns** | **21,845.41 ns** |     **?** |       **?** |      **-** |      **-** |    **3656 B** |           **?** |
|                               |            |                |             |                 |              |                 |               |              |       |         |        |        |           |             |
| MultipleSubscribers           | ShortRun   | 3              | 1           | 20              | ?            | 10,274,467.6 ns |  42,985.87 ns |  2,356.20 ns |     ? |       ? |      - |      - |    3656 B |           ? |
|                               |            |                |             |                 |              |                 |               |              |       |         |        |        |           |             |
| **PublishBurst**                  | **Job-YFEFPZ** | **10**             | **Default**     | **?**               | **50**           |     **17,780.3 ns** |     **169.95 ns** |    **112.41 ns** |     **?** |       **?** | **0.5493** |      **-** |    **9216 B** |           **?** |
|                               |            |                |             |                 |              |                 |               |              |       |         |        |        |           |             |
| PublishBurst                  | ShortRun   | 3              | 1           | ?               | 50           |     18,238.3 ns |     256.29 ns |     14.05 ns |     ? |       ? | 0.5493 |      - |    9216 B |           ? |
|                               |            |                |             |                 |              |                 |               |              |       |         |        |        |           |             |
| **PublishBurst**                  | **Job-YFEFPZ** | **10**             | **Default**     | **?**               | **100**          |     **35,493.1 ns** |     **184.24 ns** |    **109.64 ns** |     **?** |       **?** | **1.0986** |      **-** |   **18416 B** |           **?** |
|                               |            |                |             |                 |              |                 |               |              |       |         |        |        |           |             |
| PublishBurst                  | ShortRun   | 3              | 1           | ?               | 100          |     35,485.5 ns |   3,348.18 ns |    183.53 ns |     ? |       ? | 1.0986 |      - |   18416 B |           ? |
