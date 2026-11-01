```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  Job-NUBXJZ : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  MediumRun  : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  Job-ZDPOZY : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4


```
| Method                        | Job        | InvocationCount | IterationCount | LaunchCount | UnrollFactor | WarmupCount | Mean     | Error    | StdDev   | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------ |----------- |---------------- |--------------- |------------ |------------- |------------ |---------:|---------:|---------:|------:|--------:|----------:|------------:|
| ExistsAsync_False             | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           | 147.6 μs |  1.79 μs |  1.99 μs |  0.94 |    0.01 |     584 B |        0.52 |
| ExistsAsync_True              | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           | 147.0 μs |  0.90 μs |  1.00 μs |  0.94 |    0.01 |     592 B |        0.52 |
| GetAsync_CacheHit             | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           | 156.7 μs |  0.49 μs |  0.52 μs |  1.00 |    0.00 |    1128 B |        1.00 |
| GetAsync_CacheMiss            | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           | 151.9 μs |  0.76 μs |  0.82 μs |  0.97 |    0.01 |     640 B |        0.57 |
| GetOrSetAsync_CacheHit        | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           | 158.7 μs |  0.94 μs |  1.01 μs |  1.01 |    0.01 |    1432 B |        1.27 |
| SetAsync                      | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           | 164.9 μs |  1.30 μs |  1.39 μs |  1.05 |    0.01 |     872 B |        0.77 |
| SetWithSlidingExpirationAsync | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           | 317.6 μs |  1.07 μs |  1.14 μs |  2.03 |    0.01 |    1496 B |        1.33 |
|                               |            |                 |                |             |              |             |          |          |          |       |         |           |             |
| ExistsAsync_False             | MediumRun  | Default         | 15             | 2           | 16           | 10          | 147.9 μs |  0.80 μs |  1.14 μs |  0.93 |    0.01 |     584 B |        0.52 |
| ExistsAsync_True              | MediumRun  | Default         | 15             | 2           | 16           | 10          | 149.7 μs |  0.62 μs |  0.89 μs |  0.94 |    0.01 |     592 B |        0.52 |
| GetAsync_CacheHit             | MediumRun  | Default         | 15             | 2           | 16           | 10          | 158.8 μs |  0.83 μs |  1.18 μs |  1.00 |    0.01 |    1128 B |        1.00 |
| GetAsync_CacheMiss            | MediumRun  | Default         | 15             | 2           | 16           | 10          | 151.4 μs |  0.74 μs |  1.06 μs |  0.95 |    0.01 |     640 B |        0.57 |
| GetOrSetAsync_CacheHit        | MediumRun  | Default         | 15             | 2           | 16           | 10          | 158.4 μs |  0.65 μs |  0.93 μs |  1.00 |    0.01 |    1432 B |        1.27 |
| SetAsync                      | MediumRun  | Default         | 15             | 2           | 16           | 10          | 163.9 μs |  0.67 μs |  0.96 μs |  1.03 |    0.01 |     872 B |        0.77 |
| SetWithSlidingExpirationAsync | MediumRun  | Default         | 15             | 2           | 16           | 10          | 316.5 μs |  1.46 μs |  2.14 μs |  1.99 |    0.02 |    1496 B |        1.33 |
|                               |            |                 |                |             |              |             |          |          |          |       |         |           |             |
| GetOrSetAsync_CacheMiss       | Job-ZDPOZY | 1               | 20             | Default     | 1            | 5           | 706.2 μs | 19.55 μs | 21.73 μs |     ? |       ? |    3464 B |           ? |
| RemoveAsync                   | Job-ZDPOZY | 1               | 20             | Default     | 1            | 5           | 210.3 μs |  9.54 μs | 10.99 μs |     ? |       ? |    1072 B |           ? |
| RemoveByPatternAsync          | Job-ZDPOZY | 1               | 20             | Default     | 1            | 5           | 953.8 μs | 21.50 μs | 23.90 μs |     ? |       ? |    3416 B |           ? |
|                               |            |                 |                |             |              |             |          |          |          |       |         |           |             |
| GetOrSetAsync_CacheMiss       | MediumRun  | 1               | 15             | 2           | 1            | 10          | 717.7 μs | 16.70 μs | 23.42 μs |     ? |       ? |    3464 B |           ? |
| RemoveAsync                   | MediumRun  | 1               | 15             | 2           | 1            | 10          | 208.2 μs |  8.87 μs | 13.00 μs |     ? |       ? |    1072 B |           ? |
| RemoveByPatternAsync          | MediumRun  | 1               | 15             | 2           | 1            | 10          | 962.1 μs | 12.54 μs | 18.77 μs |     ? |       ? |    3416 B |           ? |
