```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-NUBXJZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4


```
| Method                        | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error        | StdDev     | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |----------- |--------------- |------------ |------------ |------------:|-------------:|-----------:|------:|--------:|-------:|-------:|----------:|------------:|
| ExistsAsync_False             | Job-NUBXJZ | 20             | Default     | 5           |    33.79 ns |     0.034 ns |   0.038 ns |  0.73 |    0.00 |      - |      - |         - |        0.00 |
| ExistsAsync_True              | Job-NUBXJZ | 20             | Default     | 5           |    38.54 ns |     0.134 ns |   0.138 ns |  0.83 |    0.01 |      - |      - |         - |        0.00 |
| GetAsync_CacheHit             | Job-NUBXJZ | 20             | Default     | 5           |    46.26 ns |     0.259 ns |   0.277 ns |  1.00 |    0.01 | 0.0086 |      - |     144 B |        1.00 |
| GetAsync_CacheMiss            | Job-NUBXJZ | 20             | Default     | 5           |    35.08 ns |     0.103 ns |   0.110 ns |  0.76 |    0.00 |      - |      - |         - |        0.00 |
| GetOrSetAsync_CacheHit        | Job-NUBXJZ | 20             | Default     | 5           |    64.20 ns |     0.492 ns |   0.567 ns |  1.39 |    0.01 | 0.0167 |      - |     280 B |        1.94 |
| GetOrSetAsync_CacheMiss       | Job-NUBXJZ | 20             | Default     | 5           | 3,891.61 ns |   312.449 ns | 334.317 ns | 84.13 |    7.05 | 0.0610 | 0.0572 |    1080 B |        7.50 |
| RemoveAsync                   | Job-NUBXJZ | 20             | Default     | 5           | 1,555.15 ns |     7.437 ns |   7.958 ns | 33.62 |    0.26 | 0.0458 |      - |     784 B |        5.44 |
| SetWithSlidingExpirationAsync | Job-NUBXJZ | 20             | Default     | 5           | 3,146.91 ns |   328.443 ns | 365.063 ns | 68.03 |    7.70 | 0.0496 | 0.0458 |     856 B |        5.94 |
| SetAsync                      | Job-NUBXJZ | 20             | Default     | 5           | 3,036.36 ns |   233.167 ns | 239.445 ns | 65.64 |    5.04 | 0.0420 | 0.0381 |     712 B |        4.94 |
|                               |            |                |             |             |             |              |            |       |         |        |        |           |             |
| ExistsAsync_False             | ShortRun   | 3              | 1           | 3           |    34.02 ns |     0.588 ns |   0.032 ns |  0.72 |    0.02 |      - |      - |         - |        0.00 |
| ExistsAsync_True              | ShortRun   | 3              | 1           | 3           |    38.21 ns |     4.172 ns |   0.229 ns |  0.81 |    0.03 |      - |      - |         - |        0.00 |
| GetAsync_CacheHit             | ShortRun   | 3              | 1           | 3           |    47.06 ns |    32.592 ns |   1.786 ns |  1.00 |    0.05 | 0.0086 |      - |     144 B |        1.00 |
| GetAsync_CacheMiss            | ShortRun   | 3              | 1           | 3           |    34.93 ns |     0.161 ns |   0.009 ns |  0.74 |    0.02 |      - |      - |         - |        0.00 |
| GetOrSetAsync_CacheHit        | ShortRun   | 3              | 1           | 3           |    64.86 ns |    13.182 ns |   0.723 ns |  1.38 |    0.05 | 0.0167 |      - |     280 B |        1.94 |
| GetOrSetAsync_CacheMiss       | ShortRun   | 3              | 1           | 3           | 3,584.57 ns | 1,304.485 ns |  71.503 ns | 76.24 |    2.79 | 0.0610 | 0.0572 |    1080 B |        7.50 |
| RemoveAsync                   | ShortRun   | 3              | 1           | 3           | 1,560.51 ns |   336.757 ns |  18.459 ns | 33.19 |    1.12 | 0.0458 |      - |     784 B |        5.44 |
| SetWithSlidingExpirationAsync | ShortRun   | 3              | 1           | 3           | 2,853.68 ns | 2,604.633 ns | 142.769 ns | 60.70 |    3.28 | 0.0420 | 0.0381 |     720 B |        5.00 |
| SetAsync                      | ShortRun   | 3              | 1           | 3           | 2,470.35 ns | 3,051.240 ns | 167.249 ns | 52.54 |    3.52 | 0.0420 | 0.0381 |     712 B |        4.94 |
