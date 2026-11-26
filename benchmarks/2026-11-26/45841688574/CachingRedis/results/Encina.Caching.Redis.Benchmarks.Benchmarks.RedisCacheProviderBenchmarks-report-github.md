```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.00GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  Job-NUBXJZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  Job-ZDPOZY : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3


```
| Method                        | Job        | InvocationCount | IterationCount | LaunchCount | UnrollFactor | WarmupCount | Mean       | Error        | StdDev      | Median     | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------ |----------- |---------------- |--------------- |------------ |------------- |------------ |-----------:|-------------:|------------:|-----------:|------:|--------:|----------:|------------:|
| ExistsAsync_False             | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |   225.5 μs |      1.75 μs |     1.88 μs |   225.1 μs |  0.95 |    0.01 |     584 B |        0.52 |
| ExistsAsync_True              | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |   227.5 μs |      1.96 μs |     2.01 μs |   227.3 μs |  0.95 |    0.01 |     592 B |        0.52 |
| GetAsync_CacheHit             | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |   238.5 μs |      2.11 μs |     2.25 μs |   237.5 μs |  1.00 |    0.01 |    1128 B |        1.00 |
| GetAsync_CacheMiss            | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |   225.9 μs |      1.58 μs |     1.75 μs |   225.9 μs |  0.95 |    0.01 |     640 B |        0.57 |
| GetOrSetAsync_CacheHit        | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |   240.8 μs |      1.57 μs |     1.68 μs |   240.3 μs |  1.01 |    0.01 |    1432 B |        1.27 |
| SetAsync                      | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |   238.6 μs |      0.86 μs |     0.88 μs |   238.5 μs |  1.00 |    0.01 |     872 B |        0.77 |
| SetWithSlidingExpirationAsync | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |   469.5 μs |      1.62 μs |     1.80 μs |   469.7 μs |  1.97 |    0.02 |    1496 B |        1.33 |
|                               |            |                 |                |             |              |             |            |              |             |            |       |         |           |             |
| ExistsAsync_False             | ShortRun   | Default         | 3              | 1           | 16           | 3           |   234.2 μs |    172.33 μs |     9.45 μs |   229.9 μs |  0.98 |    0.03 |     584 B |        0.52 |
| ExistsAsync_True              | ShortRun   | Default         | 3              | 1           | 16           | 3           |   229.8 μs |    178.45 μs |     9.78 μs |   224.6 μs |  0.96 |    0.04 |     592 B |        0.52 |
| GetAsync_CacheHit             | ShortRun   | Default         | 3              | 1           | 16           | 3           |   239.9 μs |     28.88 μs |     1.58 μs |   239.1 μs |  1.00 |    0.01 |    1128 B |        1.00 |
| GetAsync_CacheMiss            | ShortRun   | Default         | 3              | 1           | 16           | 3           |   229.3 μs |    148.19 μs |     8.12 μs |   225.8 μs |  0.96 |    0.03 |     640 B |        0.57 |
| GetOrSetAsync_CacheHit        | ShortRun   | Default         | 3              | 1           | 16           | 3           |   261.2 μs |    377.85 μs |    20.71 μs |   249.9 μs |  1.09 |    0.08 |    1420 B |        1.26 |
| SetAsync                      | ShortRun   | Default         | 3              | 1           | 16           | 3           |   258.0 μs |    263.42 μs |    14.44 μs |   258.1 μs |  1.08 |    0.05 |     872 B |        0.77 |
| SetWithSlidingExpirationAsync | ShortRun   | Default         | 3              | 1           | 16           | 3           |   482.2 μs |    327.68 μs |    17.96 μs |   472.7 μs |  2.01 |    0.07 |    1496 B |        1.33 |
|                               |            |                 |                |             |              |             |            |              |             |            |       |         |           |             |
| GetOrSetAsync_CacheMiss       | Job-ZDPOZY | 1               | 20             | Default     | 1            | 5           | 1,081.1 μs |     56.82 μs |    60.80 μs | 1,073.5 μs |     ? |       ? |    3464 B |           ? |
| RemoveAsync                   | Job-ZDPOZY | 1               | 20             | Default     | 1            | 5           |   295.5 μs |     31.44 μs |    30.88 μs |   294.8 μs |     ? |       ? |    1072 B |           ? |
| RemoveByPatternAsync          | Job-ZDPOZY | 1               | 20             | Default     | 1            | 5           | 1,606.6 μs |    321.41 μs |   343.90 μs | 1,397.4 μs |     ? |       ? |    3416 B |           ? |
|                               |            |                 |                |             |              |             |            |              |             |            |       |         |           |             |
| GetOrSetAsync_CacheMiss       | ShortRun   | 1               | 3              | 1           | 1            | 3           | 1,090.7 μs |  2,342.61 μs |   128.41 μs | 1,019.3 μs |     ? |       ? |    3464 B |           ? |
| RemoveAsync                   | ShortRun   | 1               | 3              | 1           | 1            | 3           |   289.5 μs |    380.06 μs |    20.83 μs |   290.3 μs |     ? |       ? |    1072 B |           ? |
| RemoveByPatternAsync          | ShortRun   | 1               | 3              | 1           | 1            | 3           | 3,957.4 μs | 43,620.42 μs | 2,390.98 μs | 2,937.3 μs |     ? |       ? |    3992 B |           ? |
