```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-NUBXJZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-ZDPOZY : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4


```
| Method                        | Job        | InvocationCount | IterationCount | LaunchCount | UnrollFactor | WarmupCount | Mean     | Error     | StdDev   | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------ |----------- |---------------- |--------------- |------------ |------------- |------------ |---------:|----------:|---------:|------:|--------:|----------:|------------:|
| ExistsAsync_False             | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           | 110.2 μs |   1.05 μs |  1.07 μs |  0.98 |    0.02 |     584 B |        0.52 |
| ExistsAsync_True              | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           | 110.5 μs |   0.95 μs |  1.05 μs |  0.98 |    0.02 |     592 B |        0.52 |
| GetAsync_CacheHit             | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           | 113.0 μs |   1.43 μs |  1.54 μs |  1.00 |    0.02 |    1128 B |        1.00 |
| GetAsync_CacheMiss            | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           | 111.5 μs |   1.28 μs |  1.37 μs |  0.99 |    0.02 |     640 B |        0.57 |
| GetOrSetAsync_CacheHit        | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           | 113.8 μs |   1.67 μs |  1.86 μs |  1.01 |    0.02 |    1432 B |        1.27 |
| SetAsync                      | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           | 115.8 μs |   1.19 μs |  1.27 μs |  1.02 |    0.02 |     872 B |        0.77 |
| SetWithSlidingExpirationAsync | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           | 219.4 μs |   2.29 μs |  2.45 μs |  1.94 |    0.03 |    1496 B |        1.33 |
|                               |            |                 |                |             |              |             |          |           |          |       |         |           |             |
| ExistsAsync_False             | ShortRun   | Default         | 3              | 1           | 16           | 3           | 110.6 μs |  37.53 μs |  2.06 μs |  0.98 |    0.02 |     582 B |        0.52 |
| ExistsAsync_True              | ShortRun   | Default         | 3              | 1           | 16           | 3           | 113.2 μs |  59.49 μs |  3.26 μs |  1.00 |    0.03 |     591 B |        0.53 |
| GetAsync_CacheHit             | ShortRun   | Default         | 3              | 1           | 16           | 3           | 113.4 μs |   6.32 μs |  0.35 μs |  1.00 |    0.00 |    1124 B |        1.00 |
| GetAsync_CacheMiss            | ShortRun   | Default         | 3              | 1           | 16           | 3           | 111.3 μs |  37.25 μs |  2.04 μs |  0.98 |    0.02 |     637 B |        0.57 |
| GetOrSetAsync_CacheHit        | ShortRun   | Default         | 3              | 1           | 16           | 3           | 112.8 μs |   9.92 μs |  0.54 μs |  0.99 |    0.00 |    1430 B |        1.27 |
| SetAsync                      | ShortRun   | Default         | 3              | 1           | 16           | 3           | 116.5 μs |  20.95 μs |  1.15 μs |  1.03 |    0.01 |     871 B |        0.77 |
| SetWithSlidingExpirationAsync | ShortRun   | Default         | 3              | 1           | 16           | 3           | 216.1 μs |  54.67 μs |  3.00 μs |  1.91 |    0.02 |    1496 B |        1.33 |
|                               |            |                 |                |             |              |             |          |           |          |       |         |           |             |
| GetOrSetAsync_CacheMiss       | Job-ZDPOZY | 1               | 20             | Default     | 1            | 5           | 550.4 μs |  29.66 μs | 32.97 μs |     ? |       ? |    3000 B |           ? |
| RemoveAsync                   | Job-ZDPOZY | 1               | 20             | Default     | 1            | 5           | 156.4 μs |  10.11 μs | 11.65 μs |     ? |       ? |     608 B |           ? |
| RemoveByPatternAsync          | Job-ZDPOZY | 1               | 20             | Default     | 1            | 5           | 714.3 μs |  23.51 μs | 26.13 μs |     ? |       ? |    2952 B |           ? |
|                               |            |                 |                |             |              |             |          |           |          |       |         |           |             |
| GetOrSetAsync_CacheMiss       | ShortRun   | 1               | 3              | 1           | 1            | 3           | 575.6 μs | 664.85 μs | 36.44 μs |     ? |       ? |    3000 B |           ? |
| RemoveAsync                   | ShortRun   | 1               | 3              | 1           | 1            | 3           | 167.8 μs |  97.45 μs |  5.34 μs |     ? |       ? |    1064 B |           ? |
| RemoveByPatternAsync          | ShortRun   | 1               | 3              | 1           | 1            | 3           | 739.6 μs | 263.83 μs | 14.46 μs |     ? |       ? |    2952 B |           ? |
