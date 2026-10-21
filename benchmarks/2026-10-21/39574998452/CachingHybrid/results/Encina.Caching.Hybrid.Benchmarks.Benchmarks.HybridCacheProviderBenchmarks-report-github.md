```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  Job-NUBXJZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4


```
| Method                        | Job        | IterationCount | LaunchCount | WarmupCount | Mean       | Error       | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |----------- |--------------- |------------ |------------ |-----------:|------------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| ExistsAsync_False             | Job-NUBXJZ | 20             | Default     | 5           |   776.6 ns |     8.74 ns |   9.35 ns |  0.82 |    0.01 | 0.0362 |      - |     616 B |        1.24 |
| ExistsAsync_True              | Job-NUBXJZ | 20             | Default     | 5           |   872.7 ns |    20.76 ns |  23.91 ns |  0.92 |    0.03 | 0.0362 |      - |     616 B |        1.24 |
| GetAsync_CacheHit             | Job-NUBXJZ | 20             | Default     | 5           |   951.5 ns |    11.20 ns |  12.90 ns |  1.00 |    0.02 | 0.0286 |      - |     496 B |        1.00 |
| GetAsync_CacheMiss            | Job-NUBXJZ | 20             | Default     | 5           |   731.8 ns |     9.26 ns |   9.91 ns |  0.77 |    0.01 | 0.0362 |      - |     616 B |        1.24 |
| GetOrSetAsync_CacheHit        | Job-NUBXJZ | 20             | Default     | 5           |   889.2 ns |    11.15 ns |  12.85 ns |  0.93 |    0.02 | 0.0334 |      - |     560 B |        1.13 |
| GetOrSetAsync_CacheMiss       | Job-NUBXJZ | 20             | Default     | 5           | 4,715.9 ns |   131.93 ns | 141.17 ns |  4.96 |    0.16 | 0.1068 | 0.0992 |    1792 B |        3.61 |
| GetOrSetAsync_WithTags        | Job-NUBXJZ | 20             | Default     | 5           | 8,501.4 ns |   658.32 ns | 704.39 ns |  8.94 |    0.73 | 0.1297 | 0.1221 |    2216 B |        4.47 |
| RemoveAsync                   | Job-NUBXJZ | 20             | Default     | 5           | 2,535.9 ns |    17.23 ns |  18.44 ns |  2.67 |    0.04 | 0.0648 |      - |    1136 B |        2.29 |
| RemoveByTagAsync              | Job-NUBXJZ | 20             | Default     | 5           | 7,530.8 ns |   383.15 ns | 409.96 ns |  7.92 |    0.43 | 0.1373 | 0.1221 |    2448 B |        4.94 |
| SetAsync                      | Job-NUBXJZ | 20             | Default     | 5           | 3,698.2 ns |   167.57 ns | 179.30 ns |  3.89 |    0.19 | 0.0610 | 0.0572 |    1064 B |        2.15 |
| SetWithSlidingExpirationAsync | Job-NUBXJZ | 20             | Default     | 5           | 3,769.7 ns |   318.90 ns | 354.45 ns |  3.96 |    0.37 | 0.0610 | 0.0572 |    1072 B |        2.16 |
|                               |            |                |             |             |            |             |           |       |         |        |        |           |             |
| ExistsAsync_False             | ShortRun   | 3              | 1           | 3           |   775.4 ns |   399.30 ns |  21.89 ns |  0.80 |    0.02 | 0.0362 |      - |     616 B |        1.24 |
| ExistsAsync_True              | ShortRun   | 3              | 1           | 3           |   845.8 ns |   669.70 ns |  36.71 ns |  0.87 |    0.03 | 0.0362 |      - |     616 B |        1.24 |
| GetAsync_CacheHit             | ShortRun   | 3              | 1           | 3           |   974.0 ns |    83.81 ns |   4.59 ns |  1.00 |    0.01 | 0.0286 |      - |     496 B |        1.00 |
| GetAsync_CacheMiss            | ShortRun   | 3              | 1           | 3           |   751.9 ns |   205.53 ns |  11.27 ns |  0.77 |    0.01 | 0.0362 |      - |     616 B |        1.24 |
| GetOrSetAsync_CacheHit        | ShortRun   | 3              | 1           | 3           |   813.4 ns |    61.48 ns |   3.37 ns |  0.84 |    0.00 | 0.0334 |      - |     560 B |        1.13 |
| GetOrSetAsync_CacheMiss       | ShortRun   | 3              | 1           | 3           | 4,400.4 ns | 2,483.54 ns | 136.13 ns |  4.52 |    0.12 | 0.1068 | 0.0992 |    1792 B |        3.61 |
| GetOrSetAsync_WithTags        | ShortRun   | 3              | 1           | 3           | 7,934.9 ns | 5,019.86 ns | 275.16 ns |  8.15 |    0.25 | 0.1297 | 0.1221 |    2216 B |        4.47 |
| RemoveAsync                   | ShortRun   | 3              | 1           | 3           | 2,667.1 ns | 2,235.56 ns | 122.54 ns |  2.74 |    0.11 | 0.0648 |      - |    1136 B |        2.29 |
| RemoveByTagAsync              | ShortRun   | 3              | 1           | 3           | 7,212.0 ns |   796.81 ns |  43.68 ns |  7.40 |    0.05 | 0.1678 | 0.1526 |    3042 B |        6.13 |
| SetAsync                      | ShortRun   | 3              | 1           | 3           | 3,501.6 ns |   358.02 ns |  19.62 ns |  3.59 |    0.02 | 0.0610 | 0.0572 |    1064 B |        2.15 |
| SetWithSlidingExpirationAsync | ShortRun   | 3              | 1           | 3           | 3,397.7 ns |   698.45 ns |  38.28 ns |  3.49 |    0.04 | 0.0610 | 0.0572 |    1072 B |        2.16 |
