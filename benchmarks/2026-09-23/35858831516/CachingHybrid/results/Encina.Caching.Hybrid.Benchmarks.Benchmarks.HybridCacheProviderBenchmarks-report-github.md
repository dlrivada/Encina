```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-NUBXJZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4


```
| Method                        | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error       | StdDev      | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |----------- |--------------- |------------ |------------ |------------:|------------:|------------:|------:|--------:|-------:|-------:|----------:|------------:|
| ExistsAsync_False             | Job-NUBXJZ | 20             | Default     | 5           |    897.4 ns |     3.82 ns |     4.24 ns |  0.83 |    0.00 | 0.0362 |      - |     616 B |        1.24 |
| ExistsAsync_True              | Job-NUBXJZ | 20             | Default     | 5           |    919.6 ns |    10.55 ns |    11.29 ns |  0.85 |    0.01 | 0.0362 |      - |     616 B |        1.24 |
| GetAsync_CacheHit             | Job-NUBXJZ | 20             | Default     | 5           |  1,087.8 ns |     2.83 ns |     3.14 ns |  1.00 |    0.00 | 0.0286 |      - |     496 B |        1.00 |
| GetAsync_CacheMiss            | Job-NUBXJZ | 20             | Default     | 5           |    892.3 ns |     5.11 ns |     5.68 ns |  0.82 |    0.01 | 0.0362 |      - |     616 B |        1.24 |
| GetOrSetAsync_CacheHit        | Job-NUBXJZ | 20             | Default     | 5           |  1,120.0 ns |     6.56 ns |     7.29 ns |  1.03 |    0.01 | 0.0324 |      - |     560 B |        1.13 |
| GetOrSetAsync_CacheMiss       | Job-NUBXJZ | 20             | Default     | 5           |  5,652.2 ns |   258.79 ns |   276.90 ns |  5.20 |    0.25 | 0.1068 | 0.0992 |    1792 B |        3.61 |
| GetOrSetAsync_WithTags        | Job-NUBXJZ | 20             | Default     | 5           | 10,427.9 ns |   985.93 ns | 1,054.94 ns |  9.59 |    0.94 | 0.1221 | 0.1068 |    2216 B |        4.47 |
| RemoveAsync                   | Job-NUBXJZ | 20             | Default     | 5           |  3,298.5 ns |    34.15 ns |    39.33 ns |  3.03 |    0.04 | 0.0648 |      - |    1136 B |        2.29 |
| RemoveByTagAsync              | Job-NUBXJZ | 20             | Default     | 5           |  9,140.6 ns |   420.13 ns |   449.54 ns |  8.40 |    0.40 | 0.1373 | 0.1221 |    2448 B |        4.94 |
| SetAsync                      | Job-NUBXJZ | 20             | Default     | 5           |  4,104.7 ns |   144.94 ns |   155.09 ns |  3.77 |    0.14 | 0.0610 | 0.0572 |    1064 B |        2.15 |
| SetWithSlidingExpirationAsync | Job-NUBXJZ | 20             | Default     | 5           |  4,403.4 ns |   452.37 ns |   502.81 ns |  4.05 |    0.45 | 0.0610 | 0.0572 |    1072 B |        2.16 |
|                               |            |                |             |             |             |             |             |       |         |        |        |           |             |
| ExistsAsync_False             | ShortRun   | 3              | 1           | 3           |    896.4 ns |    66.57 ns |     3.65 ns |  0.82 |    0.00 | 0.0362 |      - |     616 B |        1.24 |
| ExistsAsync_True              | ShortRun   | 3              | 1           | 3           |    938.8 ns |    54.78 ns |     3.00 ns |  0.86 |    0.00 | 0.0362 |      - |     616 B |        1.24 |
| GetAsync_CacheHit             | ShortRun   | 3              | 1           | 3           |  1,089.9 ns |    68.17 ns |     3.74 ns |  1.00 |    0.00 | 0.0286 |      - |     496 B |        1.00 |
| GetAsync_CacheMiss            | ShortRun   | 3              | 1           | 3           |    877.6 ns |    89.95 ns |     4.93 ns |  0.81 |    0.00 | 0.0362 |      - |     616 B |        1.24 |
| GetOrSetAsync_CacheHit        | ShortRun   | 3              | 1           | 3           |  1,090.6 ns |    36.16 ns |     1.98 ns |  1.00 |    0.00 | 0.0324 |      - |     560 B |        1.13 |
| GetOrSetAsync_CacheMiss       | ShortRun   | 3              | 1           | 3           |  5,461.2 ns | 2,146.60 ns |   117.66 ns |  5.01 |    0.09 | 0.1068 | 0.0992 |    1792 B |        3.61 |
| GetOrSetAsync_WithTags        | ShortRun   | 3              | 1           | 3           |  8,565.8 ns | 3,264.76 ns |   178.95 ns |  7.86 |    0.14 | 0.1831 | 0.1678 |    3415 B |        6.89 |
| RemoveAsync                   | ShortRun   | 3              | 1           | 3           |  3,290.9 ns |   293.87 ns |    16.11 ns |  3.02 |    0.02 | 0.0648 |      - |    1136 B |        2.29 |
| RemoveByTagAsync              | ShortRun   | 3              | 1           | 3           |  8,212.1 ns | 5,726.85 ns |   313.91 ns |  7.54 |    0.25 | 0.1373 | 0.1221 |    2448 B |        4.94 |
| SetAsync                      | ShortRun   | 3              | 1           | 3           |  3,786.8 ns |   932.35 ns |    51.11 ns |  3.47 |    0.04 | 0.0610 | 0.0572 |    1064 B |        2.15 |
| SetWithSlidingExpirationAsync | ShortRun   | 3              | 1           | 3           |  3,863.9 ns | 1,344.09 ns |    73.67 ns |  3.55 |    0.06 | 0.0610 | 0.0572 |    1072 B |        2.16 |
