```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.13GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-NUBXJZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-ZDPOZY : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4


```
| Method                        | Job        | InvocationCount | IterationCount | LaunchCount | UnrollFactor | WarmupCount | Mean      | Error      | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------ |----------- |---------------- |--------------- |------------ |------------- |------------ |----------:|-----------:|----------:|------:|--------:|----------:|------------:|
| ExistsAsync_False             | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           | 103.53 μs |   1.679 μs |  1.933 μs |  1.00 |    0.04 |     583 B |        0.52 |
| ExistsAsync_True              | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           | 103.62 μs |   1.142 μs |  1.173 μs |  1.00 |    0.03 |     589 B |        0.52 |
| GetAsync_CacheHit             | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           | 103.39 μs |   2.948 μs |  3.394 μs |  1.00 |    0.05 |    1126 B |        1.00 |
| GetAsync_CacheMiss            | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |  98.59 μs |   4.660 μs |  5.366 μs |  0.95 |    0.06 |     638 B |        0.57 |
| GetOrSetAsync_CacheHit        | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           | 101.22 μs |   3.707 μs |  4.269 μs |  0.98 |    0.05 |    1431 B |        1.27 |
| SetAsync                      | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           | 107.30 μs |   1.768 μs |  1.892 μs |  1.04 |    0.04 |     872 B |        0.77 |
| SetWithSlidingExpirationAsync | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           | 202.72 μs |   3.451 μs |  3.975 μs |  1.96 |    0.08 |    1495 B |        1.33 |
|                               |            |                 |                |             |              |             |           |            |           |       |         |           |             |
| ExistsAsync_False             | ShortRun   | Default         | 3              | 1           | 16           | 3           | 107.49 μs |  12.906 μs |  0.707 μs |  1.09 |    0.01 |     581 B |        0.52 |
| ExistsAsync_True              | ShortRun   | Default         | 3              | 1           | 16           | 3           | 110.51 μs |  56.540 μs |  3.099 μs |  1.12 |    0.03 |     587 B |        0.52 |
| GetAsync_CacheHit             | ShortRun   | Default         | 3              | 1           | 16           | 3           |  98.86 μs |   6.249 μs |  0.343 μs |  1.00 |    0.00 |    1128 B |        1.00 |
| GetAsync_CacheMiss            | ShortRun   | Default         | 3              | 1           | 16           | 3           | 100.86 μs |  44.171 μs |  2.421 μs |  1.02 |    0.02 |     639 B |        0.57 |
| GetOrSetAsync_CacheHit        | ShortRun   | Default         | 3              | 1           | 16           | 3           | 104.13 μs | 150.283 μs |  8.238 μs |  1.05 |    0.07 |    1429 B |        1.27 |
| SetAsync                      | ShortRun   | Default         | 3              | 1           | 16           | 3           | 107.76 μs | 148.938 μs |  8.164 μs |  1.09 |    0.07 |     871 B |        0.77 |
| SetWithSlidingExpirationAsync | ShortRun   | Default         | 3              | 1           | 16           | 3           | 203.21 μs |  31.193 μs |  1.710 μs |  2.06 |    0.02 |    1496 B |        1.33 |
|                               |            |                 |                |             |              |             |           |            |           |       |         |           |             |
| GetOrSetAsync_CacheMiss       | Job-ZDPOZY | 1               | 20             | Default     | 1            | 5           | 570.32 μs |  14.056 μs | 16.186 μs |     ? |       ? |    3464 B |           ? |
| RemoveAsync                   | Job-ZDPOZY | 1               | 20             | Default     | 1            | 5           | 171.96 μs |  15.596 μs | 17.960 μs |     ? |       ? |    1072 B |           ? |
| RemoveByPatternAsync          | Job-ZDPOZY | 1               | 20             | Default     | 1            | 5           | 698.85 μs |  24.011 μs | 27.651 μs |     ? |       ? |    3416 B |           ? |
|                               |            |                 |                |             |              |             |           |            |           |       |         |           |             |
| GetOrSetAsync_CacheMiss       | ShortRun   | 1               | 3              | 1           | 1            | 3           | 579.46 μs | 199.240 μs | 10.921 μs |     ? |       ? |    3464 B |           ? |
| RemoveAsync                   | ShortRun   | 1               | 3              | 1           | 1            | 3           | 172.85 μs | 272.156 μs | 14.918 μs |     ? |       ? |    1072 B |           ? |
| RemoveByPatternAsync          | ShortRun   | 1               | 3              | 1           | 1            | 3           | 708.82 μs | 836.672 μs | 45.861 μs |     ? |       ? |    3712 B |           ? |
