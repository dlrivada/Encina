```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.203
  [Host]     : .NET 10.0.7 (10.0.7, 10.0.726.21808), X64 RyuJIT x86-64-v3
  Job-NUBXJZ : .NET 10.0.7 (10.0.7, 10.0.726.21808), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.7 (10.0.7, 10.0.726.21808), X64 RyuJIT x86-64-v3
  Job-ZDPOZY : .NET 10.0.7 (10.0.7, 10.0.726.21808), X64 RyuJIT x86-64-v3


```
| Method                        | Job        | InvocationCount | IterationCount | LaunchCount | UnrollFactor | WarmupCount | Mean       | Error    | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------ |----------- |---------------- |--------------- |------------ |------------- |------------ |-----------:|---------:|----------:|------:|--------:|----------:|------------:|
| ExistsAsync_False             | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |   260.6 μs |  1.22 μs |   1.40 μs |  0.97 |    0.01 |     584 B |        0.52 |
| ExistsAsync_True              | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |   260.2 μs |  1.30 μs |   1.45 μs |  0.97 |    0.01 |     592 B |        0.52 |
| GetAsync_CacheHit             | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |   269.1 μs |  1.52 μs |   1.69 μs |  1.00 |    0.01 |    1128 B |        1.00 |
| GetAsync_CacheMiss            | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |   264.9 μs |  2.22 μs |   2.47 μs |  0.98 |    0.01 |     640 B |        0.57 |
| GetOrSetAsync_CacheHit        | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |   273.4 μs |  2.23 μs |   2.48 μs |  1.02 |    0.01 |    1432 B |        1.27 |
| SetAsync                      | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |   277.4 μs |  1.64 μs |   1.83 μs |  1.03 |    0.01 |     872 B |        0.77 |
| SetWithSlidingExpirationAsync | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |   547.0 μs |  3.98 μs |   4.59 μs |  2.03 |    0.02 |    1496 B |        1.33 |
|                               |            |                 |                |             |              |             |            |          |           |       |         |           |             |
| ExistsAsync_False             | MediumRun  | Default         | 15             | 2           | 16           | 10          |   260.8 μs |  1.07 μs |   1.53 μs |  0.96 |    0.01 |     584 B |        0.52 |
| ExistsAsync_True              | MediumRun  | Default         | 15             | 2           | 16           | 10          |   260.5 μs |  0.74 μs |   1.02 μs |  0.96 |    0.01 |     592 B |        0.52 |
| GetAsync_CacheHit             | MediumRun  | Default         | 15             | 2           | 16           | 10          |   270.3 μs |  1.49 μs |   2.14 μs |  1.00 |    0.01 |    1128 B |        1.00 |
| GetAsync_CacheMiss            | MediumRun  | Default         | 15             | 2           | 16           | 10          |   264.7 μs |  1.15 μs |   1.65 μs |  0.98 |    0.01 |     640 B |        0.57 |
| GetOrSetAsync_CacheHit        | MediumRun  | Default         | 15             | 2           | 16           | 10          |   273.5 μs |  1.58 μs |   2.27 μs |  1.01 |    0.01 |    1432 B |        1.27 |
| SetAsync                      | MediumRun  | Default         | 15             | 2           | 16           | 10          |   279.2 μs |  1.42 μs |   2.04 μs |  1.03 |    0.01 |     872 B |        0.77 |
| SetWithSlidingExpirationAsync | MediumRun  | Default         | 15             | 2           | 16           | 10          |   543.9 μs |  2.45 μs |   3.51 μs |  2.01 |    0.02 |    1496 B |        1.33 |
|                               |            |                 |                |             |              |             |            |          |           |       |         |           |             |
| GetOrSetAsync_CacheMiss       | Job-ZDPOZY | 1               | 20             | Default     | 1            | 5           | 1,232.1 μs | 69.11 μs |  79.58 μs |     ? |       ? |    3000 B |           ? |
| RemoveAsync                   | Job-ZDPOZY | 1               | 20             | Default     | 1            | 5           |   386.4 μs | 35.66 μs |  38.16 μs |     ? |       ? |     608 B |           ? |
| RemoveByPatternAsync          | Job-ZDPOZY | 1               | 20             | Default     | 1            | 5           | 1,608.0 μs | 61.07 μs |  67.88 μs |     ? |       ? |    2952 B |           ? |
|                               |            |                 |                |             |              |             |            |          |           |       |         |           |             |
| GetOrSetAsync_CacheMiss       | MediumRun  | 1               | 15             | 2           | 1            | 10          | 1,162.7 μs | 45.76 μs |  67.07 μs |     ? |       ? |    3000 B |           ? |
| RemoveAsync                   | MediumRun  | 1               | 15             | 2           | 1            | 10          |   332.3 μs | 23.91 μs |  35.79 μs |     ? |       ? |     608 B |           ? |
| RemoveByPatternAsync          | MediumRun  | 1               | 15             | 2           | 1            | 10          | 1,569.5 μs | 69.94 μs | 100.31 μs |     ? |       ? |    2952 B |           ? |
