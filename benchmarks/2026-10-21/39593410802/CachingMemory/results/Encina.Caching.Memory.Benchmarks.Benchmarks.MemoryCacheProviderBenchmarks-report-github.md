```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 3.41GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  Job-NUBXJZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4


```
| Method                        | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error        | StdDev     | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |----------- |--------------- |------------ |------------ |------------:|-------------:|-----------:|------:|--------:|-------:|-------:|----------:|------------:|
| ExistsAsync_False             | Job-NUBXJZ | 20             | Default     | 5           |    43.63 ns |     0.165 ns |   0.183 ns |  0.28 |    0.01 |      - |      - |         - |        0.00 |
| ExistsAsync_True              | Job-NUBXJZ | 20             | Default     | 5           |    48.76 ns |     0.324 ns |   0.374 ns |  0.31 |    0.01 |      - |      - |         - |        0.00 |
| GetAsync_CacheHit             | Job-NUBXJZ | 20             | Default     | 5           |   157.79 ns |     2.604 ns |   2.999 ns |  1.00 |    0.03 | 0.0017 |      - |     144 B |        1.00 |
| GetAsync_CacheMiss            | Job-NUBXJZ | 20             | Default     | 5           |   107.93 ns |     0.893 ns |   1.028 ns |  0.68 |    0.01 |      - |      - |         - |        0.00 |
| GetOrSetAsync_CacheHit        | Job-NUBXJZ | 20             | Default     | 5           |   198.31 ns |     2.389 ns |   2.656 ns |  1.26 |    0.03 | 0.0033 |      - |     280 B |        1.94 |
| GetOrSetAsync_CacheMiss       | Job-NUBXJZ | 20             | Default     | 5           | 4,058.90 ns |   639.798 ns | 684.577 ns | 25.73 |    4.25 | 0.0114 | 0.0076 |    1080 B |        7.50 |
| RemoveAsync                   | Job-NUBXJZ | 20             | Default     | 5           | 2,612.55 ns |    21.614 ns |  24.891 ns | 16.56 |    0.35 | 0.0076 |      - |     784 B |        5.44 |
| SetWithSlidingExpirationAsync | Job-NUBXJZ | 20             | Default     | 5           | 3,133.82 ns |   404.522 ns | 449.625 ns | 19.87 |    2.80 | 0.0076 | 0.0038 |     852 B |        5.92 |
| SetAsync                      | Job-NUBXJZ | 20             | Default     | 5           | 3,200.58 ns |   467.378 ns | 500.089 ns | 20.29 |    3.11 | 0.0076 | 0.0038 |     712 B |        4.94 |
|                               |            |                |             |             |             |              |            |       |         |        |        |           |             |
| ExistsAsync_False             | ShortRun   | 3              | 1           | 3           |    44.10 ns |    12.375 ns |   0.678 ns |  0.27 |    0.01 |      - |      - |         - |        0.00 |
| ExistsAsync_True              | ShortRun   | 3              | 1           | 3           |    49.31 ns |     4.255 ns |   0.233 ns |  0.30 |    0.00 |      - |      - |         - |        0.00 |
| GetAsync_CacheHit             | ShortRun   | 3              | 1           | 3           |   161.90 ns |    52.245 ns |   2.864 ns |  1.00 |    0.02 | 0.0017 |      - |     144 B |        1.00 |
| GetAsync_CacheMiss            | ShortRun   | 3              | 1           | 3           |   107.48 ns |    18.767 ns |   1.029 ns |  0.66 |    0.01 |      - |      - |         - |        0.00 |
| GetOrSetAsync_CacheHit        | ShortRun   | 3              | 1           | 3           |   207.36 ns |    14.236 ns |   0.780 ns |  1.28 |    0.02 | 0.0033 |      - |     280 B |        1.94 |
| GetOrSetAsync_CacheMiss       | ShortRun   | 3              | 1           | 3           | 3,398.95 ns | 4,105.828 ns | 225.054 ns | 21.00 |    1.25 | 0.0114 | 0.0076 |    1080 B |        7.50 |
| RemoveAsync                   | ShortRun   | 3              | 1           | 3           | 2,541.44 ns | 1,960.762 ns | 107.476 ns | 15.70 |    0.62 | 0.0076 |      - |     784 B |        5.44 |
| SetWithSlidingExpirationAsync | ShortRun   | 3              | 1           | 3           | 2,850.76 ns | 1,332.328 ns |  73.029 ns | 17.61 |    0.48 | 0.0076 | 0.0038 |     720 B |        5.00 |
| SetAsync                      | ShortRun   | 3              | 1           | 3           | 2,741.74 ns | 9,364.256 ns | 513.286 ns | 16.94 |    2.76 | 0.0076 | 0.0038 |     712 B |        4.94 |
