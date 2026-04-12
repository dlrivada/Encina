```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method                        | Job        | IterationCount | LaunchCount | WarmupCount | subscriberCount | messageCount | Mean            | Error        | StdDev       | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |----------- |--------------- |------------ |------------ |---------------- |------------- |----------------:|-------------:|-------------:|------:|--------:|-------:|-------:|----------:|------------:|
| **PublishAsync_NoSubscriber**     | **Job-YFEFPZ** | **10**             | **Default**     | **3**           | **?**               | **?**            |      **1,118.3 ns** |      **2.71 ns** |      **1.42 ns** |  **3.82** |    **0.01** | **0.0095** |      **-** |     **168 B** |        **1.31** |
| SubscribeAndUnsubscribe       | Job-YFEFPZ | 10             | Default     | 3           | ?               | ?            |      2,290.3 ns |     28.80 ns |     19.05 ns |  7.83 |    0.07 | 0.0458 | 0.0420 |     776 B |        6.06 |
| PublishAsync_SingleSubscriber | Job-YFEFPZ | 10             | Default     | 3           | ?               | ?            |        292.6 ns |      1.45 ns |      0.87 ns |  1.00 |    0.00 | 0.0076 |      - |     128 B |        1.00 |
|                               |            |                |             |             |                 |              |                 |              |              |       |         |        |        |           |             |
| PublishAsync_NoSubscriber     | MediumRun  | 15             | 2           | 10          | ?               | ?            |      1,125.2 ns |      3.67 ns |      5.15 ns |  3.87 |    0.02 | 0.0095 |      - |     168 B |        1.31 |
| SubscribeAndUnsubscribe       | MediumRun  | 15             | 2           | 10          | ?               | ?            |      2,307.5 ns |     17.34 ns |     24.87 ns |  7.94 |    0.09 | 0.0458 | 0.0420 |     776 B |        6.06 |
| PublishAsync_SingleSubscriber | MediumRun  | 15             | 2           | 10          | ?               | ?            |        290.6 ns |      0.58 ns |      0.83 ns |  1.00 |    0.00 | 0.0076 |      - |     128 B |        1.00 |
|                               |            |                |             |             |                 |              |                 |              |              |       |         |        |        |           |             |
| **MultipleSubscribers**           | **Job-YFEFPZ** | **10**             | **Default**     | **3**           | **5**               | **?**            | **10,285,698.2 ns** | **43,953.55 ns** | **26,156.05 ns** |     **?** |       **?** |      **-** |      **-** |    **1856 B** |           **?** |
|                               |            |                |             |             |                 |              |                 |              |              |       |         |        |        |           |             |
| MultipleSubscribers           | MediumRun  | 15             | 2           | 10          | 5               | ?            | 10,300,257.7 ns | 21,603.98 ns | 32,335.82 ns |     ? |       ? |      - |      - |    1856 B |           ? |
|                               |            |                |             |             |                 |              |                 |              |              |       |         |        |        |           |             |
| **PublishBurst**                  | **Job-YFEFPZ** | **10**             | **Default**     | **3**           | **?**               | **10**           |      **3,607.8 ns** |     **14.84 ns** |      **9.81 ns** |     **?** |       **?** | **0.1106** |      **-** |    **1856 B** |           **?** |
|                               |            |                |             |             |                 |              |                 |              |              |       |         |        |        |           |             |
| PublishBurst                  | MediumRun  | 15             | 2           | 10          | ?               | 10           |      3,422.8 ns |      5.83 ns |      7.79 ns |     ? |       ? | 0.1106 |      - |    1856 B |           ? |
|                               |            |                |             |             |                 |              |                 |              |              |       |         |        |        |           |             |
| **MultipleSubscribers**           | **Job-YFEFPZ** | **10**             | **Default**     | **3**           | **10**              | **?**            | **10,266,660.8 ns** | **29,325.91 ns** | **19,397.27 ns** |     **?** |       **?** |      **-** |      **-** |    **2456 B** |           **?** |
|                               |            |                |             |             |                 |              |                 |              |              |       |         |        |        |           |             |
| MultipleSubscribers           | MediumRun  | 15             | 2           | 10          | 10              | ?            | 10,234,371.5 ns | 14,477.00 ns | 21,668.49 ns |     ? |       ? |      - |      - |    2456 B |           ? |
|                               |            |                |             |             |                 |              |                 |              |              |       |         |        |        |           |             |
| **MultipleSubscribers**           | **Job-YFEFPZ** | **10**             | **Default**     | **3**           | **20**              | **?**            | **10,228,293.9 ns** | **43,992.30 ns** | **29,098.19 ns** |     **?** |       **?** |      **-** |      **-** |    **3656 B** |           **?** |
|                               |            |                |             |             |                 |              |                 |              |              |       |         |        |        |           |             |
| MultipleSubscribers           | MediumRun  | 15             | 2           | 10          | 20              | ?            | 10,225,904.5 ns | 11,537.44 ns | 17,268.69 ns |     ? |       ? |      - |      - |    3656 B |           ? |
|                               |            |                |             |             |                 |              |                 |              |              |       |         |        |        |           |             |
| **PublishBurst**                  | **Job-YFEFPZ** | **10**             | **Default**     | **3**           | **?**               | **50**           |     **17,083.9 ns** |    **172.83 ns** |    **114.32 ns** |     **?** |       **?** | **0.5493** |      **-** |    **9216 B** |           **?** |
|                               |            |                |             |             |                 |              |                 |              |              |       |         |        |        |           |             |
| PublishBurst                  | MediumRun  | 15             | 2           | 10          | ?               | 50           |     16,968.4 ns |     52.13 ns |     71.35 ns |     ? |       ? | 0.5493 |      - |    9216 B |           ? |
|                               |            |                |             |             |                 |              |                 |              |              |       |         |        |        |           |             |
| **PublishBurst**                  | **Job-YFEFPZ** | **10**             | **Default**     | **3**           | **?**               | **100**          |     **33,826.7 ns** |     **72.76 ns** |     **43.30 ns** |     **?** |       **?** | **1.0986** |      **-** |   **18416 B** |           **?** |
|                               |            |                |             |             |                 |              |                 |              |              |       |         |        |        |           |             |
| PublishBurst                  | MediumRun  | 15             | 2           | 10          | ?               | 100          |     33,806.5 ns |    222.32 ns |    304.32 ns |     ? |       ? | 1.0986 |      - |   18416 B |           ? |
