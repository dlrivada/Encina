```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-NUBXJZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4


```
| Method                        | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error        | StdDev     | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |----------- |--------------- |------------ |------------ |------------:|-------------:|-----------:|------:|--------:|-------:|-------:|----------:|------------:|
| ExistsAsync_False             | Job-NUBXJZ | 20             | Default     | 5           |    44.58 ns |     0.789 ns |   0.909 ns |  0.59 |    0.01 |      - |      - |         - |        0.00 |
| ExistsAsync_True              | Job-NUBXJZ | 20             | Default     | 5           |    49.66 ns |     0.576 ns |   0.663 ns |  0.66 |    0.01 |      - |      - |         - |        0.00 |
| GetAsync_CacheHit             | Job-NUBXJZ | 20             | Default     | 5           |    75.64 ns |     0.459 ns |   0.511 ns |  1.00 |    0.01 | 0.0017 |      - |     144 B |        1.00 |
| GetAsync_CacheMiss            | Job-NUBXJZ | 20             | Default     | 5           |    45.17 ns |     0.298 ns |   0.331 ns |  0.60 |    0.01 |      - |      - |         - |        0.00 |
| GetOrSetAsync_CacheHit        | Job-NUBXJZ | 20             | Default     | 5           |   121.38 ns |     2.465 ns |   2.839 ns |  1.60 |    0.04 | 0.0033 |      - |     280 B |        1.94 |
| GetOrSetAsync_CacheMiss       | Job-NUBXJZ | 20             | Default     | 5           | 4,337.21 ns |   565.122 ns | 604.675 ns | 57.34 |    7.79 | 0.0114 | 0.0076 |    1080 B |        7.50 |
| RemoveAsync                   | Job-NUBXJZ | 20             | Default     | 5           | 2,433.96 ns |    32.851 ns |  37.831 ns | 32.18 |    0.53 | 0.0076 |      - |     784 B |        5.44 |
| SetWithSlidingExpirationAsync | Job-NUBXJZ | 20             | Default     | 5           | 3,544.27 ns |   407.918 ns | 453.399 ns | 46.86 |    5.85 | 0.0076 | 0.0038 |     817 B |        5.67 |
| SetAsync                      | Job-NUBXJZ | 20             | Default     | 5           | 3,375.13 ns |   595.234 ns | 636.894 ns | 44.62 |    8.20 | 0.0076 | 0.0038 |     712 B |        4.94 |
|                               |            |                |             |             |             |              |            |       |         |        |        |           |             |
| ExistsAsync_False             | ShortRun   | 3              | 1           | 3           |    43.70 ns |     9.142 ns |   0.501 ns |  0.58 |    0.01 |      - |      - |         - |        0.00 |
| ExistsAsync_True              | ShortRun   | 3              | 1           | 3           |    50.76 ns |     4.848 ns |   0.266 ns |  0.67 |    0.01 |      - |      - |         - |        0.00 |
| GetAsync_CacheHit             | ShortRun   | 3              | 1           | 3           |    75.59 ns |    11.640 ns |   0.638 ns |  1.00 |    0.01 | 0.0017 |      - |     144 B |        1.00 |
| GetAsync_CacheMiss            | ShortRun   | 3              | 1           | 3           |    45.84 ns |    11.472 ns |   0.629 ns |  0.61 |    0.01 |      - |      - |         - |        0.00 |
| GetOrSetAsync_CacheHit        | ShortRun   | 3              | 1           | 3           |   121.60 ns |    65.119 ns |   3.569 ns |  1.61 |    0.04 | 0.0033 |      - |     280 B |        1.94 |
| GetOrSetAsync_CacheMiss       | ShortRun   | 3              | 1           | 3           | 3,936.49 ns | 7,406.637 ns | 405.983 ns | 52.08 |    4.67 | 0.0114 | 0.0076 |    1080 B |        7.50 |
| RemoveAsync                   | ShortRun   | 3              | 1           | 3           | 2,502.02 ns |   474.941 ns |  26.033 ns | 33.10 |    0.38 | 0.0076 |      - |     784 B |        5.44 |
| SetWithSlidingExpirationAsync | ShortRun   | 3              | 1           | 3           | 2,979.71 ns | 8,139.979 ns | 446.180 ns | 39.42 |    5.12 | 0.0076 | 0.0038 |     720 B |        5.00 |
| SetAsync                      | ShortRun   | 3              | 1           | 3           | 2,979.36 ns | 5,034.241 ns | 275.944 ns | 39.42 |    3.17 | 0.0076 | 0.0038 |     712 B |        4.94 |
