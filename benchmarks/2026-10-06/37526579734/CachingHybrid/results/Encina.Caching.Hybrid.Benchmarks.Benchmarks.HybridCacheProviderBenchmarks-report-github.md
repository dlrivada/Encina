```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-NUBXJZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4


```
| Method                        | Job        | IterationCount | LaunchCount | WarmupCount | Mean       | Error       | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |----------- |--------------- |------------ |------------ |-----------:|------------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| ExistsAsync_False             | Job-NUBXJZ | 20             | Default     | 5           |   636.9 ns |     5.17 ns |   5.08 ns |  0.79 |    0.01 | 0.0067 |      - |     616 B |        1.24 |
| ExistsAsync_True              | Job-NUBXJZ | 20             | Default     | 5           |   627.5 ns |     7.06 ns |   6.93 ns |  0.78 |    0.01 | 0.0067 |      - |     616 B |        1.24 |
| GetAsync_CacheHit             | Job-NUBXJZ | 20             | Default     | 5           |   803.7 ns |     5.91 ns |   6.57 ns |  1.00 |    0.01 | 0.0057 |      - |     496 B |        1.00 |
| GetAsync_CacheMiss            | Job-NUBXJZ | 20             | Default     | 5           |   632.7 ns |     2.91 ns |   2.86 ns |  0.79 |    0.01 | 0.0067 |      - |     616 B |        1.24 |
| GetOrSetAsync_CacheHit        | Job-NUBXJZ | 20             | Default     | 5           |   833.2 ns |    17.51 ns |  18.74 ns |  1.04 |    0.02 | 0.0067 |      - |     560 B |        1.13 |
| GetOrSetAsync_CacheMiss       | Job-NUBXJZ | 20             | Default     | 5           | 4,029.3 ns |   247.13 ns | 253.78 ns |  5.01 |    0.31 | 0.0153 | 0.0076 |    1792 B |        3.61 |
| GetOrSetAsync_WithTags        | Job-NUBXJZ | 20             | Default     | 5           | 7,495.0 ns |   494.55 ns | 529.16 ns |  9.33 |    0.65 | 0.0229 | 0.0153 |    2216 B |        4.47 |
| RemoveAsync                   | Job-NUBXJZ | 20             | Default     | 5           | 2,442.4 ns |    78.11 ns |  80.22 ns |  3.04 |    0.10 | 0.0114 |      - |    1136 B |        2.29 |
| RemoveByTagAsync              | Job-NUBXJZ | 20             | Default     | 5           | 6,363.6 ns |   341.78 ns | 365.70 ns |  7.92 |    0.45 | 0.0229 | 0.0153 |    2448 B |        4.94 |
| SetAsync                      | Job-NUBXJZ | 20             | Default     | 5           | 2,915.3 ns |   138.47 ns | 148.16 ns |  3.63 |    0.18 | 0.0114 | 0.0076 |    1064 B |        2.15 |
| SetWithSlidingExpirationAsync | Job-NUBXJZ | 20             | Default     | 5           | 3,272.1 ns |   326.45 ns | 362.85 ns |  4.07 |    0.44 | 0.0114 | 0.0076 |    1160 B |        2.34 |
|                               |            |                |             |             |            |             |           |       |         |        |        |           |             |
| ExistsAsync_False             | ShortRun   | 3              | 1           | 3           |   651.5 ns |   102.39 ns |   5.61 ns |  0.79 |    0.04 | 0.0067 |      - |     616 B |        1.24 |
| ExistsAsync_True              | ShortRun   | 3              | 1           | 3           |   634.5 ns |   143.83 ns |   7.88 ns |  0.77 |    0.04 | 0.0067 |      - |     616 B |        1.24 |
| GetAsync_CacheHit             | ShortRun   | 3              | 1           | 3           |   825.9 ns |   984.99 ns |  53.99 ns |  1.00 |    0.08 | 0.0057 |      - |     496 B |        1.00 |
| GetAsync_CacheMiss            | ShortRun   | 3              | 1           | 3           |   709.3 ns |   215.81 ns |  11.83 ns |  0.86 |    0.05 | 0.0067 |      - |     616 B |        1.24 |
| GetOrSetAsync_CacheHit        | ShortRun   | 3              | 1           | 3           |   821.7 ns |    15.27 ns |   0.84 ns |  1.00 |    0.05 | 0.0067 |      - |     560 B |        1.13 |
| GetOrSetAsync_CacheMiss       | ShortRun   | 3              | 1           | 3           | 4,602.8 ns | 2,061.64 ns | 113.01 ns |  5.59 |    0.33 | 0.0191 | 0.0153 |    1792 B |        3.61 |
| GetOrSetAsync_WithTags        | ShortRun   | 3              | 1           | 3           | 6,552.0 ns | 8,169.13 ns | 447.78 ns |  7.95 |    0.64 | 0.0229 | 0.0153 |    2216 B |        4.47 |
| RemoveAsync                   | ShortRun   | 3              | 1           | 3           | 2,429.2 ns |   937.50 ns |  51.39 ns |  2.95 |    0.17 | 0.0114 |      - |    1136 B |        2.29 |
| RemoveByTagAsync              | ShortRun   | 3              | 1           | 3           | 5,910.9 ns | 3,016.21 ns | 165.33 ns |  7.18 |    0.43 | 0.0229 | 0.0153 |    2448 B |        4.94 |
| SetAsync                      | ShortRun   | 3              | 1           | 3           | 2,863.1 ns | 1,409.21 ns |  77.24 ns |  3.48 |    0.21 | 0.0114 | 0.0076 |    1064 B |        2.15 |
| SetWithSlidingExpirationAsync | ShortRun   | 3              | 1           | 3           | 2,783.1 ns |   997.35 ns |  54.67 ns |  3.38 |    0.19 | 0.0114 | 0.0076 |    1073 B |        2.16 |
