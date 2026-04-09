```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method                        | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error        | StdDev     | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |----------- |--------------- |------------ |------------ |------------:|-------------:|-----------:|------:|--------:|-------:|-------:|----------:|------------:|
| GetAsync_CacheHit             | Job-YFEFPZ | 10             | Default     | 3           |    83.26 ns |     1.227 ns |   0.812 ns |  1.00 |    0.01 | 0.0086 |      - |     144 B |        1.00 |
| GetAsync_CacheMiss            | Job-YFEFPZ | 10             | Default     | 3           |    53.19 ns |     0.042 ns |   0.025 ns |  0.64 |    0.01 |      - |      - |         - |        0.00 |
| SetAsync                      | Job-YFEFPZ | 10             | Default     | 3           | 3,765.09 ns |   464.144 ns | 276.205 ns | 45.22 |    3.17 | 0.0420 | 0.0381 |     712 B |        4.94 |
| ExistsAsync_True              | Job-YFEFPZ | 10             | Default     | 3           |    60.41 ns |     0.140 ns |   0.083 ns |  0.73 |    0.01 |      - |      - |         - |        0.00 |
| ExistsAsync_False             | Job-YFEFPZ | 10             | Default     | 3           |    52.59 ns |     0.030 ns |   0.020 ns |  0.63 |    0.01 |      - |      - |         - |        0.00 |
| GetOrSetAsync_CacheHit        | Job-YFEFPZ | 10             | Default     | 3           |   134.13 ns |     1.211 ns |   0.801 ns |  1.61 |    0.02 | 0.0167 |      - |     280 B |        1.94 |
| GetOrSetAsync_CacheMiss       | Job-YFEFPZ | 10             | Default     | 3           | 5,141.35 ns |   640.620 ns | 381.223 ns | 61.76 |    4.38 | 0.0610 | 0.0572 |    1080 B |        7.50 |
| RemoveAsync                   | Job-YFEFPZ | 10             | Default     | 3           | 2,778.63 ns |    18.350 ns |  12.138 ns | 33.38 |    0.34 | 0.0458 |      - |     784 B |        5.44 |
| SetWithSlidingExpirationAsync | Job-YFEFPZ | 10             | Default     | 3           | 4,093.33 ns | 1,400.139 ns | 833.200 ns | 49.17 |    9.50 | 0.0420 | 0.0381 |     720 B |        5.00 |
|                               |            |                |             |             |             |              |            |       |         |        |        |           |             |
| GetAsync_CacheHit             | MediumRun  | 15             | 2           | 10          |    83.82 ns |     0.691 ns |   1.035 ns |  1.00 |    0.02 | 0.0086 |      - |     144 B |        1.00 |
| GetAsync_CacheMiss            | MediumRun  | 15             | 2           | 10          |    55.14 ns |     0.239 ns |   0.327 ns |  0.66 |    0.01 |      - |      - |         - |        0.00 |
| SetAsync                      | MediumRun  | 15             | 2           | 10          | 3,823.42 ns |   240.400 ns | 337.007 ns | 45.62 |    3.99 | 0.0381 | 0.0305 |     712 B |        4.94 |
| ExistsAsync_True              | MediumRun  | 15             | 2           | 10          |    60.37 ns |     0.045 ns |   0.065 ns |  0.72 |    0.01 |      - |      - |         - |        0.00 |
| ExistsAsync_False             | MediumRun  | 15             | 2           | 10          |    52.28 ns |     0.085 ns |   0.122 ns |  0.62 |    0.01 |      - |      - |         - |        0.00 |
| GetOrSetAsync_CacheHit        | MediumRun  | 15             | 2           | 10          |   131.24 ns |     0.898 ns |   1.316 ns |  1.57 |    0.02 | 0.0167 |      - |     280 B |        1.94 |
| GetOrSetAsync_CacheMiss       | MediumRun  | 15             | 2           | 10          | 5,420.04 ns |   277.532 ns | 398.028 ns | 64.67 |    4.73 | 0.0610 | 0.0572 |    1080 B |        7.50 |
| RemoveAsync                   | MediumRun  | 15             | 2           | 10          | 2,739.99 ns |    16.896 ns |  24.232 ns | 32.70 |    0.49 | 0.0458 |      - |     784 B |        5.44 |
| SetWithSlidingExpirationAsync | MediumRun  | 15             | 2           | 10          | 4,409.31 ns |   538.981 ns | 772.991 ns | 52.61 |    9.09 | 0.0420 | 0.0381 |     720 B |        5.00 |
