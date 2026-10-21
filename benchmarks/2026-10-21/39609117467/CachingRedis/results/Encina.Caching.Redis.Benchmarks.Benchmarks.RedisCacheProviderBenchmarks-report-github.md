```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  Job-NUBXJZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  Job-ZDPOZY : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3


```
| Method                        | Job        | InvocationCount | IterationCount | LaunchCount | UnrollFactor | WarmupCount | Mean       | Error       | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------ |----------- |---------------- |--------------- |------------ |------------- |------------ |-----------:|------------:|----------:|------:|--------:|----------:|------------:|
| ExistsAsync_False             | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |   203.6 μs |     0.52 μs |   0.60 μs |  0.93 |    0.00 |     584 B |        0.52 |
| ExistsAsync_True              | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |   204.5 μs |     0.86 μs |   0.92 μs |  0.93 |    0.01 |     592 B |        0.52 |
| GetAsync_CacheHit             | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |   219.6 μs |     0.83 μs |   0.92 μs |  1.00 |    0.01 |    1128 B |        1.00 |
| GetAsync_CacheMiss            | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |   215.4 μs |     7.11 μs |   7.61 μs |  0.98 |    0.03 |     640 B |        0.57 |
| GetOrSetAsync_CacheHit        | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |   299.1 μs |    53.55 μs |  61.67 μs |  1.36 |    0.27 |    1432 B |        1.27 |
| SetAsync                      | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |   233.8 μs |     7.73 μs |   8.27 μs |  1.06 |    0.04 |     872 B |        0.77 |
| SetWithSlidingExpirationAsync | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |   446.1 μs |    12.25 μs |  12.03 μs |  2.03 |    0.05 |    1496 B |        1.33 |
|                               |            |                 |                |             |              |             |            |             |           |       |         |           |             |
| ExistsAsync_False             | ShortRun   | Default         | 3              | 1           | 16           | 3           |   205.0 μs |     6.13 μs |   0.34 μs |  0.93 |    0.02 |     583 B |        0.52 |
| ExistsAsync_True              | ShortRun   | Default         | 3              | 1           | 16           | 3           |   202.0 μs |    10.79 μs |   0.59 μs |  0.92 |    0.02 |     589 B |        0.52 |
| GetAsync_CacheHit             | ShortRun   | Default         | 3              | 1           | 16           | 3           |   220.2 μs |    84.01 μs |   4.60 μs |  1.00 |    0.03 |    1128 B |        1.00 |
| GetAsync_CacheMiss            | ShortRun   | Default         | 3              | 1           | 16           | 3           |   206.7 μs |    18.32 μs |   1.00 μs |  0.94 |    0.02 |     640 B |        0.57 |
| GetOrSetAsync_CacheHit        | ShortRun   | Default         | 3              | 1           | 16           | 3           |   229.2 μs |    58.46 μs |   3.20 μs |  1.04 |    0.02 |    1432 B |        1.27 |
| SetAsync                      | ShortRun   | Default         | 3              | 1           | 16           | 3           |   273.9 μs |   134.98 μs |   7.40 μs |  1.24 |    0.04 |     872 B |        0.77 |
| SetWithSlidingExpirationAsync | ShortRun   | Default         | 3              | 1           | 16           | 3           |   442.9 μs |   213.00 μs |  11.68 μs |  2.01 |    0.06 |    1496 B |        1.33 |
|                               |            |                 |                |             |              |             |            |             |           |       |         |           |             |
| GetOrSetAsync_CacheMiss       | Job-ZDPOZY | 1               | 20             | Default     | 1            | 5           | 1,078.8 μs |   118.91 μs | 122.11 μs |     ? |       ? |    3464 B |           ? |
| RemoveAsync                   | Job-ZDPOZY | 1               | 20             | Default     | 1            | 5           |   331.1 μs |    62.86 μs |  64.55 μs |     ? |       ? |    1072 B |           ? |
| RemoveByPatternAsync          | Job-ZDPOZY | 1               | 20             | Default     | 1            | 5           | 1,770.8 μs |   470.54 μs | 503.47 μs |     ? |       ? |    3416 B |           ? |
|                               |            |                 |                |             |              |             |            |             |           |       |         |           |             |
| GetOrSetAsync_CacheMiss       | ShortRun   | 1               | 3              | 1           | 1            | 3           | 1,292.6 μs |   702.96 μs |  38.53 μs |     ? |       ? |    3464 B |           ? |
| RemoveAsync                   | ShortRun   | 1               | 3              | 1           | 1            | 3           |   294.9 μs |   437.88 μs |  24.00 μs |     ? |       ? |    1072 B |           ? |
| RemoveByPatternAsync          | ShortRun   | 1               | 3              | 1           | 1            | 3           | 2,102.1 μs | 6,302.11 μs | 345.44 μs |     ? |       ? |    3712 B |           ? |
