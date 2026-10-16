```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  Job-NUBXJZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  Job-ZDPOZY : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4


```
| Method                        | Job        | InvocationCount | IterationCount | LaunchCount | UnrollFactor | WarmupCount | Mean       | Error     | StdDev   | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------ |----------- |---------------- |--------------- |------------ |------------- |------------ |-----------:|----------:|---------:|------:|--------:|----------:|------------:|
| ExistsAsync_False             | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |   148.1 μs |   5.07 μs |  5.83 μs |  1.01 |    0.04 |     584 B |        0.52 |
| ExistsAsync_True              | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |   145.0 μs |   5.41 μs |  6.23 μs |  0.99 |    0.04 |     592 B |        0.52 |
| GetAsync_CacheHit             | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |   146.8 μs |   1.33 μs |  1.47 μs |  1.00 |    0.01 |    1128 B |        1.00 |
| GetAsync_CacheMiss            | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |   147.1 μs |   5.34 μs |  6.14 μs |  1.00 |    0.04 |     640 B |        0.57 |
| GetOrSetAsync_CacheHit        | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |   150.4 μs |   4.27 μs |  4.57 μs |  1.02 |    0.03 |    1432 B |        1.27 |
| SetAsync                      | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |   150.2 μs |   1.05 μs |  1.13 μs |  1.02 |    0.01 |     872 B |        0.77 |
| SetWithSlidingExpirationAsync | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |   295.7 μs |   3.15 μs |  3.50 μs |  2.01 |    0.03 |    1496 B |        1.33 |
|                               |            |                 |                |             |              |             |            |           |          |       |         |           |             |
| ExistsAsync_False             | ShortRun   | Default         | 3              | 1           | 16           | 3           |   141.7 μs |  64.69 μs |  3.55 μs |  0.97 |    0.02 |     584 B |        0.52 |
| ExistsAsync_True              | ShortRun   | Default         | 3              | 1           | 16           | 3           |   138.5 μs |  11.53 μs |  0.63 μs |  0.95 |    0.01 |     592 B |        0.52 |
| GetAsync_CacheHit             | ShortRun   | Default         | 3              | 1           | 16           | 3           |   146.0 μs |  39.99 μs |  2.19 μs |  1.00 |    0.02 |    1128 B |        1.00 |
| GetAsync_CacheMiss            | ShortRun   | Default         | 3              | 1           | 16           | 3           |   138.8 μs |  10.54 μs |  0.58 μs |  0.95 |    0.01 |     640 B |        0.57 |
| GetOrSetAsync_CacheHit        | ShortRun   | Default         | 3              | 1           | 16           | 3           |   150.8 μs |  27.08 μs |  1.48 μs |  1.03 |    0.02 |    1432 B |        1.27 |
| SetAsync                      | ShortRun   | Default         | 3              | 1           | 16           | 3           |   165.9 μs |  22.10 μs |  1.21 μs |  1.14 |    0.02 |     872 B |        0.77 |
| SetWithSlidingExpirationAsync | ShortRun   | Default         | 3              | 1           | 16           | 3           |   298.6 μs |  98.48 μs |  5.40 μs |  2.05 |    0.04 |    1496 B |        1.33 |
|                               |            |                 |                |             |              |             |            |           |          |       |         |           |             |
| GetOrSetAsync_CacheMiss       | Job-ZDPOZY | 1               | 20             | Default     | 1            | 5           |   843.0 μs |  67.08 μs | 74.56 μs |     ? |       ? |    3000 B |           ? |
| RemoveAsync                   | Job-ZDPOZY | 1               | 20             | Default     | 1            | 5           |   255.3 μs |  24.81 μs | 27.57 μs |     ? |       ? |     608 B |           ? |
| RemoveByPatternAsync          | Job-ZDPOZY | 1               | 20             | Default     | 1            | 5           | 1,259.3 μs |  69.00 μs | 73.83 μs |     ? |       ? |    2952 B |           ? |
|                               |            |                 |                |             |              |             |            |           |          |       |         |           |             |
| GetOrSetAsync_CacheMiss       | ShortRun   | 1               | 3              | 1           | 1            | 3           |   857.7 μs | 721.06 μs | 39.52 μs |     ? |       ? |    3000 B |           ? |
| RemoveAsync                   | ShortRun   | 1               | 3              | 1           | 1            | 3           |   255.9 μs | 682.23 μs | 37.40 μs |     ? |       ? |     608 B |           ? |
| RemoveByPatternAsync          | ShortRun   | 1               | 3              | 1           | 1            | 3           | 1,097.6 μs |  76.79 μs |  4.21 μs |     ? |       ? |    3248 B |           ? |
