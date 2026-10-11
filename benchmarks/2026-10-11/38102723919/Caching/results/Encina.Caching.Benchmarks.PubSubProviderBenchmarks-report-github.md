```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  MediumRun  : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4


```
| Method                        | Job        | IterationCount | LaunchCount | WarmupCount | subscriberCount | messageCount | Mean            | Error        | StdDev       | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |----------- |--------------- |------------ |------------ |---------------- |------------- |----------------:|-------------:|-------------:|------:|--------:|-------:|-------:|----------:|------------:|
| **PublishAsync_NoSubscriber**     | **Job-YFEFPZ** | **10**             | **Default**     | **3**           | **?**               | **?**            |        **859.7 ns** |      **3.12 ns** |      **1.85 ns** |  **3.74** |    **0.02** | **0.0095** |      **-** |     **168 B** |        **1.31** |
| SubscribeAndUnsubscribe       | Job-YFEFPZ | 10             | Default     | 3           | ?               | ?            |      1,705.7 ns |      9.40 ns |      5.60 ns |  7.42 |    0.04 | 0.0458 | 0.0439 |     776 B |        6.06 |
| PublishAsync_SingleSubscriber | Job-YFEFPZ | 10             | Default     | 3           | ?               | ?            |        229.8 ns |      1.53 ns |      1.01 ns |  1.00 |    0.01 | 0.0076 |      - |     128 B |        1.00 |
|                               |            |                |             |             |                 |              |                 |              |              |       |         |        |        |           |             |
| PublishAsync_NoSubscriber     | MediumRun  | 15             | 2           | 10          | ?               | ?            |        859.7 ns |      1.97 ns |      2.96 ns |  3.76 |    0.02 | 0.0095 |      - |     168 B |        1.31 |
| SubscribeAndUnsubscribe       | MediumRun  | 15             | 2           | 10          | ?               | ?            |      1,693.3 ns |      6.91 ns |      9.91 ns |  7.40 |    0.06 | 0.0458 | 0.0439 |     776 B |        6.06 |
| PublishAsync_SingleSubscriber | MediumRun  | 15             | 2           | 10          | ?               | ?            |        228.8 ns |      0.84 ns |      1.20 ns |  1.00 |    0.01 | 0.0076 |      - |     128 B |        1.00 |
|                               |            |                |             |             |                 |              |                 |              |              |       |         |        |        |           |             |
| **MultipleSubscribers**           | **Job-YFEFPZ** | **10**             | **Default**     | **3**           | **5**               | **?**            | **10,194,740.3 ns** | **15,728.24 ns** | **10,403.26 ns** |     **?** |       **?** |      **-** |      **-** |    **1856 B** |           **?** |
|                               |            |                |             |             |                 |              |                 |              |              |       |         |        |        |           |             |
| MultipleSubscribers           | MediumRun  | 15             | 2           | 10          | 5               | ?            | 10,226,842.1 ns | 13,403.28 ns | 20,061.40 ns |     ? |       ? |      - |      - |    1856 B |           ? |
|                               |            |                |             |             |                 |              |                 |              |              |       |         |        |        |           |             |
| **PublishBurst**                  | **Job-YFEFPZ** | **10**             | **Default**     | **3**           | **?**               | **10**           |      **2,664.9 ns** |      **7.51 ns** |      **4.47 ns** |     **?** |       **?** | **0.1106** |      **-** |    **1856 B** |           **?** |
|                               |            |                |             |             |                 |              |                 |              |              |       |         |        |        |           |             |
| PublishBurst                  | MediumRun  | 15             | 2           | 10          | ?               | 10           |      2,680.3 ns |      7.40 ns |     10.62 ns |     ? |       ? | 0.1106 |      - |    1856 B |           ? |
|                               |            |                |             |             |                 |              |                 |              |              |       |         |        |        |           |             |
| **MultipleSubscribers**           | **Job-YFEFPZ** | **10**             | **Default**     | **3**           | **10**              | **?**            | **10,203,084.9 ns** |  **6,939.92 ns** |  **4,590.33 ns** |     **?** |       **?** |      **-** |      **-** |    **2456 B** |           **?** |
|                               |            |                |             |             |                 |              |                 |              |              |       |         |        |        |           |             |
| MultipleSubscribers           | MediumRun  | 15             | 2           | 10          | 10              | ?            | 10,224,050.5 ns | 10,612.68 ns | 15,555.93 ns |     ? |       ? |      - |      - |    2456 B |           ? |
|                               |            |                |             |             |                 |              |                 |              |              |       |         |        |        |           |             |
| **MultipleSubscribers**           | **Job-YFEFPZ** | **10**             | **Default**     | **3**           | **20**              | **?**            | **10,212,786.3 ns** | **12,909.52 ns** |  **8,538.85 ns** |     **?** |       **?** |      **-** |      **-** |    **3656 B** |           **?** |
|                               |            |                |             |             |                 |              |                 |              |              |       |         |        |        |           |             |
| MultipleSubscribers           | MediumRun  | 15             | 2           | 10          | 20              | ?            | 10,219,724.9 ns | 13,156.70 ns | 19,692.33 ns |     ? |       ? |      - |      - |    3656 B |           ? |
|                               |            |                |             |             |                 |              |                 |              |              |       |         |        |        |           |             |
| **PublishBurst**                  | **Job-YFEFPZ** | **10**             | **Default**     | **3**           | **?**               | **50**           |     **13,217.8 ns** |     **92.06 ns** |     **54.78 ns** |     **?** |       **?** | **0.5493** |      **-** |    **9216 B** |           **?** |
|                               |            |                |             |             |                 |              |                 |              |              |       |         |        |        |           |             |
| PublishBurst                  | MediumRun  | 15             | 2           | 10          | ?               | 50           |     13,334.8 ns |     88.26 ns |    132.11 ns |     ? |       ? | 0.5493 |      - |    9216 B |           ? |
|                               |            |                |             |             |                 |              |                 |              |              |       |         |        |        |           |             |
| **PublishBurst**                  | **Job-YFEFPZ** | **10**             | **Default**     | **3**           | **?**               | **100**          |     **26,473.7 ns** |     **90.05 ns** |     **53.59 ns** |     **?** |       **?** | **1.0986** |      **-** |   **18416 B** |           **?** |
|                               |            |                |             |             |                 |              |                 |              |              |       |         |        |        |           |             |
| PublishBurst                  | MediumRun  | 15             | 2           | 10          | ?               | 100          |     26,259.8 ns |    144.23 ns |    211.41 ns |     ? |       ? | 1.0986 |      - |   18416 B |           ? |
