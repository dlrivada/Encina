```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 2.70GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-NUBXJZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-ZDPOZY : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4


```
| Method                        | Job        | InvocationCount | IterationCount | LaunchCount | UnrollFactor | WarmupCount | Mean      | Error        | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------ |----------- |---------------- |--------------- |------------ |------------- |------------ |----------:|-------------:|----------:|------:|--------:|----------:|------------:|
| ExistsAsync_False             | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |  78.19 μs |     1.355 μs |  1.507 μs |  0.68 |    0.01 |     584 B |        0.52 |
| ExistsAsync_True              | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |  96.12 μs |     1.123 μs |  1.248 μs |  0.83 |    0.01 |     592 B |        0.52 |
| GetAsync_CacheHit             | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           | 115.37 μs |     0.716 μs |  0.796 μs |  1.00 |    0.01 |    1128 B |        1.00 |
| GetAsync_CacheMiss            | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |  99.67 μs |     1.185 μs |  1.317 μs |  0.86 |    0.01 |     640 B |        0.57 |
| GetOrSetAsync_CacheHit        | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           | 115.68 μs |     0.934 μs |  1.038 μs |  1.00 |    0.01 |    1432 B |        1.27 |
| SetAsync                      | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           | 114.85 μs |     2.073 μs |  2.387 μs |  1.00 |    0.02 |     872 B |        0.77 |
| SetWithSlidingExpirationAsync | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           | 223.89 μs |     2.589 μs |  2.981 μs |  1.94 |    0.03 |    1496 B |        1.33 |
|                               |            |                 |                |             |              |             |           |              |           |       |         |           |             |
| ExistsAsync_False             | ShortRun   | Default         | 3              | 1           | 16           | 3           |  95.70 μs |    49.362 μs |  2.706 μs |  0.83 |    0.03 |     583 B |        0.52 |
| ExistsAsync_True              | ShortRun   | Default         | 3              | 1           | 16           | 3           |  96.11 μs |    34.123 μs |  1.870 μs |  0.83 |    0.02 |     592 B |        0.52 |
| GetAsync_CacheHit             | ShortRun   | Default         | 3              | 1           | 16           | 3           | 115.18 μs |    55.468 μs |  3.040 μs |  1.00 |    0.03 |    1128 B |        1.00 |
| GetAsync_CacheMiss            | ShortRun   | Default         | 3              | 1           | 16           | 3           | 103.21 μs |    22.860 μs |  1.253 μs |  0.90 |    0.02 |     640 B |        0.57 |
| GetOrSetAsync_CacheHit        | ShortRun   | Default         | 3              | 1           | 16           | 3           | 110.80 μs |    41.783 μs |  2.290 μs |  0.96 |    0.03 |    1426 B |        1.26 |
| SetAsync                      | ShortRun   | Default         | 3              | 1           | 16           | 3           | 118.32 μs |    54.828 μs |  3.005 μs |  1.03 |    0.03 |     872 B |        0.77 |
| SetWithSlidingExpirationAsync | ShortRun   | Default         | 3              | 1           | 16           | 3           | 221.18 μs |     1.704 μs |  0.093 μs |  1.92 |    0.04 |    1496 B |        1.33 |
|                               |            |                 |                |             |              |             |           |              |           |       |         |           |             |
| GetOrSetAsync_CacheMiss       | Job-ZDPOZY | 1               | 20             | Default     | 1            | 5           | 649.93 μs |    35.612 μs | 41.010 μs |     ? |       ? |    3464 B |           ? |
| RemoveAsync                   | Job-ZDPOZY | 1               | 20             | Default     | 1            | 5           | 202.85 μs |    28.378 μs | 32.680 μs |     ? |       ? |    1072 B |           ? |
| RemoveByPatternAsync          | Job-ZDPOZY | 1               | 20             | Default     | 1            | 5           | 791.30 μs |    37.562 μs | 41.751 μs |     ? |       ? |    4168 B |           ? |
|                               |            |                 |                |             |              |             |           |              |           |       |         |           |             |
| GetOrSetAsync_CacheMiss       | ShortRun   | 1               | 3              | 1           | 1            | 3           | 709.50 μs |   147.698 μs |  8.096 μs |     ? |       ? |    3464 B |           ? |
| RemoveAsync                   | ShortRun   | 1               | 3              | 1           | 1            | 3           | 202.58 μs |   494.924 μs | 27.128 μs |     ? |       ? |    1560 B |           ? |
| RemoveByPatternAsync          | ShortRun   | 1               | 3              | 1           | 1            | 3           | 781.40 μs | 1,249.345 μs | 68.481 μs |     ? |       ? |    3712 B |           ? |
