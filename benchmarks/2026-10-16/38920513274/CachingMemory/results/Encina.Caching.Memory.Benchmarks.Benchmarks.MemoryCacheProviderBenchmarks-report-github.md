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
| ExistsAsync_False             | Job-NUBXJZ | 20             | Default     | 5           |    37.38 ns |     0.312 ns |   0.346 ns |  0.57 |    0.03 |      - |      - |         - |        0.00 |
| ExistsAsync_True              | Job-NUBXJZ | 20             | Default     | 5           |    41.31 ns |     0.717 ns |   0.797 ns |  0.63 |    0.03 |      - |      - |         - |        0.00 |
| GetAsync_CacheHit             | Job-NUBXJZ | 20             | Default     | 5           |    66.17 ns |     2.734 ns |   3.148 ns |  1.00 |    0.07 | 0.0017 |      - |     144 B |        1.00 |
| GetAsync_CacheMiss            | Job-NUBXJZ | 20             | Default     | 5           |    39.33 ns |     1.467 ns |   1.631 ns |  0.60 |    0.04 |      - |      - |         - |        0.00 |
| GetOrSetAsync_CacheHit        | Job-NUBXJZ | 20             | Default     | 5           |   111.30 ns |     2.185 ns |   2.429 ns |  1.69 |    0.09 | 0.0033 |      - |     280 B |        1.94 |
| GetOrSetAsync_CacheMiss       | Job-NUBXJZ | 20             | Default     | 5           | 4,235.10 ns |   599.906 ns | 641.893 ns | 64.14 |    9.92 | 0.0114 | 0.0076 |    1102 B |        7.65 |
| RemoveAsync                   | Job-NUBXJZ | 20             | Default     | 5           | 1,945.90 ns |    22.583 ns |  23.191 ns | 29.47 |    1.40 | 0.0076 |      - |     784 B |        5.44 |
| SetWithSlidingExpirationAsync | Job-NUBXJZ | 20             | Default     | 5           | 3,316.90 ns |   413.338 ns | 459.424 ns | 50.23 |    7.17 | 0.0076 | 0.0038 |     805 B |        5.59 |
| SetAsync                      | Job-NUBXJZ | 20             | Default     | 5           | 3,185.14 ns |   468.971 ns | 501.794 ns | 48.24 |    7.73 | 0.0076 | 0.0038 |     712 B |        4.94 |
|                               |            |                |             |             |             |              |            |       |         |        |        |           |             |
| ExistsAsync_False             | ShortRun   | 3              | 1           | 3           |    37.92 ns |    20.152 ns |   1.105 ns |  0.55 |    0.01 |      - |      - |         - |        0.00 |
| ExistsAsync_True              | ShortRun   | 3              | 1           | 3           |    42.03 ns |    13.445 ns |   0.737 ns |  0.62 |    0.01 |      - |      - |         - |        0.00 |
| GetAsync_CacheHit             | ShortRun   | 3              | 1           | 3           |    68.33 ns |     6.750 ns |   0.370 ns |  1.00 |    0.01 | 0.0017 |      - |     144 B |        1.00 |
| GetAsync_CacheMiss            | ShortRun   | 3              | 1           | 3           |    38.42 ns |    25.889 ns |   1.419 ns |  0.56 |    0.02 |      - |      - |         - |        0.00 |
| GetOrSetAsync_CacheHit        | ShortRun   | 3              | 1           | 3           |   108.32 ns |    33.480 ns |   1.835 ns |  1.59 |    0.02 | 0.0033 |      - |     280 B |        1.94 |
| GetOrSetAsync_CacheMiss       | ShortRun   | 3              | 1           | 3           | 3,782.08 ns | 6,223.248 ns | 341.117 ns | 55.35 |    4.33 | 0.0114 | 0.0076 |    1080 B |        7.50 |
| RemoveAsync                   | ShortRun   | 3              | 1           | 3           | 1,884.75 ns |   304.170 ns |  16.673 ns | 27.58 |    0.25 | 0.0076 |      - |     784 B |        5.44 |
| SetWithSlidingExpirationAsync | ShortRun   | 3              | 1           | 3           | 2,545.74 ns | 3,171.833 ns | 173.859 ns | 37.25 |    2.21 | 0.0076 | 0.0038 |     720 B |        5.00 |
| SetAsync                      | ShortRun   | 3              | 1           | 3           | 2,980.17 ns | 7,286.732 ns | 399.410 ns | 43.61 |    5.07 | 0.0076 | 0.0038 |     712 B |        4.94 |
