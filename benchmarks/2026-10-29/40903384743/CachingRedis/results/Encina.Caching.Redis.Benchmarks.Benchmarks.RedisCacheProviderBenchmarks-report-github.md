```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.01GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  Job-NUBXJZ : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  Job-ZDPOZY : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4


```
| Method                        | Job        | InvocationCount | IterationCount | LaunchCount | UnrollFactor | WarmupCount | Mean      | Error      | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------ |----------- |---------------- |--------------- |------------ |------------- |------------ |----------:|-----------:|----------:|------:|--------:|----------:|------------:|
| ExistsAsync_False             | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |  93.62 μs |   2.218 μs |  2.555 μs |  0.96 |    0.04 |     579 B |        0.51 |
| ExistsAsync_True              | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |  91.52 μs |   2.521 μs |  2.802 μs |  0.94 |    0.04 |     590 B |        0.52 |
| GetAsync_CacheHit             | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |  97.53 μs |   2.662 μs |  3.065 μs |  1.00 |    0.04 |    1127 B |        1.00 |
| GetAsync_CacheMiss            | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |  99.14 μs |   2.454 μs |  2.826 μs |  1.02 |    0.04 |     639 B |        0.57 |
| GetOrSetAsync_CacheHit        | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |  99.07 μs |   1.908 μs |  2.198 μs |  1.02 |    0.04 |    1431 B |        1.27 |
| SetAsync                      | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           | 103.61 μs |   2.304 μs |  2.560 μs |  1.06 |    0.04 |     872 B |        0.77 |
| SetWithSlidingExpirationAsync | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           | 206.58 μs |   3.866 μs |  4.297 μs |  2.12 |    0.08 |    1496 B |        1.33 |
|                               |            |                 |                |             |              |             |           |            |           |       |         |           |             |
| ExistsAsync_False             | ShortRun   | Default         | 3              | 1           | 16           | 3           |  95.01 μs |  60.457 μs |  3.314 μs |  0.98 |    0.03 |     583 B |        0.52 |
| ExistsAsync_True              | ShortRun   | Default         | 3              | 1           | 16           | 3           |  91.36 μs |  52.675 μs |  2.887 μs |  0.94 |    0.03 |     590 B |        0.52 |
| GetAsync_CacheHit             | ShortRun   | Default         | 3              | 1           | 16           | 3           |  96.94 μs |   6.172 μs |  0.338 μs |  1.00 |    0.00 |    1127 B |        1.00 |
| GetAsync_CacheMiss            | ShortRun   | Default         | 3              | 1           | 16           | 3           |  99.77 μs |  71.754 μs |  3.933 μs |  1.03 |    0.04 |     639 B |        0.57 |
| GetOrSetAsync_CacheHit        | ShortRun   | Default         | 3              | 1           | 16           | 3           |  99.11 μs |  59.915 μs |  3.284 μs |  1.02 |    0.03 |    1431 B |        1.27 |
| SetAsync                      | ShortRun   | Default         | 3              | 1           | 16           | 3           | 103.68 μs |  59.379 μs |  3.255 μs |  1.07 |    0.03 |     872 B |        0.77 |
| SetWithSlidingExpirationAsync | ShortRun   | Default         | 3              | 1           | 16           | 3           | 205.96 μs |  18.095 μs |  0.992 μs |  2.12 |    0.01 |    1496 B |        1.33 |
|                               |            |                 |                |             |              |             |           |            |           |       |         |           |             |
| GetOrSetAsync_CacheMiss       | Job-ZDPOZY | 1               | 20             | Default     | 1            | 5           | 549.89 μs |   9.713 μs | 10.393 μs |     ? |       ? |    3464 B |           ? |
| RemoveAsync                   | Job-ZDPOZY | 1               | 20             | Default     | 1            | 5           | 159.62 μs |   9.360 μs | 10.404 μs |     ? |       ? |    1072 B |           ? |
| RemoveByPatternAsync          | Job-ZDPOZY | 1               | 20             | Default     | 1            | 5           | 696.79 μs |  24.824 μs | 28.588 μs |     ? |       ? |    3416 B |           ? |
|                               |            |                 |                |             |              |             |           |            |           |       |         |           |             |
| GetOrSetAsync_CacheMiss       | ShortRun   | 1               | 3              | 1           | 1            | 3           | 570.44 μs | 170.275 μs |  9.333 μs |     ? |       ? |    3464 B |           ? |
| RemoveAsync                   | ShortRun   | 1               | 3              | 1           | 1            | 3           | 170.37 μs | 359.040 μs | 19.680 μs |     ? |       ? |    1528 B |           ? |
| RemoveByPatternAsync          | ShortRun   | 1               | 3              | 1           | 1            | 3           | 717.00 μs | 471.913 μs | 25.867 μs |     ? |       ? |    3416 B |           ? |
