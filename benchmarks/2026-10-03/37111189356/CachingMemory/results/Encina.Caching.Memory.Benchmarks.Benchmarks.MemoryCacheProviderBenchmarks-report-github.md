```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.78GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-NUBXJZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3


```
| Method                        | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error        | StdDev     | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |----------- |--------------- |------------ |------------ |------------:|-------------:|-----------:|------:|--------:|-------:|-------:|----------:|------------:|
| ExistsAsync_False             | Job-NUBXJZ | 20             | Default     | 5           |    52.75 ns |     0.038 ns |   0.043 ns |  0.57 |    0.00 |      - |      - |         - |        0.00 |
| ExistsAsync_True              | Job-NUBXJZ | 20             | Default     | 5           |    60.06 ns |     0.047 ns |   0.051 ns |  0.65 |    0.00 |      - |      - |         - |        0.00 |
| GetAsync_CacheHit             | Job-NUBXJZ | 20             | Default     | 5           |    92.35 ns |     0.439 ns |   0.506 ns |  1.00 |    0.01 | 0.0086 |      - |     144 B |        1.00 |
| GetAsync_CacheMiss            | Job-NUBXJZ | 20             | Default     | 5           |    59.03 ns |     0.035 ns |   0.036 ns |  0.64 |    0.00 |      - |      - |         - |        0.00 |
| GetOrSetAsync_CacheHit        | Job-NUBXJZ | 20             | Default     | 5           |   136.11 ns |     0.561 ns |   0.624 ns |  1.47 |    0.01 | 0.0167 |      - |     280 B |        1.94 |
| GetOrSetAsync_CacheMiss       | Job-NUBXJZ | 20             | Default     | 5           | 5,205.70 ns |   456.652 ns | 488.612 ns | 56.37 |    5.16 | 0.0610 | 0.0534 |    1080 B |        7.50 |
| RemoveAsync                   | Job-NUBXJZ | 20             | Default     | 5           | 2,872.14 ns |    23.834 ns |  27.447 ns | 31.10 |    0.33 | 0.0458 |      - |     784 B |        5.44 |
| SetWithSlidingExpirationAsync | Job-NUBXJZ | 20             | Default     | 5           | 4,403.62 ns |   570.921 ns | 634.577 ns | 47.68 |    6.70 | 0.0420 | 0.0381 |     720 B |        5.00 |
| SetAsync                      | Job-NUBXJZ | 20             | Default     | 5           | 4,041.77 ns |   272.946 ns | 292.049 ns | 43.77 |    3.09 | 0.0420 | 0.0381 |     712 B |        4.94 |
|                               |            |                |             |             |             |              |            |       |         |        |        |           |             |
| ExistsAsync_False             | ShortRun   | 3              | 1           | 3           |    52.20 ns |     0.430 ns |   0.024 ns |  0.61 |    0.00 |      - |      - |         - |        0.00 |
| ExistsAsync_True              | ShortRun   | 3              | 1           | 3           |    60.32 ns |     0.098 ns |   0.005 ns |  0.70 |    0.00 |      - |      - |         - |        0.00 |
| GetAsync_CacheHit             | ShortRun   | 3              | 1           | 3           |    85.82 ns |     2.593 ns |   0.142 ns |  1.00 |    0.00 | 0.0086 |      - |     144 B |        1.00 |
| GetAsync_CacheMiss            | ShortRun   | 3              | 1           | 3           |    53.08 ns |     0.181 ns |   0.010 ns |  0.62 |    0.00 |      - |      - |         - |        0.00 |
| GetOrSetAsync_CacheHit        | ShortRun   | 3              | 1           | 3           |   163.26 ns |     9.672 ns |   0.530 ns |  1.90 |    0.01 | 0.0167 |      - |     280 B |        1.94 |
| GetOrSetAsync_CacheMiss       | ShortRun   | 3              | 1           | 3           | 4,391.19 ns | 1,292.455 ns |  70.844 ns | 51.17 |    0.72 | 0.0610 | 0.0534 |    1080 B |        7.50 |
| RemoveAsync                   | ShortRun   | 3              | 1           | 3           | 2,900.60 ns |   473.771 ns |  25.969 ns | 33.80 |    0.27 | 0.0458 |      - |     784 B |        5.44 |
| SetWithSlidingExpirationAsync | ShortRun   | 3              | 1           | 3           | 3,422.45 ns | 5,223.388 ns | 286.312 ns | 39.88 |    2.89 | 0.0381 | 0.0305 |     720 B |        5.00 |
| SetAsync                      | ShortRun   | 3              | 1           | 3           | 3,727.09 ns | 3,080.309 ns | 168.842 ns | 43.43 |    1.71 | 0.0420 | 0.0381 |     712 B |        4.94 |
