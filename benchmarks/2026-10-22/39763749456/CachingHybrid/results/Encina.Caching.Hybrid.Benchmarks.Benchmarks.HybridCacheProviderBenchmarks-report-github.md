```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.87GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  Job-NUBXJZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3


```
| Method                        | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |----------- |--------------- |------------ |------------ |----------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| ExistsAsync_False             | Job-NUBXJZ | 20             | Default     | 5           |  1.369 μs | 0.0032 μs | 0.0034 μs |  0.84 |    0.00 | 0.0362 |      - |     616 B |        1.24 |
| ExistsAsync_True              | Job-NUBXJZ | 20             | Default     | 5           |  1.423 μs | 0.0042 μs | 0.0045 μs |  0.88 |    0.00 | 0.0362 |      - |     616 B |        1.24 |
| GetAsync_CacheHit             | Job-NUBXJZ | 20             | Default     | 5           |  1.623 μs | 0.0038 μs | 0.0041 μs |  1.00 |    0.00 | 0.0286 |      - |     496 B |        1.00 |
| GetAsync_CacheMiss            | Job-NUBXJZ | 20             | Default     | 5           |  1.343 μs | 0.0092 μs | 0.0102 μs |  0.83 |    0.01 | 0.0362 |      - |     616 B |        1.24 |
| GetOrSetAsync_CacheHit        | Job-NUBXJZ | 20             | Default     | 5           |  1.371 μs | 0.0031 μs | 0.0034 μs |  0.84 |    0.00 | 0.0324 |      - |     560 B |        1.13 |
| GetOrSetAsync_CacheMiss       | Job-NUBXJZ | 20             | Default     | 5           |  6.993 μs | 0.1683 μs | 0.1801 μs |  4.31 |    0.11 | 0.1068 | 0.0992 |    1792 B |        3.61 |
| GetOrSetAsync_WithTags        | Job-NUBXJZ | 20             | Default     | 5           | 11.789 μs | 0.6984 μs | 0.7473 μs |  7.26 |    0.45 | 0.1221 | 0.1068 |    2216 B |        4.47 |
| RemoveAsync                   | Job-NUBXJZ | 20             | Default     | 5           |  4.825 μs | 0.0590 μs | 0.0679 μs |  2.97 |    0.04 | 0.0610 |      - |    1136 B |        2.29 |
| RemoveByTagAsync              | Job-NUBXJZ | 20             | Default     | 5           | 10.790 μs | 0.3610 μs | 0.3862 μs |  6.65 |    0.23 | 0.1373 | 0.1221 |    2448 B |        4.94 |
| SetAsync                      | Job-NUBXJZ | 20             | Default     | 5           |  5.053 μs | 0.1238 μs | 0.1325 μs |  3.11 |    0.08 | 0.0610 | 0.0534 |    1064 B |        2.15 |
| SetWithSlidingExpirationAsync | Job-NUBXJZ | 20             | Default     | 5           |  4.876 μs | 0.2663 μs | 0.2616 μs |  3.00 |    0.16 | 0.0763 | 0.0687 |    1314 B |        2.65 |
|                               |            |                |             |             |           |           |           |       |         |        |        |           |             |
| ExistsAsync_False             | ShortRun   | 3              | 1           | 3           |  1.377 μs | 0.1464 μs | 0.0080 μs |  0.85 |    0.00 | 0.0362 |      - |     616 B |        1.24 |
| ExistsAsync_True              | ShortRun   | 3              | 1           | 3           |  1.458 μs | 0.0468 μs | 0.0026 μs |  0.90 |    0.00 | 0.0362 |      - |     616 B |        1.24 |
| GetAsync_CacheHit             | ShortRun   | 3              | 1           | 3           |  1.629 μs | 0.1000 μs | 0.0055 μs |  1.00 |    0.00 | 0.0286 |      - |     496 B |        1.00 |
| GetAsync_CacheMiss            | ShortRun   | 3              | 1           | 3           |  1.342 μs | 0.1299 μs | 0.0071 μs |  0.82 |    0.00 | 0.0362 |      - |     616 B |        1.24 |
| GetOrSetAsync_CacheHit        | ShortRun   | 3              | 1           | 3           |  1.428 μs | 0.0085 μs | 0.0005 μs |  0.88 |    0.00 | 0.0324 |      - |     560 B |        1.13 |
| GetOrSetAsync_CacheMiss       | ShortRun   | 3              | 1           | 3           |  6.794 μs | 2.0513 μs | 0.1124 μs |  4.17 |    0.06 | 0.1068 | 0.0992 |    1792 B |        3.61 |
| GetOrSetAsync_WithTags        | ShortRun   | 3              | 1           | 3           |  9.917 μs | 3.6715 μs | 0.2012 μs |  6.09 |    0.11 | 0.1831 | 0.1678 |    3413 B |        6.88 |
| RemoveAsync                   | ShortRun   | 3              | 1           | 3           |  4.831 μs | 1.1417 μs | 0.0626 μs |  2.97 |    0.03 | 0.0610 |      - |    1136 B |        2.29 |
| RemoveByTagAsync              | ShortRun   | 3              | 1           | 3           | 10.232 μs | 1.1467 μs | 0.0629 μs |  6.28 |    0.04 | 0.1831 | 0.1678 |    3648 B |        7.35 |
| SetAsync                      | ShortRun   | 3              | 1           | 3           |  4.589 μs | 1.6049 μs | 0.0880 μs |  2.82 |    0.05 | 0.0610 | 0.0534 |    1064 B |        2.15 |
| SetWithSlidingExpirationAsync | ShortRun   | 3              | 1           | 3           |  4.623 μs | 2.7575 μs | 0.1511 μs |  2.84 |    0.08 | 0.0610 | 0.0534 |    1072 B |        2.16 |
