```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-NUBXJZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-ZDPOZY : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4


```
| Method                        | Job        | InvocationCount | IterationCount | LaunchCount | UnrollFactor | WarmupCount | Mean       | Error       | StdDev   | Median     | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------ |----------- |---------------- |--------------- |------------ |------------- |------------ |-----------:|------------:|---------:|-----------:|------:|--------:|----------:|------------:|
| ExistsAsync_False             | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |   160.3 μs |     0.61 μs |  0.70 μs |   160.4 μs |  0.96 |    0.01 |     584 B |        0.52 |
| ExistsAsync_True              | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |   156.8 μs |     0.96 μs |  1.06 μs |   156.9 μs |  0.94 |    0.01 |     592 B |        0.52 |
| GetAsync_CacheHit             | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |   166.6 μs |     0.96 μs |  1.02 μs |   166.2 μs |  1.00 |    0.01 |    1128 B |        1.00 |
| GetAsync_CacheMiss            | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |   158.6 μs |     0.43 μs |  0.46 μs |   158.5 μs |  0.95 |    0.01 |     640 B |        0.57 |
| GetOrSetAsync_CacheHit        | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |   168.9 μs |     0.88 μs |  0.94 μs |   168.7 μs |  1.01 |    0.01 |    1432 B |        1.27 |
| SetAsync                      | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |   171.3 μs |     0.73 μs |  0.81 μs |   171.4 μs |  1.03 |    0.01 |     872 B |        0.77 |
| SetWithSlidingExpirationAsync | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |   333.3 μs |     1.05 μs |  1.17 μs |   333.2 μs |  2.00 |    0.01 |    1496 B |        1.33 |
|                               |            |                 |                |             |              |             |            |             |          |            |       |         |           |             |
| ExistsAsync_False             | ShortRun   | Default         | 3              | 1           | 16           | 3           |   159.8 μs |    13.33 μs |  0.73 μs |   159.7 μs |  0.96 |    0.00 |     584 B |        0.52 |
| ExistsAsync_True              | ShortRun   | Default         | 3              | 1           | 16           | 3           |   158.2 μs |    17.84 μs |  0.98 μs |   157.7 μs |  0.95 |    0.01 |     592 B |        0.52 |
| GetAsync_CacheHit             | ShortRun   | Default         | 3              | 1           | 16           | 3           |   167.0 μs |     6.07 μs |  0.33 μs |   166.8 μs |  1.00 |    0.00 |    1128 B |        1.00 |
| GetAsync_CacheMiss            | ShortRun   | Default         | 3              | 1           | 16           | 3           |   159.4 μs |    16.29 μs |  0.89 μs |   159.7 μs |  0.95 |    0.00 |     640 B |        0.57 |
| GetOrSetAsync_CacheHit        | ShortRun   | Default         | 3              | 1           | 16           | 3           |   168.9 μs |    10.29 μs |  0.56 μs |   168.8 μs |  1.01 |    0.00 |    1432 B |        1.27 |
| SetAsync                      | ShortRun   | Default         | 3              | 1           | 16           | 3           |   170.0 μs |    12.59 μs |  0.69 μs |   170.1 μs |  1.02 |    0.00 |     872 B |        0.77 |
| SetWithSlidingExpirationAsync | ShortRun   | Default         | 3              | 1           | 16           | 3           |   336.3 μs |     3.92 μs |  0.21 μs |   336.3 μs |  2.01 |    0.00 |    1496 B |        1.33 |
|                               |            |                 |                |             |              |             |            |             |          |            |       |         |           |             |
| GetOrSetAsync_CacheMiss       | Job-ZDPOZY | 1               | 20             | Default     | 1            | 5           |   811.8 μs |    44.86 μs | 51.66 μs |   827.2 μs |     ? |       ? |    3000 B |           ? |
| RemoveAsync                   | Job-ZDPOZY | 1               | 20             | Default     | 1            | 5           |   249.8 μs |    74.29 μs | 85.55 μs |   199.7 μs |     ? |       ? |     608 B |           ? |
| RemoveByPatternAsync          | Job-ZDPOZY | 1               | 20             | Default     | 1            | 5           | 1,074.7 μs |    35.71 μs | 38.21 μs | 1,085.5 μs |     ? |       ? |    2952 B |           ? |
|                               |            |                 |                |             |              |             |            |             |          |            |       |         |           |             |
| GetOrSetAsync_CacheMiss       | ShortRun   | 1               | 3              | 1           | 1            | 3           |   807.7 μs | 1,044.31 μs | 57.24 μs |   776.6 μs |     ? |       ? |    3000 B |           ? |
| RemoveAsync                   | ShortRun   | 1               | 3              | 1           | 1            | 3           |   252.2 μs | 1,448.92 μs | 79.42 μs |   221.4 μs |     ? |       ? |    8824 B |           ? |
| RemoveByPatternAsync          | ShortRun   | 1               | 3              | 1           | 1            | 3           | 1,111.8 μs | 1,795.36 μs | 98.41 μs | 1,063.9 μs |     ? |       ? |    3248 B |           ? |
