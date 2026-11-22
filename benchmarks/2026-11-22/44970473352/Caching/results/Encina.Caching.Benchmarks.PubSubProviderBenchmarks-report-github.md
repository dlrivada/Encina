```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.70GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3


```
| Method                        | Job        | IterationCount | LaunchCount | WarmupCount | subscriberCount | messageCount | Mean            | Error        | StdDev       | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |----------- |--------------- |------------ |------------ |---------------- |------------- |----------------:|-------------:|-------------:|------:|--------:|-------:|-------:|----------:|------------:|
| **PublishAsync_NoSubscriber**     | **Job-YFEFPZ** | **10**             | **Default**     | **3**           | **?**               | **?**            |        **960.7 ns** |      **2.80 ns** |      **1.66 ns** |  **3.02** |    **0.01** | **0.0095** |      **-** |     **168 B** |        **1.31** |
| SubscribeAndUnsubscribe       | Job-YFEFPZ | 10             | Default     | 3           | ?               | ?            |      2,275.0 ns |     42.69 ns |     25.40 ns |  7.14 |    0.08 | 0.0458 | 0.0420 |     776 B |        6.06 |
| PublishAsync_SingleSubscriber | Job-YFEFPZ | 10             | Default     | 3           | ?               | ?            |        318.6 ns |      1.53 ns |      1.01 ns |  1.00 |    0.00 | 0.0076 |      - |     128 B |        1.00 |
|                               |            |                |             |             |                 |              |                 |              |              |       |         |        |        |           |             |
| PublishAsync_NoSubscriber     | MediumRun  | 15             | 2           | 10          | ?               | ?            |        968.1 ns |      1.99 ns |      2.78 ns |  3.20 |    0.02 | 0.0095 |      - |     168 B |        1.31 |
| SubscribeAndUnsubscribe       | MediumRun  | 15             | 2           | 10          | ?               | ?            |      2,246.3 ns |     27.30 ns |     40.02 ns |  7.42 |    0.14 | 0.0458 | 0.0420 |     776 B |        6.06 |
| PublishAsync_SingleSubscriber | MediumRun  | 15             | 2           | 10          | ?               | ?            |        302.6 ns |      1.53 ns |      2.24 ns |  1.00 |    0.01 | 0.0076 |      - |     128 B |        1.00 |
|                               |            |                |             |             |                 |              |                 |              |              |       |         |        |        |           |             |
| **MultipleSubscribers**           | **Job-YFEFPZ** | **10**             | **Default**     | **3**           | **5**               | **?**            | **10,266,716.5 ns** | **17,487.20 ns** | **11,566.70 ns** |     **?** |       **?** |      **-** |      **-** |    **1856 B** |           **?** |
|                               |            |                |             |             |                 |              |                 |              |              |       |         |        |        |           |             |
| MultipleSubscribers           | MediumRun  | 15             | 2           | 10          | 5               | ?            | 10,252,217.5 ns | 16,059.22 ns | 23,539.39 ns |     ? |       ? |      - |      - |    1856 B |           ? |
|                               |            |                |             |             |                 |              |                 |              |              |       |         |        |        |           |             |
| **PublishBurst**                  | **Job-YFEFPZ** | **10**             | **Default**     | **3**           | **?**               | **10**           |      **3,673.5 ns** |     **26.97 ns** |     **17.84 ns** |     **?** |       **?** | **0.1106** |      **-** |    **1856 B** |           **?** |
|                               |            |                |             |             |                 |              |                 |              |              |       |         |        |        |           |             |
| PublishBurst                  | MediumRun  | 15             | 2           | 10          | ?               | 10           |      3,946.8 ns |     88.72 ns |    130.05 ns |     ? |       ? | 0.1068 |      - |    1856 B |           ? |
|                               |            |                |             |             |                 |              |                 |              |              |       |         |        |        |           |             |
| **MultipleSubscribers**           | **Job-YFEFPZ** | **10**             | **Default**     | **3**           | **10**              | **?**            | **10,199,822.0 ns** | **14,645.53 ns** |  **9,687.11 ns** |     **?** |       **?** |      **-** |      **-** |    **2456 B** |           **?** |
|                               |            |                |             |             |                 |              |                 |              |              |       |         |        |        |           |             |
| MultipleSubscribers           | MediumRun  | 15             | 2           | 10          | 10              | ?            | 10,232,889.7 ns | 31,008.13 ns | 45,451.31 ns |     ? |       ? |      - |      - |    2456 B |           ? |
|                               |            |                |             |             |                 |              |                 |              |              |       |         |        |        |           |             |
| **MultipleSubscribers**           | **Job-YFEFPZ** | **10**             | **Default**     | **3**           | **20**              | **?**            | **10,264,717.6 ns** | **28,779.40 ns** | **19,035.79 ns** |     **?** |       **?** |      **-** |      **-** |    **3656 B** |           **?** |
|                               |            |                |             |             |                 |              |                 |              |              |       |         |        |        |           |             |
| MultipleSubscribers           | MediumRun  | 15             | 2           | 10          | 20              | ?            | 10,229,134.9 ns | 18,242.27 ns | 27,304.17 ns |     ? |       ? |      - |      - |    3656 B |           ? |
|                               |            |                |             |             |                 |              |                 |              |              |       |         |        |        |           |             |
| **PublishBurst**                  | **Job-YFEFPZ** | **10**             | **Default**     | **3**           | **?**               | **50**           |     **18,078.5 ns** |     **96.67 ns** |     **63.94 ns** |     **?** |       **?** | **0.5493** |      **-** |    **9216 B** |           **?** |
|                               |            |                |             |             |                 |              |                 |              |              |       |         |        |        |           |             |
| PublishBurst                  | MediumRun  | 15             | 2           | 10          | ?               | 50           |     18,739.9 ns |     87.43 ns |    119.68 ns |     ? |       ? | 0.5493 |      - |    9216 B |           ? |
|                               |            |                |             |             |                 |              |                 |              |              |       |         |        |        |           |             |
| **PublishBurst**                  | **Job-YFEFPZ** | **10**             | **Default**     | **3**           | **?**               | **100**          |     **36,187.9 ns** |    **432.82 ns** |    **286.28 ns** |     **?** |       **?** | **1.0986** |      **-** |   **18416 B** |           **?** |
|                               |            |                |             |             |                 |              |                 |              |              |       |         |        |        |           |             |
| PublishBurst                  | MediumRun  | 15             | 2           | 10          | ?               | 100          |     37,665.0 ns |    447.44 ns |    655.84 ns |     ? |       ? | 1.0986 |      - |   18416 B |           ? |
