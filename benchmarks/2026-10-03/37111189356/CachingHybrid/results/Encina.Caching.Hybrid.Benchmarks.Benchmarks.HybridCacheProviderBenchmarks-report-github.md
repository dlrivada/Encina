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
| ExistsAsync_False             | Job-NUBXJZ | 20             | Default     | 5           |  1.120 μs | 0.0016 μs | 0.0016 μs |  0.80 |    0.00 | 0.0362 |      - |     616 B |        1.24 |
| ExistsAsync_True              | Job-NUBXJZ | 20             | Default     | 5           |  1.183 μs | 0.0031 μs | 0.0035 μs |  0.85 |    0.00 | 0.0362 |      - |     616 B |        1.24 |
| GetAsync_CacheHit             | Job-NUBXJZ | 20             | Default     | 5           |  1.395 μs | 0.0027 μs | 0.0030 μs |  1.00 |    0.00 | 0.0286 |      - |     496 B |        1.00 |
| GetAsync_CacheMiss            | Job-NUBXJZ | 20             | Default     | 5           |  1.131 μs | 0.0032 μs | 0.0034 μs |  0.81 |    0.00 | 0.0362 |      - |     616 B |        1.24 |
| GetOrSetAsync_CacheHit        | Job-NUBXJZ | 20             | Default     | 5           |  1.420 μs | 0.0019 μs | 0.0022 μs |  1.02 |    0.00 | 0.0324 |      - |     560 B |        1.13 |
| GetOrSetAsync_CacheMiss       | Job-NUBXJZ | 20             | Default     | 5           |  6.660 μs | 0.1035 μs | 0.1063 μs |  4.77 |    0.07 | 0.1068 | 0.0992 |    1792 B |        3.61 |
| GetOrSetAsync_WithTags        | Job-NUBXJZ | 20             | Default     | 5           | 11.262 μs | 0.7315 μs | 0.7827 μs |  8.07 |    0.55 | 0.1221 | 0.1068 |    2216 B |        4.47 |
| RemoveAsync                   | Job-NUBXJZ | 20             | Default     | 5           |  4.422 μs | 0.0307 μs | 0.0328 μs |  3.17 |    0.02 | 0.0610 |      - |    1136 B |        2.29 |
| RemoveByTagAsync              | Job-NUBXJZ | 20             | Default     | 5           |  9.735 μs | 0.1857 μs | 0.1907 μs |  6.98 |    0.13 | 0.1373 | 0.1221 |    2448 B |        4.94 |
| SetAsync                      | Job-NUBXJZ | 20             | Default     | 5           |  4.228 μs | 0.1229 μs | 0.1315 μs |  3.03 |    0.09 | 0.0610 | 0.0534 |    1064 B |        2.15 |
| SetWithSlidingExpirationAsync | Job-NUBXJZ | 20             | Default     | 5           |  4.231 μs | 0.1928 μs | 0.1804 μs |  3.03 |    0.13 | 0.0610 | 0.0572 |    1072 B |        2.16 |
|                               |            |                |             |             |           |           |           |       |         |        |        |           |             |
| ExistsAsync_False             | ShortRun   | 3              | 1           | 3           |  1.139 μs | 0.0961 μs | 0.0053 μs |  0.81 |    0.01 | 0.0362 |      - |     616 B |        1.24 |
| ExistsAsync_True              | ShortRun   | 3              | 1           | 3           |  1.184 μs | 0.0203 μs | 0.0011 μs |  0.84 |    0.01 | 0.0362 |      - |     616 B |        1.24 |
| GetAsync_CacheHit             | ShortRun   | 3              | 1           | 3           |  1.413 μs | 0.1812 μs | 0.0099 μs |  1.00 |    0.01 | 0.0286 |      - |     496 B |        1.00 |
| GetAsync_CacheMiss            | ShortRun   | 3              | 1           | 3           |  1.123 μs | 0.0111 μs | 0.0006 μs |  0.79 |    0.00 | 0.0362 |      - |     616 B |        1.24 |
| GetOrSetAsync_CacheHit        | ShortRun   | 3              | 1           | 3           |  1.432 μs | 0.1839 μs | 0.0101 μs |  1.01 |    0.01 | 0.0324 |      - |     560 B |        1.13 |
| GetOrSetAsync_CacheMiss       | ShortRun   | 3              | 1           | 3           |  6.302 μs | 1.1547 μs | 0.0633 μs |  4.46 |    0.05 | 0.1068 | 0.0992 |    1792 B |        3.61 |
| GetOrSetAsync_WithTags        | ShortRun   | 3              | 1           | 3           |  9.901 μs | 3.4608 μs | 0.1897 μs |  7.01 |    0.12 | 0.1831 | 0.1678 |    3417 B |        6.89 |
| RemoveAsync                   | ShortRun   | 3              | 1           | 3           |  4.451 μs | 0.3377 μs | 0.0185 μs |  3.15 |    0.02 | 0.0610 |      - |    1136 B |        2.29 |
| RemoveByTagAsync              | ShortRun   | 3              | 1           | 3           |  9.464 μs | 2.5239 μs | 0.1383 μs |  6.70 |    0.09 | 0.1678 | 0.1526 |    3049 B |        6.15 |
| SetAsync                      | ShortRun   | 3              | 1           | 3           |  3.975 μs | 0.8786 μs | 0.0482 μs |  2.81 |    0.03 | 0.0610 | 0.0534 |    1064 B |        2.15 |
| SetWithSlidingExpirationAsync | ShortRun   | 3              | 1           | 3           |  4.061 μs | 0.4534 μs | 0.0249 μs |  2.87 |    0.02 | 0.0610 | 0.0572 |    1072 B |        2.16 |
