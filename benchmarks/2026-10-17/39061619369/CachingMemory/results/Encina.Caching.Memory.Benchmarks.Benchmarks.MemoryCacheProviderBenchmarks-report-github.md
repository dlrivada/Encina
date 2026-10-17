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
| ExistsAsync_False             | Job-NUBXJZ | 20             | Default     | 5           |    50.54 ns |     0.113 ns |   0.130 ns |  0.27 |    0.00 |      - |      - |         - |        0.00 |
| ExistsAsync_True              | Job-NUBXJZ | 20             | Default     | 5           |    56.30 ns |     0.100 ns |   0.115 ns |  0.30 |    0.00 |      - |      - |         - |        0.00 |
| GetAsync_CacheHit             | Job-NUBXJZ | 20             | Default     | 5           |   188.43 ns |     0.470 ns |   0.541 ns |  1.00 |    0.00 | 0.0017 |      - |     144 B |        1.00 |
| GetAsync_CacheMiss            | Job-NUBXJZ | 20             | Default     | 5           |   125.37 ns |     0.541 ns |   0.623 ns |  0.67 |    0.00 |      - |      - |         - |        0.00 |
| GetOrSetAsync_CacheHit        | Job-NUBXJZ | 20             | Default     | 5           |   244.16 ns |     1.635 ns |   1.817 ns |  1.30 |    0.01 | 0.0033 |      - |     280 B |        1.94 |
| GetOrSetAsync_CacheMiss       | Job-NUBXJZ | 20             | Default     | 5           | 4,341.39 ns |   312.278 ns | 306.699 ns | 23.04 |    1.58 | 0.0114 | 0.0076 |    1114 B |        7.74 |
| RemoveAsync                   | Job-NUBXJZ | 20             | Default     | 5           | 2,847.01 ns |    18.913 ns |  21.780 ns | 15.11 |    0.12 | 0.0076 |      - |     784 B |        5.44 |
| SetWithSlidingExpirationAsync | Job-NUBXJZ | 20             | Default     | 5           | 3,567.88 ns |   493.725 ns | 548.774 ns | 18.93 |    2.84 | 0.0076 | 0.0076 |     845 B |        5.87 |
| SetAsync                      | Job-NUBXJZ | 20             | Default     | 5           | 3,525.93 ns |   597.525 ns | 639.345 ns | 18.71 |    3.30 | 0.0076 | 0.0038 |     712 B |        4.94 |
|                               |            |                |             |             |             |              |            |       |         |        |        |           |             |
| ExistsAsync_False             | ShortRun   | 3              | 1           | 3           |    50.65 ns |     2.653 ns |   0.145 ns |  0.27 |    0.00 |      - |      - |         - |        0.00 |
| ExistsAsync_True              | ShortRun   | 3              | 1           | 3           |    56.33 ns |     1.008 ns |   0.055 ns |  0.30 |    0.00 |      - |      - |         - |        0.00 |
| GetAsync_CacheHit             | ShortRun   | 3              | 1           | 3           |   188.41 ns |    10.670 ns |   0.585 ns |  1.00 |    0.00 | 0.0017 |      - |     144 B |        1.00 |
| GetAsync_CacheMiss            | ShortRun   | 3              | 1           | 3           |   127.71 ns |     8.026 ns |   0.440 ns |  0.68 |    0.00 |      - |      - |         - |        0.00 |
| GetOrSetAsync_CacheHit        | ShortRun   | 3              | 1           | 3           |   239.70 ns |    12.739 ns |   0.698 ns |  1.27 |    0.00 | 0.0033 |      - |     280 B |        1.94 |
| GetOrSetAsync_CacheMiss       | ShortRun   | 3              | 1           | 3           | 4,339.62 ns | 5,920.292 ns | 324.511 ns | 23.03 |    1.49 | 0.0114 | 0.0076 |    1080 B |        7.50 |
| RemoveAsync                   | ShortRun   | 3              | 1           | 3           | 2,751.82 ns |   182.479 ns |  10.002 ns | 14.61 |    0.06 | 0.0076 |      - |     784 B |        5.44 |
| SetWithSlidingExpirationAsync | ShortRun   | 3              | 1           | 3           | 2,975.95 ns | 8,790.154 ns | 481.818 ns | 15.80 |    2.22 | 0.0076 | 0.0038 |     720 B |        5.00 |
| SetAsync                      | ShortRun   | 3              | 1           | 3           | 3,125.02 ns | 5,005.437 ns | 274.365 ns | 16.59 |    1.26 | 0.0076 | 0.0038 |     712 B |        4.94 |
