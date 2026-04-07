```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 3.39GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4
  MediumRun  : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4


```
| Method                        | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error      | StdDev     | Median      | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |----------- |--------------- |------------ |------------ |------------:|-----------:|-----------:|------------:|------:|--------:|-------:|-------:|----------:|------------:|
| GetAsync_CacheHit             | Job-YFEFPZ | 10             | Default     | 3           |   101.99 ns |   3.004 ns |   1.987 ns |   102.86 ns |  1.00 |    0.03 | 0.0057 |      - |     144 B |        1.00 |
| GetAsync_CacheMiss            | Job-YFEFPZ | 10             | Default     | 3           |    50.19 ns |   0.244 ns |   0.145 ns |    50.13 ns |  0.49 |    0.01 |      - |      - |         - |        0.00 |
| SetAsync                      | Job-YFEFPZ | 10             | Default     | 3           | 3,857.65 ns | 504.672 ns | 263.953 ns | 3,876.22 ns | 37.84 |    2.54 | 0.0229 | 0.0153 |     712 B |        4.94 |
| ExistsAsync_True              | Job-YFEFPZ | 10             | Default     | 3           |    61.80 ns |   0.086 ns |   0.057 ns |    61.80 ns |  0.61 |    0.01 |      - |      - |         - |        0.00 |
| ExistsAsync_False             | Job-YFEFPZ | 10             | Default     | 3           |    48.60 ns |   0.081 ns |   0.048 ns |    48.59 ns |  0.48 |    0.01 |      - |      - |         - |        0.00 |
| GetOrSetAsync_CacheHit        | Job-YFEFPZ | 10             | Default     | 3           |   159.68 ns |   3.203 ns |   2.119 ns |   159.81 ns |  1.57 |    0.04 | 0.0110 |      - |     280 B |        1.94 |
| GetOrSetAsync_CacheMiss       | Job-YFEFPZ | 10             | Default     | 3           | 5,511.33 ns | 898.100 ns | 534.445 ns | 5,577.84 ns | 54.05 |    5.07 | 0.0381 | 0.0305 |    1080 B |        7.50 |
| RemoveAsync                   | Job-YFEFPZ | 10             | Default     | 3           | 2,651.13 ns |  26.030 ns |  17.217 ns | 2,658.63 ns | 26.00 |    0.51 | 0.0305 |      - |     784 B |        5.44 |
| SetWithSlidingExpirationAsync | Job-YFEFPZ | 10             | Default     | 3           | 4,354.51 ns | 615.928 ns | 366.529 ns | 4,351.21 ns | 42.71 |    3.50 | 0.0267 | 0.0229 |     740 B |        5.14 |
|                               |            |                |             |             |             |            |            |             |       |         |        |        |           |             |
| GetAsync_CacheHit             | MediumRun  | 15             | 2           | 10          |    88.91 ns |   0.982 ns |   1.470 ns |    88.73 ns |  1.00 |    0.02 | 0.0057 |      - |     144 B |        1.00 |
| GetAsync_CacheMiss            | MediumRun  | 15             | 2           | 10          |    51.87 ns |   0.844 ns |   1.237 ns |    52.92 ns |  0.58 |    0.02 |      - |      - |         - |        0.00 |
| SetAsync                      | MediumRun  | 15             | 2           | 10          | 4,164.04 ns | 275.282 ns | 376.809 ns | 4,161.22 ns | 46.85 |    4.23 | 0.0267 | 0.0229 |     712 B |        4.94 |
| ExistsAsync_True              | MediumRun  | 15             | 2           | 10          |    55.83 ns |   0.306 ns |   0.458 ns |    55.82 ns |  0.63 |    0.01 |      - |      - |         - |        0.00 |
| ExistsAsync_False             | MediumRun  | 15             | 2           | 10          |    48.05 ns |   0.096 ns |   0.144 ns |    48.03 ns |  0.54 |    0.01 |      - |      - |         - |        0.00 |
| GetOrSetAsync_CacheHit        | MediumRun  | 15             | 2           | 10          |   134.89 ns |   1.142 ns |   1.709 ns |   134.31 ns |  1.52 |    0.03 | 0.0110 |      - |     280 B |        1.94 |
| GetOrSetAsync_CacheMiss       | MediumRun  | 15             | 2           | 10          | 5,348.58 ns | 359.787 ns | 504.371 ns | 5,232.31 ns | 60.17 |    5.66 | 0.0381 | 0.0305 |    1080 B |        7.50 |
| RemoveAsync                   | MediumRun  | 15             | 2           | 10          | 2,571.17 ns |  14.573 ns |  21.812 ns | 2,571.93 ns | 28.93 |    0.53 | 0.0305 |      - |     784 B |        5.44 |
| SetWithSlidingExpirationAsync | MediumRun  | 15             | 2           | 10          | 5,188.57 ns | 485.763 ns | 712.025 ns | 5,103.42 ns | 58.37 |    7.93 | 0.0267 | 0.0229 |     720 B |        5.00 |
