```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.87GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  Job-NUBXJZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  Job-ZDPOZY : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3


```
| Method                        | Job        | InvocationCount | IterationCount | LaunchCount | UnrollFactor | WarmupCount | Mean       | Error     | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------ |----------- |---------------- |--------------- |------------ |------------- |------------ |-----------:|----------:|----------:|------:|--------:|----------:|------------:|
| ExistsAsync_False             | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |   187.7 μs |   1.25 μs |   1.33 μs |  0.90 |    0.01 |     584 B |        0.52 |
| ExistsAsync_True              | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |   190.8 μs |   4.66 μs |   5.18 μs |  0.91 |    0.03 |     592 B |        0.52 |
| GetAsync_CacheHit             | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |   209.1 μs |   2.30 μs |   2.36 μs |  1.00 |    0.02 |    1128 B |        1.00 |
| GetAsync_CacheMiss            | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |   190.3 μs |   1.87 μs |   1.92 μs |  0.91 |    0.01 |     640 B |        0.57 |
| GetOrSetAsync_CacheHit        | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |   208.6 μs |   2.10 μs |   2.16 μs |  1.00 |    0.01 |    1432 B |        1.27 |
| SetAsync                      | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |   213.2 μs |   4.60 μs |   5.12 μs |  1.02 |    0.03 |     872 B |        0.77 |
| SetWithSlidingExpirationAsync | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |   414.6 μs |   4.72 μs |   5.05 μs |  1.98 |    0.03 |    1496 B |        1.33 |
|                               |            |                 |                |             |              |             |            |           |           |       |         |           |             |
| ExistsAsync_False             | MediumRun  | Default         | 15             | 2           | 16           | 10          |   194.6 μs |   4.76 μs |   6.98 μs |  0.92 |    0.07 |     584 B |        0.52 |
| ExistsAsync_True              | MediumRun  | Default         | 15             | 2           | 16           | 10          |   195.0 μs |   4.41 μs |   6.46 μs |  0.92 |    0.06 |     592 B |        0.52 |
| GetAsync_CacheHit             | MediumRun  | Default         | 15             | 2           | 16           | 10          |   213.0 μs |  10.13 μs |  14.84 μs |  1.00 |    0.09 |    1128 B |        1.00 |
| GetAsync_CacheMiss            | MediumRun  | Default         | 15             | 2           | 16           | 10          |   191.5 μs |   1.59 μs |   2.18 μs |  0.90 |    0.06 |     640 B |        0.57 |
| GetOrSetAsync_CacheHit        | MediumRun  | Default         | 15             | 2           | 16           | 10          |   210.9 μs |   5.38 μs |   7.72 μs |  0.99 |    0.07 |    1432 B |        1.27 |
| SetAsync                      | MediumRun  | Default         | 15             | 2           | 16           | 10          |   211.7 μs |   2.88 μs |   4.13 μs |  1.00 |    0.06 |     872 B |        0.77 |
| SetWithSlidingExpirationAsync | MediumRun  | Default         | 15             | 2           | 16           | 10          |   417.9 μs |   5.16 μs |   7.57 μs |  1.97 |    0.13 |    1496 B |        1.33 |
|                               |            |                 |                |             |              |             |            |           |           |       |         |           |             |
| GetOrSetAsync_CacheMiss       | Job-ZDPOZY | 1               | 20             | Default     | 1            | 5           |   946.2 μs |  42.71 μs |  43.86 μs |     ? |       ? |    3464 B |           ? |
| RemoveAsync                   | Job-ZDPOZY | 1               | 20             | Default     | 1            | 5           |   295.7 μs |  33.07 μs |  33.96 μs |     ? |       ? |    1072 B |           ? |
| RemoveByPatternAsync          | Job-ZDPOZY | 1               | 20             | Default     | 1            | 5           | 1,323.5 μs | 102.83 μs | 100.99 μs |     ? |       ? |    3416 B |           ? |
|                               |            |                 |                |             |              |             |            |           |           |       |         |           |             |
| GetOrSetAsync_CacheMiss       | MediumRun  | 1               | 15             | 2           | 1            | 10          |   932.5 μs |  20.05 μs |  28.75 μs |     ? |       ? |    3464 B |           ? |
| RemoveAsync                   | MediumRun  | 1               | 15             | 2           | 1            | 10          |   280.9 μs |  21.95 μs |  30.77 μs |     ? |       ? |    1072 B |           ? |
| RemoveByPatternAsync          | MediumRun  | 1               | 15             | 2           | 1            | 10          | 1,267.7 μs |  32.65 μs |  46.83 μs |     ? |       ? |    3416 B |           ? |
