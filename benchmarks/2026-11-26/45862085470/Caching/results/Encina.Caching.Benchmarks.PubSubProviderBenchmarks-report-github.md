```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 2.89GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                        | Job        | IterationCount | LaunchCount | subscriberCount | messageCount | Mean            | Error         | StdDev       | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |----------- |--------------- |------------ |---------------- |------------- |----------------:|--------------:|-------------:|------:|--------:|-------:|-------:|----------:|------------:|
| **PublishAsync_NoSubscriber**     | **Job-YFEFPZ** | **10**             | **Default**     | **?**               | **?**            |        **665.2 ns** |       **3.17 ns** |      **2.10 ns** |  **2.12** |    **0.01** | **0.0019** |      **-** |     **168 B** |        **1.31** |
| SubscribeAndUnsubscribe       | Job-YFEFPZ | 10             | Default     | ?               | ?            |      2,075.2 ns |      34.76 ns |     20.69 ns |  6.60 |    0.07 | 0.0076 | 0.0038 |     776 B |        6.06 |
| PublishAsync_SingleSubscriber | Job-YFEFPZ | 10             | Default     | ?               | ?            |        314.5 ns |       2.36 ns |      1.56 ns |  1.00 |    0.01 | 0.0014 |      - |     128 B |        1.00 |
|                               |            |                |             |                 |              |                 |               |              |       |         |        |        |           |             |
| PublishAsync_NoSubscriber     | ShortRun   | 3              | 1           | ?               | ?            |        660.6 ns |      75.23 ns |      4.12 ns |  2.09 |    0.01 | 0.0019 |      - |     168 B |        1.31 |
| SubscribeAndUnsubscribe       | ShortRun   | 3              | 1           | ?               | ?            |      2,053.1 ns |     442.87 ns |     24.28 ns |  6.50 |    0.07 | 0.0076 | 0.0038 |     776 B |        6.06 |
| PublishAsync_SingleSubscriber | ShortRun   | 3              | 1           | ?               | ?            |        316.0 ns |      17.17 ns |      0.94 ns |  1.00 |    0.00 | 0.0014 |      - |     128 B |        1.00 |
|                               |            |                |             |                 |              |                 |               |              |       |         |        |        |           |             |
| **MultipleSubscribers**           | **Job-YFEFPZ** | **10**             | **Default**     | **5**               | **?**            | **10,227,474.8 ns** |  **18,173.65 ns** | **12,020.75 ns** |     **?** |       **?** |      **-** |      **-** |    **1856 B** |           **?** |
|                               |            |                |             |                 |              |                 |               |              |       |         |        |        |           |             |
| MultipleSubscribers           | ShortRun   | 3              | 1           | 5               | ?            | 10,234,075.2 ns | 348,119.63 ns | 19,081.61 ns |     ? |       ? |      - |      - |    1873 B |           ? |
|                               |            |                |             |                 |              |                 |               |              |       |         |        |        |           |             |
| **PublishBurst**                  | **Job-YFEFPZ** | **10**             | **Default**     | **?**               | **10**           |      **3,753.3 ns** |      **33.14 ns** |     **21.92 ns** |     **?** |       **?** | **0.0191** |      **-** |    **1856 B** |           **?** |
|                               |            |                |             |                 |              |                 |               |              |       |         |        |        |           |             |
| PublishBurst                  | ShortRun   | 3              | 1           | ?               | 10           |      3,720.7 ns |     417.48 ns |     22.88 ns |     ? |       ? | 0.0191 |      - |    1856 B |           ? |
|                               |            |                |             |                 |              |                 |               |              |       |         |        |        |           |             |
| **MultipleSubscribers**           | **Job-YFEFPZ** | **10**             | **Default**     | **10**              | **?**            | **10,174,583.1 ns** |  **33,046.27 ns** | **21,858.06 ns** |     **?** |       **?** |      **-** |      **-** |    **2456 B** |           **?** |
|                               |            |                |             |                 |              |                 |               |              |       |         |        |        |           |             |
| MultipleSubscribers           | ShortRun   | 3              | 1           | 10              | ?            | 10,181,845.1 ns | 223,988.08 ns | 12,277.54 ns |     ? |       ? |      - |      - |    2456 B |           ? |
|                               |            |                |             |                 |              |                 |               |              |       |         |        |        |           |             |
| **MultipleSubscribers**           | **Job-YFEFPZ** | **10**             | **Default**     | **20**              | **?**            | **10,188,590.9 ns** |  **25,234.92 ns** | **16,691.34 ns** |     **?** |       **?** |      **-** |      **-** |    **3656 B** |           **?** |
|                               |            |                |             |                 |              |                 |               |              |       |         |        |        |           |             |
| MultipleSubscribers           | ShortRun   | 3              | 1           | 20              | ?            | 10,225,904.0 ns | 531,409.85 ns | 29,128.37 ns |     ? |       ? |      - |      - |    3656 B |           ? |
|                               |            |                |             |                 |              |                 |               |              |       |         |        |        |           |             |
| **PublishBurst**                  | **Job-YFEFPZ** | **10**             | **Default**     | **?**               | **50**           |     **18,362.4 ns** |     **126.30 ns** |     **83.54 ns** |     **?** |       **?** | **0.0916** |      **-** |    **9216 B** |           **?** |
|                               |            |                |             |                 |              |                 |               |              |       |         |        |        |           |             |
| PublishBurst                  | ShortRun   | 3              | 1           | ?               | 50           |     18,733.5 ns |   3,230.64 ns |    177.08 ns |     ? |       ? | 0.0916 |      - |    9216 B |           ? |
|                               |            |                |             |                 |              |                 |               |              |       |         |        |        |           |             |
| **PublishBurst**                  | **Job-YFEFPZ** | **10**             | **Default**     | **?**               | **100**          |     **37,263.7 ns** |     **322.25 ns** |    **213.15 ns** |     **?** |       **?** | **0.1831** |      **-** |   **18416 B** |           **?** |
|                               |            |                |             |                 |              |                 |               |              |       |         |        |        |           |             |
| PublishBurst                  | ShortRun   | 3              | 1           | ?               | 100          |     36,827.9 ns |   2,743.88 ns |    150.40 ns |     ? |       ? | 0.1831 |      - |   18416 B |           ? |
