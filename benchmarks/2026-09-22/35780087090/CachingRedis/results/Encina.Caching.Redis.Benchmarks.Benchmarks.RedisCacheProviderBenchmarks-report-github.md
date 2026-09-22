```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-NUBXJZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-ZDPOZY : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4


```
| Method                        | Job        | InvocationCount | IterationCount | LaunchCount | UnrollFactor | WarmupCount | Mean     | Error     | StdDev   | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------ |----------- |---------------- |--------------- |------------ |------------- |------------ |---------:|----------:|---------:|------:|--------:|----------:|------------:|
| ExistsAsync_False             | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           | 110.5 μs |   0.72 μs |  0.81 μs |  0.98 |    0.01 |     584 B |        0.52 |
| ExistsAsync_True              | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           | 109.9 μs |   0.75 μs |  0.84 μs |  0.97 |    0.01 |     591 B |        0.52 |
| GetAsync_CacheHit             | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           | 112.8 μs |   0.56 μs |  0.59 μs |  1.00 |    0.01 |    1128 B |        1.00 |
| GetAsync_CacheMiss            | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           | 111.8 μs |   0.94 μs |  1.09 μs |  0.99 |    0.01 |     640 B |        0.57 |
| GetOrSetAsync_CacheHit        | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           | 112.8 μs |   0.59 μs |  0.63 μs |  1.00 |    0.01 |    1432 B |        1.27 |
| SetAsync                      | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           | 115.5 μs |   1.10 μs |  1.22 μs |  1.02 |    0.01 |     872 B |        0.77 |
| SetWithSlidingExpirationAsync | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           | 217.4 μs |   1.38 μs |  1.54 μs |  1.93 |    0.02 |    1496 B |        1.33 |
|                               |            |                 |                |             |              |             |          |           |          |       |         |           |             |
| ExistsAsync_False             | ShortRun   | Default         | 3              | 1           | 16           | 3           | 108.7 μs |  11.41 μs |  0.63 μs |  0.98 |    0.01 |     582 B |        0.52 |
| ExistsAsync_True              | ShortRun   | Default         | 3              | 1           | 16           | 3           | 113.7 μs | 105.59 μs |  5.79 μs |  1.02 |    0.05 |     591 B |        0.52 |
| GetAsync_CacheHit             | ShortRun   | Default         | 3              | 1           | 16           | 3           | 111.5 μs |   7.39 μs |  0.41 μs |  1.00 |    0.00 |    1126 B |        1.00 |
| GetAsync_CacheMiss            | ShortRun   | Default         | 3              | 1           | 16           | 3           | 114.2 μs |  20.82 μs |  1.14 μs |  1.02 |    0.01 |     637 B |        0.57 |
| GetOrSetAsync_CacheHit        | ShortRun   | Default         | 3              | 1           | 16           | 3           | 112.6 μs |  24.38 μs |  1.34 μs |  1.01 |    0.01 |    1430 B |        1.27 |
| SetAsync                      | ShortRun   | Default         | 3              | 1           | 16           | 3           | 119.1 μs | 163.41 μs |  8.96 μs |  1.07 |    0.07 |     870 B |        0.77 |
| SetWithSlidingExpirationAsync | ShortRun   | Default         | 3              | 1           | 16           | 3           | 217.7 μs |  86.66 μs |  4.75 μs |  1.95 |    0.04 |    1495 B |        1.33 |
|                               |            |                 |                |             |              |             |          |           |          |       |         |           |             |
| GetOrSetAsync_CacheMiss       | Job-ZDPOZY | 1               | 20             | Default     | 1            | 5           | 533.9 μs |  20.68 μs | 22.99 μs |     ? |       ? |    3000 B |           ? |
| RemoveAsync                   | Job-ZDPOZY | 1               | 20             | Default     | 1            | 5           | 154.6 μs |   7.41 μs |  8.54 μs |     ? |       ? |     608 B |           ? |
| RemoveByPatternAsync          | Job-ZDPOZY | 1               | 20             | Default     | 1            | 5           | 718.9 μs |  28.93 μs | 33.32 μs |     ? |       ? |    2952 B |           ? |
|                               |            |                 |                |             |              |             |          |           |          |       |         |           |             |
| GetOrSetAsync_CacheMiss       | ShortRun   | 1               | 3              | 1           | 1            | 3           | 562.5 μs | 327.48 μs | 17.95 μs |     ? |       ? |    3000 B |           ? |
| RemoveAsync                   | ShortRun   | 1               | 3              | 1           | 1            | 3           | 164.1 μs | 191.50 μs | 10.50 μs |     ? |       ? |     640 B |           ? |
| RemoveByPatternAsync          | ShortRun   | 1               | 3              | 1           | 1            | 3           | 757.8 μs | 201.45 μs | 11.04 μs |     ? |       ? |    3248 B |           ? |
