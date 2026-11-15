```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6973P-C 3.56GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                        | Job        | IterationCount | LaunchCount | subscriberCount | messageCount | Mean            | Error         | StdDev       | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |----------- |--------------- |------------ |---------------- |------------- |----------------:|--------------:|-------------:|------:|--------:|-------:|-------:|----------:|------------:|
| **PublishAsync_NoSubscriber**     | **Job-YFEFPZ** | **10**             | **Default**     | **?**               | **?**            |        **544.0 ns** |       **2.54 ns** |      **1.51 ns** |  **2.26** |    **0.03** | **0.0019** |      **-** |     **168 B** |        **1.31** |
| SubscribeAndUnsubscribe       | Job-YFEFPZ | 10             | Default     | ?               | ?            |      1,947.6 ns |      47.92 ns |     28.51 ns |  8.08 |    0.14 | 0.0076 | 0.0057 |     776 B |        6.06 |
| PublishAsync_SingleSubscriber | Job-YFEFPZ | 10             | Default     | ?               | ?            |        241.1 ns |       4.14 ns |      2.74 ns |  1.00 |    0.02 | 0.0014 |      - |     128 B |        1.00 |
|                               |            |                |             |                 |              |                 |               |              |       |         |        |        |           |             |
| PublishAsync_NoSubscriber     | ShortRun   | 3              | 1           | ?               | ?            |        545.8 ns |      78.18 ns |      4.29 ns |  2.23 |    0.03 | 0.0019 |      - |     168 B |        1.31 |
| SubscribeAndUnsubscribe       | ShortRun   | 3              | 1           | ?               | ?            |      1,812.9 ns |     367.22 ns |     20.13 ns |  7.42 |    0.11 | 0.0076 | 0.0057 |     776 B |        6.06 |
| PublishAsync_SingleSubscriber | ShortRun   | 3              | 1           | ?               | ?            |        244.3 ns |      56.95 ns |      3.12 ns |  1.00 |    0.02 | 0.0014 |      - |     128 B |        1.00 |
|                               |            |                |             |                 |              |                 |               |              |       |         |        |        |           |             |
| **MultipleSubscribers**           | **Job-YFEFPZ** | **10**             | **Default**     | **5**               | **?**            | **10,186,323.8 ns** |  **22,820.75 ns** | **15,094.51 ns** |     **?** |       **?** |      **-** |      **-** |    **1856 B** |           **?** |
|                               |            |                |             |                 |              |                 |               |              |       |         |        |        |           |             |
| MultipleSubscribers           | ShortRun   | 3              | 1           | 5               | ?            | 10,168,063.8 ns | 165,852.53 ns |  9,090.94 ns |     ? |       ? |      - |      - |    1856 B |           ? |
|                               |            |                |             |                 |              |                 |               |              |       |         |        |        |           |             |
| **PublishBurst**                  | **Job-YFEFPZ** | **10**             | **Default**     | **?**               | **10**           |      **2,833.2 ns** |      **26.74 ns** |     **17.69 ns** |     **?** |       **?** | **0.0191** |      **-** |    **1856 B** |           **?** |
|                               |            |                |             |                 |              |                 |               |              |       |         |        |        |           |             |
| PublishBurst                  | ShortRun   | 3              | 1           | ?               | 10           |      2,846.4 ns |     390.79 ns |     21.42 ns |     ? |       ? | 0.0191 |      - |    1856 B |           ? |
|                               |            |                |             |                 |              |                 |               |              |       |         |        |        |           |             |
| **MultipleSubscribers**           | **Job-YFEFPZ** | **10**             | **Default**     | **10**              | **?**            | **10,229,276.7 ns** |  **63,114.01 ns** | **41,746.01 ns** |     **?** |       **?** |      **-** |      **-** |    **2456 B** |           **?** |
|                               |            |                |             |                 |              |                 |               |              |       |         |        |        |           |             |
| MultipleSubscribers           | ShortRun   | 3              | 1           | 10              | ?            | 10,272,262.4 ns | 612,746.56 ns | 33,586.71 ns |     ? |       ? |      - |      - |    2456 B |           ? |
|                               |            |                |             |                 |              |                 |               |              |       |         |        |        |           |             |
| **MultipleSubscribers**           | **Job-YFEFPZ** | **10**             | **Default**     | **20**              | **?**            | **10,252,175.7 ns** |  **28,645.91 ns** | **18,947.49 ns** |     **?** |       **?** |      **-** |      **-** |    **3656 B** |           **?** |
|                               |            |                |             |                 |              |                 |               |              |       |         |        |        |           |             |
| MultipleSubscribers           | ShortRun   | 3              | 1           | 20              | ?            | 10,234,912.1 ns | 121,362.63 ns |  6,652.30 ns |     ? |       ? |      - |      - |    3656 B |           ? |
|                               |            |                |             |                 |              |                 |               |              |       |         |        |        |           |             |
| **PublishBurst**                  | **Job-YFEFPZ** | **10**             | **Default**     | **?**               | **50**           |     **13,938.8 ns** |     **111.89 ns** |     **66.58 ns** |     **?** |       **?** | **0.1068** |      **-** |    **9216 B** |           **?** |
|                               |            |                |             |                 |              |                 |               |              |       |         |        |        |           |             |
| PublishBurst                  | ShortRun   | 3              | 1           | ?               | 50           |     14,074.7 ns |   2,955.48 ns |    162.00 ns |     ? |       ? | 0.1068 |      - |    9216 B |           ? |
|                               |            |                |             |                 |              |                 |               |              |       |         |        |        |           |             |
| **PublishBurst**                  | **Job-YFEFPZ** | **10**             | **Default**     | **?**               | **100**          |     **28,096.5 ns** |     **427.29 ns** |    **282.63 ns** |     **?** |       **?** | **0.2136** |      **-** |   **18416 B** |           **?** |
|                               |            |                |             |                 |              |                 |               |              |       |         |        |        |           |             |
| PublishBurst                  | ShortRun   | 3              | 1           | ?               | 100          |     28,236.6 ns |   1,609.71 ns |     88.23 ns |     ? |       ? | 0.2136 |      - |   18416 B |           ? |
