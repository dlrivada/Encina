```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  Job-NUBXJZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  Job-ZDPOZY : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4


```
| Method                        | Job        | InvocationCount | IterationCount | LaunchCount | UnrollFactor | WarmupCount | Mean       | Error       | StdDev    | Median     | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------ |----------- |---------------- |--------------- |------------ |------------- |------------ |-----------:|------------:|----------:|-----------:|------:|--------:|----------:|------------:|
| ExistsAsync_False             | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |   163.3 μs |     0.76 μs |   0.85 μs |   163.5 μs |  0.96 |    0.01 |     584 B |        0.52 |
| ExistsAsync_True              | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |   163.3 μs |     0.66 μs |   0.73 μs |   163.4 μs |  0.96 |    0.01 |     592 B |        0.52 |
| GetAsync_CacheHit             | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |   169.7 μs |     0.55 μs |   0.54 μs |   169.7 μs |  1.00 |    0.00 |    1128 B |        1.00 |
| GetAsync_CacheMiss            | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |   164.0 μs |     0.88 μs |   0.97 μs |   163.8 μs |  0.97 |    0.01 |     640 B |        0.57 |
| GetOrSetAsync_CacheHit        | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |   172.0 μs |     0.52 μs |   0.51 μs |   172.0 μs |  1.01 |    0.00 |    1432 B |        1.27 |
| SetAsync                      | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |   175.3 μs |     0.65 μs |   0.67 μs |   175.2 μs |  1.03 |    0.00 |     872 B |        0.77 |
| SetWithSlidingExpirationAsync | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |   343.5 μs |     1.12 μs |   1.24 μs |   343.7 μs |  2.02 |    0.01 |    1496 B |        1.33 |
|                               |            |                 |                |             |              |             |            |             |           |            |       |         |           |             |
| ExistsAsync_False             | ShortRun   | Default         | 3              | 1           | 16           | 3           |   164.1 μs |    19.19 μs |   1.05 μs |   164.1 μs |  0.95 |    0.01 |     584 B |        0.52 |
| ExistsAsync_True              | ShortRun   | Default         | 3              | 1           | 16           | 3           |   163.3 μs |    10.75 μs |   0.59 μs |   163.5 μs |  0.95 |    0.00 |     592 B |        0.53 |
| GetAsync_CacheHit             | ShortRun   | Default         | 3              | 1           | 16           | 3           |   172.2 μs |    10.31 μs |   0.57 μs |   172.3 μs |  1.00 |    0.00 |    1127 B |        1.00 |
| GetAsync_CacheMiss            | ShortRun   | Default         | 3              | 1           | 16           | 3           |   164.6 μs |     8.26 μs |   0.45 μs |   164.7 μs |  0.96 |    0.00 |     640 B |        0.57 |
| GetOrSetAsync_CacheHit        | ShortRun   | Default         | 3              | 1           | 16           | 3           |   172.7 μs |     8.63 μs |   0.47 μs |   172.9 μs |  1.00 |    0.00 |    1432 B |        1.27 |
| SetAsync                      | ShortRun   | Default         | 3              | 1           | 16           | 3           |   176.0 μs |     1.78 μs |   0.10 μs |   176.0 μs |  1.02 |    0.00 |     872 B |        0.77 |
| SetWithSlidingExpirationAsync | ShortRun   | Default         | 3              | 1           | 16           | 3           |   345.7 μs |    84.60 μs |   4.64 μs |   343.2 μs |  2.01 |    0.02 |    1496 B |        1.33 |
|                               |            |                 |                |             |              |             |            |             |           |            |       |         |           |             |
| GetOrSetAsync_CacheMiss       | Job-ZDPOZY | 1               | 20             | Default     | 1            | 5           |   710.5 μs |    18.04 μs |  20.78 μs |   708.4 μs |     ? |       ? |    3000 B |           ? |
| RemoveAsync                   | Job-ZDPOZY | 1               | 20             | Default     | 1            | 5           |   199.8 μs |    11.04 μs |  12.71 μs |   199.7 μs |     ? |       ? |     608 B |           ? |
| RemoveByPatternAsync          | Job-ZDPOZY | 1               | 20             | Default     | 1            | 5           | 1,014.4 μs |    41.31 μs |  47.57 μs | 1,019.0 μs |     ? |       ? |    2952 B |           ? |
|                               |            |                 |                |             |              |             |            |             |           |            |       |         |           |             |
| GetOrSetAsync_CacheMiss       | ShortRun   | 1               | 3              | 1           | 1            | 3           |   734.9 μs |   792.45 μs |  43.44 μs |   730.2 μs |     ? |       ? |    3000 B |           ? |
| RemoveAsync                   | ShortRun   | 1               | 3              | 1           | 1            | 3           |   218.4 μs |    87.06 μs |   4.77 μs |   221.0 μs |     ? |       ? |     608 B |           ? |
| RemoveByPatternAsync          | ShortRun   | 1               | 3              | 1           | 1            | 3           | 1,248.3 μs | 8,548.53 μs | 468.57 μs |   979.7 μs |     ? |       ? |    2952 B |           ? |
