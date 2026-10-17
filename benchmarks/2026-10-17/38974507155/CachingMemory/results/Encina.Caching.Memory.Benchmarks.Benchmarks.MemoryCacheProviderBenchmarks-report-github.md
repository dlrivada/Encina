```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  Job-NUBXJZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4


```
| Method                        | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error        | StdDev     | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |----------- |--------------- |------------ |------------ |------------:|-------------:|-----------:|------:|--------:|-------:|-------:|----------:|------------:|
| ExistsAsync_False             | Job-NUBXJZ | 20             | Default     | 5           |    41.63 ns |     0.391 ns |   0.450 ns |  0.30 |    0.01 |      - |      - |         - |        0.00 |
| ExistsAsync_True              | Job-NUBXJZ | 20             | Default     | 5           |    45.45 ns |     0.618 ns |   0.712 ns |  0.33 |    0.01 |      - |      - |         - |        0.00 |
| GetAsync_CacheHit             | Job-NUBXJZ | 20             | Default     | 5           |   139.83 ns |     2.444 ns |   2.814 ns |  1.00 |    0.03 | 0.0017 |      - |     144 B |        1.00 |
| GetAsync_CacheMiss            | Job-NUBXJZ | 20             | Default     | 5           |    96.65 ns |     0.835 ns |   0.962 ns |  0.69 |    0.02 |      - |      - |         - |        0.00 |
| GetOrSetAsync_CacheHit        | Job-NUBXJZ | 20             | Default     | 5           |   181.09 ns |     2.260 ns |   2.603 ns |  1.30 |    0.03 | 0.0033 |      - |     280 B |        1.94 |
| GetOrSetAsync_CacheMiss       | Job-NUBXJZ | 20             | Default     | 5           | 3,819.64 ns |   442.086 ns | 473.027 ns | 27.33 |    3.34 | 0.0114 | 0.0076 |    1127 B |        7.83 |
| RemoveAsync                   | Job-NUBXJZ | 20             | Default     | 5           | 2,222.76 ns |    22.499 ns |  25.910 ns | 15.90 |    0.37 | 0.0076 |      - |     784 B |        5.44 |
| SetWithSlidingExpirationAsync | Job-NUBXJZ | 20             | Default     | 5           | 2,897.03 ns |   350.357 ns | 389.421 ns | 20.73 |    2.75 | 0.0076 | 0.0038 |     822 B |        5.71 |
| SetAsync                      | Job-NUBXJZ | 20             | Default     | 5           | 3,020.97 ns |   410.920 ns | 439.680 ns | 21.61 |    3.09 | 0.0076 | 0.0038 |     712 B |        4.94 |
|                               |            |                |             |             |             |              |            |       |         |        |        |           |             |
| ExistsAsync_False             | ShortRun   | 3              | 1           | 3           |    41.53 ns |     1.408 ns |   0.077 ns |  0.29 |    0.00 |      - |      - |         - |        0.00 |
| ExistsAsync_True              | ShortRun   | 3              | 1           | 3           |    45.10 ns |    11.134 ns |   0.610 ns |  0.32 |    0.00 |      - |      - |         - |        0.00 |
| GetAsync_CacheHit             | ShortRun   | 3              | 1           | 3           |   141.06 ns |    10.579 ns |   0.580 ns |  1.00 |    0.01 | 0.0017 |      - |     144 B |        1.00 |
| GetAsync_CacheMiss            | ShortRun   | 3              | 1           | 3           |    97.76 ns |    13.973 ns |   0.766 ns |  0.69 |    0.01 |      - |      - |         - |        0.00 |
| GetOrSetAsync_CacheHit        | ShortRun   | 3              | 1           | 3           |   182.38 ns |   132.931 ns |   7.286 ns |  1.29 |    0.04 | 0.0033 |      - |     280 B |        1.94 |
| GetOrSetAsync_CacheMiss       | ShortRun   | 3              | 1           | 3           | 3,442.12 ns | 4,635.530 ns | 254.089 ns | 24.40 |    1.56 | 0.0114 | 0.0076 |    1080 B |        7.50 |
| RemoveAsync                   | ShortRun   | 3              | 1           | 3           | 2,221.98 ns |   230.761 ns |  12.649 ns | 15.75 |    0.10 | 0.0076 |      - |     784 B |        5.44 |
| SetWithSlidingExpirationAsync | ShortRun   | 3              | 1           | 3           | 2,678.71 ns | 3,519.481 ns | 192.915 ns | 18.99 |    1.19 | 0.0076 | 0.0038 |     720 B |        5.00 |
| SetAsync                      | ShortRun   | 3              | 1           | 3           | 2,332.11 ns | 2,645.801 ns | 145.025 ns | 16.53 |    0.89 | 0.0076 | 0.0038 |     712 B |        4.94 |
