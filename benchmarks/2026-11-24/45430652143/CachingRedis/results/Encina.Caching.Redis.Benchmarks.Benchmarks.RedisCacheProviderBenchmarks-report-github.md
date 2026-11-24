```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.15GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-NUBXJZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-ZDPOZY : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4


```
| Method                        | Job        | InvocationCount | IterationCount | LaunchCount | UnrollFactor | WarmupCount | Mean       | Error        | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------ |----------- |---------------- |--------------- |------------ |------------- |------------ |-----------:|-------------:|----------:|------:|--------:|----------:|------------:|
| ExistsAsync_False             | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |   105.0 μs |      2.16 μs |   2.48 μs |  1.00 |    0.03 |     583 B |        0.52 |
| ExistsAsync_True              | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |   103.2 μs |      1.20 μs |   1.34 μs |  0.98 |    0.02 |     590 B |        0.52 |
| GetAsync_CacheHit             | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |   104.9 μs |      1.59 μs |   1.70 μs |  1.00 |    0.02 |    1126 B |        1.00 |
| GetAsync_CacheMiss            | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |   102.7 μs |      2.04 μs |   2.35 μs |  0.98 |    0.03 |     637 B |        0.57 |
| GetOrSetAsync_CacheHit        | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |   104.4 μs |      1.99 μs |   2.21 μs |  0.99 |    0.03 |    1431 B |        1.27 |
| SetAsync                      | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |   105.9 μs |      1.24 μs |   1.27 μs |  1.01 |    0.02 |     872 B |        0.77 |
| SetWithSlidingExpirationAsync | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |   206.1 μs |      1.94 μs |   1.99 μs |  1.96 |    0.04 |    1496 B |        1.33 |
|                               |            |                 |                |             |              |             |            |              |           |       |         |           |             |
| ExistsAsync_False             | ShortRun   | Default         | 3              | 1           | 16           | 3           |   104.6 μs |     78.85 μs |   4.32 μs |  1.01 |    0.04 |     580 B |        0.52 |
| ExistsAsync_True              | ShortRun   | Default         | 3              | 1           | 16           | 3           |   103.8 μs |     15.13 μs |   0.83 μs |  1.00 |    0.02 |     590 B |        0.52 |
| GetAsync_CacheHit             | ShortRun   | Default         | 3              | 1           | 16           | 3           |   103.7 μs |     46.12 μs |   2.53 μs |  1.00 |    0.03 |    1125 B |        1.00 |
| GetAsync_CacheMiss            | ShortRun   | Default         | 3              | 1           | 16           | 3           |   102.0 μs |     24.50 μs |   1.34 μs |  0.98 |    0.02 |     637 B |        0.57 |
| GetOrSetAsync_CacheHit        | ShortRun   | Default         | 3              | 1           | 16           | 3           |   107.0 μs |     93.14 μs |   5.11 μs |  1.03 |    0.05 |    1428 B |        1.27 |
| SetAsync                      | ShortRun   | Default         | 3              | 1           | 16           | 3           |   109.4 μs |     86.03 μs |   4.72 μs |  1.06 |    0.05 |     868 B |        0.77 |
| SetWithSlidingExpirationAsync | ShortRun   | Default         | 3              | 1           | 16           | 3           |   208.7 μs |     11.47 μs |   0.63 μs |  2.01 |    0.04 |    1495 B |        1.33 |
|                               |            |                 |                |             |              |             |            |              |           |       |         |           |             |
| GetOrSetAsync_CacheMiss       | Job-ZDPOZY | 1               | 20             | Default     | 1            | 5           |   559.3 μs |     13.47 μs |  15.51 μs |     ? |       ? |    3464 B |           ? |
| RemoveAsync                   | Job-ZDPOZY | 1               | 20             | Default     | 1            | 5           |   164.9 μs |      8.63 μs |   9.59 μs |     ? |       ? |    1072 B |           ? |
| RemoveByPatternAsync          | Job-ZDPOZY | 1               | 20             | Default     | 1            | 5           |   714.9 μs |     17.76 μs |  20.46 μs |     ? |       ? |    4168 B |           ? |
|                               |            |                 |                |             |              |             |            |              |           |       |         |           |             |
| GetOrSetAsync_CacheMiss       | ShortRun   | 1               | 3              | 1           | 1            | 3           | 2,315.8 μs | 12,155.65 μs | 666.29 μs |     ? |       ? |    3464 B |           ? |
| RemoveAsync                   | ShortRun   | 1               | 3              | 1           | 1            | 3           |   176.9 μs |    220.54 μs |  12.09 μs |     ? |       ? |    1072 B |           ? |
| RemoveByPatternAsync          | ShortRun   | 1               | 3              | 1           | 1            | 3           |   734.2 μs |    583.33 μs |  31.97 μs |     ? |       ? |    3712 B |           ? |
