```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-NUBXJZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-ZDPOZY : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4


```
| Method                        | Job        | InvocationCount | IterationCount | LaunchCount | UnrollFactor | WarmupCount | Mean       | Error       | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------ |----------- |---------------- |--------------- |------------ |------------- |------------ |-----------:|------------:|----------:|------:|--------:|----------:|------------:|
| ExistsAsync_False             | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |   147.0 μs |     1.33 μs |   1.42 μs |  0.91 |    0.02 |     584 B |        0.52 |
| ExistsAsync_True              | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |   145.5 μs |     1.18 μs |   1.26 μs |  0.90 |    0.02 |     592 B |        0.52 |
| GetAsync_CacheHit             | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |   161.0 μs |     3.07 μs |   3.41 μs |  1.00 |    0.03 |    1128 B |        1.00 |
| GetAsync_CacheMiss            | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |   150.6 μs |     1.69 μs |   1.74 μs |  0.94 |    0.02 |     640 B |        0.57 |
| GetOrSetAsync_CacheHit        | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |   159.0 μs |     1.46 μs |   1.49 μs |  0.99 |    0.02 |    1432 B |        1.27 |
| SetAsync                      | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |   163.9 μs |     2.55 μs |   2.73 μs |  1.02 |    0.03 |     871 B |        0.77 |
| SetWithSlidingExpirationAsync | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |   319.5 μs |     1.75 μs |   1.94 μs |  1.99 |    0.04 |    1496 B |        1.33 |
|                               |            |                 |                |             |              |             |            |             |           |       |         |           |             |
| ExistsAsync_False             | ShortRun   | Default         | 3              | 1           | 16           | 3           |   144.9 μs |    22.59 μs |   1.24 μs |  0.90 |    0.01 |     584 B |        0.52 |
| ExistsAsync_True              | ShortRun   | Default         | 3              | 1           | 16           | 3           |   148.3 μs |    10.29 μs |   0.56 μs |  0.92 |    0.01 |     592 B |        0.52 |
| GetAsync_CacheHit             | ShortRun   | Default         | 3              | 1           | 16           | 3           |   161.9 μs |    48.16 μs |   2.64 μs |  1.00 |    0.02 |    1128 B |        1.00 |
| GetAsync_CacheMiss            | ShortRun   | Default         | 3              | 1           | 16           | 3           |   150.6 μs |    11.53 μs |   0.63 μs |  0.93 |    0.01 |     640 B |        0.57 |
| GetOrSetAsync_CacheHit        | ShortRun   | Default         | 3              | 1           | 16           | 3           |   159.6 μs |    17.69 μs |   0.97 μs |  0.99 |    0.01 |    1432 B |        1.27 |
| SetAsync                      | ShortRun   | Default         | 3              | 1           | 16           | 3           |   161.8 μs |    23.04 μs |   1.26 μs |  1.00 |    0.02 |     872 B |        0.77 |
| SetWithSlidingExpirationAsync | ShortRun   | Default         | 3              | 1           | 16           | 3           |   317.4 μs |    44.49 μs |   2.44 μs |  1.96 |    0.03 |    1496 B |        1.33 |
|                               |            |                 |                |             |              |             |            |             |           |       |         |           |             |
| GetOrSetAsync_CacheMiss       | Job-ZDPOZY | 1               | 20             | Default     | 1            | 5           |   743.7 μs |    27.95 μs |  32.18 μs |     ? |       ? |    3464 B |           ? |
| RemoveAsync                   | Job-ZDPOZY | 1               | 20             | Default     | 1            | 5           |   229.5 μs |    12.42 μs |  13.80 μs |     ? |       ? |    1072 B |           ? |
| RemoveByPatternAsync          | Job-ZDPOZY | 1               | 20             | Default     | 1            | 5           | 1,032.3 μs |    29.55 μs |  34.03 μs |     ? |       ? |    3416 B |           ? |
|                               |            |                 |                |             |              |             |            |             |           |       |         |           |             |
| GetOrSetAsync_CacheMiss       | ShortRun   | 1               | 3              | 1           | 1            | 3           |   891.3 μs | 2,575.93 μs | 141.20 μs |     ? |       ? |    3464 B |           ? |
| RemoveAsync                   | ShortRun   | 1               | 3              | 1           | 1            | 3           |   397.9 μs | 1,849.40 μs | 101.37 μs |     ? |       ? |    1136 B |           ? |
| RemoveByPatternAsync          | ShortRun   | 1               | 3              | 1           | 1            | 3           | 1,022.6 μs |   276.12 μs |  15.14 μs |     ? |       ? |    3712 B |           ? |
