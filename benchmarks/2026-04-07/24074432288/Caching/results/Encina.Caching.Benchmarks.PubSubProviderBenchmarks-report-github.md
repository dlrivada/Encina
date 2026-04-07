```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4
  MediumRun  : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4


```
| Method                        | Job        | IterationCount | LaunchCount | WarmupCount | subscriberCount | messageCount | Mean            | Error        | StdDev       | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |----------- |--------------- |------------ |------------ |---------------- |------------- |----------------:|-------------:|-------------:|------:|--------:|-------:|-------:|----------:|------------:|
| **PublishAsync_SingleSubscriber** | **Job-YFEFPZ** | **10**             | **Default**     | **3**           | **?**               | **?**            |        **296.4 ns** |      **2.01 ns** |      **1.20 ns** |  **1.00** |    **0.01** | **0.0048** |      **-** |     **128 B** |        **1.00** |
| PublishAsync_NoSubscriber     | Job-YFEFPZ | 10             | Default     | 3           | ?               | ?            |        649.9 ns |      1.28 ns |      0.76 ns |  2.19 |    0.01 | 0.0067 |      - |     168 B |        1.31 |
| SubscribeAndUnsubscribe       | Job-YFEFPZ | 10             | Default     | 3           | ?               | ?            |      2,057.3 ns |     67.49 ns |     44.64 ns |  6.94 |    0.15 | 0.0305 | 0.0267 |     776 B |        6.06 |
|                               |            |                |             |             |                 |              |                 |              |              |       |         |        |        |           |             |
| PublishAsync_SingleSubscriber | MediumRun  | 15             | 2           | 10          | ?               | ?            |        293.5 ns |      0.62 ns |      0.93 ns |  1.00 |    0.00 | 0.0048 |      - |     128 B |        1.00 |
| PublishAsync_NoSubscriber     | MediumRun  | 15             | 2           | 10          | ?               | ?            |        640.8 ns |      1.24 ns |      1.78 ns |  2.18 |    0.01 | 0.0067 |      - |     168 B |        1.31 |
| SubscribeAndUnsubscribe       | MediumRun  | 15             | 2           | 10          | ?               | ?            |      2,088.9 ns |     18.22 ns |     26.13 ns |  7.12 |    0.09 | 0.0305 | 0.0267 |     776 B |        6.06 |
|                               |            |                |             |             |                 |              |                 |              |              |       |         |        |        |           |             |
| **MultipleSubscribers**           | **Job-YFEFPZ** | **10**             | **Default**     | **3**           | **5**               | **?**            | **10,311,904.6 ns** | **56,544.10 ns** | **33,648.49 ns** |     **?** |       **?** |      **-** |      **-** |    **1856 B** |           **?** |
|                               |            |                |             |             |                 |              |                 |              |              |       |         |        |        |           |             |
| MultipleSubscribers           | MediumRun  | 15             | 2           | 10          | 5               | ?            | 10,313,481.1 ns | 31,514.68 ns | 46,193.82 ns |     ? |       ? |      - |      - |    1856 B |           ? |
|                               |            |                |             |             |                 |              |                 |              |              |       |         |        |        |           |             |
| **PublishBurst**                  | **Job-YFEFPZ** | **10**             | **Default**     | **3**           | **?**               | **10**           |      **3,786.6 ns** |     **71.00 ns** |     **46.96 ns** |     **?** |       **?** | **0.0725** |      **-** |    **1856 B** |           **?** |
|                               |            |                |             |             |                 |              |                 |              |              |       |         |        |        |           |             |
| PublishBurst                  | MediumRun  | 15             | 2           | 10          | ?               | 10           |      3,589.2 ns |      9.10 ns |     13.62 ns |     ? |       ? | 0.0725 |      - |    1856 B |           ? |
|                               |            |                |             |             |                 |              |                 |              |              |       |         |        |        |           |             |
| **MultipleSubscribers**           | **Job-YFEFPZ** | **10**             | **Default**     | **3**           | **10**              | **?**            | **10,298,697.6 ns** | **36,763.75 ns** | **21,877.52 ns** |     **?** |       **?** |      **-** |      **-** |    **2456 B** |           **?** |
|                               |            |                |             |             |                 |              |                 |              |              |       |         |        |        |           |             |
| MultipleSubscribers           | MediumRun  | 15             | 2           | 10          | 10              | ?            | 10,331,521.1 ns | 34,095.43 ns | 51,032.44 ns |     ? |       ? |      - |      - |    2456 B |           ? |
|                               |            |                |             |             |                 |              |                 |              |              |       |         |        |        |           |             |
| **MultipleSubscribers**           | **Job-YFEFPZ** | **10**             | **Default**     | **3**           | **20**              | **?**            | **10,329,086.1 ns** | **48,538.00 ns** | **32,104.88 ns** |     **?** |       **?** |      **-** |      **-** |    **3656 B** |           **?** |
|                               |            |                |             |             |                 |              |                 |              |              |       |         |        |        |           |             |
| MultipleSubscribers           | MediumRun  | 15             | 2           | 10          | 20              | ?            | 10,326,676.7 ns | 30,225.92 ns | 44,304.76 ns |     ? |       ? |      - |      - |    3656 B |           ? |
|                               |            |                |             |             |                 |              |                 |              |              |       |         |        |        |           |             |
| **PublishBurst**                  | **Job-YFEFPZ** | **10**             | **Default**     | **3**           | **?**               | **50**           |     **18,416.1 ns** |     **30.96 ns** |     **18.42 ns** |     **?** |       **?** | **0.3662** |      **-** |    **9216 B** |           **?** |
|                               |            |                |             |             |                 |              |                 |              |              |       |         |        |        |           |             |
| PublishBurst                  | MediumRun  | 15             | 2           | 10          | ?               | 50           |     17,868.2 ns |     67.29 ns |    100.71 ns |     ? |       ? | 0.3662 |      - |    9216 B |           ? |
|                               |            |                |             |             |                 |              |                 |              |              |       |         |        |        |           |             |
| **PublishBurst**                  | **Job-YFEFPZ** | **10**             | **Default**     | **3**           | **?**               | **100**          |     **36,856.8 ns** |     **35.29 ns** |     **18.46 ns** |     **?** |       **?** | **0.7324** |      **-** |   **18416 B** |           **?** |
|                               |            |                |             |             |                 |              |                 |              |              |       |         |        |        |           |             |
| PublishBurst                  | MediumRun  | 15             | 2           | 10          | ?               | 100          |     35,554.0 ns |     90.48 ns |    132.63 ns |     ? |       ? | 0.7324 |      - |   18416 B |           ? |
