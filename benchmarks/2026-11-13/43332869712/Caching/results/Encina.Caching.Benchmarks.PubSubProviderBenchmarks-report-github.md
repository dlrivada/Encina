```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                        | Job        | IterationCount | LaunchCount | subscriberCount | messageCount | Mean            | Error           | StdDev       | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |----------- |--------------- |------------ |---------------- |------------- |----------------:|----------------:|-------------:|------:|--------:|-------:|-------:|----------:|------------:|
| **PublishAsync_NoSubscriber**     | **Job-YFEFPZ** | **10**             | **Default**     | **?**               | **?**            |        **911.7 ns** |         **5.04 ns** |      **2.64 ns** |  **3.93** |    **0.01** | **0.0095** |      **-** |     **168 B** |        **1.31** |
| SubscribeAndUnsubscribe       | Job-YFEFPZ | 10             | Default     | ?               | ?            |      1,867.0 ns |        23.68 ns |     15.66 ns |  8.05 |    0.07 | 0.0458 | 0.0439 |     776 B |        6.06 |
| PublishAsync_SingleSubscriber | Job-YFEFPZ | 10             | Default     | ?               | ?            |        232.0 ns |         1.01 ns |      0.60 ns |  1.00 |    0.00 | 0.0076 |      - |     128 B |        1.00 |
|                               |            |                |             |                 |              |                 |                 |              |       |         |        |        |           |             |
| PublishAsync_NoSubscriber     | ShortRun   | 3              | 1           | ?               | ?            |        910.1 ns |        58.85 ns |      3.23 ns |  3.92 |    0.01 | 0.0095 |      - |     168 B |        1.31 |
| SubscribeAndUnsubscribe       | ShortRun   | 3              | 1           | ?               | ?            |      1,802.1 ns |       525.64 ns |     28.81 ns |  7.76 |    0.11 | 0.0458 | 0.0439 |     776 B |        6.06 |
| PublishAsync_SingleSubscriber | ShortRun   | 3              | 1           | ?               | ?            |        232.2 ns |         9.29 ns |      0.51 ns |  1.00 |    0.00 | 0.0076 |      - |     128 B |        1.00 |
|                               |            |                |             |                 |              |                 |                 |              |       |         |        |        |           |             |
| **MultipleSubscribers**           | **Job-YFEFPZ** | **10**             | **Default**     | **5**               | **?**            | **10,177,434.9 ns** |    **39,577.68 ns** | **26,178.19 ns** |     **?** |       **?** |      **-** |      **-** |    **1856 B** |           **?** |
|                               |            |                |             |                 |              |                 |                 |              |       |         |        |        |           |             |
| MultipleSubscribers           | ShortRun   | 3              | 1           | 5               | ?            | 10,267,146.6 ns | 1,055,716.95 ns | 57,867.41 ns |     ? |       ? |      - |      - |    1856 B |           ? |
|                               |            |                |             |                 |              |                 |                 |              |       |         |        |        |           |             |
| **PublishBurst**                  | **Job-YFEFPZ** | **10**             | **Default**     | **?**               | **10**           |      **2,733.6 ns** |        **24.29 ns** |     **16.07 ns** |     **?** |       **?** | **0.1106** |      **-** |    **1856 B** |           **?** |
|                               |            |                |             |                 |              |                 |                 |              |       |         |        |        |           |             |
| PublishBurst                  | ShortRun   | 3              | 1           | ?               | 10           |      2,729.4 ns |       246.00 ns |     13.48 ns |     ? |       ? | 0.1106 |      - |    1856 B |           ? |
|                               |            |                |             |                 |              |                 |                 |              |       |         |        |        |           |             |
| **MultipleSubscribers**           | **Job-YFEFPZ** | **10**             | **Default**     | **10**              | **?**            | **10,258,432.1 ns** |    **38,445.26 ns** | **25,429.16 ns** |     **?** |       **?** |      **-** |      **-** |    **2456 B** |           **?** |
|                               |            |                |             |                 |              |                 |                 |              |       |         |        |        |           |             |
| MultipleSubscribers           | ShortRun   | 3              | 1           | 10              | ?            | 10,267,691.1 ns |   321,603.21 ns | 17,628.16 ns |     ? |       ? |      - |      - |    2456 B |           ? |
|                               |            |                |             |                 |              |                 |                 |              |       |         |        |        |           |             |
| **MultipleSubscribers**           | **Job-YFEFPZ** | **10**             | **Default**     | **20**              | **?**            | **10,271,347.4 ns** |    **44,820.33 ns** | **29,645.87 ns** |     **?** |       **?** |      **-** |      **-** |    **3656 B** |           **?** |
|                               |            |                |             |                 |              |                 |                 |              |       |         |        |        |           |             |
| MultipleSubscribers           | ShortRun   | 3              | 1           | 20              | ?            | 10,248,883.6 ns |   278,606.95 ns | 15,271.39 ns |     ? |       ? |      - |      - |    3656 B |           ? |
|                               |            |                |             |                 |              |                 |                 |              |       |         |        |        |           |             |
| **PublishBurst**                  | **Job-YFEFPZ** | **10**             | **Default**     | **?**               | **50**           |     **13,599.9 ns** |       **114.43 ns** |     **68.09 ns** |     **?** |       **?** | **0.5493** |      **-** |    **9216 B** |           **?** |
|                               |            |                |             |                 |              |                 |                 |              |       |         |        |        |           |             |
| PublishBurst                  | ShortRun   | 3              | 1           | ?               | 50           |     13,562.3 ns |     1,261.14 ns |     69.13 ns |     ? |       ? | 0.5493 |      - |    9216 B |           ? |
|                               |            |                |             |                 |              |                 |                 |              |       |         |        |        |           |             |
| **PublishBurst**                  | **Job-YFEFPZ** | **10**             | **Default**     | **?**               | **100**          |     **27,331.2 ns** |       **479.82 ns** |    **317.37 ns** |     **?** |       **?** | **1.0986** |      **-** |   **18416 B** |           **?** |
|                               |            |                |             |                 |              |                 |                 |              |       |         |        |        |           |             |
| PublishBurst                  | ShortRun   | 3              | 1           | ?               | 100          |     27,067.9 ns |     3,460.64 ns |    189.69 ns |     ? |       ? | 1.0986 |      - |   18416 B |           ? |
