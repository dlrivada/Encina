```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.79GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                        | Job        | IterationCount | LaunchCount | subscriberCount | messageCount | Mean            | Error           | StdDev       | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |----------- |--------------- |------------ |---------------- |------------- |----------------:|----------------:|-------------:|------:|--------:|-------:|-------:|----------:|------------:|
| **PublishAsync_NoSubscriber**     | **Job-YFEFPZ** | **10**             | **Default**     | **?**               | **?**            |        **646.3 ns** |         **7.59 ns** |      **4.52 ns** |  **2.24** |    **0.02** | **0.0067** |      **-** |     **168 B** |        **1.31** |
| SubscribeAndUnsubscribe       | Job-YFEFPZ | 10             | Default     | ?               | ?            |      2,223.2 ns |        24.47 ns |     16.18 ns |  7.71 |    0.05 | 0.0305 | 0.0267 |     776 B |        6.06 |
| PublishAsync_SingleSubscriber | Job-YFEFPZ | 10             | Default     | ?               | ?            |        288.2 ns |         0.53 ns |      0.32 ns |  1.00 |    0.00 | 0.0048 |      - |     128 B |        1.00 |
|                               |            |                |             |                 |              |                 |                 |              |       |         |        |        |           |             |
| PublishAsync_NoSubscriber     | ShortRun   | 3              | 1           | ?               | ?            |        642.8 ns |        25.00 ns |      1.37 ns |  2.20 |    0.01 | 0.0067 |      - |     168 B |        1.31 |
| SubscribeAndUnsubscribe       | ShortRun   | 3              | 1           | ?               | ?            |      2,178.3 ns |       244.39 ns |     13.40 ns |  7.47 |    0.04 | 0.0305 | 0.0267 |     776 B |        6.06 |
| PublishAsync_SingleSubscriber | ShortRun   | 3              | 1           | ?               | ?            |        291.6 ns |        14.50 ns |      0.79 ns |  1.00 |    0.00 | 0.0048 |      - |     128 B |        1.00 |
|                               |            |                |             |                 |              |                 |                 |              |       |         |        |        |           |             |
| **MultipleSubscribers**           | **Job-YFEFPZ** | **10**             | **Default**     | **5**               | **?**            | **10,450,209.0 ns** |    **31,070.32 ns** | **16,250.38 ns** |     **?** |       **?** |      **-** |      **-** |    **1856 B** |           **?** |
|                               |            |                |             |                 |              |                 |                 |              |       |         |        |        |           |             |
| MultipleSubscribers           | ShortRun   | 3              | 1           | 5               | ?            | 10,444,287.0 ns |   731,327.77 ns | 40,086.54 ns |     ? |       ? |      - |      - |    1856 B |           ? |
|                               |            |                |             |                 |              |                 |                 |              |       |         |        |        |           |             |
| **PublishBurst**                  | **Job-YFEFPZ** | **10**             | **Default**     | **?**               | **10**           |      **3,570.9 ns** |         **6.17 ns** |      **3.67 ns** |     **?** |       **?** | **0.0725** |      **-** |    **1856 B** |           **?** |
|                               |            |                |             |                 |              |                 |                 |              |       |         |        |        |           |             |
| PublishBurst                  | ShortRun   | 3              | 1           | ?               | 10           |      3,536.6 ns |       110.07 ns |      6.03 ns |     ? |       ? | 0.0725 |      - |    1856 B |           ? |
|                               |            |                |             |                 |              |                 |                 |              |       |         |        |        |           |             |
| **MultipleSubscribers**           | **Job-YFEFPZ** | **10**             | **Default**     | **10**              | **?**            | **10,379,608.9 ns** |    **65,040.75 ns** | **43,020.43 ns** |     **?** |       **?** |      **-** |      **-** |    **2456 B** |           **?** |
|                               |            |                |             |                 |              |                 |                 |              |       |         |        |        |           |             |
| MultipleSubscribers           | ShortRun   | 3              | 1           | 10              | ?            | 10,388,007.3 ns |   904,875.81 ns | 49,599.30 ns |     ? |       ? |      - |      - |    2456 B |           ? |
|                               |            |                |             |                 |              |                 |                 |              |       |         |        |        |           |             |
| **MultipleSubscribers**           | **Job-YFEFPZ** | **10**             | **Default**     | **20**              | **?**            | **10,378,289.5 ns** |   **100,727.55 ns** | **66,625.04 ns** |     **?** |       **?** |      **-** |      **-** |    **3656 B** |           **?** |
|                               |            |                |             |                 |              |                 |                 |              |       |         |        |        |           |             |
| MultipleSubscribers           | ShortRun   | 3              | 1           | 20              | ?            | 10,354,068.8 ns | 1,189,609.52 ns | 65,206.51 ns |     ? |       ? |      - |      - |    3656 B |           ? |
|                               |            |                |             |                 |              |                 |                 |              |       |         |        |        |           |             |
| **PublishBurst**                  | **Job-YFEFPZ** | **10**             | **Default**     | **?**               | **50**           |     **17,493.8 ns** |        **41.26 ns** |     **24.55 ns** |     **?** |       **?** | **0.3662** |      **-** |    **9216 B** |           **?** |
|                               |            |                |             |                 |              |                 |                 |              |       |         |        |        |           |             |
| PublishBurst                  | ShortRun   | 3              | 1           | ?               | 50           |     17,642.8 ns |       803.14 ns |     44.02 ns |     ? |       ? | 0.3662 |      - |    9216 B |           ? |
|                               |            |                |             |                 |              |                 |                 |              |       |         |        |        |           |             |
| **PublishBurst**                  | **Job-YFEFPZ** | **10**             | **Default**     | **?**               | **100**          |     **34,793.9 ns** |        **41.24 ns** |     **24.54 ns** |     **?** |       **?** | **0.7324** |      **-** |   **18416 B** |           **?** |
|                               |            |                |             |                 |              |                 |                 |              |       |         |        |        |           |             |
| PublishBurst                  | ShortRun   | 3              | 1           | ?               | 100          |     35,248.0 ns |       989.35 ns |     54.23 ns |     ? |       ? | 0.7324 |      - |   18416 B |           ? |
