```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-NUBXJZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4


```
| Method                        | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error        | StdDev     | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |----------- |--------------- |------------ |------------ |------------:|-------------:|-----------:|------:|--------:|-------:|-------:|----------:|------------:|
| ExistsAsync_False             | Job-NUBXJZ | 20             | Default     | 5           |    52.67 ns |     0.035 ns |   0.040 ns |  0.64 |    0.00 |      - |      - |         - |        0.00 |
| ExistsAsync_True              | Job-NUBXJZ | 20             | Default     | 5           |    55.13 ns |     0.063 ns |   0.070 ns |  0.67 |    0.00 |      - |      - |         - |        0.00 |
| GetAsync_CacheHit             | Job-NUBXJZ | 20             | Default     | 5           |    82.17 ns |     0.263 ns |   0.293 ns |  1.00 |    0.00 | 0.0057 |      - |     144 B |        1.00 |
| GetAsync_CacheMiss            | Job-NUBXJZ | 20             | Default     | 5           |    54.93 ns |     0.069 ns |   0.077 ns |  0.67 |    0.00 |      - |      - |         - |        0.00 |
| GetOrSetAsync_CacheHit        | Job-NUBXJZ | 20             | Default     | 5           |   129.14 ns |     0.356 ns |   0.381 ns |  1.57 |    0.01 | 0.0110 |      - |     280 B |        1.94 |
| GetOrSetAsync_CacheMiss       | Job-NUBXJZ | 20             | Default     | 5           | 4,999.55 ns |   452.355 ns | 484.015 ns | 60.84 |    5.74 | 0.0420 | 0.0381 |    1080 B |        7.50 |
| RemoveAsync                   | Job-NUBXJZ | 20             | Default     | 5           | 2,571.70 ns |    23.984 ns |  26.659 ns | 31.30 |    0.33 | 0.0305 |      - |     784 B |        5.44 |
| SetWithSlidingExpirationAsync | Job-NUBXJZ | 20             | Default     | 5           | 3,717.07 ns |   460.377 ns | 511.708 ns | 45.24 |    6.07 | 0.0343 | 0.0305 |     864 B |        6.00 |
| SetAsync                      | Job-NUBXJZ | 20             | Default     | 5           | 3,413.62 ns |   265.739 ns | 272.895 ns | 41.54 |    3.23 | 0.0267 | 0.0229 |     712 B |        4.94 |
|                               |            |                |             |             |             |              |            |       |         |        |        |           |             |
| ExistsAsync_False             | ShortRun   | 3              | 1           | 3           |    51.81 ns |     1.053 ns |   0.058 ns |  0.60 |    0.00 |      - |      - |         - |        0.00 |
| ExistsAsync_True              | ShortRun   | 3              | 1           | 3           |    60.07 ns |     0.229 ns |   0.013 ns |  0.69 |    0.00 |      - |      - |         - |        0.00 |
| GetAsync_CacheHit             | ShortRun   | 3              | 1           | 3           |    87.02 ns |     5.671 ns |   0.311 ns |  1.00 |    0.00 | 0.0057 |      - |     144 B |        1.00 |
| GetAsync_CacheMiss            | ShortRun   | 3              | 1           | 3           |    51.82 ns |     0.847 ns |   0.046 ns |  0.60 |    0.00 |      - |      - |         - |        0.00 |
| GetOrSetAsync_CacheHit        | ShortRun   | 3              | 1           | 3           |   130.26 ns |     4.350 ns |   0.238 ns |  1.50 |    0.01 | 0.0110 |      - |     280 B |        1.94 |
| GetOrSetAsync_CacheMiss       | ShortRun   | 3              | 1           | 3           | 4,221.94 ns | 6,318.520 ns | 346.339 ns | 48.52 |    3.45 | 0.0420 | 0.0381 |    1080 B |        7.50 |
| RemoveAsync                   | ShortRun   | 3              | 1           | 3           | 2,576.94 ns |   191.124 ns |  10.476 ns | 29.61 |    0.14 | 0.0305 |      - |     784 B |        5.44 |
| SetWithSlidingExpirationAsync | ShortRun   | 3              | 1           | 3           | 3,313.32 ns | 1,299.146 ns |  71.211 ns | 38.07 |    0.72 | 0.0267 | 0.0229 |     720 B |        5.00 |
| SetAsync                      | ShortRun   | 3              | 1           | 3           | 3,254.25 ns | 2,646.165 ns | 145.045 ns | 37.40 |    1.45 | 0.0267 | 0.0229 |     712 B |        4.94 |
