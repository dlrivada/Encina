```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.23GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  Job-NUBXJZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  Job-ZDPOZY : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3


```
| Method                        | Job        | InvocationCount | IterationCount | LaunchCount | UnrollFactor | WarmupCount | Mean       | Error     | StdDev   | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------ |----------- |---------------- |--------------- |------------ |------------- |------------ |-----------:|----------:|---------:|------:|--------:|----------:|------------:|
| ExistsAsync_False             | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |   224.1 μs |   1.32 μs |  1.52 μs |  0.93 |    0.01 |     584 B |        0.52 |
| ExistsAsync_True              | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |   223.1 μs |   0.93 μs |  0.96 μs |  0.93 |    0.01 |     592 B |        0.52 |
| GetAsync_CacheHit             | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |   241.1 μs |   2.11 μs |  2.34 μs |  1.00 |    0.01 |    1128 B |        1.00 |
| GetAsync_CacheMiss            | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |   223.9 μs |   1.18 μs |  1.26 μs |  0.93 |    0.01 |     640 B |        0.57 |
| GetOrSetAsync_CacheHit        | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |   241.2 μs |   3.70 μs |  3.63 μs |  1.00 |    0.02 |    1432 B |        1.27 |
| SetAsync                      | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |   238.2 μs |   1.33 μs |  1.36 μs |  0.99 |    0.01 |     872 B |        0.77 |
| SetWithSlidingExpirationAsync | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |   468.4 μs |   2.84 μs |  3.15 μs |  1.94 |    0.02 |    1496 B |        1.33 |
|                               |            |                 |                |             |              |             |            |           |          |       |         |           |             |
| ExistsAsync_False             | ShortRun   | Default         | 3              | 1           | 16           | 3           |   229.4 μs | 165.08 μs |  9.05 μs |  0.93 |    0.04 |     584 B |        0.52 |
| ExistsAsync_True              | ShortRun   | Default         | 3              | 1           | 16           | 3           |   234.0 μs | 169.84 μs |  9.31 μs |  0.95 |    0.05 |     592 B |        0.52 |
| GetAsync_CacheHit             | ShortRun   | Default         | 3              | 1           | 16           | 3           |   247.6 μs | 174.95 μs |  9.59 μs |  1.00 |    0.05 |    1128 B |        1.00 |
| GetAsync_CacheMiss            | ShortRun   | Default         | 3              | 1           | 16           | 3           |   229.6 μs | 176.74 μs |  9.69 μs |  0.93 |    0.05 |     640 B |        0.57 |
| GetOrSetAsync_CacheHit        | ShortRun   | Default         | 3              | 1           | 16           | 3           |   242.4 μs |  53.04 μs |  2.91 μs |  0.98 |    0.03 |    1432 B |        1.27 |
| SetAsync                      | ShortRun   | Default         | 3              | 1           | 16           | 3           |   247.1 μs | 172.62 μs |  9.46 μs |  1.00 |    0.05 |     872 B |        0.77 |
| SetWithSlidingExpirationAsync | ShortRun   | Default         | 3              | 1           | 16           | 3           |   484.0 μs | 393.04 μs | 21.54 μs |  1.96 |    0.10 |    1496 B |        1.33 |
|                               |            |                 |                |             |              |             |            |           |          |       |         |           |             |
| GetOrSetAsync_CacheMiss       | Job-ZDPOZY | 1               | 20             | Default     | 1            | 5           |   997.0 μs |  17.40 μs | 18.62 μs |     ? |       ? |    3464 B |           ? |
| RemoveAsync                   | Job-ZDPOZY | 1               | 20             | Default     | 1            | 5           |   269.0 μs |  11.18 μs | 12.88 μs |     ? |       ? |    1072 B |           ? |
| RemoveByPatternAsync          | Job-ZDPOZY | 1               | 20             | Default     | 1            | 5           | 1,397.1 μs |  36.34 μs | 35.69 μs |     ? |       ? |    3416 B |           ? |
|                               |            |                 |                |             |              |             |            |           |          |       |         |           |             |
| GetOrSetAsync_CacheMiss       | ShortRun   | 1               | 3              | 1           | 1            | 3           | 1,002.5 μs | 676.45 μs | 37.08 μs |     ? |       ? |    3528 B |           ? |
| RemoveAsync                   | ShortRun   | 1               | 3              | 1           | 1            | 3           |   277.9 μs |  44.74 μs |  2.45 μs |     ? |       ? |    1072 B |           ? |
| RemoveByPatternAsync          | ShortRun   | 1               | 3              | 1           | 1            | 3           | 1,423.7 μs | 932.35 μs | 51.11 μs |     ? |       ? |    3416 B |           ? |
