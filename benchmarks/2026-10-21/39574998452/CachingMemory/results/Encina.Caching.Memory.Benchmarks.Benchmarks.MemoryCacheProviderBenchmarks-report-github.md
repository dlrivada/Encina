```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 3.12GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  Job-NUBXJZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4


```
| Method                        | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error        | StdDev     | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |----------- |--------------- |------------ |------------ |------------:|-------------:|-----------:|------:|--------:|-------:|-------:|----------:|------------:|
| ExistsAsync_False             | Job-NUBXJZ | 20             | Default     | 5           |    49.07 ns |     0.037 ns |   0.042 ns |  0.26 |    0.00 |      - |      - |         - |        0.00 |
| ExistsAsync_True              | Job-NUBXJZ | 20             | Default     | 5           |    57.02 ns |     0.068 ns |   0.076 ns |  0.30 |    0.00 |      - |      - |         - |        0.00 |
| GetAsync_CacheHit             | Job-NUBXJZ | 20             | Default     | 5           |   192.01 ns |     0.297 ns |   0.342 ns |  1.00 |    0.00 | 0.0057 |      - |     144 B |        1.00 |
| GetAsync_CacheMiss            | Job-NUBXJZ | 20             | Default     | 5           |   130.26 ns |     0.115 ns |   0.123 ns |  0.68 |    0.00 |      - |      - |         - |        0.00 |
| GetOrSetAsync_CacheHit        | Job-NUBXJZ | 20             | Default     | 5           |   249.46 ns |     0.607 ns |   0.674 ns |  1.30 |    0.00 | 0.0110 |      - |     280 B |        1.94 |
| GetOrSetAsync_CacheMiss       | Job-NUBXJZ | 20             | Default     | 5           | 4,593.36 ns |   341.332 ns | 365.222 ns | 23.92 |    1.85 | 0.0381 | 0.0305 |    1080 B |        7.50 |
| RemoveAsync                   | Job-NUBXJZ | 20             | Default     | 5           | 2,976.76 ns |    21.142 ns |  24.347 ns | 15.50 |    0.13 | 0.0305 |      - |     784 B |        5.44 |
| SetWithSlidingExpirationAsync | Job-NUBXJZ | 20             | Default     | 5           | 3,379.08 ns |   474.055 ns | 507.234 ns | 17.60 |    2.57 | 0.0229 | 0.0153 |     720 B |        5.00 |
| SetAsync                      | Job-NUBXJZ | 20             | Default     | 5           | 3,748.06 ns |   184.057 ns | 189.013 ns | 19.52 |    0.96 | 0.0267 | 0.0229 |     712 B |        4.94 |
|                               |            |                |             |             |             |              |            |       |         |        |        |           |             |
| ExistsAsync_False             | ShortRun   | 3              | 1           | 3           |    54.86 ns |     6.866 ns |   0.376 ns |  0.29 |    0.00 |      - |      - |         - |        0.00 |
| ExistsAsync_True              | ShortRun   | 3              | 1           | 3           |    59.55 ns |     2.047 ns |   0.112 ns |  0.31 |    0.00 |      - |      - |         - |        0.00 |
| GetAsync_CacheHit             | ShortRun   | 3              | 1           | 3           |   191.30 ns |     9.604 ns |   0.526 ns |  1.00 |    0.00 | 0.0057 |      - |     144 B |        1.00 |
| GetAsync_CacheMiss            | ShortRun   | 3              | 1           | 3           |   132.66 ns |     9.443 ns |   0.518 ns |  0.69 |    0.00 |      - |      - |         - |        0.00 |
| GetOrSetAsync_CacheHit        | ShortRun   | 3              | 1           | 3           |   250.87 ns |    10.209 ns |   0.560 ns |  1.31 |    0.00 | 0.0110 |      - |     280 B |        1.94 |
| GetOrSetAsync_CacheMiss       | ShortRun   | 3              | 1           | 3           | 3,999.12 ns | 5,084.857 ns | 278.718 ns | 20.91 |    1.26 | 0.0381 | 0.0305 |    1080 B |        7.50 |
| RemoveAsync                   | ShortRun   | 3              | 1           | 3           | 3,069.04 ns |   550.685 ns |  30.185 ns | 16.04 |    0.14 | 0.0305 |      - |     784 B |        5.44 |
| SetWithSlidingExpirationAsync | ShortRun   | 3              | 1           | 3           | 3,304.19 ns | 3,414.742 ns | 187.174 ns | 17.27 |    0.85 | 0.0267 | 0.0229 |     720 B |        5.00 |
| SetAsync                      | ShortRun   | 3              | 1           | 3           | 2,940.36 ns | 1,077.974 ns |  59.087 ns | 15.37 |    0.27 | 0.0229 | 0.0153 |     712 B |        4.94 |
