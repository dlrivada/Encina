```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 2.85GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  Job-NUBXJZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  Job-ZDPOZY : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3


```
| Method                        | Job        | InvocationCount | IterationCount | LaunchCount | UnrollFactor | WarmupCount | Mean       | Error     | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------ |----------- |---------------- |--------------- |------------ |------------- |------------ |-----------:|----------:|----------:|------:|--------:|----------:|------------:|
| ExistsAsync_False             | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |   189.8 μs |   0.90 μs |   1.04 μs |  0.92 |    0.01 |     584 B |        0.52 |
| ExistsAsync_True              | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |   189.4 μs |   1.11 μs |   1.24 μs |  0.92 |    0.01 |     591 B |        0.52 |
| GetAsync_CacheHit             | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |   205.9 μs |   1.54 μs |   1.65 μs |  1.00 |    0.01 |    1128 B |        1.00 |
| GetAsync_CacheMiss            | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |   194.2 μs |   0.84 μs |   0.90 μs |  0.94 |    0.01 |     640 B |        0.57 |
| GetOrSetAsync_CacheHit        | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |   208.5 μs |   1.16 μs |   1.14 μs |  1.01 |    0.01 |    1432 B |        1.27 |
| SetAsync                      | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |   210.1 μs |   0.74 μs |   0.76 μs |  1.02 |    0.01 |     872 B |        0.77 |
| SetWithSlidingExpirationAsync | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |   414.3 μs |   1.57 μs |   1.75 μs |  2.01 |    0.02 |    1496 B |        1.33 |
|                               |            |                 |                |             |              |             |            |           |           |       |         |           |             |
| ExistsAsync_False             | MediumRun  | Default         | 15             | 2           | 16           | 10          |   191.1 μs |   0.81 μs |   1.21 μs |  0.92 |    0.01 |     584 B |        0.52 |
| ExistsAsync_True              | MediumRun  | Default         | 15             | 2           | 16           | 10          |   190.3 μs |   0.80 μs |   1.15 μs |  0.92 |    0.01 |     592 B |        0.52 |
| GetAsync_CacheHit             | MediumRun  | Default         | 15             | 2           | 16           | 10          |   206.6 μs |   0.86 μs |   1.29 μs |  1.00 |    0.01 |    1128 B |        1.00 |
| GetAsync_CacheMiss            | MediumRun  | Default         | 15             | 2           | 16           | 10          |   193.6 μs |   0.79 μs |   1.18 μs |  0.94 |    0.01 |     640 B |        0.57 |
| GetOrSetAsync_CacheHit        | MediumRun  | Default         | 15             | 2           | 16           | 10          |   208.3 μs |   0.88 μs |   1.29 μs |  1.01 |    0.01 |    1432 B |        1.27 |
| SetAsync                      | MediumRun  | Default         | 15             | 2           | 16           | 10          |   211.3 μs |   0.91 μs |   1.34 μs |  1.02 |    0.01 |     872 B |        0.77 |
| SetWithSlidingExpirationAsync | MediumRun  | Default         | 15             | 2           | 16           | 10          |   416.1 μs |   1.19 μs |   1.75 μs |  2.01 |    0.01 |    1496 B |        1.33 |
|                               |            |                 |                |             |              |             |            |           |           |       |         |           |             |
| GetOrSetAsync_CacheMiss       | Job-ZDPOZY | 1               | 20             | Default     | 1            | 5           |   942.3 μs |  21.97 μs |  23.51 μs |     ? |       ? |    3464 B |           ? |
| RemoveAsync                   | Job-ZDPOZY | 1               | 20             | Default     | 1            | 5           |   273.4 μs |  14.35 μs |  14.10 μs |     ? |       ? |    1072 B |           ? |
| RemoveByPatternAsync          | Job-ZDPOZY | 1               | 20             | Default     | 1            | 5           | 1,611.4 μs | 398.16 μs | 442.56 μs |     ? |       ? |    3416 B |           ? |
|                               |            |                 |                |             |              |             |            |           |           |       |         |           |             |
| GetOrSetAsync_CacheMiss       | MediumRun  | 1               | 15             | 2           | 1            | 10          | 1,030.9 μs | 202.94 μs | 284.49 μs |     ? |       ? |    3464 B |           ? |
| RemoveAsync                   | MediumRun  | 1               | 15             | 2           | 1            | 10          |   304.2 μs |  42.70 μs |  58.45 μs |     ? |       ? |    1104 B |           ? |
| RemoveByPatternAsync          | MediumRun  | 1               | 15             | 2           | 1            | 10          | 1,247.1 μs |  24.08 μs |  35.29 μs |     ? |       ? |    3416 B |           ? |
