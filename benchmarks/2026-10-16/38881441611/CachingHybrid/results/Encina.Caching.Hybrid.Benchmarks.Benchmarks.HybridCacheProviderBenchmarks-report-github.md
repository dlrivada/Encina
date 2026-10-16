```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  Job-NUBXJZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4


```
| Method                        | Job        | IterationCount | LaunchCount | WarmupCount | Mean       | Error       | StdDev      | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |----------- |--------------- |------------ |------------ |-----------:|------------:|------------:|------:|--------:|-------:|-------:|----------:|------------:|
| ExistsAsync_False             | Job-NUBXJZ | 20             | Default     | 5           |   912.2 ns |     1.88 ns |     2.01 ns |  0.86 |    0.00 | 0.0362 |      - |     616 B |        1.24 |
| ExistsAsync_True              | Job-NUBXJZ | 20             | Default     | 5           |   921.1 ns |     1.44 ns |     1.54 ns |  0.86 |    0.00 | 0.0362 |      - |     616 B |        1.24 |
| GetAsync_CacheHit             | Job-NUBXJZ | 20             | Default     | 5           | 1,065.5 ns |     1.73 ns |     2.00 ns |  1.00 |    0.00 | 0.0286 |      - |     496 B |        1.00 |
| GetAsync_CacheMiss            | Job-NUBXJZ | 20             | Default     | 5           |   882.6 ns |     1.06 ns |     1.09 ns |  0.83 |    0.00 | 0.0362 |      - |     616 B |        1.24 |
| GetOrSetAsync_CacheHit        | Job-NUBXJZ | 20             | Default     | 5           | 1,090.6 ns |     2.33 ns |     2.50 ns |  1.02 |    0.00 | 0.0324 |      - |     560 B |        1.13 |
| GetOrSetAsync_CacheMiss       | Job-NUBXJZ | 20             | Default     | 5           | 5,339.1 ns |    80.90 ns |    83.08 ns |  5.01 |    0.08 | 0.1068 | 0.0992 |    1792 B |        3.61 |
| GetOrSetAsync_WithTags        | Job-NUBXJZ | 20             | Default     | 5           | 9,898.1 ns |   986.35 ns | 1,055.38 ns |  9.29 |    0.96 | 0.1221 | 0.1068 |    2216 B |        4.47 |
| RemoveAsync                   | Job-NUBXJZ | 20             | Default     | 5           | 3,416.3 ns |    39.44 ns |    45.42 ns |  3.21 |    0.04 | 0.0648 |      - |    1136 B |        2.29 |
| RemoveByTagAsync              | Job-NUBXJZ | 20             | Default     | 5           | 8,132.8 ns |   237.65 ns |   244.04 ns |  7.63 |    0.22 | 0.1373 | 0.1221 |    2448 B |        4.94 |
| SetAsync                      | Job-NUBXJZ | 20             | Default     | 5           | 3,866.0 ns |   132.04 ns |   141.28 ns |  3.63 |    0.13 | 0.0610 | 0.0572 |    1064 B |        2.15 |
| SetWithSlidingExpirationAsync | Job-NUBXJZ | 20             | Default     | 5           | 4,034.5 ns |   452.08 ns |   502.49 ns |  3.79 |    0.46 | 0.0610 | 0.0572 |    1072 B |        2.16 |
|                               |            |                |             |             |            |             |             |       |         |        |        |           |             |
| ExistsAsync_False             | ShortRun   | 3              | 1           | 3           |   899.3 ns |    29.75 ns |     1.63 ns |  0.84 |    0.00 | 0.0362 |      - |     616 B |        1.24 |
| ExistsAsync_True              | ShortRun   | 3              | 1           | 3           |   920.5 ns |    23.58 ns |     1.29 ns |  0.86 |    0.00 | 0.0362 |      - |     616 B |        1.24 |
| GetAsync_CacheHit             | ShortRun   | 3              | 1           | 3           | 1,074.4 ns |    42.28 ns |     2.32 ns |  1.00 |    0.00 | 0.0286 |      - |     496 B |        1.00 |
| GetAsync_CacheMiss            | ShortRun   | 3              | 1           | 3           |   865.0 ns |    17.91 ns |     0.98 ns |  0.81 |    0.00 | 0.0362 |      - |     616 B |        1.24 |
| GetOrSetAsync_CacheHit        | ShortRun   | 3              | 1           | 3           | 1,089.3 ns |    30.94 ns |     1.70 ns |  1.01 |    0.00 | 0.0324 |      - |     560 B |        1.13 |
| GetOrSetAsync_CacheMiss       | ShortRun   | 3              | 1           | 3           | 5,083.5 ns |   843.25 ns |    46.22 ns |  4.73 |    0.04 | 0.1068 | 0.0992 |    1792 B |        3.61 |
| GetOrSetAsync_WithTags        | ShortRun   | 3              | 1           | 3           | 7,871.8 ns | 4,142.77 ns |   227.08 ns |  7.33 |    0.18 | 0.1221 | 0.1068 |    2216 B |        4.47 |
| RemoveAsync                   | ShortRun   | 3              | 1           | 3           | 3,376.5 ns | 1,299.67 ns |    71.24 ns |  3.14 |    0.06 | 0.0648 |      - |    1136 B |        2.29 |
| RemoveByTagAsync              | ShortRun   | 3              | 1           | 3           | 7,885.7 ns | 3,415.64 ns |   187.22 ns |  7.34 |    0.15 | 0.1373 | 0.1221 |    2448 B |        4.94 |
| SetAsync                      | ShortRun   | 3              | 1           | 3           | 3,596.5 ns |   524.99 ns |    28.78 ns |  3.35 |    0.02 | 0.0610 | 0.0572 |    1064 B |        2.15 |
| SetWithSlidingExpirationAsync | ShortRun   | 3              | 1           | 3           | 3,555.4 ns |   569.44 ns |    31.21 ns |  3.31 |    0.03 | 0.0610 | 0.0572 |    1072 B |        2.16 |
