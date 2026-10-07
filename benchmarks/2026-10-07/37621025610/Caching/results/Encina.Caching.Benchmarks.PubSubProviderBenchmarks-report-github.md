```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.61GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method                        | Job        | IterationCount | LaunchCount | subscriberCount | messageCount | Mean            | Error         | StdDev       | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |----------- |--------------- |------------ |---------------- |------------- |----------------:|--------------:|-------------:|------:|--------:|-------:|-------:|----------:|------------:|
| **PublishAsync_NoSubscriber**     | **Job-YFEFPZ** | **10**             | **Default**     | **?**               | **?**            |        **918.0 ns** |       **2.89 ns** |      **1.72 ns** |  **2.99** |    **0.01** | **0.0095** |      **-** |     **168 B** |        **1.31** |
| SubscribeAndUnsubscribe       | Job-YFEFPZ | 10             | Default     | ?               | ?            |      2,226.0 ns |      76.16 ns |     45.32 ns |  7.24 |    0.14 | 0.0458 | 0.0420 |     776 B |        6.06 |
| PublishAsync_SingleSubscriber | Job-YFEFPZ | 10             | Default     | ?               | ?            |        307.3 ns |       0.97 ns |      0.64 ns |  1.00 |    0.00 | 0.0076 |      - |     128 B |        1.00 |
|                               |            |                |             |                 |              |                 |               |              |       |         |        |        |           |             |
| PublishAsync_NoSubscriber     | ShortRun   | 3              | 1           | ?               | ?            |        922.2 ns |      22.92 ns |      1.26 ns |  3.06 |    0.00 | 0.0095 |      - |     168 B |        1.31 |
| SubscribeAndUnsubscribe       | ShortRun   | 3              | 1           | ?               | ?            |      2,135.4 ns |     288.59 ns |     15.82 ns |  7.08 |    0.05 | 0.0458 | 0.0420 |     776 B |        6.06 |
| PublishAsync_SingleSubscriber | ShortRun   | 3              | 1           | ?               | ?            |        301.7 ns |       0.59 ns |      0.03 ns |  1.00 |    0.00 | 0.0076 |      - |     128 B |        1.00 |
|                               |            |                |             |                 |              |                 |               |              |       |         |        |        |           |             |
| **MultipleSubscribers**           | **Job-YFEFPZ** | **10**             | **Default**     | **5**               | **?**            | **10,340,127.2 ns** |  **40,282.30 ns** | **26,644.24 ns** |     **?** |       **?** |      **-** |      **-** |    **1856 B** |           **?** |
|                               |            |                |             |                 |              |                 |               |              |       |         |        |        |           |             |
| MultipleSubscribers           | ShortRun   | 3              | 1           | 5               | ?            | 10,301,938.1 ns | 272,827.59 ns | 14,954.60 ns |     ? |       ? |      - |      - |    1856 B |           ? |
|                               |            |                |             |                 |              |                 |               |              |       |         |        |        |           |             |
| **PublishBurst**                  | **Job-YFEFPZ** | **10**             | **Default**     | **?**               | **10**           |      **3,637.7 ns** |      **11.65 ns** |      **7.70 ns** |     **?** |       **?** | **0.1106** |      **-** |    **1856 B** |           **?** |
|                               |            |                |             |                 |              |                 |               |              |       |         |        |        |           |             |
| PublishBurst                  | ShortRun   | 3              | 1           | ?               | 10           |      3,604.1 ns |     244.82 ns |     13.42 ns |     ? |       ? | 0.1106 |      - |    1856 B |           ? |
|                               |            |                |             |                 |              |                 |               |              |       |         |        |        |           |             |
| **MultipleSubscribers**           | **Job-YFEFPZ** | **10**             | **Default**     | **10**              | **?**            | **10,236,818.9 ns** |  **30,431.66 ns** | **20,128.66 ns** |     **?** |       **?** |      **-** |      **-** |    **2456 B** |           **?** |
|                               |            |                |             |                 |              |                 |               |              |       |         |        |        |           |             |
| MultipleSubscribers           | ShortRun   | 3              | 1           | 10              | ?            | 10,238,556.2 ns | 355,620.14 ns | 19,492.74 ns |     ? |       ? |      - |      - |    2456 B |           ? |
|                               |            |                |             |                 |              |                 |               |              |       |         |        |        |           |             |
| **MultipleSubscribers**           | **Job-YFEFPZ** | **10**             | **Default**     | **20**              | **?**            | **10,239,243.5 ns** |  **35,128.59 ns** | **23,235.39 ns** |     **?** |       **?** |      **-** |      **-** |    **3656 B** |           **?** |
|                               |            |                |             |                 |              |                 |               |              |       |         |        |        |           |             |
| MultipleSubscribers           | ShortRun   | 3              | 1           | 20              | ?            | 10,249,308.6 ns | 183,323.07 ns | 10,048.56 ns |     ? |       ? |      - |      - |    3656 B |           ? |
|                               |            |                |             |                 |              |                 |               |              |       |         |        |        |           |             |
| **PublishBurst**                  | **Job-YFEFPZ** | **10**             | **Default**     | **?**               | **50**           |     **18,146.9 ns** |      **44.51 ns** |     **26.49 ns** |     **?** |       **?** | **0.5493** |      **-** |    **9216 B** |           **?** |
|                               |            |                |             |                 |              |                 |               |              |       |         |        |        |           |             |
| PublishBurst                  | ShortRun   | 3              | 1           | ?               | 50           |     18,257.9 ns |     541.79 ns |     29.70 ns |     ? |       ? | 0.5493 |      - |    9216 B |           ? |
|                               |            |                |             |                 |              |                 |               |              |       |         |        |        |           |             |
| **PublishBurst**                  | **Job-YFEFPZ** | **10**             | **Default**     | **?**               | **100**          |     **38,116.6 ns** |     **309.28 ns** |    **204.57 ns** |     **?** |       **?** | **1.0986** |      **-** |   **18416 B** |           **?** |
|                               |            |                |             |                 |              |                 |               |              |       |         |        |        |           |             |
| PublishBurst                  | ShortRun   | 3              | 1           | ?               | 100          |     35,597.5 ns |   1,665.21 ns |     91.28 ns |     ? |       ? | 1.0986 |      - |   18416 B |           ? |
