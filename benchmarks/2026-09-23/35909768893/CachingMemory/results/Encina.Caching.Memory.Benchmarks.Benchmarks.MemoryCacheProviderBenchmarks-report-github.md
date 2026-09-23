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
| ExistsAsync_False             | Job-NUBXJZ | 20             | Default     | 5           |    54.06 ns |     0.031 ns |   0.032 ns |  0.66 |    0.01 |      - |      - |         - |        0.00 |
| ExistsAsync_True              | Job-NUBXJZ | 20             | Default     | 5           |    57.77 ns |     0.089 ns |   0.103 ns |  0.71 |    0.01 |      - |      - |         - |        0.00 |
| GetAsync_CacheHit             | Job-NUBXJZ | 20             | Default     | 5           |    81.56 ns |     0.628 ns |   0.723 ns |  1.00 |    0.01 | 0.0057 |      - |     144 B |        1.00 |
| GetAsync_CacheMiss            | Job-NUBXJZ | 20             | Default     | 5           |    56.41 ns |     0.027 ns |   0.029 ns |  0.69 |    0.01 |      - |      - |         - |        0.00 |
| GetOrSetAsync_CacheHit        | Job-NUBXJZ | 20             | Default     | 5           |   122.31 ns |     1.260 ns |   1.451 ns |  1.50 |    0.02 | 0.0110 |      - |     280 B |        1.94 |
| GetOrSetAsync_CacheMiss       | Job-NUBXJZ | 20             | Default     | 5           | 4,869.04 ns |   326.329 ns | 335.116 ns | 59.70 |    4.03 | 0.0420 | 0.0381 |    1080 B |        7.50 |
| RemoveAsync                   | Job-NUBXJZ | 20             | Default     | 5           | 2,669.91 ns |    23.067 ns |  25.639 ns | 32.74 |    0.42 | 0.0305 |      - |     784 B |        5.44 |
| SetWithSlidingExpirationAsync | Job-NUBXJZ | 20             | Default     | 5           | 3,905.61 ns |   585.624 ns | 650.919 ns | 47.89 |    7.79 | 0.0267 | 0.0229 |     729 B |        5.06 |
| SetAsync                      | Job-NUBXJZ | 20             | Default     | 5           | 3,541.79 ns |   511.672 ns | 547.483 ns | 43.43 |    6.54 | 0.0267 | 0.0229 |     712 B |        4.94 |
|                               |            |                |             |             |             |              |            |       |         |        |        |           |             |
| ExistsAsync_False             | ShortRun   | 3              | 1           | 3           |    51.21 ns |     0.979 ns |   0.054 ns |  0.61 |    0.00 |      - |      - |         - |        0.00 |
| ExistsAsync_True              | ShortRun   | 3              | 1           | 3           |    59.87 ns |     0.592 ns |   0.032 ns |  0.71 |    0.00 |      - |      - |         - |        0.00 |
| GetAsync_CacheHit             | ShortRun   | 3              | 1           | 3           |    84.41 ns |     7.682 ns |   0.421 ns |  1.00 |    0.01 | 0.0057 |      - |     144 B |        1.00 |
| GetAsync_CacheMiss            | ShortRun   | 3              | 1           | 3           |    56.63 ns |     0.946 ns |   0.052 ns |  0.67 |    0.00 |      - |      - |         - |        0.00 |
| GetOrSetAsync_CacheHit        | ShortRun   | 3              | 1           | 3           |   124.74 ns |    34.239 ns |   1.877 ns |  1.48 |    0.02 | 0.0110 |      - |     280 B |        1.94 |
| GetOrSetAsync_CacheMiss       | ShortRun   | 3              | 1           | 3           | 4,087.00 ns | 6,531.454 ns | 358.011 ns | 48.42 |    3.68 | 0.0420 | 0.0381 |    1080 B |        7.50 |
| RemoveAsync                   | ShortRun   | 3              | 1           | 3           | 2,577.17 ns |   504.726 ns |  27.666 ns | 30.53 |    0.31 | 0.0305 |      - |     784 B |        5.44 |
| SetWithSlidingExpirationAsync | ShortRun   | 3              | 1           | 3           | 3,151.56 ns | 5,450.285 ns | 298.748 ns | 37.34 |    3.07 | 0.0267 | 0.0229 |     720 B |        5.00 |
| SetAsync                      | ShortRun   | 3              | 1           | 3           | 3,212.50 ns | 4,533.738 ns | 248.509 ns | 38.06 |    2.55 | 0.0267 | 0.0229 |     712 B |        4.94 |
