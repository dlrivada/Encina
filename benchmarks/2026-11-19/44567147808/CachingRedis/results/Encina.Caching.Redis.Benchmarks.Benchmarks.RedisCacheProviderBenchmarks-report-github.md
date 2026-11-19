```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.59GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  Job-NUBXJZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  Job-ZDPOZY : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3


```
| Method                        | Job        | InvocationCount | IterationCount | LaunchCount | UnrollFactor | WarmupCount | Mean       | Error        | StdDev      | Median     | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------ |----------- |---------------- |--------------- |------------ |------------- |------------ |-----------:|-------------:|------------:|-----------:|------:|--------:|----------:|------------:|
| ExistsAsync_False             | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |   224.7 μs |      0.91 μs |     0.97 μs |   224.4 μs |  0.93 |    0.01 |     584 B |        0.52 |
| ExistsAsync_True              | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |   225.8 μs |      1.16 μs |     1.29 μs |   225.7 μs |  0.93 |    0.01 |     592 B |        0.52 |
| GetAsync_CacheHit             | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |   241.6 μs |      1.77 μs |     1.89 μs |   241.1 μs |  1.00 |    0.01 |    1128 B |        1.00 |
| GetAsync_CacheMiss            | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |   227.6 μs |      1.52 μs |     1.63 μs |   227.4 μs |  0.94 |    0.01 |     640 B |        0.57 |
| GetOrSetAsync_CacheHit        | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |   241.7 μs |      1.54 μs |     1.72 μs |   241.1 μs |  1.00 |    0.01 |    1432 B |        1.27 |
| SetAsync                      | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |   240.8 μs |      1.06 μs |     1.14 μs |   240.9 μs |  1.00 |    0.01 |     872 B |        0.77 |
| SetWithSlidingExpirationAsync | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |   473.4 μs |      2.05 μs |     2.28 μs |   473.5 μs |  1.96 |    0.02 |    1496 B |        1.33 |
|                               |            |                 |                |             |              |             |            |              |             |            |       |         |           |             |
| ExistsAsync_False             | ShortRun   | Default         | 3              | 1           | 16           | 3           |   230.8 μs |    197.28 μs |    10.81 μs |   224.9 μs |  0.95 |    0.04 |     584 B |        0.52 |
| ExistsAsync_True              | ShortRun   | Default         | 3              | 1           | 16           | 3           |   230.1 μs |    168.26 μs |     9.22 μs |   226.6 μs |  0.94 |    0.03 |     592 B |        0.52 |
| GetAsync_CacheHit             | ShortRun   | Default         | 3              | 1           | 16           | 3           |   243.7 μs |     55.68 μs |     3.05 μs |   244.8 μs |  1.00 |    0.02 |    1128 B |        1.00 |
| GetAsync_CacheMiss            | ShortRun   | Default         | 3              | 1           | 16           | 3           |   230.9 μs |    162.69 μs |     8.92 μs |   227.4 μs |  0.95 |    0.03 |     640 B |        0.57 |
| GetOrSetAsync_CacheHit        | ShortRun   | Default         | 3              | 1           | 16           | 3           |   244.5 μs |     17.98 μs |     0.99 μs |   244.1 μs |  1.00 |    0.01 |    1432 B |        1.27 |
| SetAsync                      | ShortRun   | Default         | 3              | 1           | 16           | 3           |   243.8 μs |     44.49 μs |     2.44 μs |   242.4 μs |  1.00 |    0.01 |     872 B |        0.77 |
| SetWithSlidingExpirationAsync | ShortRun   | Default         | 3              | 1           | 16           | 3           |   472.8 μs |     51.98 μs |     2.85 μs |   471.7 μs |  1.94 |    0.02 |    1496 B |        1.33 |
|                               |            |                 |                |             |              |             |            |              |             |            |       |         |           |             |
| GetOrSetAsync_CacheMiss       | Job-ZDPOZY | 1               | 20             | Default     | 1            | 5           |   990.5 μs |     12.10 μs |    13.44 μs |   991.8 μs |     ? |       ? |    3464 B |           ? |
| RemoveAsync                   | Job-ZDPOZY | 1               | 20             | Default     | 1            | 5           |   280.6 μs |     12.26 μs |    13.63 μs |   282.0 μs |     ? |       ? |    1072 B |           ? |
| RemoveByPatternAsync          | Job-ZDPOZY | 1               | 20             | Default     | 1            | 5           | 1,472.5 μs |    132.30 μs |   135.86 μs | 1,416.4 μs |     ? |       ? |    3416 B |           ? |
|                               |            |                 |                |             |              |             |            |              |             |            |       |         |           |             |
| GetOrSetAsync_CacheMiss       | ShortRun   | 1               | 3              | 1           | 1            | 3           | 1,054.9 μs |    783.98 μs |    42.97 μs | 1,037.4 μs |     ? |       ? |    3464 B |           ? |
| RemoveAsync                   | ShortRun   | 1               | 3              | 1           | 1            | 3           |   296.8 μs |    266.22 μs |    14.59 μs |   304.8 μs |     ? |       ? |    1072 B |           ? |
| RemoveByPatternAsync          | ShortRun   | 1               | 3              | 1           | 1            | 3           | 2,812.1 μs | 40,734.11 μs | 2,232.77 μs | 1,541.1 μs |     ? |       ? |    3712 B |           ? |
