```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-NUBXJZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3


```
| Method                        | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |----------- |--------------- |------------ |------------ |----------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| ExistsAsync_False             | Job-NUBXJZ | 20             | Default     | 5           |  1.150 μs | 0.0036 μs | 0.0041 μs |  0.81 |    0.00 | 0.0362 |      - |     616 B |        1.24 |
| ExistsAsync_True              | Job-NUBXJZ | 20             | Default     | 5           |  1.195 μs | 0.0042 μs | 0.0046 μs |  0.84 |    0.00 | 0.0362 |      - |     616 B |        1.24 |
| GetAsync_CacheHit             | Job-NUBXJZ | 20             | Default     | 5           |  1.429 μs | 0.0031 μs | 0.0035 μs |  1.00 |    0.00 | 0.0286 |      - |     496 B |        1.00 |
| GetAsync_CacheMiss            | Job-NUBXJZ | 20             | Default     | 5           |  1.108 μs | 0.0021 μs | 0.0022 μs |  0.78 |    0.00 | 0.0362 |      - |     616 B |        1.24 |
| GetOrSetAsync_CacheHit        | Job-NUBXJZ | 20             | Default     | 5           |  1.433 μs | 0.0035 μs | 0.0040 μs |  1.00 |    0.00 | 0.0324 |      - |     560 B |        1.13 |
| GetOrSetAsync_CacheMiss       | Job-NUBXJZ | 20             | Default     | 5           |  7.018 μs | 0.1205 μs | 0.1237 μs |  4.91 |    0.08 | 0.1068 | 0.0992 |    1792 B |        3.61 |
| GetOrSetAsync_WithTags        | Job-NUBXJZ | 20             | Default     | 5           | 11.687 μs | 0.7191 μs | 0.7694 μs |  8.18 |    0.52 | 0.1221 | 0.1068 |    2216 B |        4.47 |
| RemoveAsync                   | Job-NUBXJZ | 20             | Default     | 5           |  4.404 μs | 0.1672 μs | 0.1925 μs |  3.08 |    0.13 | 0.0610 |      - |    1136 B |        2.29 |
| RemoveByTagAsync              | Job-NUBXJZ | 20             | Default     | 5           | 10.508 μs | 0.2736 μs | 0.2810 μs |  7.35 |    0.19 | 0.1373 | 0.1221 |    2448 B |        4.94 |
| SetAsync                      | Job-NUBXJZ | 20             | Default     | 5           |  4.392 μs | 0.1471 μs | 0.1574 μs |  3.07 |    0.11 | 0.0610 | 0.0534 |    1064 B |        2.15 |
| SetWithSlidingExpirationAsync | Job-NUBXJZ | 20             | Default     | 5           |  4.314 μs | 0.1600 μs | 0.1571 μs |  3.02 |    0.11 | 0.0763 | 0.0687 |    1342 B |        2.71 |
|                               |            |                |             |             |           |           |           |       |         |        |        |           |             |
| ExistsAsync_False             | ShortRun   | 3              | 1           | 3           |  1.166 μs | 0.0995 μs | 0.0055 μs |  0.82 |    0.00 | 0.0362 |      - |     616 B |        1.24 |
| ExistsAsync_True              | ShortRun   | 3              | 1           | 3           |  1.212 μs | 0.0310 μs | 0.0017 μs |  0.86 |    0.00 | 0.0362 |      - |     616 B |        1.24 |
| GetAsync_CacheHit             | ShortRun   | 3              | 1           | 3           |  1.414 μs | 0.0899 μs | 0.0049 μs |  1.00 |    0.00 | 0.0286 |      - |     496 B |        1.00 |
| GetAsync_CacheMiss            | ShortRun   | 3              | 1           | 3           |  1.129 μs | 0.0635 μs | 0.0035 μs |  0.80 |    0.00 | 0.0362 |      - |     616 B |        1.24 |
| GetOrSetAsync_CacheHit        | ShortRun   | 3              | 1           | 3           |  1.417 μs | 0.0374 μs | 0.0021 μs |  1.00 |    0.00 | 0.0324 |      - |     560 B |        1.13 |
| GetOrSetAsync_CacheMiss       | ShortRun   | 3              | 1           | 3           |  6.660 μs | 1.9477 μs | 0.1068 μs |  4.71 |    0.07 | 0.1068 | 0.0992 |    1792 B |        3.61 |
| GetOrSetAsync_WithTags        | ShortRun   | 3              | 1           | 3           |  9.980 μs | 5.4839 μs | 0.3006 μs |  7.06 |    0.19 | 0.1221 | 0.1068 |    2216 B |        4.47 |
| RemoveAsync                   | ShortRun   | 3              | 1           | 3           |  4.436 μs | 1.8233 μs | 0.0999 μs |  3.14 |    0.06 | 0.0610 |      - |    1136 B |        2.29 |
| RemoveByTagAsync              | ShortRun   | 3              | 1           | 3           |  9.758 μs | 1.0787 μs | 0.0591 μs |  6.90 |    0.04 | 0.1678 | 0.1526 |    3042 B |        6.13 |
| SetAsync                      | ShortRun   | 3              | 1           | 3           |  4.257 μs | 0.6104 μs | 0.0335 μs |  3.01 |    0.02 | 0.0610 | 0.0534 |    1064 B |        2.15 |
| SetWithSlidingExpirationAsync | ShortRun   | 3              | 1           | 3           |  4.119 μs | 0.2000 μs | 0.0110 μs |  2.91 |    0.01 | 0.0610 | 0.0534 |    1072 B |        2.16 |
