```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 2.70GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-NUBXJZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-ZDPOZY : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4


```
| Method                        | Job        | InvocationCount | IterationCount | LaunchCount | UnrollFactor | WarmupCount | Mean      | Error      | StdDev     | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------ |----------- |---------------- |--------------- |------------ |------------- |------------ |----------:|-----------:|-----------:|------:|--------:|----------:|------------:|
| ExistsAsync_False             | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           | 105.08 μs |   3.251 μs |   3.744 μs |  0.88 |    0.03 |     584 B |        0.52 |
| ExistsAsync_True              | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |  97.77 μs |   2.108 μs |   2.428 μs |  0.82 |    0.02 |     592 B |        0.52 |
| GetAsync_CacheHit             | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           | 119.06 μs |   1.716 μs |   1.976 μs |  1.00 |    0.02 |    1128 B |        1.00 |
| GetAsync_CacheMiss            | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           | 103.58 μs |   3.064 μs |   3.528 μs |  0.87 |    0.03 |     640 B |        0.57 |
| GetOrSetAsync_CacheHit        | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           | 122.27 μs |   2.226 μs |   2.474 μs |  1.03 |    0.03 |    1432 B |        1.27 |
| SetAsync                      | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           | 123.79 μs |   2.337 μs |   2.692 μs |  1.04 |    0.03 |     872 B |        0.77 |
| SetWithSlidingExpirationAsync | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           | 242.43 μs |   2.766 μs |   3.186 μs |  2.04 |    0.04 |    1496 B |        1.33 |
|                               |            |                 |                |             |              |             |           |            |            |       |         |           |             |
| ExistsAsync_False             | ShortRun   | Default         | 3              | 1           | 16           | 3           |  94.23 μs |   8.387 μs |   0.460 μs |  0.78 |    0.03 |     584 B |        0.52 |
| ExistsAsync_True              | ShortRun   | Default         | 3              | 1           | 16           | 3           |  93.65 μs | 109.563 μs |   6.005 μs |  0.78 |    0.05 |     592 B |        0.52 |
| GetAsync_CacheHit             | ShortRun   | Default         | 3              | 1           | 16           | 3           | 120.75 μs |  86.145 μs |   4.722 μs |  1.00 |    0.05 |    1128 B |        1.00 |
| GetAsync_CacheMiss            | ShortRun   | Default         | 3              | 1           | 16           | 3           | 110.16 μs |  22.103 μs |   1.212 μs |  0.91 |    0.03 |     637 B |        0.56 |
| GetOrSetAsync_CacheHit        | ShortRun   | Default         | 3              | 1           | 16           | 3           | 125.44 μs |  23.359 μs |   1.280 μs |  1.04 |    0.04 |    1432 B |        1.27 |
| SetAsync                      | ShortRun   | Default         | 3              | 1           | 16           | 3           | 120.41 μs |  26.339 μs |   1.444 μs |  1.00 |    0.03 |     872 B |        0.77 |
| SetWithSlidingExpirationAsync | ShortRun   | Default         | 3              | 1           | 16           | 3           | 241.31 μs |  76.766 μs |   4.208 μs |  2.00 |    0.07 |    1496 B |        1.33 |
|                               |            |                 |                |             |              |             |           |            |            |       |         |           |             |
| GetOrSetAsync_CacheMiss       | Job-ZDPOZY | 1               | 20             | Default     | 1            | 5           | 722.63 μs | 128.534 μs | 131.995 μs |     ? |       ? |    3464 B |           ? |
| RemoveAsync                   | Job-ZDPOZY | 1               | 20             | Default     | 1            | 5           | 203.35 μs |  26.412 μs |  28.260 μs |     ? |       ? |    1072 B |           ? |
| RemoveByPatternAsync          | Job-ZDPOZY | 1               | 20             | Default     | 1            | 5           | 809.68 μs |  36.889 μs |  42.481 μs |     ? |       ? |    3416 B |           ? |
|                               |            |                 |                |             |              |             |           |            |            |       |         |           |             |
| GetOrSetAsync_CacheMiss       | ShortRun   | 1               | 3              | 1           | 1            | 3           | 704.45 μs | 829.519 μs |  45.469 μs |     ? |       ? |    3464 B |           ? |
| RemoveAsync                   | ShortRun   | 1               | 3              | 1           | 1            | 3           | 221.91 μs | 799.337 μs |  43.814 μs |     ? |       ? |    9288 B |           ? |
| RemoveByPatternAsync          | ShortRun   | 1               | 3              | 1           | 1            | 3           | 862.19 μs | 438.638 μs |  24.043 μs |     ? |       ? |    3416 B |           ? |
