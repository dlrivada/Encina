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
| ExistsAsync_False             | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |   252.7 μs |     1.02 μs |   1.17 μs |  0.96 |    0.01 |     584 B |        0.52 |
| ExistsAsync_True              | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |   252.3 μs |     0.72 μs |   0.78 μs |  0.95 |    0.00 |     592 B |        0.52 |
| GetAsync_CacheHit             | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |   264.5 μs |     0.79 μs |   0.84 μs |  1.00 |    0.00 |    1128 B |        1.00 |
| GetAsync_CacheMiss            | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |   254.2 μs |     1.17 μs |   1.30 μs |  0.96 |    0.01 |     640 B |        0.57 |
| GetOrSetAsync_CacheHit        | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |   264.5 μs |     0.70 μs |   0.72 μs |  1.00 |    0.00 |    1432 B |        1.27 |
| SetAsync                      | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |   270.0 μs |     1.59 μs |   1.63 μs |  1.02 |    0.01 |     872 B |        0.77 |
| SetWithSlidingExpirationAsync | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |   527.1 μs |     2.31 μs |   2.57 μs |  1.99 |    0.01 |    1496 B |        1.33 |
|                               |            |                 |                |             |              |             |            |             |           |       |         |           |             |
| ExistsAsync_False             | ShortRun   | Default         | 3              | 1           | 16           | 3           |   253.0 μs |    19.96 μs |   1.09 μs |  0.95 |    0.00 |     584 B |        0.52 |
| ExistsAsync_True              | ShortRun   | Default         | 3              | 1           | 16           | 3           |   253.2 μs |    12.87 μs |   0.71 μs |  0.95 |    0.00 |     592 B |        0.52 |
| GetAsync_CacheHit             | ShortRun   | Default         | 3              | 1           | 16           | 3           |   265.6 μs |    16.20 μs |   0.89 μs |  1.00 |    0.00 |    1128 B |        1.00 |
| GetAsync_CacheMiss            | ShortRun   | Default         | 3              | 1           | 16           | 3           |   253.2 μs |    12.48 μs |   0.68 μs |  0.95 |    0.00 |     640 B |        0.57 |
| GetOrSetAsync_CacheHit        | ShortRun   | Default         | 3              | 1           | 16           | 3           |   264.0 μs |    22.68 μs |   1.24 μs |  0.99 |    0.00 |    1432 B |        1.27 |
| SetAsync                      | ShortRun   | Default         | 3              | 1           | 16           | 3           |   270.4 μs |    27.51 μs |   1.51 μs |  1.02 |    0.01 |     872 B |        0.77 |
| SetWithSlidingExpirationAsync | ShortRun   | Default         | 3              | 1           | 16           | 3           |   524.7 μs |    29.37 μs |   1.61 μs |  1.98 |    0.01 |    1496 B |        1.33 |
|                               |            |                 |                |             |              |             |            |             |           |       |         |           |             |
| GetOrSetAsync_CacheMiss       | Job-ZDPOZY | 1               | 20             | Default     | 1            | 5           | 1,158.9 μs |    46.13 μs |  51.27 μs |     ? |       ? |    3000 B |           ? |
| RemoveAsync                   | Job-ZDPOZY | 1               | 20             | Default     | 1            | 5           |   307.6 μs |    29.79 μs |  31.87 μs |     ? |       ? |     608 B |           ? |
| RemoveByPatternAsync          | Job-ZDPOZY | 1               | 20             | Default     | 1            | 5           | 1,604.6 μs |    70.47 μs |  81.16 μs |     ? |       ? |    2952 B |           ? |
|                               |            |                 |                |             |              |             |            |             |           |       |         |           |             |
| GetOrSetAsync_CacheMiss       | ShortRun   | 1               | 3              | 1           | 1            | 3           | 1,157.8 μs |   376.33 μs |  20.63 μs |     ? |       ? |    3000 B |           ? |
| RemoveAsync                   | ShortRun   | 1               | 3              | 1           | 1            | 3           |   329.6 μs | 1,290.70 μs |  70.75 μs |     ? |       ? |    8824 B |           ? |
| RemoveByPatternAsync          | ShortRun   | 1               | 3              | 1           | 1            | 3           | 1,742.5 μs | 4,244.26 μs | 232.64 μs |     ? |       ? |    2952 B |           ? |
