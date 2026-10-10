```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-NUBXJZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-ZDPOZY : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4


```
| Method                        | Job        | InvocationCount | IterationCount | LaunchCount | UnrollFactor | WarmupCount | Mean       | Error        | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------ |----------- |---------------- |--------------- |------------ |------------- |------------ |-----------:|-------------:|----------:|------:|--------:|----------:|------------:|
| ExistsAsync_False             | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |   107.9 μs |      3.41 μs |   3.93 μs |  0.97 |    0.04 |     584 B |        0.52 |
| ExistsAsync_True              | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |   108.7 μs |      2.04 μs |   2.27 μs |  0.98 |    0.02 |     592 B |        0.53 |
| GetAsync_CacheHit             | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |   111.5 μs |      1.20 μs |   1.33 μs |  1.00 |    0.02 |    1127 B |        1.00 |
| GetAsync_CacheMiss            | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |   113.4 μs |      0.97 μs |   1.00 μs |  1.02 |    0.01 |     640 B |        0.57 |
| GetOrSetAsync_CacheHit        | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |   111.7 μs |      3.08 μs |   3.43 μs |  1.00 |    0.03 |    1432 B |        1.27 |
| SetAsync                      | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |   111.9 μs |      1.34 μs |   1.49 μs |  1.00 |    0.02 |     872 B |        0.77 |
| SetWithSlidingExpirationAsync | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |   213.6 μs |      2.44 μs |   2.81 μs |  1.92 |    0.03 |    1500 B |        1.33 |
|                               |            |                 |                |             |              |             |            |              |           |       |         |           |             |
| ExistsAsync_False             | ShortRun   | Default         | 3              | 1           | 16           | 3           |   113.6 μs |     52.69 μs |   2.89 μs |  1.01 |    0.02 |     582 B |        0.52 |
| ExistsAsync_True              | ShortRun   | Default         | 3              | 1           | 16           | 3           |   109.8 μs |     42.07 μs |   2.31 μs |  0.97 |    0.02 |     588 B |        0.52 |
| GetAsync_CacheHit             | ShortRun   | Default         | 3              | 1           | 16           | 3           |   112.9 μs |      9.68 μs |   0.53 μs |  1.00 |    0.01 |    1126 B |        1.00 |
| GetAsync_CacheMiss            | ShortRun   | Default         | 3              | 1           | 16           | 3           |   110.4 μs |     15.31 μs |   0.84 μs |  0.98 |    0.01 |     638 B |        0.57 |
| GetOrSetAsync_CacheHit        | ShortRun   | Default         | 3              | 1           | 16           | 3           |   114.0 μs |     32.88 μs |   1.80 μs |  1.01 |    0.01 |    1428 B |        1.27 |
| SetAsync                      | ShortRun   | Default         | 3              | 1           | 16           | 3           |   113.7 μs |     71.09 μs |   3.90 μs |  1.01 |    0.03 |     869 B |        0.77 |
| SetWithSlidingExpirationAsync | ShortRun   | Default         | 3              | 1           | 16           | 3           |   214.7 μs |     23.93 μs |   1.31 μs |  1.90 |    0.01 |    1494 B |        1.33 |
|                               |            |                 |                |             |              |             |            |              |           |       |         |           |             |
| GetOrSetAsync_CacheMiss       | Job-ZDPOZY | 1               | 20             | Default     | 1            | 5           |   498.7 μs |     16.05 μs |  18.48 μs |     ? |       ? |    3000 B |           ? |
| RemoveAsync                   | Job-ZDPOZY | 1               | 20             | Default     | 1            | 5           |   154.6 μs |     13.53 μs |  15.58 μs |     ? |       ? |     608 B |           ? |
| RemoveByPatternAsync          | Job-ZDPOZY | 1               | 20             | Default     | 1            | 5           |   762.7 μs |    148.12 μs | 152.11 μs |     ? |       ? |    3248 B |           ? |
|                               |            |                 |                |             |              |             |            |              |           |       |         |           |             |
| GetOrSetAsync_CacheMiss       | ShortRun   | 1               | 3              | 1           | 1            | 3           |   557.0 μs |    354.26 μs |  19.42 μs |     ? |       ? |    3000 B |           ? |
| RemoveAsync                   | ShortRun   | 1               | 3              | 1           | 1            | 3           |   188.7 μs |    602.72 μs |  33.04 μs |     ? |       ? |     608 B |           ? |
| RemoveByPatternAsync          | ShortRun   | 1               | 3              | 1           | 1            | 3           | 1,322.7 μs | 13,625.28 μs | 746.85 μs |     ? |       ? |    3720 B |           ? |
