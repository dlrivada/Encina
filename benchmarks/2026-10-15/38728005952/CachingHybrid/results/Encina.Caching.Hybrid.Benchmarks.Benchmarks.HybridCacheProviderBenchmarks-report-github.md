```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  Job-NUBXJZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4


```
| Method                        | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error       | StdDev      | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |----------- |--------------- |------------ |------------ |------------:|------------:|------------:|------:|--------:|-------:|-------:|----------:|------------:|
| ExistsAsync_False             | Job-NUBXJZ | 20             | Default     | 5           |    927.3 ns |     3.09 ns |     3.56 ns |  0.85 |    0.00 | 0.0362 |      - |     616 B |        1.24 |
| ExistsAsync_True              | Job-NUBXJZ | 20             | Default     | 5           |    943.6 ns |     3.79 ns |     4.05 ns |  0.87 |    0.00 | 0.0362 |      - |     616 B |        1.24 |
| GetAsync_CacheHit             | Job-NUBXJZ | 20             | Default     | 5           |  1,086.6 ns |     2.10 ns |     2.41 ns |  1.00 |    0.00 | 0.0286 |      - |     496 B |        1.00 |
| GetAsync_CacheMiss            | Job-NUBXJZ | 20             | Default     | 5           |    895.2 ns |     1.73 ns |     1.85 ns |  0.82 |    0.00 | 0.0362 |      - |     616 B |        1.24 |
| GetOrSetAsync_CacheHit        | Job-NUBXJZ | 20             | Default     | 5           |  1,092.9 ns |     1.81 ns |     1.93 ns |  1.01 |    0.00 | 0.0324 |      - |     560 B |        1.13 |
| GetOrSetAsync_CacheMiss       | Job-NUBXJZ | 20             | Default     | 5           |  5,727.1 ns |   181.37 ns |   194.07 ns |  5.27 |    0.17 | 0.1068 | 0.0992 |    1792 B |        3.61 |
| GetOrSetAsync_WithTags        | Job-NUBXJZ | 20             | Default     | 5           | 10,467.0 ns |   960.04 ns | 1,027.23 ns |  9.63 |    0.92 | 0.1221 | 0.1068 |    2216 B |        4.47 |
| RemoveAsync                   | Job-NUBXJZ | 20             | Default     | 5           |  3,307.4 ns |    44.07 ns |    50.75 ns |  3.04 |    0.05 | 0.0648 |      - |    1136 B |        2.29 |
| RemoveByTagAsync              | Job-NUBXJZ | 20             | Default     | 5           |  8,495.1 ns |   367.76 ns |   377.66 ns |  7.82 |    0.34 | 0.1373 | 0.1221 |    2448 B |        4.94 |
| SetAsync                      | Job-NUBXJZ | 20             | Default     | 5           |  4,061.1 ns |   186.74 ns |   199.81 ns |  3.74 |    0.18 | 0.0610 | 0.0572 |    1064 B |        2.15 |
| SetWithSlidingExpirationAsync | Job-NUBXJZ | 20             | Default     | 5           |  4,361.3 ns |   527.82 ns |   586.68 ns |  4.01 |    0.53 | 0.0725 | 0.0687 |    1264 B |        2.55 |
|                               |            |                |             |             |             |             |             |       |         |        |        |           |             |
| ExistsAsync_False             | ShortRun   | 3              | 1           | 3           |    913.0 ns |    16.62 ns |     0.91 ns |  0.82 |    0.00 | 0.0362 |      - |     616 B |        1.24 |
| ExistsAsync_True              | ShortRun   | 3              | 1           | 3           |    929.5 ns |    44.58 ns |     2.44 ns |  0.84 |    0.00 | 0.0362 |      - |     616 B |        1.24 |
| GetAsync_CacheHit             | ShortRun   | 3              | 1           | 3           |  1,108.8 ns |    46.11 ns |     2.53 ns |  1.00 |    0.00 | 0.0286 |      - |     496 B |        1.00 |
| GetAsync_CacheMiss            | ShortRun   | 3              | 1           | 3           |    883.5 ns |    51.88 ns |     2.84 ns |  0.80 |    0.00 | 0.0362 |      - |     616 B |        1.24 |
| GetOrSetAsync_CacheHit        | ShortRun   | 3              | 1           | 3           |  1,101.4 ns |    63.40 ns |     3.48 ns |  0.99 |    0.00 | 0.0324 |      - |     560 B |        1.13 |
| GetOrSetAsync_CacheMiss       | ShortRun   | 3              | 1           | 3           |  5,538.0 ns |   607.31 ns |    33.29 ns |  4.99 |    0.03 | 0.1068 | 0.0992 |    1792 B |        3.61 |
| GetOrSetAsync_WithTags        | ShortRun   | 3              | 1           | 3           |  7,674.7 ns | 3,569.57 ns |   195.66 ns |  6.92 |    0.15 | 0.1221 | 0.1068 |    2216 B |        4.47 |
| RemoveAsync                   | ShortRun   | 3              | 1           | 3           |  3,345.7 ns |   258.43 ns |    14.17 ns |  3.02 |    0.01 | 0.0648 |      - |    1136 B |        2.29 |
| RemoveByTagAsync              | ShortRun   | 3              | 1           | 3           |  7,872.6 ns | 7,647.88 ns |   419.21 ns |  7.10 |    0.33 | 0.1678 | 0.1526 |    3043 B |        6.14 |
| SetAsync                      | ShortRun   | 3              | 1           | 3           |  3,754.6 ns |   326.37 ns |    17.89 ns |  3.39 |    0.02 | 0.0610 | 0.0572 |    1064 B |        2.15 |
| SetWithSlidingExpirationAsync | ShortRun   | 3              | 1           | 3           |  3,576.3 ns |   492.25 ns |    26.98 ns |  3.23 |    0.02 | 0.0610 | 0.0534 |    1072 B |        2.16 |
