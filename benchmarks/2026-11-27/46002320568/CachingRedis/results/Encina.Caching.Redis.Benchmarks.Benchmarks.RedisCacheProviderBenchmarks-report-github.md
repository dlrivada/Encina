```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.26GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-NUBXJZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-ZDPOZY : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4


```
| Method                        | Job        | InvocationCount | IterationCount | LaunchCount | UnrollFactor | WarmupCount | Mean      | Error      | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------ |----------- |---------------- |--------------- |------------ |------------- |------------ |----------:|-----------:|----------:|------:|--------:|----------:|------------:|
| ExistsAsync_False             | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |  98.78 μs |   2.947 μs |  3.153 μs |  1.02 |    0.05 |     582 B |        0.52 |
| ExistsAsync_True              | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |  97.92 μs |   2.226 μs |  2.474 μs |  1.01 |    0.05 |     588 B |        0.52 |
| GetAsync_CacheHit             | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |  97.46 μs |   3.508 μs |  4.040 μs |  1.00 |    0.06 |    1123 B |        1.00 |
| GetAsync_CacheMiss            | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |  97.39 μs |   3.876 μs |  4.464 μs |  1.00 |    0.06 |     638 B |        0.57 |
| GetOrSetAsync_CacheHit        | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |  99.57 μs |   3.306 μs |  3.807 μs |  1.02 |    0.06 |    1431 B |        1.27 |
| SetAsync                      | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           | 100.95 μs |   2.616 μs |  2.908 μs |  1.04 |    0.05 |     868 B |        0.77 |
| SetWithSlidingExpirationAsync | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           | 199.71 μs |   6.531 μs |  7.259 μs |  2.05 |    0.11 |    1496 B |        1.33 |
|                               |            |                 |                |             |              |             |           |            |           |       |         |           |             |
| ExistsAsync_False             | ShortRun   | Default         | 3              | 1           | 16           | 3           |  96.91 μs |  70.456 μs |  3.862 μs |  0.91 |    0.03 |     582 B |        0.52 |
| ExistsAsync_True              | ShortRun   | Default         | 3              | 1           | 16           | 3           | 100.14 μs |  34.309 μs |  1.881 μs |  0.94 |    0.02 |     591 B |        0.53 |
| GetAsync_CacheHit             | ShortRun   | Default         | 3              | 1           | 16           | 3           | 107.11 μs |  35.311 μs |  1.935 μs |  1.00 |    0.02 |    1123 B |        1.00 |
| GetAsync_CacheMiss            | ShortRun   | Default         | 3              | 1           | 16           | 3           |  96.73 μs |  37.682 μs |  2.065 μs |  0.90 |    0.02 |     639 B |        0.57 |
| GetOrSetAsync_CacheHit        | ShortRun   | Default         | 3              | 1           | 16           | 3           | 103.82 μs |  48.507 μs |  2.659 μs |  0.97 |    0.03 |    1430 B |        1.27 |
| SetAsync                      | ShortRun   | Default         | 3              | 1           | 16           | 3           | 107.68 μs | 170.931 μs |  9.369 μs |  1.01 |    0.08 |     870 B |        0.77 |
| SetWithSlidingExpirationAsync | ShortRun   | Default         | 3              | 1           | 16           | 3           | 204.33 μs | 145.581 μs |  7.980 μs |  1.91 |    0.07 |    1496 B |        1.33 |
|                               |            |                 |                |             |              |             |           |            |           |       |         |           |             |
| GetOrSetAsync_CacheMiss       | Job-ZDPOZY | 1               | 20             | Default     | 1            | 5           | 534.50 μs |  19.060 μs | 21.950 μs |     ? |       ? |    3464 B |           ? |
| RemoveAsync                   | Job-ZDPOZY | 1               | 20             | Default     | 1            | 5           | 166.58 μs |   7.504 μs |  8.642 μs |     ? |       ? |    1072 B |           ? |
| RemoveByPatternAsync          | Job-ZDPOZY | 1               | 20             | Default     | 1            | 5           | 710.42 μs |  34.327 μs | 36.730 μs |     ? |       ? |    3416 B |           ? |
|                               |            |                 |                |             |              |             |           |            |           |       |         |           |             |
| GetOrSetAsync_CacheMiss       | ShortRun   | 1               | 3              | 1           | 1            | 3           | 568.31 μs | 715.631 μs | 39.226 μs |     ? |       ? |    3464 B |           ? |
| RemoveAsync                   | ShortRun   | 1               | 3              | 1           | 1            | 3           | 168.72 μs | 215.832 μs | 11.831 μs |     ? |       ? |    1072 B |           ? |
| RemoveByPatternAsync          | ShortRun   | 1               | 3              | 1           | 1            | 3           | 732.46 μs | 136.014 μs |  7.455 μs |     ? |       ? |    3416 B |           ? |
