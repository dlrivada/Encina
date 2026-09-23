```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-NUBXJZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-ZDPOZY : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4


```
| Method                        | Job        | InvocationCount | IterationCount | LaunchCount | UnrollFactor | WarmupCount | Mean       | Error       | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------ |----------- |---------------- |--------------- |------------ |------------- |------------ |-----------:|------------:|----------:|------:|--------:|----------:|------------:|
| ExistsAsync_False             | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |   105.6 μs |     1.44 μs |   1.60 μs |  0.96 |    0.03 |     584 B |        0.52 |
| ExistsAsync_True              | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |   107.2 μs |     1.26 μs |   1.40 μs |  0.97 |    0.02 |     592 B |        0.52 |
| GetAsync_CacheHit             | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |   110.3 μs |     2.12 μs |   2.45 μs |  1.00 |    0.03 |    1128 B |        1.00 |
| GetAsync_CacheMiss            | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |   108.7 μs |     1.32 μs |   1.52 μs |  0.99 |    0.03 |     639 B |        0.57 |
| GetOrSetAsync_CacheHit        | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |   111.4 μs |     0.78 μs |   0.83 μs |  1.01 |    0.02 |    1432 B |        1.27 |
| SetAsync                      | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |   113.1 μs |     1.57 μs |   1.75 μs |  1.03 |    0.03 |     872 B |        0.77 |
| SetWithSlidingExpirationAsync | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |   221.5 μs |     3.12 μs |   3.47 μs |  2.01 |    0.05 |    1496 B |        1.33 |
|                               |            |                 |                |             |              |             |            |             |           |       |         |           |             |
| ExistsAsync_False             | ShortRun   | Default         | 3              | 1           | 16           | 3           |   107.7 μs |     6.82 μs |   0.37 μs |  0.98 |    0.01 |     583 B |        0.52 |
| ExistsAsync_True              | ShortRun   | Default         | 3              | 1           | 16           | 3           |   105.6 μs |    29.94 μs |   1.64 μs |  0.96 |    0.02 |     590 B |        0.52 |
| GetAsync_CacheHit             | ShortRun   | Default         | 3              | 1           | 16           | 3           |   110.3 μs |    26.95 μs |   1.48 μs |  1.00 |    0.02 |    1126 B |        1.00 |
| GetAsync_CacheMiss            | ShortRun   | Default         | 3              | 1           | 16           | 3           |   109.0 μs |    14.46 μs |   0.79 μs |  0.99 |    0.01 |     637 B |        0.57 |
| GetOrSetAsync_CacheHit        | ShortRun   | Default         | 3              | 1           | 16           | 3           |   113.0 μs |    57.03 μs |   3.13 μs |  1.02 |    0.03 |    1430 B |        1.27 |
| SetAsync                      | ShortRun   | Default         | 3              | 1           | 16           | 3           |   113.4 μs |    26.25 μs |   1.44 μs |  1.03 |    0.02 |     869 B |        0.77 |
| SetWithSlidingExpirationAsync | ShortRun   | Default         | 3              | 1           | 16           | 3           |   209.8 μs |    16.70 μs |   0.92 μs |  1.90 |    0.02 |    1496 B |        1.33 |
|                               |            |                 |                |             |              |             |            |             |           |       |         |           |             |
| GetOrSetAsync_CacheMiss       | Job-ZDPOZY | 1               | 20             | Default     | 1            | 5           |   530.8 μs |    28.77 μs |  31.98 μs |     ? |       ? |    3000 B |           ? |
| RemoveAsync                   | Job-ZDPOZY | 1               | 20             | Default     | 1            | 5           |   146.4 μs |     8.34 μs |   9.27 μs |     ? |       ? |     608 B |           ? |
| RemoveByPatternAsync          | Job-ZDPOZY | 1               | 20             | Default     | 1            | 5           |   932.4 μs |   367.96 μs | 393.71 μs |     ? |       ? |    3248 B |           ? |
|                               |            |                 |                |             |              |             |            |             |           |       |         |           |             |
| GetOrSetAsync_CacheMiss       | ShortRun   | 1               | 3              | 1           | 1            | 3           |   632.2 μs | 2,047.22 μs | 112.21 μs |     ? |       ? |    3000 B |           ? |
| RemoveAsync                   | ShortRun   | 1               | 3              | 1           | 1            | 3           |   193.0 μs |   890.45 μs |  48.81 μs |     ? |       ? |     640 B |           ? |
| RemoveByPatternAsync          | ShortRun   | 1               | 3              | 1           | 1            | 3           | 1,068.8 μs | 5,630.37 μs | 308.62 μs |     ? |       ? |    2952 B |           ? |
