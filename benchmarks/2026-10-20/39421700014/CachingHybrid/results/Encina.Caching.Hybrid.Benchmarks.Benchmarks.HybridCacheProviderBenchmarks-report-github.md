```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  Job-NUBXJZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3


```
| Method                        | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |----------- |--------------- |------------ |------------ |----------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| ExistsAsync_False             | Job-NUBXJZ | 20             | Default     | 5           |  1.334 μs | 0.0029 μs | 0.0032 μs |  0.80 |    0.00 | 0.0362 |      - |     616 B |        1.24 |
| ExistsAsync_True              | Job-NUBXJZ | 20             | Default     | 5           |  1.405 μs | 0.0028 μs | 0.0030 μs |  0.84 |    0.00 | 0.0362 |      - |     616 B |        1.24 |
| GetAsync_CacheHit             | Job-NUBXJZ | 20             | Default     | 5           |  1.669 μs | 0.0028 μs | 0.0032 μs |  1.00 |    0.00 | 0.0286 |      - |     496 B |        1.00 |
| GetAsync_CacheMiss            | Job-NUBXJZ | 20             | Default     | 5           |  1.328 μs | 0.0032 μs | 0.0033 μs |  0.80 |    0.00 | 0.0362 |      - |     616 B |        1.24 |
| GetOrSetAsync_CacheHit        | Job-NUBXJZ | 20             | Default     | 5           |  1.459 μs | 0.0022 μs | 0.0025 μs |  0.87 |    0.00 | 0.0324 |      - |     560 B |        1.13 |
| GetOrSetAsync_CacheMiss       | Job-NUBXJZ | 20             | Default     | 5           |  7.585 μs | 0.2327 μs | 0.2490 μs |  4.55 |    0.15 | 0.1068 | 0.0992 |    1792 B |        3.61 |
| GetOrSetAsync_WithTags        | Job-NUBXJZ | 20             | Default     | 5           | 12.510 μs | 1.0084 μs | 1.0790 μs |  7.50 |    0.63 | 0.1221 | 0.1068 |    2216 B |        4.47 |
| RemoveAsync                   | Job-NUBXJZ | 20             | Default     | 5           |  5.118 μs | 0.0691 μs | 0.0796 μs |  3.07 |    0.05 | 0.0610 |      - |    1136 B |        2.29 |
| RemoveByTagAsync              | Job-NUBXJZ | 20             | Default     | 5           | 11.234 μs | 0.3886 μs | 0.3817 μs |  6.73 |    0.22 | 0.1373 | 0.1221 |    2448 B |        4.94 |
| SetAsync                      | Job-NUBXJZ | 20             | Default     | 5           |  5.046 μs | 0.1616 μs | 0.1729 μs |  3.02 |    0.10 | 0.0610 | 0.0534 |    1064 B |        2.15 |
| SetWithSlidingExpirationAsync | Job-NUBXJZ | 20             | Default     | 5           |  5.089 μs | 0.6487 μs | 0.6662 μs |  3.05 |    0.39 | 0.0687 | 0.0610 |    1267 B |        2.55 |
|                               |            |                |             |             |           |           |           |       |         |        |        |           |             |
| ExistsAsync_False             | ShortRun   | 3              | 1           | 3           |  1.306 μs | 0.0271 μs | 0.0015 μs |  0.79 |    0.00 | 0.0362 |      - |     616 B |        1.24 |
| ExistsAsync_True              | ShortRun   | 3              | 1           | 3           |  1.427 μs | 0.1455 μs | 0.0080 μs |  0.86 |    0.00 | 0.0362 |      - |     616 B |        1.24 |
| GetAsync_CacheHit             | ShortRun   | 3              | 1           | 3           |  1.655 μs | 0.0793 μs | 0.0043 μs |  1.00 |    0.00 | 0.0286 |      - |     496 B |        1.00 |
| GetAsync_CacheMiss            | ShortRun   | 3              | 1           | 3           |  1.316 μs | 0.0849 μs | 0.0047 μs |  0.80 |    0.00 | 0.0362 |      - |     616 B |        1.24 |
| GetOrSetAsync_CacheHit        | ShortRun   | 3              | 1           | 3           |  1.438 μs | 0.0358 μs | 0.0020 μs |  0.87 |    0.00 | 0.0324 |      - |     560 B |        1.13 |
| GetOrSetAsync_CacheMiss       | ShortRun   | 3              | 1           | 3           |  7.076 μs | 4.9447 μs | 0.2710 μs |  4.28 |    0.14 | 0.1068 | 0.0992 |    1792 B |        3.61 |
| GetOrSetAsync_WithTags        | ShortRun   | 3              | 1           | 3           | 11.289 μs | 4.3211 μs | 0.2369 μs |  6.82 |    0.12 | 0.1831 | 0.1678 |    3415 B |        6.89 |
| RemoveAsync                   | ShortRun   | 3              | 1           | 3           |  5.120 μs | 1.7523 μs | 0.0961 μs |  3.09 |    0.05 | 0.0610 |      - |    1136 B |        2.29 |
| RemoveByTagAsync              | ShortRun   | 3              | 1           | 3           | 10.452 μs | 9.1033 μs | 0.4990 μs |  6.32 |    0.26 | 0.1373 | 0.1221 |    2448 B |        4.94 |
| SetAsync                      | ShortRun   | 3              | 1           | 3           |  4.562 μs | 2.6095 μs | 0.1430 μs |  2.76 |    0.08 | 0.0610 | 0.0534 |    1064 B |        2.15 |
| SetWithSlidingExpirationAsync | ShortRun   | 3              | 1           | 3           |  4.638 μs | 2.3162 μs | 0.1270 μs |  2.80 |    0.07 | 0.0610 | 0.0534 |    1072 B |        2.16 |
