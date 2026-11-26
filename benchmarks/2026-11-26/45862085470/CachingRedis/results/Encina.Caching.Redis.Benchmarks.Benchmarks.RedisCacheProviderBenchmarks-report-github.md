```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.50GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-NUBXJZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-ZDPOZY : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4


```
| Method                        | Job        | InvocationCount | IterationCount | LaunchCount | UnrollFactor | WarmupCount | Mean      | Error      | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------ |----------- |---------------- |--------------- |------------ |------------- |------------ |----------:|-----------:|----------:|------:|--------:|----------:|------------:|
| ExistsAsync_False             | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |  89.86 μs |   1.620 μs |  1.865 μs |  0.95 |    0.04 |     583 B |        0.52 |
| ExistsAsync_True              | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |  92.93 μs |   2.893 μs |  3.332 μs |  0.98 |    0.05 |     589 B |        0.52 |
| GetAsync_CacheHit             | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |  94.66 μs |   3.268 μs |  3.633 μs |  1.00 |    0.05 |    1126 B |        1.00 |
| GetAsync_CacheMiss            | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |  92.38 μs |   2.428 μs |  2.698 μs |  0.98 |    0.05 |     639 B |        0.57 |
| GetOrSetAsync_CacheHit        | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |  91.93 μs |   2.834 μs |  3.263 μs |  0.97 |    0.05 |    1431 B |        1.27 |
| SetAsync                      | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |  95.94 μs |   3.044 μs |  3.506 μs |  1.02 |    0.05 |     868 B |        0.77 |
| SetWithSlidingExpirationAsync | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           | 188.66 μs |   4.964 μs |  5.717 μs |  2.00 |    0.10 |    1496 B |        1.33 |
|                               |            |                 |                |             |              |             |           |            |           |       |         |           |             |
| ExistsAsync_False             | ShortRun   | Default         | 3              | 1           | 16           | 3           |  90.95 μs |  45.640 μs |  2.502 μs |  0.94 |    0.02 |     582 B |        0.52 |
| ExistsAsync_True              | ShortRun   | Default         | 3              | 1           | 16           | 3           |  93.82 μs |  44.017 μs |  2.413 μs |  0.97 |    0.02 |     591 B |        0.53 |
| GetAsync_CacheHit             | ShortRun   | Default         | 3              | 1           | 16           | 3           |  96.45 μs |  11.469 μs |  0.629 μs |  1.00 |    0.01 |    1125 B |        1.00 |
| GetAsync_CacheMiss            | ShortRun   | Default         | 3              | 1           | 16           | 3           |  95.57 μs |  32.982 μs |  1.808 μs |  0.99 |    0.02 |     639 B |        0.57 |
| GetOrSetAsync_CacheHit        | ShortRun   | Default         | 3              | 1           | 16           | 3           |  93.63 μs |   9.973 μs |  0.547 μs |  0.97 |    0.01 |    1431 B |        1.27 |
| SetAsync                      | ShortRun   | Default         | 3              | 1           | 16           | 3           |  96.73 μs |  38.577 μs |  2.115 μs |  1.00 |    0.02 |     871 B |        0.77 |
| SetWithSlidingExpirationAsync | ShortRun   | Default         | 3              | 1           | 16           | 3           | 202.60 μs | 173.995 μs |  9.537 μs |  2.10 |    0.09 |    1500 B |        1.33 |
|                               |            |                 |                |             |              |             |           |            |           |       |         |           |             |
| GetOrSetAsync_CacheMiss       | Job-ZDPOZY | 1               | 20             | Default     | 1            | 5           | 506.10 μs |  19.401 μs | 21.564 μs |     ? |       ? |    3464 B |           ? |
| RemoveAsync                   | Job-ZDPOZY | 1               | 20             | Default     | 1            | 5           | 146.92 μs |   6.783 μs |  7.812 μs |     ? |       ? |    1072 B |           ? |
| RemoveByPatternAsync          | Job-ZDPOZY | 1               | 20             | Default     | 1            | 5           | 649.77 μs |  63.880 μs | 62.739 μs |     ? |       ? |    3416 B |           ? |
|                               |            |                 |                |             |              |             |           |            |           |       |         |           |             |
| GetOrSetAsync_CacheMiss       | ShortRun   | 1               | 3              | 1           | 1            | 3           | 541.33 μs | 753.480 μs | 41.301 μs |     ? |       ? |    3464 B |           ? |
| RemoveAsync                   | ShortRun   | 1               | 3              | 1           | 1            | 3           | 183.65 μs | 520.214 μs | 28.515 μs |     ? |       ? |    9288 B |           ? |
| RemoveByPatternAsync          | ShortRun   | 1               | 3              | 1           | 1            | 3           | 697.13 μs | 525.152 μs | 28.785 μs |     ? |       ? |    3416 B |           ? |
