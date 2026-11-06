```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.79GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                        | Job        | IterationCount | LaunchCount | subscriberCount | messageCount | Mean            | Error           | StdDev       | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |----------- |--------------- |------------ |---------------- |------------- |----------------:|----------------:|-------------:|------:|--------:|-------:|-------:|----------:|------------:|
| **PublishAsync_NoSubscriber**     | **Job-YFEFPZ** | **10**             | **Default**     | **?**               | **?**            |        **666.2 ns** |         **1.65 ns** |      **1.09 ns** |  **2.29** |    **0.01** | **0.0067** |      **-** |     **168 B** |        **1.31** |
| SubscribeAndUnsubscribe       | Job-YFEFPZ | 10             | Default     | ?               | ?            |      2,379.5 ns |       206.82 ns |    136.80 ns |  8.18 |    0.45 | 0.0305 | 0.0267 |     776 B |        6.06 |
| PublishAsync_SingleSubscriber | Job-YFEFPZ | 10             | Default     | ?               | ?            |        290.8 ns |         1.00 ns |      0.66 ns |  1.00 |    0.00 | 0.0048 |      - |     128 B |        1.00 |
|                               |            |                |             |                 |              |                 |                 |              |       |         |        |        |           |             |
| PublishAsync_NoSubscriber     | ShortRun   | 3              | 1           | ?               | ?            |        664.8 ns |        24.93 ns |      1.37 ns |  2.26 |    0.03 | 0.0067 |      - |     168 B |        1.31 |
| SubscribeAndUnsubscribe       | ShortRun   | 3              | 1           | ?               | ?            |      2,247.9 ns |       876.89 ns |     48.07 ns |  7.64 |    0.17 | 0.0305 | 0.0267 |     776 B |        6.06 |
| PublishAsync_SingleSubscriber | ShortRun   | 3              | 1           | ?               | ?            |        294.2 ns |        83.70 ns |      4.59 ns |  1.00 |    0.02 | 0.0048 |      - |     128 B |        1.00 |
|                               |            |                |             |                 |              |                 |                 |              |       |         |        |        |           |             |
| **MultipleSubscribers**           | **Job-YFEFPZ** | **10**             | **Default**     | **5**               | **?**            | **10,283,942.3 ns** |    **45,249.47 ns** | **29,929.73 ns** |     **?** |       **?** |      **-** |      **-** |    **1856 B** |           **?** |
|                               |            |                |             |                 |              |                 |                 |              |       |         |        |        |           |             |
| MultipleSubscribers           | ShortRun   | 3              | 1           | 5               | ?            | 10,336,809.1 ns | 1,445,836.03 ns | 79,251.15 ns |     ? |       ? |      - |      - |    1856 B |           ? |
|                               |            |                |             |                 |              |                 |                 |              |       |         |        |        |           |             |
| **PublishBurst**                  | **Job-YFEFPZ** | **10**             | **Default**     | **?**               | **10**           |      **3,534.3 ns** |        **13.85 ns** |      **8.24 ns** |     **?** |       **?** | **0.0725** |      **-** |    **1856 B** |           **?** |
|                               |            |                |             |                 |              |                 |                 |              |       |         |        |        |           |             |
| PublishBurst                  | ShortRun   | 3              | 1           | ?               | 10           |      3,603.4 ns |       484.71 ns |     26.57 ns |     ? |       ? | 0.0725 |      - |    1856 B |           ? |
|                               |            |                |             |                 |              |                 |                 |              |       |         |        |        |           |             |
| **MultipleSubscribers**           | **Job-YFEFPZ** | **10**             | **Default**     | **10**              | **?**            | **10,287,472.6 ns** |    **43,267.78 ns** | **25,747.96 ns** |     **?** |       **?** |      **-** |      **-** |    **2456 B** |           **?** |
|                               |            |                |             |                 |              |                 |                 |              |       |         |        |        |           |             |
| MultipleSubscribers           | ShortRun   | 3              | 1           | 10              | ?            | 10,283,607.0 ns |   497,196.32 ns | 27,253.01 ns |     ? |       ? |      - |      - |    2456 B |           ? |
|                               |            |                |             |                 |              |                 |                 |              |       |         |        |        |           |             |
| **MultipleSubscribers**           | **Job-YFEFPZ** | **10**             | **Default**     | **20**              | **?**            | **10,222,659.7 ns** |    **26,180.02 ns** | **17,316.46 ns** |     **?** |       **?** |      **-** |      **-** |    **3656 B** |           **?** |
|                               |            |                |             |                 |              |                 |                 |              |       |         |        |        |           |             |
| MultipleSubscribers           | ShortRun   | 3              | 1           | 20              | ?            | 10,232,499.1 ns |   505,718.02 ns | 27,720.11 ns |     ? |       ? |      - |      - |    3656 B |           ? |
|                               |            |                |             |                 |              |                 |                 |              |       |         |        |        |           |             |
| **PublishBurst**                  | **Job-YFEFPZ** | **10**             | **Default**     | **?**               | **50**           |     **17,751.0 ns** |       **212.92 ns** |    **140.83 ns** |     **?** |       **?** | **0.3662** |      **-** |    **9216 B** |           **?** |
|                               |            |                |             |                 |              |                 |                 |              |       |         |        |        |           |             |
| PublishBurst                  | ShortRun   | 3              | 1           | ?               | 50           |     18,753.6 ns |    25,244.11 ns |  1,383.72 ns |     ? |       ? | 0.3662 |      - |    9216 B |           ? |
|                               |            |                |             |                 |              |                 |                 |              |       |         |        |        |           |             |
| **PublishBurst**                  | **Job-YFEFPZ** | **10**             | **Default**     | **?**               | **100**          |     **35,028.9 ns** |       **173.89 ns** |    **115.02 ns** |     **?** |       **?** | **0.7324** |      **-** |   **18416 B** |           **?** |
|                               |            |                |             |                 |              |                 |                 |              |       |         |        |        |           |             |
| PublishBurst                  | ShortRun   | 3              | 1           | ?               | 100          |     35,733.6 ns |     1,942.66 ns |    106.48 ns |     ? |       ? | 0.7324 |      - |   18416 B |           ? |
