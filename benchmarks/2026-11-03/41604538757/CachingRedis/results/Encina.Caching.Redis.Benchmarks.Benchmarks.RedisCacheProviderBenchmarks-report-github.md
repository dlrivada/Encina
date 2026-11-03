```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.24GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  Job-NUBXJZ : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  Job-ZDPOZY : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3


```
| Method                        | Job        | InvocationCount | IterationCount | LaunchCount | UnrollFactor | WarmupCount | Mean       | Error        | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------ |----------- |---------------- |--------------- |------------ |------------- |------------ |-----------:|-------------:|----------:|------:|--------:|----------:|------------:|
| ExistsAsync_False             | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |   283.7 μs |     10.54 μs |  12.13 μs |  0.93 |    0.05 |     584 B |        0.52 |
| ExistsAsync_True              | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |   285.5 μs |     12.35 μs |  13.73 μs |  0.94 |    0.06 |     592 B |        0.52 |
| GetAsync_CacheHit             | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |   304.7 μs |     10.15 μs |  11.28 μs |  1.00 |    0.05 |    1128 B |        1.00 |
| GetAsync_CacheMiss            | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |   288.6 μs |      9.41 μs |  10.46 μs |  0.95 |    0.05 |     640 B |        0.57 |
| GetOrSetAsync_CacheHit        | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |   302.2 μs |      7.76 μs |   8.62 μs |  0.99 |    0.05 |    1432 B |        1.27 |
| SetAsync                      | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |   315.1 μs |      9.48 μs |  10.92 μs |  1.04 |    0.05 |     872 B |        0.77 |
| SetWithSlidingExpirationAsync | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |   585.4 μs |     14.52 μs |  16.14 μs |  1.92 |    0.09 |    1496 B |        1.33 |
|                               |            |                 |                |             |              |             |            |              |           |       |         |           |             |
| ExistsAsync_False             | ShortRun   | Default         | 3              | 1           | 16           | 3           |   284.9 μs |    188.71 μs |  10.34 μs |  0.92 |    0.03 |     584 B |        0.52 |
| ExistsAsync_True              | ShortRun   | Default         | 3              | 1           | 16           | 3           |   279.9 μs |    214.50 μs |  11.76 μs |  0.90 |    0.04 |     592 B |        0.52 |
| GetAsync_CacheHit             | ShortRun   | Default         | 3              | 1           | 16           | 3           |   310.5 μs |    135.70 μs |   7.44 μs |  1.00 |    0.03 |    1128 B |        1.00 |
| GetAsync_CacheMiss            | ShortRun   | Default         | 3              | 1           | 16           | 3           |   285.2 μs |    299.77 μs |  16.43 μs |  0.92 |    0.05 |     640 B |        0.57 |
| GetOrSetAsync_CacheHit        | ShortRun   | Default         | 3              | 1           | 16           | 3           |   306.2 μs |    291.59 μs |  15.98 μs |  0.99 |    0.05 |    1432 B |        1.27 |
| SetAsync                      | ShortRun   | Default         | 3              | 1           | 16           | 3           |   309.4 μs |    215.18 μs |  11.79 μs |  1.00 |    0.04 |     872 B |        0.77 |
| SetWithSlidingExpirationAsync | ShortRun   | Default         | 3              | 1           | 16           | 3           |   595.0 μs |    368.33 μs |  20.19 μs |  1.92 |    0.07 |    1496 B |        1.33 |
|                               |            |                 |                |             |              |             |            |              |           |       |         |           |             |
| GetOrSetAsync_CacheMiss       | Job-ZDPOZY | 1               | 20             | Default     | 1            | 5           | 1,228.9 μs |    279.37 μs | 298.92 μs |     ? |       ? |    3464 B |           ? |
| RemoveAsync                   | Job-ZDPOZY | 1               | 20             | Default     | 1            | 5           |   315.2 μs |     36.11 μs |  37.08 μs |     ? |       ? |    1072 B |           ? |
| RemoveByPatternAsync          | Job-ZDPOZY | 1               | 20             | Default     | 1            | 5           | 1,806.6 μs |    326.47 μs | 375.96 μs |     ? |       ? |    3416 B |           ? |
|                               |            |                 |                |             |              |             |            |              |           |       |         |           |             |
| GetOrSetAsync_CacheMiss       | ShortRun   | 1               | 3              | 1           | 1            | 3           | 1,045.9 μs |    795.48 μs |  43.60 μs |     ? |       ? |    3464 B |           ? |
| RemoveAsync                   | ShortRun   | 1               | 3              | 1           | 1            | 3           |   313.8 μs |    467.18 μs |  25.61 μs |     ? |       ? |    1072 B |           ? |
| RemoveByPatternAsync          | ShortRun   | 1               | 3              | 1           | 1            | 3           | 2,083.1 μs | 10,334.04 μs | 566.44 μs |     ? |       ? |    3416 B |           ? |
