```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.11GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  Job-NUBXJZ : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  Job-ZDPOZY : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4


```
| Method                        | Job        | InvocationCount | IterationCount | LaunchCount | UnrollFactor | WarmupCount | Mean     | Error       | StdDev    | Median   | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------ |----------- |---------------- |--------------- |------------ |------------- |------------ |---------:|------------:|----------:|---------:|------:|--------:|----------:|------------:|
| ExistsAsync_False             | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           | 111.5 μs |     2.34 μs |   2.70 μs | 111.5 μs |  0.99 |    0.03 |     582 B |        0.52 |
| ExistsAsync_True              | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           | 110.0 μs |     1.18 μs |   1.31 μs | 110.2 μs |  0.97 |    0.02 |     589 B |        0.52 |
| GetAsync_CacheHit             | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           | 113.0 μs |     1.87 μs |   2.08 μs | 113.2 μs |  1.00 |    0.03 |    1127 B |        1.00 |
| GetAsync_CacheMiss            | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           | 110.6 μs |     2.30 μs |   2.55 μs | 110.9 μs |  0.98 |    0.03 |     640 B |        0.57 |
| GetOrSetAsync_CacheHit        | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           | 114.5 μs |     1.37 μs |   1.46 μs | 114.7 μs |  1.01 |    0.02 |    1432 B |        1.27 |
| SetAsync                      | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           | 108.5 μs |     1.32 μs |   1.41 μs | 108.2 μs |  0.96 |    0.02 |     872 B |        0.77 |
| SetWithSlidingExpirationAsync | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           | 204.3 μs |     3.35 μs |   3.44 μs | 204.1 μs |  1.81 |    0.04 |    1496 B |        1.33 |
|                               |            |                 |                |             |              |             |          |             |           |          |       |         |           |             |
| ExistsAsync_False             | ShortRun   | Default         | 3              | 1           | 16           | 3           | 108.0 μs |     6.35 μs |   0.35 μs | 108.0 μs |  0.95 |    0.04 |     581 B |        0.52 |
| ExistsAsync_True              | ShortRun   | Default         | 3              | 1           | 16           | 3           | 109.7 μs |    67.71 μs |   3.71 μs | 108.0 μs |  0.96 |    0.05 |     590 B |        0.52 |
| GetAsync_CacheHit             | ShortRun   | Default         | 3              | 1           | 16           | 3           | 114.4 μs |    94.61 μs |   5.19 μs | 113.3 μs |  1.00 |    0.06 |    1128 B |        1.00 |
| GetAsync_CacheMiss            | ShortRun   | Default         | 3              | 1           | 16           | 3           | 111.8 μs |    21.63 μs |   1.19 μs | 111.6 μs |  0.98 |    0.04 |     638 B |        0.57 |
| GetOrSetAsync_CacheHit        | ShortRun   | Default         | 3              | 1           | 16           | 3           | 106.2 μs |    22.83 μs |   1.25 μs | 106.7 μs |  0.93 |    0.04 |    1430 B |        1.27 |
| SetAsync                      | ShortRun   | Default         | 3              | 1           | 16           | 3           | 105.7 μs |     7.32 μs |   0.40 μs | 105.8 μs |  0.93 |    0.04 |     869 B |        0.77 |
| SetWithSlidingExpirationAsync | ShortRun   | Default         | 3              | 1           | 16           | 3           | 208.3 μs |    33.61 μs |   1.84 μs | 209.3 μs |  1.82 |    0.07 |    1495 B |        1.33 |
|                               |            |                 |                |             |              |             |          |             |           |          |       |         |           |             |
| GetOrSetAsync_CacheMiss       | Job-ZDPOZY | 1               | 20             | Default     | 1            | 5           | 565.0 μs |    16.81 μs |  19.36 μs | 560.6 μs |     ? |       ? |    3464 B |           ? |
| RemoveAsync                   | Job-ZDPOZY | 1               | 20             | Default     | 1            | 5           | 171.2 μs |     7.90 μs |   9.10 μs | 172.3 μs |     ? |       ? |    1072 B |           ? |
| RemoveByPatternAsync          | Job-ZDPOZY | 1               | 20             | Default     | 1            | 5           | 743.4 μs |    30.40 μs |  35.01 μs | 741.7 μs |     ? |       ? |    3416 B |           ? |
|                               |            |                 |                |             |              |             |          |             |           |          |       |         |           |             |
| GetOrSetAsync_CacheMiss       | ShortRun   | 1               | 3              | 1           | 1            | 3           | 601.7 μs | 1,205.00 μs |  66.05 μs | 612.4 μs |     ? |       ? |    3464 B |           ? |
| RemoveAsync                   | ShortRun   | 1               | 3              | 1           | 1            | 3           | 320.3 μs | 4,933.76 μs | 270.44 μs | 169.6 μs |     ? |       ? |    1072 B |           ? |
| RemoveByPatternAsync          | ShortRun   | 1               | 3              | 1           | 1            | 3           | 752.4 μs |   360.32 μs |  19.75 μs | 759.3 μs |     ? |       ? |    3416 B |           ? |
