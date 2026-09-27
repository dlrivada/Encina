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
| ExistsAsync_False             | Job-NUBXJZ | 20             | Default     | 5           |  1.113 μs | 0.0045 μs | 0.0050 μs |  0.80 |    0.00 | 0.0362 |      - |     616 B |        1.24 |
| ExistsAsync_True              | Job-NUBXJZ | 20             | Default     | 5           |  1.151 μs | 0.0030 μs | 0.0034 μs |  0.83 |    0.00 | 0.0362 |      - |     616 B |        1.24 |
| GetAsync_CacheHit             | Job-NUBXJZ | 20             | Default     | 5           |  1.390 μs | 0.0035 μs | 0.0040 μs |  1.00 |    0.00 | 0.0286 |      - |     496 B |        1.00 |
| GetAsync_CacheMiss            | Job-NUBXJZ | 20             | Default     | 5           |  1.076 μs | 0.0026 μs | 0.0030 μs |  0.77 |    0.00 | 0.0362 |      - |     616 B |        1.24 |
| GetOrSetAsync_CacheHit        | Job-NUBXJZ | 20             | Default     | 5           |  1.368 μs | 0.0026 μs | 0.0029 μs |  0.98 |    0.00 | 0.0324 |      - |     560 B |        1.13 |
| GetOrSetAsync_CacheMiss       | Job-NUBXJZ | 20             | Default     | 5           |  6.926 μs | 0.1906 μs | 0.2039 μs |  4.98 |    0.14 | 0.1068 | 0.0992 |    1792 B |        3.61 |
| GetOrSetAsync_WithTags        | Job-NUBXJZ | 20             | Default     | 5           | 11.594 μs | 0.7691 μs | 0.8229 μs |  8.34 |    0.58 | 0.1221 | 0.1068 |    2216 B |        4.47 |
| RemoveAsync                   | Job-NUBXJZ | 20             | Default     | 5           |  4.204 μs | 0.0730 μs | 0.0840 μs |  3.02 |    0.06 | 0.0610 |      - |    1136 B |        2.29 |
| RemoveByTagAsync              | Job-NUBXJZ | 20             | Default     | 5           | 10.408 μs | 0.3037 μs | 0.2983 μs |  7.49 |    0.21 | 0.1373 | 0.1221 |    2448 B |        4.94 |
| SetAsync                      | Job-NUBXJZ | 20             | Default     | 5           |  4.399 μs | 0.2071 μs | 0.2216 μs |  3.17 |    0.16 | 0.0610 | 0.0534 |    1064 B |        2.15 |
| SetWithSlidingExpirationAsync | Job-NUBXJZ | 20             | Default     | 5           |  4.273 μs | 0.1710 μs | 0.1680 μs |  3.07 |    0.12 | 0.0763 | 0.0687 |    1300 B |        2.62 |
|                               |            |                |             |             |           |           |           |       |         |        |        |           |             |
| ExistsAsync_False             | ShortRun   | 3              | 1           | 3           |  1.113 μs | 0.1253 μs | 0.0069 μs |  0.81 |    0.00 | 0.0362 |      - |     616 B |        1.24 |
| ExistsAsync_True              | ShortRun   | 3              | 1           | 3           |  1.173 μs | 0.0568 μs | 0.0031 μs |  0.85 |    0.00 | 0.0362 |      - |     616 B |        1.24 |
| GetAsync_CacheHit             | ShortRun   | 3              | 1           | 3           |  1.377 μs | 0.0147 μs | 0.0008 μs |  1.00 |    0.00 | 0.0286 |      - |     496 B |        1.00 |
| GetAsync_CacheMiss            | ShortRun   | 3              | 1           | 3           |  1.089 μs | 0.1646 μs | 0.0090 μs |  0.79 |    0.01 | 0.0362 |      - |     616 B |        1.24 |
| GetOrSetAsync_CacheHit        | ShortRun   | 3              | 1           | 3           |  1.408 μs | 0.2397 μs | 0.0131 μs |  1.02 |    0.01 | 0.0324 |      - |     560 B |        1.13 |
| GetOrSetAsync_CacheMiss       | ShortRun   | 3              | 1           | 3           |  6.551 μs | 2.2758 μs | 0.1247 μs |  4.76 |    0.08 | 0.1068 | 0.0992 |    1792 B |        3.61 |
| GetOrSetAsync_WithTags        | ShortRun   | 3              | 1           | 3           | 10.162 μs | 1.8700 μs | 0.1025 μs |  7.38 |    0.06 | 0.1831 | 0.1678 |    3419 B |        6.89 |
| RemoveAsync                   | ShortRun   | 3              | 1           | 3           |  4.234 μs | 0.3735 μs | 0.0205 μs |  3.08 |    0.01 | 0.0610 |      - |    1136 B |        2.29 |
| RemoveByTagAsync              | ShortRun   | 3              | 1           | 3           |  9.735 μs | 2.9809 μs | 0.1634 μs |  7.07 |    0.10 | 0.1678 | 0.1526 |    3047 B |        6.14 |
| SetAsync                      | ShortRun   | 3              | 1           | 3           |  3.971 μs | 1.1057 μs | 0.0606 μs |  2.88 |    0.04 | 0.0610 | 0.0534 |    1064 B |        2.15 |
| SetWithSlidingExpirationAsync | ShortRun   | 3              | 1           | 3           |  4.005 μs | 0.8555 μs | 0.0469 μs |  2.91 |    0.03 | 0.0610 | 0.0534 |    1072 B |        2.16 |
