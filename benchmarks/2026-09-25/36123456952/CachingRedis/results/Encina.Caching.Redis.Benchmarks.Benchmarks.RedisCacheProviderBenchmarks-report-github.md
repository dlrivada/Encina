```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-NUBXJZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-ZDPOZY : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3


```
| Method                        | Job        | InvocationCount | IterationCount | LaunchCount | UnrollFactor | WarmupCount | Mean       | Error       | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------ |----------- |---------------- |--------------- |------------ |------------- |------------ |-----------:|------------:|----------:|------:|--------:|----------:|------------:|
| ExistsAsync_False             | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |   260.0 μs |     0.98 μs |   1.13 μs |  0.95 |    0.01 |     584 B |        0.52 |
| ExistsAsync_True              | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |   260.4 μs |     1.47 μs |   1.63 μs |  0.95 |    0.01 |     592 B |        0.52 |
| GetAsync_CacheHit             | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |   273.4 μs |     1.39 μs |   1.48 μs |  1.00 |    0.01 |    1128 B |        1.00 |
| GetAsync_CacheMiss            | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |   263.1 μs |     1.57 μs |   1.68 μs |  0.96 |    0.01 |     640 B |        0.57 |
| GetOrSetAsync_CacheHit        | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |   274.4 μs |     1.33 μs |   1.42 μs |  1.00 |    0.01 |    1432 B |        1.27 |
| SetAsync                      | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |   280.0 μs |     1.34 μs |   1.49 μs |  1.02 |    0.01 |     872 B |        0.77 |
| SetWithSlidingExpirationAsync | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |   548.1 μs |     1.72 μs |   1.91 μs |  2.01 |    0.01 |    1496 B |        1.33 |
|                               |            |                 |                |             |              |             |            |             |           |       |         |           |             |
| ExistsAsync_False             | ShortRun   | Default         | 3              | 1           | 16           | 3           |   257.7 μs |    27.88 μs |   1.53 μs |  0.95 |    0.01 |     584 B |        0.52 |
| ExistsAsync_True              | ShortRun   | Default         | 3              | 1           | 16           | 3           |   260.5 μs |    37.52 μs |   2.06 μs |  0.96 |    0.01 |     592 B |        0.52 |
| GetAsync_CacheHit             | ShortRun   | Default         | 3              | 1           | 16           | 3           |   271.0 μs |    44.11 μs |   2.42 μs |  1.00 |    0.01 |    1128 B |        1.00 |
| GetAsync_CacheMiss            | ShortRun   | Default         | 3              | 1           | 16           | 3           |   261.4 μs |    21.57 μs |   1.18 μs |  0.96 |    0.01 |     640 B |        0.57 |
| GetOrSetAsync_CacheHit        | ShortRun   | Default         | 3              | 1           | 16           | 3           |   274.3 μs |    68.77 μs |   3.77 μs |  1.01 |    0.01 |    1432 B |        1.27 |
| SetAsync                      | ShortRun   | Default         | 3              | 1           | 16           | 3           |   281.8 μs |    17.86 μs |   0.98 μs |  1.04 |    0.01 |     872 B |        0.77 |
| SetWithSlidingExpirationAsync | ShortRun   | Default         | 3              | 1           | 16           | 3           |   548.5 μs |    96.81 μs |   5.31 μs |  2.02 |    0.02 |    1496 B |        1.33 |
|                               |            |                 |                |             |              |             |            |             |           |       |         |           |             |
| GetOrSetAsync_CacheMiss       | Job-ZDPOZY | 1               | 20             | Default     | 1            | 5           | 1,043.1 μs |    31.41 μs |  36.17 μs |     ? |       ? |    3000 B |           ? |
| RemoveAsync                   | Job-ZDPOZY | 1               | 20             | Default     | 1            | 5           |   284.3 μs |    12.96 μs |  14.93 μs |     ? |       ? |     608 B |           ? |
| RemoveByPatternAsync          | Job-ZDPOZY | 1               | 20             | Default     | 1            | 5           | 1,550.4 μs |    49.95 μs |  55.52 μs |     ? |       ? |    2952 B |           ? |
|                               |            |                 |                |             |              |             |            |             |           |       |         |           |             |
| GetOrSetAsync_CacheMiss       | ShortRun   | 1               | 3              | 1           | 1            | 3           | 1,100.2 μs |    75.95 μs |   4.16 μs |     ? |       ? |    3000 B |           ? |
| RemoveAsync                   | ShortRun   | 1               | 3              | 1           | 1            | 3           |   302.0 μs |   290.20 μs |  15.91 μs |     ? |       ? |     640 B |           ? |
| RemoveByPatternAsync          | ShortRun   | 1               | 3              | 1           | 1            | 3           | 1,606.5 μs | 2,685.97 μs | 147.23 μs |     ? |       ? |    3248 B |           ? |
