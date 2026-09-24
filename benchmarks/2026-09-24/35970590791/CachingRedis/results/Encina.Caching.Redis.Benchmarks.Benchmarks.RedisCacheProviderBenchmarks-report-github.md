```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-NUBXJZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-ZDPOZY : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4


```
| Method                        | Job        | InvocationCount | IterationCount | LaunchCount | UnrollFactor | WarmupCount | Mean     | Error     | StdDev   | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------ |----------- |---------------- |--------------- |------------ |------------- |------------ |---------:|----------:|---------:|------:|--------:|----------:|------------:|
| ExistsAsync_False             | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           | 159.7 μs |   0.57 μs |  0.66 μs |  0.94 |    0.00 |     584 B |        0.52 |
| ExistsAsync_True              | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           | 156.0 μs |   1.00 μs |  1.11 μs |  0.92 |    0.01 |     592 B |        0.52 |
| GetAsync_CacheHit             | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           | 170.0 μs |   0.38 μs |  0.42 μs |  1.00 |    0.00 |    1128 B |        1.00 |
| GetAsync_CacheMiss            | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           | 161.4 μs |   0.66 μs |  0.71 μs |  0.95 |    0.00 |     640 B |        0.57 |
| GetOrSetAsync_CacheHit        | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           | 170.2 μs |   0.39 μs |  0.42 μs |  1.00 |    0.00 |    1432 B |        1.27 |
| SetAsync                      | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           | 172.2 μs |   0.60 μs |  0.66 μs |  1.01 |    0.00 |     872 B |        0.77 |
| SetWithSlidingExpirationAsync | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           | 339.4 μs |   0.78 μs |  0.83 μs |  2.00 |    0.01 |    1496 B |        1.33 |
|                               |            |                 |                |             |              |             |          |           |          |       |         |           |             |
| ExistsAsync_False             | ShortRun   | Default         | 3              | 1           | 16           | 3           | 159.6 μs |  37.76 μs |  2.07 μs |  0.94 |    0.01 |     584 B |        0.52 |
| ExistsAsync_True              | ShortRun   | Default         | 3              | 1           | 16           | 3           | 160.3 μs |  12.00 μs |  0.66 μs |  0.94 |    0.00 |     592 B |        0.52 |
| GetAsync_CacheHit             | ShortRun   | Default         | 3              | 1           | 16           | 3           | 169.8 μs |   0.18 μs |  0.01 μs |  1.00 |    0.00 |    1128 B |        1.00 |
| GetAsync_CacheMiss            | ShortRun   | Default         | 3              | 1           | 16           | 3           | 161.5 μs |   7.22 μs |  0.40 μs |  0.95 |    0.00 |     640 B |        0.57 |
| GetOrSetAsync_CacheHit        | ShortRun   | Default         | 3              | 1           | 16           | 3           | 170.4 μs |   4.83 μs |  0.26 μs |  1.00 |    0.00 |    1432 B |        1.27 |
| SetAsync                      | ShortRun   | Default         | 3              | 1           | 16           | 3           | 169.5 μs |   2.31 μs |  0.13 μs |  1.00 |    0.00 |     872 B |        0.77 |
| SetWithSlidingExpirationAsync | ShortRun   | Default         | 3              | 1           | 16           | 3           | 346.7 μs | 205.34 μs | 11.26 μs |  2.04 |    0.06 |    1496 B |        1.33 |
|                               |            |                 |                |             |              |             |          |           |          |       |         |           |             |
| GetOrSetAsync_CacheMiss       | Job-ZDPOZY | 1               | 20             | Default     | 1            | 5           | 697.2 μs |  17.70 μs | 20.38 μs |     ? |       ? |    3000 B |           ? |
| RemoveAsync                   | Job-ZDPOZY | 1               | 20             | Default     | 1            | 5           | 201.8 μs |  12.00 μs | 13.81 μs |     ? |       ? |     608 B |           ? |
| RemoveByPatternAsync          | Job-ZDPOZY | 1               | 20             | Default     | 1            | 5           | 957.7 μs |  24.31 μs | 27.02 μs |     ? |       ? |    2952 B |           ? |
|                               |            |                 |                |             |              |             |          |           |          |       |         |           |             |
| GetOrSetAsync_CacheMiss       | ShortRun   | 1               | 3              | 1           | 1            | 3           | 728.0 μs |  94.21 μs |  5.16 μs |     ? |       ? |    3000 B |           ? |
| RemoveAsync                   | ShortRun   | 1               | 3              | 1           | 1            | 3           | 208.4 μs | 128.65 μs |  7.05 μs |     ? |       ? |     608 B |           ? |
| RemoveByPatternAsync          | ShortRun   | 1               | 3              | 1           | 1            | 3           | 971.7 μs |  60.33 μs |  3.31 μs |     ? |       ? |    3248 B |           ? |
