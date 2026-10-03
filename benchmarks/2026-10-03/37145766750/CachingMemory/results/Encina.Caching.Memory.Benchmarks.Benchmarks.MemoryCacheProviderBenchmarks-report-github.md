```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-NUBXJZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3


```
| Method                        | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error        | StdDev     | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |----------- |--------------- |------------ |------------ |------------:|-------------:|-----------:|------:|--------:|-------:|-------:|----------:|------------:|
| ExistsAsync_False             | Job-NUBXJZ | 20             | Default     | 5           |    52.25 ns |     0.034 ns |   0.036 ns |  0.63 |    0.01 |      - |      - |         - |        0.00 |
| ExistsAsync_True              | Job-NUBXJZ | 20             | Default     | 5           |    60.23 ns |     0.049 ns |   0.050 ns |  0.73 |    0.01 |      - |      - |         - |        0.00 |
| GetAsync_CacheHit             | Job-NUBXJZ | 20             | Default     | 5           |    82.65 ns |     0.880 ns |   1.014 ns |  1.00 |    0.02 | 0.0086 |      - |     144 B |        1.00 |
| GetAsync_CacheMiss            | Job-NUBXJZ | 20             | Default     | 5           |    53.13 ns |     0.061 ns |   0.065 ns |  0.64 |    0.01 |      - |      - |         - |        0.00 |
| GetOrSetAsync_CacheHit        | Job-NUBXJZ | 20             | Default     | 5           |   133.84 ns |     0.365 ns |   0.390 ns |  1.62 |    0.02 | 0.0167 |      - |     280 B |        1.94 |
| GetOrSetAsync_CacheMiss       | Job-NUBXJZ | 20             | Default     | 5           | 5,323.68 ns |   374.897 ns | 401.135 ns | 64.42 |    4.79 | 0.0610 | 0.0572 |    1080 B |        7.50 |
| RemoveAsync                   | Job-NUBXJZ | 20             | Default     | 5           | 2,910.00 ns |    26.472 ns |  30.485 ns | 35.21 |    0.56 | 0.0458 |      - |     784 B |        5.44 |
| SetWithSlidingExpirationAsync | Job-NUBXJZ | 20             | Default     | 5           | 4,176.86 ns |   537.619 ns | 597.562 ns | 50.54 |    7.07 | 0.0420 | 0.0381 |     720 B |        5.00 |
| SetAsync                      | Job-NUBXJZ | 20             | Default     | 5           | 3,893.91 ns |   356.084 ns | 381.005 ns | 47.12 |    4.52 | 0.0420 | 0.0381 |     712 B |        4.94 |
|                               |            |                |             |             |             |              |            |       |         |        |        |           |             |
| ExistsAsync_False             | ShortRun   | 3              | 1           | 3           |    52.70 ns |     0.361 ns |   0.020 ns |  0.63 |    0.00 |      - |      - |         - |        0.00 |
| ExistsAsync_True              | ShortRun   | 3              | 1           | 3           |    61.04 ns |     0.787 ns |   0.043 ns |  0.73 |    0.00 |      - |      - |         - |        0.00 |
| GetAsync_CacheHit             | ShortRun   | 3              | 1           | 3           |    84.16 ns |     2.320 ns |   0.127 ns |  1.00 |    0.00 | 0.0086 |      - |     144 B |        1.00 |
| GetAsync_CacheMiss            | ShortRun   | 3              | 1           | 3           |    53.18 ns |     0.693 ns |   0.038 ns |  0.63 |    0.00 |      - |      - |         - |        0.00 |
| GetOrSetAsync_CacheHit        | ShortRun   | 3              | 1           | 3           |   130.31 ns |     4.894 ns |   0.268 ns |  1.55 |    0.00 | 0.0167 |      - |     280 B |        1.94 |
| GetOrSetAsync_CacheMiss       | ShortRun   | 3              | 1           | 3           | 4,593.77 ns | 2,383.931 ns | 130.671 ns | 54.59 |    1.35 | 0.0610 | 0.0572 |    1080 B |        7.50 |
| RemoveAsync                   | ShortRun   | 3              | 1           | 3           | 2,907.14 ns |   353.360 ns |  19.369 ns | 34.54 |    0.20 | 0.0458 |      - |     784 B |        5.44 |
| SetWithSlidingExpirationAsync | ShortRun   | 3              | 1           | 3           | 3,298.71 ns | 4,715.333 ns | 258.463 ns | 39.20 |    2.66 | 0.0420 | 0.0381 |     720 B |        5.00 |
| SetAsync                      | ShortRun   | 3              | 1           | 3           | 3,555.46 ns | 1,786.493 ns |  97.924 ns | 42.25 |    1.01 | 0.0420 | 0.0381 |     712 B |        4.94 |
