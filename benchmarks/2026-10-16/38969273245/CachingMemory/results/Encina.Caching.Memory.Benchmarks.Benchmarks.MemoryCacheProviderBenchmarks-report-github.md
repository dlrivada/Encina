```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  Job-NUBXJZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4


```
| Method                        | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error        | StdDev     | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |----------- |--------------- |------------ |------------ |------------:|-------------:|-----------:|------:|--------:|-------:|-------:|----------:|------------:|
| ExistsAsync_False             | Job-NUBXJZ | 20             | Default     | 5           |    48.32 ns |     0.570 ns |   0.656 ns |  0.27 |    0.01 |      - |      - |         - |        0.00 |
| ExistsAsync_True              | Job-NUBXJZ | 20             | Default     | 5           |    53.08 ns |     0.578 ns |   0.666 ns |  0.30 |    0.01 |      - |      - |         - |        0.00 |
| GetAsync_CacheHit             | Job-NUBXJZ | 20             | Default     | 5           |   179.10 ns |     2.435 ns |   2.805 ns |  1.00 |    0.02 | 0.0017 |      - |     144 B |        1.00 |
| GetAsync_CacheMiss            | Job-NUBXJZ | 20             | Default     | 5           |   112.91 ns |     2.297 ns |   2.646 ns |  0.63 |    0.02 |      - |      - |         - |        0.00 |
| GetOrSetAsync_CacheHit        | Job-NUBXJZ | 20             | Default     | 5           |   215.39 ns |     4.180 ns |   4.814 ns |  1.20 |    0.03 | 0.0033 |      - |     280 B |        1.94 |
| GetOrSetAsync_CacheMiss       | Job-NUBXJZ | 20             | Default     | 5           | 4,240.09 ns |   385.180 ns | 412.139 ns | 23.68 |    2.27 | 0.0114 | 0.0076 |    1080 B |        7.50 |
| RemoveAsync                   | Job-NUBXJZ | 20             | Default     | 5           | 2,747.85 ns |    47.008 ns |  54.135 ns | 15.35 |    0.38 | 0.0076 |      - |     784 B |        5.44 |
| SetWithSlidingExpirationAsync | Job-NUBXJZ | 20             | Default     | 5           | 3,182.00 ns |   453.096 ns | 503.615 ns | 17.77 |    2.76 | 0.0076 | 0.0038 |     867 B |        6.02 |
| SetAsync                      | Job-NUBXJZ | 20             | Default     | 5           | 3,302.42 ns |   521.786 ns | 558.306 ns | 18.44 |    3.05 | 0.0076 | 0.0038 |     712 B |        4.94 |
|                               |            |                |             |             |             |              |            |       |         |        |        |           |             |
| ExistsAsync_False             | ShortRun   | 3              | 1           | 3           |    48.02 ns |     6.754 ns |   0.370 ns |  0.28 |    0.00 |      - |      - |         - |        0.00 |
| ExistsAsync_True              | ShortRun   | 3              | 1           | 3           |    52.62 ns |    17.623 ns |   0.966 ns |  0.31 |    0.01 |      - |      - |         - |        0.00 |
| GetAsync_CacheHit             | ShortRun   | 3              | 1           | 3           |   171.15 ns |    43.592 ns |   2.389 ns |  1.00 |    0.02 | 0.0017 |      - |     144 B |        1.00 |
| GetAsync_CacheMiss            | ShortRun   | 3              | 1           | 3           |   109.17 ns |    20.878 ns |   1.144 ns |  0.64 |    0.01 |      - |      - |         - |        0.00 |
| GetOrSetAsync_CacheHit        | ShortRun   | 3              | 1           | 3           |   226.04 ns |    94.917 ns |   5.203 ns |  1.32 |    0.03 | 0.0033 |      - |     280 B |        1.94 |
| GetOrSetAsync_CacheMiss       | ShortRun   | 3              | 1           | 3           | 3,692.35 ns | 9,810.901 ns | 537.769 ns | 21.58 |    2.73 | 0.0114 | 0.0076 |    1080 B |        7.50 |
| RemoveAsync                   | ShortRun   | 3              | 1           | 3           | 2,837.30 ns |   470.869 ns |  25.810 ns | 16.58 |    0.24 | 0.0076 |      - |     784 B |        5.44 |
| SetWithSlidingExpirationAsync | ShortRun   | 3              | 1           | 3           | 2,989.74 ns | 6,747.409 ns | 369.848 ns | 17.47 |    1.88 | 0.0076 | 0.0038 |     720 B |        5.00 |
| SetAsync                      | ShortRun   | 3              | 1           | 3           | 2,965.21 ns | 1,677.778 ns |  91.965 ns | 17.33 |    0.51 | 0.0076 | 0.0038 |     712 B |        4.94 |
