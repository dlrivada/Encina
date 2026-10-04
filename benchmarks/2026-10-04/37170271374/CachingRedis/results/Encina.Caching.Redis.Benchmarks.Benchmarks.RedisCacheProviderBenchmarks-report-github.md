```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-NUBXJZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  MediumRun  : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-ZDPOZY : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4


```
| Method                        | Job        | InvocationCount | IterationCount | LaunchCount | UnrollFactor | WarmupCount | Mean       | Error    | StdDev   | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------ |----------- |---------------- |--------------- |------------ |------------- |------------ |-----------:|---------:|---------:|------:|--------:|----------:|------------:|
| ExistsAsync_False             | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |   139.0 μs |  2.08 μs |  2.13 μs |  0.93 |    0.03 |     584 B |        0.52 |
| ExistsAsync_True              | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |   144.2 μs |  4.74 μs |  5.46 μs |  0.96 |    0.04 |     592 B |        0.52 |
| GetAsync_CacheHit             | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |   149.7 μs |  3.29 μs |  3.52 μs |  1.00 |    0.03 |    1128 B |        1.00 |
| GetAsync_CacheMiss            | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |   135.5 μs |  1.15 μs |  1.23 μs |  0.91 |    0.02 |     640 B |        0.57 |
| GetOrSetAsync_CacheHit        | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |   148.5 μs |  2.98 μs |  3.43 μs |  0.99 |    0.03 |    1432 B |        1.27 |
| SetAsync                      | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |   149.4 μs |  5.37 μs |  6.19 μs |  1.00 |    0.05 |     872 B |        0.77 |
| SetWithSlidingExpirationAsync | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |   298.5 μs | 10.79 μs | 12.43 μs |  1.99 |    0.09 |    1496 B |        1.33 |
|                               |            |                 |                |             |              |             |            |          |          |       |         |           |             |
| ExistsAsync_False             | MediumRun  | Default         | 15             | 2           | 16           | 10          |   145.3 μs |  3.90 μs |  5.84 μs |  1.00 |    0.04 |     584 B |        0.52 |
| ExistsAsync_True              | MediumRun  | Default         | 15             | 2           | 16           | 10          |   146.2 μs |  4.80 μs |  7.18 μs |  1.01 |    0.05 |     592 B |        0.52 |
| GetAsync_CacheHit             | MediumRun  | Default         | 15             | 2           | 16           | 10          |   145.4 μs |  1.94 μs |  2.72 μs |  1.00 |    0.03 |    1128 B |        1.00 |
| GetAsync_CacheMiss            | MediumRun  | Default         | 15             | 2           | 16           | 10          |   143.7 μs |  3.91 μs |  5.85 μs |  0.99 |    0.04 |     640 B |        0.57 |
| GetOrSetAsync_CacheHit        | MediumRun  | Default         | 15             | 2           | 16           | 10          |   146.2 μs |  1.20 μs |  1.80 μs |  1.01 |    0.02 |    1432 B |        1.27 |
| SetAsync                      | MediumRun  | Default         | 15             | 2           | 16           | 10          |   150.4 μs |  3.09 μs |  4.63 μs |  1.03 |    0.04 |     872 B |        0.77 |
| SetWithSlidingExpirationAsync | MediumRun  | Default         | 15             | 2           | 16           | 10          |   291.6 μs |  5.26 μs |  7.88 μs |  2.01 |    0.06 |    1496 B |        1.33 |
|                               |            |                 |                |             |              |             |            |          |          |       |         |           |             |
| GetOrSetAsync_CacheMiss       | Job-ZDPOZY | 1               | 20             | Default     | 1            | 5           |   788.1 μs | 38.52 μs | 41.22 μs |     ? |       ? |    3000 B |           ? |
| RemoveAsync                   | Job-ZDPOZY | 1               | 20             | Default     | 1            | 5           |   250.8 μs | 20.79 μs | 23.94 μs |     ? |       ? |     608 B |           ? |
| RemoveByPatternAsync          | Job-ZDPOZY | 1               | 20             | Default     | 1            | 5           | 1,097.6 μs | 51.44 μs | 55.04 μs |     ? |       ? |    2952 B |           ? |
|                               |            |                 |                |             |              |             |            |          |          |       |         |           |             |
| GetOrSetAsync_CacheMiss       | MediumRun  | 1               | 15             | 2           | 1            | 10          |   817.8 μs | 36.75 μs | 52.71 μs |     ? |       ? |    3000 B |           ? |
| RemoveAsync                   | MediumRun  | 1               | 15             | 2           | 1            | 10          |   238.5 μs | 10.66 μs | 14.59 μs |     ? |       ? |     608 B |           ? |
| RemoveByPatternAsync          | MediumRun  | 1               | 15             | 2           | 1            | 10          | 1,067.8 μs | 32.57 μs | 47.75 μs |     ? |       ? |    2952 B |           ? |
