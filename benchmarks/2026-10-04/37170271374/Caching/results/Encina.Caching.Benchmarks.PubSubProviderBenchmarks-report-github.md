```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  MediumRun  : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4


```
| Method                        | Job        | IterationCount | LaunchCount | WarmupCount | subscriberCount | messageCount | Mean            | Error        | StdDev       | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |----------- |--------------- |------------ |------------ |---------------- |------------- |----------------:|-------------:|-------------:|------:|--------:|-------:|-------:|----------:|------------:|
| **PublishAsync_NoSubscriber**     | **Job-YFEFPZ** | **10**             | **Default**     | **3**           | **?**               | **?**            |        **646.4 ns** |      **4.67 ns** |      **2.78 ns** |  **3.72** |    **0.03** | **0.0095** |      **-** |     **168 B** |        **1.31** |
| SubscribeAndUnsubscribe       | Job-YFEFPZ | 10             | Default     | 3           | ?               | ?            |      1,321.5 ns |     37.99 ns |     22.61 ns |  7.61 |    0.13 | 0.0458 | 0.0439 |     776 B |        6.06 |
| PublishAsync_SingleSubscriber | Job-YFEFPZ | 10             | Default     | 3           | ?               | ?            |        173.7 ns |      2.19 ns |      1.14 ns |  1.00 |    0.01 | 0.0076 |      - |     128 B |        1.00 |
|                               |            |                |             |             |                 |              |                 |              |              |       |         |        |        |           |             |
| PublishAsync_NoSubscriber     | MediumRun  | 15             | 2           | 10          | ?               | ?            |        638.9 ns |      5.28 ns |      7.40 ns |  3.55 |    0.10 | 0.0095 |      - |     168 B |        1.31 |
| SubscribeAndUnsubscribe       | MediumRun  | 15             | 2           | 10          | ?               | ?            |      1,320.9 ns |     16.20 ns |     23.75 ns |  7.34 |    0.23 | 0.0458 | 0.0439 |     776 B |        6.06 |
| PublishAsync_SingleSubscriber | MediumRun  | 15             | 2           | 10          | ?               | ?            |        180.0 ns |      3.25 ns |      4.66 ns |  1.00 |    0.04 | 0.0076 |      - |     128 B |        1.00 |
|                               |            |                |             |             |                 |              |                 |              |              |       |         |        |        |           |             |
| **MultipleSubscribers**           | **Job-YFEFPZ** | **10**             | **Default**     | **3**           | **5**               | **?**            | **10,147,533.8 ns** | **22,369.17 ns** | **13,311.54 ns** |     **?** |       **?** |      **-** |      **-** |    **1856 B** |           **?** |
|                               |            |                |             |             |                 |              |                 |              |              |       |         |        |        |           |             |
| MultipleSubscribers           | MediumRun  | 15             | 2           | 10          | 5               | ?            | 10,150,815.3 ns | 10,452.33 ns | 14,990.43 ns |     ? |       ? |      - |      - |    1856 B |           ? |
|                               |            |                |             |             |                 |              |                 |              |              |       |         |        |        |           |             |
| **PublishBurst**                  | **Job-YFEFPZ** | **10**             | **Default**     | **3**           | **?**               | **10**           |      **2,129.8 ns** |     **48.63 ns** |     **28.94 ns** |     **?** |       **?** | **0.1106** |      **-** |    **1856 B** |           **?** |
|                               |            |                |             |             |                 |              |                 |              |              |       |         |        |        |           |             |
| PublishBurst                  | MediumRun  | 15             | 2           | 10          | ?               | 10           |      2,108.6 ns |      7.86 ns |     10.76 ns |     ? |       ? | 0.1106 |      - |    1856 B |           ? |
|                               |            |                |             |             |                 |              |                 |              |              |       |         |        |        |           |             |
| **MultipleSubscribers**           | **Job-YFEFPZ** | **10**             | **Default**     | **3**           | **10**              | **?**            | **10,148,416.2 ns** | **27,741.04 ns** | **18,348.98 ns** |     **?** |       **?** |      **-** |      **-** |    **2456 B** |           **?** |
|                               |            |                |             |             |                 |              |                 |              |              |       |         |        |        |           |             |
| MultipleSubscribers           | MediumRun  | 15             | 2           | 10          | 10              | ?            | 10,188,872.1 ns | 23,801.30 ns | 34,887.63 ns |     ? |       ? |      - |      - |    2456 B |           ? |
|                               |            |                |             |             |                 |              |                 |              |              |       |         |        |        |           |             |
| **MultipleSubscribers**           | **Job-YFEFPZ** | **10**             | **Default**     | **3**           | **20**              | **?**            | **10,155,818.9 ns** | **30,509.03 ns** | **20,179.83 ns** |     **?** |       **?** |      **-** |      **-** |    **3656 B** |           **?** |
|                               |            |                |             |             |                 |              |                 |              |              |       |         |        |        |           |             |
| MultipleSubscribers           | MediumRun  | 15             | 2           | 10          | 20              | ?            | 10,161,618.2 ns | 16,719.17 ns | 25,024.47 ns |     ? |       ? |      - |      - |    3656 B |           ? |
|                               |            |                |             |             |                 |              |                 |              |              |       |         |        |        |           |             |
| **PublishBurst**                  | **Job-YFEFPZ** | **10**             | **Default**     | **3**           | **?**               | **50**           |     **10,592.4 ns** |    **241.13 ns** |    **143.49 ns** |     **?** |       **?** | **0.5493** |      **-** |    **9216 B** |           **?** |
|                               |            |                |             |             |                 |              |                 |              |              |       |         |        |        |           |             |
| PublishBurst                  | MediumRun  | 15             | 2           | 10          | ?               | 50           |     10,613.9 ns |    201.38 ns |    295.18 ns |     ? |       ? | 0.5493 |      - |    9216 B |           ? |
|                               |            |                |             |             |                 |              |                 |              |              |       |         |        |        |           |             |
| **PublishBurst**                  | **Job-YFEFPZ** | **10**             | **Default**     | **3**           | **?**               | **100**          |     **20,742.5 ns** |    **586.69 ns** |    **306.85 ns** |     **?** |       **?** | **1.0986** |      **-** |   **18416 B** |           **?** |
|                               |            |                |             |             |                 |              |                 |              |              |       |         |        |        |           |             |
| PublishBurst                  | MediumRun  | 15             | 2           | 10          | ?               | 100          |     21,422.0 ns |    756.02 ns |  1,108.17 ns |     ? |       ? | 1.0986 |      - |   18416 B |           ? |
