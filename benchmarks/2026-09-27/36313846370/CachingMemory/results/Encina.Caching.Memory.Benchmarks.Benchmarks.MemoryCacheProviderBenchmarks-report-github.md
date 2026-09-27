```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-NUBXJZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4


```
| Method                        | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error         | StdDev     | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |----------- |--------------- |------------ |------------ |------------:|--------------:|-----------:|------:|--------:|-------:|-------:|----------:|------------:|
| ExistsAsync_False             | Job-NUBXJZ | 20             | Default     | 5           |    41.42 ns |      1.405 ns |   1.619 ns |  0.67 |    0.03 |      - |      - |         - |        0.00 |
| ExistsAsync_True              | Job-NUBXJZ | 20             | Default     | 5           |    50.74 ns |      1.250 ns |   1.338 ns |  0.82 |    0.03 |      - |      - |         - |        0.00 |
| GetAsync_CacheHit             | Job-NUBXJZ | 20             | Default     | 5           |    61.65 ns |      1.144 ns |   1.272 ns |  1.00 |    0.03 | 0.0017 |      - |     144 B |        1.00 |
| GetAsync_CacheMiss            | Job-NUBXJZ | 20             | Default     | 5           |    43.09 ns |      0.797 ns |   0.886 ns |  0.70 |    0.02 |      - |      - |         - |        0.00 |
| GetOrSetAsync_CacheHit        | Job-NUBXJZ | 20             | Default     | 5           |    92.40 ns |      3.925 ns |   4.520 ns |  1.50 |    0.08 | 0.0033 |      - |     280 B |        1.94 |
| GetOrSetAsync_CacheMiss       | Job-NUBXJZ | 20             | Default     | 5           | 3,871.58 ns |    565.001 ns | 604.545 ns | 62.82 |    9.63 | 0.0114 | 0.0076 |    1102 B |        7.65 |
| RemoveAsync                   | Job-NUBXJZ | 20             | Default     | 5           | 2,030.82 ns |     50.558 ns |  58.223 ns | 32.95 |    1.13 | 0.0076 |      - |     784 B |        5.44 |
| SetWithSlidingExpirationAsync | Job-NUBXJZ | 20             | Default     | 5           | 3,108.36 ns |    316.369 ns | 351.643 ns | 50.44 |    5.65 | 0.0076 | 0.0038 |     807 B |        5.60 |
| SetAsync                      | Job-NUBXJZ | 20             | Default     | 5           | 3,047.01 ns |    465.987 ns | 498.601 ns | 49.44 |    7.94 | 0.0076 | 0.0038 |     712 B |        4.94 |
|                               |            |                |             |             |             |               |            |       |         |        |        |           |             |
| ExistsAsync_False             | ShortRun   | 3              | 1           | 3           |    40.05 ns |      9.172 ns |   0.503 ns |  0.62 |    0.01 |      - |      - |         - |        0.00 |
| ExistsAsync_True              | ShortRun   | 3              | 1           | 3           |    43.52 ns |     21.994 ns |   1.206 ns |  0.68 |    0.02 |      - |      - |         - |        0.00 |
| GetAsync_CacheHit             | ShortRun   | 3              | 1           | 3           |    64.15 ns |      6.212 ns |   0.341 ns |  1.00 |    0.01 | 0.0017 |      - |     144 B |        1.00 |
| GetAsync_CacheMiss            | ShortRun   | 3              | 1           | 3           |    40.58 ns |     14.012 ns |   0.768 ns |  0.63 |    0.01 |      - |      - |         - |        0.00 |
| GetOrSetAsync_CacheHit        | ShortRun   | 3              | 1           | 3           |    95.04 ns |     39.727 ns |   2.178 ns |  1.48 |    0.03 | 0.0033 |      - |     280 B |        1.94 |
| GetOrSetAsync_CacheMiss       | ShortRun   | 3              | 1           | 3           | 3,395.73 ns |  4,997.644 ns | 273.938 ns | 52.94 |    3.71 | 0.0114 | 0.0076 |    1080 B |        7.50 |
| RemoveAsync                   | ShortRun   | 3              | 1           | 3           | 2,023.38 ns |    194.135 ns |  10.641 ns | 31.54 |    0.20 | 0.0076 |      - |     784 B |        5.44 |
| SetWithSlidingExpirationAsync | ShortRun   | 3              | 1           | 3           | 2,940.93 ns | 11,699.066 ns | 641.265 ns | 45.85 |    8.66 | 0.0076 | 0.0038 |     720 B |        5.00 |
| SetAsync                      | ShortRun   | 3              | 1           | 3           | 2,586.06 ns |  4,619.934 ns | 253.234 ns | 40.32 |    3.42 | 0.0076 | 0.0038 |     712 B |        4.94 |
