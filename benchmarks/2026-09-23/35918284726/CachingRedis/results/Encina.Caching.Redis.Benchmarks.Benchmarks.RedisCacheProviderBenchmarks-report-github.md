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
| ExistsAsync_False             | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           | 107.3 μs |   2.35 μs |  2.61 μs |  0.98 |    0.03 |     584 B |        0.52 |
| ExistsAsync_True              | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           | 107.8 μs |   1.65 μs |  1.84 μs |  0.99 |    0.02 |     592 B |        0.52 |
| GetAsync_CacheHit             | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           | 109.5 μs |   1.25 μs |  1.39 μs |  1.00 |    0.02 |    1128 B |        1.00 |
| GetAsync_CacheMiss            | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           | 107.5 μs |   0.87 μs |  0.93 μs |  0.98 |    0.01 |     640 B |        0.57 |
| GetOrSetAsync_CacheHit        | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           | 111.4 μs |   0.92 μs |  0.98 μs |  1.02 |    0.02 |    1432 B |        1.27 |
| SetAsync                      | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           | 114.3 μs |   0.94 μs |  1.05 μs |  1.04 |    0.02 |     872 B |        0.77 |
| SetWithSlidingExpirationAsync | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           | 215.1 μs |   1.59 μs |  1.83 μs |  1.97 |    0.03 |    1496 B |        1.33 |
|                               |            |                 |                |             |              |             |          |           |          |       |         |           |             |
| ExistsAsync_False             | ShortRun   | Default         | 3              | 1           | 16           | 3           | 109.6 μs |  20.74 μs |  1.14 μs |  0.99 |    0.01 |     582 B |        0.52 |
| ExistsAsync_True              | ShortRun   | Default         | 3              | 1           | 16           | 3           | 109.8 μs |   4.36 μs |  0.24 μs |  1.00 |    0.00 |     589 B |        0.52 |
| GetAsync_CacheHit             | ShortRun   | Default         | 3              | 1           | 16           | 3           | 110.2 μs |   4.78 μs |  0.26 μs |  1.00 |    0.00 |    1124 B |        1.00 |
| GetAsync_CacheMiss            | ShortRun   | Default         | 3              | 1           | 16           | 3           | 108.6 μs |   2.79 μs |  0.15 μs |  0.99 |    0.00 |     638 B |        0.57 |
| GetOrSetAsync_CacheHit        | ShortRun   | Default         | 3              | 1           | 16           | 3           | 111.3 μs |   9.53 μs |  0.52 μs |  1.01 |    0.00 |    1429 B |        1.27 |
| SetAsync                      | ShortRun   | Default         | 3              | 1           | 16           | 3           | 113.5 μs |  20.25 μs |  1.11 μs |  1.03 |    0.01 |     872 B |        0.78 |
| SetWithSlidingExpirationAsync | ShortRun   | Default         | 3              | 1           | 16           | 3           | 213.7 μs |  33.82 μs |  1.85 μs |  1.94 |    0.02 |    1496 B |        1.33 |
|                               |            |                 |                |             |              |             |          |           |          |       |         |           |             |
| GetOrSetAsync_CacheMiss       | Job-ZDPOZY | 1               | 20             | Default     | 1            | 5           | 531.9 μs |  20.87 μs | 24.04 μs |     ? |       ? |    3000 B |           ? |
| RemoveAsync                   | Job-ZDPOZY | 1               | 20             | Default     | 1            | 5           | 157.1 μs |  12.23 μs | 14.09 μs |     ? |       ? |     608 B |           ? |
| RemoveByPatternAsync          | Job-ZDPOZY | 1               | 20             | Default     | 1            | 5           | 676.1 μs |  28.68 μs | 31.87 μs |     ? |       ? |    2952 B |           ? |
|                               |            |                 |                |             |              |             |          |           |          |       |         |           |             |
| GetOrSetAsync_CacheMiss       | ShortRun   | 1               | 3              | 1           | 1            | 3           | 538.2 μs | 380.71 μs | 20.87 μs |     ? |       ? |    3000 B |           ? |
| RemoveAsync                   | ShortRun   | 1               | 3              | 1           | 1            | 3           | 151.9 μs | 228.88 μs | 12.55 μs |     ? |       ? |     608 B |           ? |
| RemoveByPatternAsync          | ShortRun   | 1               | 3              | 1           | 1            | 3           | 709.3 μs | 327.42 μs | 17.95 μs |     ? |       ? |    3248 B |           ? |
