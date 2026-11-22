```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 2.70GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-NUBXJZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun  : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-ZDPOZY : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4


```
| Method                        | Job        | InvocationCount | IterationCount | LaunchCount | UnrollFactor | WarmupCount | Mean      | Error     | StdDev    | Median    | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------ |----------- |---------------- |--------------- |------------ |------------- |------------ |----------:|----------:|----------:|----------:|------:|--------:|----------:|------------:|
| ExistsAsync_False             | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |  83.30 μs |  1.547 μs |  1.782 μs |  83.37 μs |  0.75 |    0.02 |     583 B |        0.52 |
| ExistsAsync_True              | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |  94.35 μs |  0.933 μs |  1.075 μs |  94.18 μs |  0.85 |    0.02 |     592 B |        0.52 |
| GetAsync_CacheHit             | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           | 111.55 μs |  1.441 μs |  1.659 μs | 111.20 μs |  1.00 |    0.02 |    1128 B |        1.00 |
| GetAsync_CacheMiss            | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |  97.88 μs |  1.071 μs |  1.234 μs |  97.91 μs |  0.88 |    0.02 |     640 B |        0.57 |
| GetOrSetAsync_CacheHit        | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           | 111.42 μs |  0.534 μs |  0.593 μs | 111.37 μs |  1.00 |    0.02 |    1432 B |        1.27 |
| SetAsync                      | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           | 118.24 μs |  0.648 μs |  0.721 μs | 118.37 μs |  1.06 |    0.02 |     872 B |        0.77 |
| SetWithSlidingExpirationAsync | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           | 223.18 μs |  1.757 μs |  2.023 μs | 223.24 μs |  2.00 |    0.03 |    1496 B |        1.33 |
|                               |            |                 |                |             |              |             |           |           |           |           |       |         |           |             |
| ExistsAsync_False             | MediumRun  | Default         | 15             | 2           | 16           | 10          |  88.70 μs |  4.717 μs |  6.765 μs |  84.39 μs |  0.80 |    0.06 |     581 B |        0.52 |
| ExistsAsync_True              | MediumRun  | Default         | 15             | 2           | 16           | 10          |  93.12 μs |  4.020 μs |  6.017 μs |  94.99 μs |  0.84 |    0.05 |     592 B |        0.52 |
| GetAsync_CacheHit             | MediumRun  | Default         | 15             | 2           | 16           | 10          | 111.39 μs |  0.869 μs |  1.300 μs | 111.53 μs |  1.00 |    0.02 |    1128 B |        1.00 |
| GetAsync_CacheMiss            | MediumRun  | Default         | 15             | 2           | 16           | 10          | 100.56 μs |  2.879 μs |  4.309 μs | 101.02 μs |  0.90 |    0.04 |     640 B |        0.57 |
| GetOrSetAsync_CacheHit        | MediumRun  | Default         | 15             | 2           | 16           | 10          | 110.69 μs |  0.858 μs |  1.230 μs | 110.62 μs |  0.99 |    0.02 |    1432 B |        1.27 |
| SetAsync                      | MediumRun  | Default         | 15             | 2           | 16           | 10          | 117.44 μs |  0.951 μs |  1.423 μs | 117.42 μs |  1.05 |    0.02 |     872 B |        0.77 |
| SetWithSlidingExpirationAsync | MediumRun  | Default         | 15             | 2           | 16           | 10          | 223.63 μs |  1.080 μs |  1.616 μs | 223.61 μs |  2.01 |    0.03 |    1496 B |        1.33 |
|                               |            |                 |                |             |              |             |           |           |           |           |       |         |           |             |
| GetOrSetAsync_CacheMiss       | Job-ZDPOZY | 1               | 20             | Default     | 1            | 5           | 649.20 μs | 38.917 μs | 43.256 μs | 649.58 μs |     ? |       ? |    3464 B |           ? |
| RemoveAsync                   | Job-ZDPOZY | 1               | 20             | Default     | 1            | 5           | 204.65 μs | 19.034 μs | 21.156 μs | 204.25 μs |     ? |       ? |    1072 B |           ? |
| RemoveByPatternAsync          | Job-ZDPOZY | 1               | 20             | Default     | 1            | 5           | 806.93 μs | 33.990 μs | 37.780 μs | 808.15 μs |     ? |       ? |    3416 B |           ? |
|                               |            |                 |                |             |              |             |           |           |           |           |       |         |           |             |
| GetOrSetAsync_CacheMiss       | MediumRun  | 1               | 15             | 2           | 1            | 10          | 644.78 μs | 35.613 μs | 51.075 μs | 646.97 μs |     ? |       ? |    3464 B |           ? |
| RemoveAsync                   | MediumRun  | 1               | 15             | 2           | 1            | 10          | 222.84 μs | 20.233 μs | 29.658 μs | 227.94 μs |     ? |       ? |    1072 B |           ? |
| RemoveByPatternAsync          | MediumRun  | 1               | 15             | 2           | 1            | 10          | 825.82 μs | 26.776 μs | 37.536 μs | 831.03 μs |     ? |       ? |    4168 B |           ? |
