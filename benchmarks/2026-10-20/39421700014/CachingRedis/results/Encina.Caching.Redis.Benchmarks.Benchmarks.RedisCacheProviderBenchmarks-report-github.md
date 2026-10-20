```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  Job-NUBXJZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  Job-ZDPOZY : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3


```
| Method                        | Job        | InvocationCount | IterationCount | LaunchCount | UnrollFactor | WarmupCount | Mean       | Error        | StdDev    | Median     | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------ |----------- |---------------- |--------------- |------------ |------------- |------------ |-----------:|-------------:|----------:|-----------:|------:|--------:|----------:|------------:|
| ExistsAsync_False             | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |   266.7 μs |      1.62 μs |   1.86 μs |   266.2 μs |  0.95 |    0.01 |     584 B |        0.52 |
| ExistsAsync_True              | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |   265.4 μs |      1.42 μs |   1.58 μs |   265.6 μs |  0.94 |    0.01 |     592 B |        0.52 |
| GetAsync_CacheHit             | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |   281.1 μs |      1.67 μs |   1.79 μs |   280.9 μs |  1.00 |    0.01 |    1128 B |        1.00 |
| GetAsync_CacheMiss            | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |   271.7 μs |      1.23 μs |   1.37 μs |   271.6 μs |  0.97 |    0.01 |     640 B |        0.57 |
| GetOrSetAsync_CacheHit        | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |   280.6 μs |      1.21 μs |   1.34 μs |   280.7 μs |  1.00 |    0.01 |    1432 B |        1.27 |
| SetAsync                      | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |   289.9 μs |      2.72 μs |   2.91 μs |   289.2 μs |  1.03 |    0.01 |     872 B |        0.77 |
| SetWithSlidingExpirationAsync | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |   563.7 μs |      1.37 μs |   1.46 μs |   563.5 μs |  2.01 |    0.01 |    1496 B |        1.33 |
|                               |            |                 |                |             |              |             |            |              |           |            |       |         |           |             |
| ExistsAsync_False             | ShortRun   | Default         | 3              | 1           | 16           | 3           |   266.0 μs |     20.35 μs |   1.12 μs |   266.5 μs |  0.95 |    0.01 |     584 B |        0.52 |
| ExistsAsync_True              | ShortRun   | Default         | 3              | 1           | 16           | 3           |   265.2 μs |     44.77 μs |   2.45 μs |   264.8 μs |  0.95 |    0.01 |     592 B |        0.52 |
| GetAsync_CacheHit             | ShortRun   | Default         | 3              | 1           | 16           | 3           |   280.4 μs |     28.43 μs |   1.56 μs |   280.3 μs |  1.00 |    0.01 |    1128 B |        1.00 |
| GetAsync_CacheMiss            | ShortRun   | Default         | 3              | 1           | 16           | 3           |   271.7 μs |     25.45 μs |   1.39 μs |   272.1 μs |  0.97 |    0.01 |     640 B |        0.57 |
| GetOrSetAsync_CacheHit        | ShortRun   | Default         | 3              | 1           | 16           | 3           |   282.8 μs |     47.24 μs |   2.59 μs |   281.9 μs |  1.01 |    0.01 |    1432 B |        1.27 |
| SetAsync                      | ShortRun   | Default         | 3              | 1           | 16           | 3           |   289.0 μs |     52.00 μs |   2.85 μs |   290.1 μs |  1.03 |    0.01 |     872 B |        0.77 |
| SetWithSlidingExpirationAsync | ShortRun   | Default         | 3              | 1           | 16           | 3           |   567.5 μs |     46.21 μs |   2.53 μs |   566.9 μs |  2.02 |    0.01 |    1496 B |        1.33 |
|                               |            |                 |                |             |              |             |            |              |           |            |       |         |           |             |
| GetOrSetAsync_CacheMiss       | Job-ZDPOZY | 1               | 20             | Default     | 1            | 5           | 1,120.0 μs |     51.69 μs |  57.46 μs | 1,096.9 μs |     ? |       ? |    3464 B |           ? |
| RemoveAsync                   | Job-ZDPOZY | 1               | 20             | Default     | 1            | 5           |   329.9 μs |     27.69 μs |  29.63 μs |   331.9 μs |     ? |       ? |    1072 B |           ? |
| RemoveByPatternAsync          | Job-ZDPOZY | 1               | 20             | Default     | 1            | 5           | 1,560.0 μs |     78.89 μs |  90.85 μs | 1,567.8 μs |     ? |       ? |    3416 B |           ? |
|                               |            |                 |                |             |              |             |            |              |           |            |       |         |           |             |
| GetOrSetAsync_CacheMiss       | ShortRun   | 1               | 3              | 1           | 1            | 3           | 1,116.1 μs |    533.24 μs |  29.23 μs | 1,117.6 μs |     ? |       ? |    3464 B |           ? |
| RemoveAsync                   | ShortRun   | 1               | 3              | 1           | 1            | 3           |   375.2 μs |  2,051.67 μs | 112.46 μs |   339.3 μs |     ? |       ? |    9288 B |           ? |
| RemoveByPatternAsync          | ShortRun   | 1               | 3              | 1           | 1            | 3           | 1,961.2 μs | 13,347.62 μs | 731.63 μs | 1,553.2 μs |     ? |       ? |    3416 B |           ? |
