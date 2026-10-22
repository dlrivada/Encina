```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.94GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  Job-NUBXJZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  Job-ZDPOZY : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3


```
| Method                        | Job        | InvocationCount | IterationCount | LaunchCount | UnrollFactor | WarmupCount | Mean       | Error       | StdDev    | Median     | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------ |----------- |---------------- |--------------- |------------ |------------- |------------ |-----------:|------------:|----------:|-----------:|------:|--------:|----------:|------------:|
| ExistsAsync_False             | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |   263.7 μs |     1.67 μs |   1.86 μs |   263.1 μs |  0.96 |    0.01 |     584 B |        0.52 |
| ExistsAsync_True              | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |   260.9 μs |     1.15 μs |   1.28 μs |   260.8 μs |  0.95 |    0.01 |     592 B |        0.52 |
| GetAsync_CacheHit             | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |   274.5 μs |     1.98 μs |   2.20 μs |   274.3 μs |  1.00 |    0.01 |    1128 B |        1.00 |
| GetAsync_CacheMiss            | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |   267.3 μs |     1.38 μs |   1.47 μs |   266.9 μs |  0.97 |    0.01 |     640 B |        0.57 |
| GetOrSetAsync_CacheHit        | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |   278.3 μs |     1.73 μs |   1.92 μs |   278.3 μs |  1.01 |    0.01 |    1432 B |        1.27 |
| SetAsync                      | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |   282.3 μs |     2.85 μs |   3.17 μs |   281.9 μs |  1.03 |    0.01 |     872 B |        0.77 |
| SetWithSlidingExpirationAsync | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |   549.9 μs |     1.66 μs |   1.71 μs |   550.2 μs |  2.00 |    0.02 |    1496 B |        1.33 |
|                               |            |                 |                |             |              |             |            |             |           |            |       |         |           |             |
| ExistsAsync_False             | ShortRun   | Default         | 3              | 1           | 16           | 3           |   261.3 μs |    24.84 μs |   1.36 μs |   261.9 μs |  0.94 |    0.01 |     584 B |        0.52 |
| ExistsAsync_True              | ShortRun   | Default         | 3              | 1           | 16           | 3           |   262.5 μs |    31.15 μs |   1.71 μs |   262.6 μs |  0.95 |    0.01 |     592 B |        0.52 |
| GetAsync_CacheHit             | ShortRun   | Default         | 3              | 1           | 16           | 3           |   277.5 μs |    17.93 μs |   0.98 μs |   277.4 μs |  1.00 |    0.00 |    1128 B |        1.00 |
| GetAsync_CacheMiss            | ShortRun   | Default         | 3              | 1           | 16           | 3           |   264.5 μs |     5.37 μs |   0.29 μs |   264.5 μs |  0.95 |    0.00 |     640 B |        0.57 |
| GetOrSetAsync_CacheHit        | ShortRun   | Default         | 3              | 1           | 16           | 3           |   278.5 μs |    24.49 μs |   1.34 μs |   279.2 μs |  1.00 |    0.01 |    1432 B |        1.27 |
| SetAsync                      | ShortRun   | Default         | 3              | 1           | 16           | 3           |   280.9 μs |     9.85 μs |   0.54 μs |   280.7 μs |  1.01 |    0.00 |     872 B |        0.77 |
| SetWithSlidingExpirationAsync | ShortRun   | Default         | 3              | 1           | 16           | 3           |   550.9 μs |   108.29 μs |   5.94 μs |   548.6 μs |  1.99 |    0.02 |    1496 B |        1.33 |
|                               |            |                 |                |             |              |             |            |             |           |            |       |         |           |             |
| GetOrSetAsync_CacheMiss       | Job-ZDPOZY | 1               | 20             | Default     | 1            | 5           | 1,058.1 μs |    30.50 μs |  33.90 μs | 1,058.4 μs |     ? |       ? |    3464 B |           ? |
| RemoveAsync                   | Job-ZDPOZY | 1               | 20             | Default     | 1            | 5           |   285.9 μs |    15.57 μs |  17.93 μs |   280.6 μs |     ? |       ? |    1072 B |           ? |
| RemoveByPatternAsync          | Job-ZDPOZY | 1               | 20             | Default     | 1            | 5           | 1,511.9 μs |    48.00 μs |  53.36 μs | 1,504.4 μs |     ? |       ? |    3416 B |           ? |
|                               |            |                 |                |             |              |             |            |             |           |            |       |         |           |             |
| GetOrSetAsync_CacheMiss       | ShortRun   | 1               | 3              | 1           | 1            | 3           | 1,068.0 μs |   673.81 μs |  36.93 μs | 1,062.1 μs |     ? |       ? |    3464 B |           ? |
| RemoveAsync                   | ShortRun   | 1               | 3              | 1           | 1            | 3           |   576.3 μs | 9,047.40 μs | 495.92 μs |   308.1 μs |     ? |       ? |    1136 B |           ? |
| RemoveByPatternAsync          | ShortRun   | 1               | 3              | 1           | 1            | 3           | 1,496.8 μs |   375.73 μs |  20.60 μs | 1,489.9 μs |     ? |       ? |    3712 B |           ? |
