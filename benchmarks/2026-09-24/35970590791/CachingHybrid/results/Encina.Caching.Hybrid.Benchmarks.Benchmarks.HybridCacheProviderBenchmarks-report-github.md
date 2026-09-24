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
| ExistsAsync_False             | Job-NUBXJZ | 20             | Default     | 5           |  1.193 μs | 0.0016 μs | 0.0017 μs |  0.83 |    0.00 | 0.0362 |      - |     616 B |        1.24 |
| ExistsAsync_True              | Job-NUBXJZ | 20             | Default     | 5           |  1.211 μs | 0.0014 μs | 0.0015 μs |  0.84 |    0.00 | 0.0362 |      - |     616 B |        1.24 |
| GetAsync_CacheHit             | Job-NUBXJZ | 20             | Default     | 5           |  1.440 μs | 0.0022 μs | 0.0023 μs |  1.00 |    0.00 | 0.0286 |      - |     496 B |        1.00 |
| GetAsync_CacheMiss            | Job-NUBXJZ | 20             | Default     | 5           |  1.151 μs | 0.0012 μs | 0.0014 μs |  0.80 |    0.00 | 0.0362 |      - |     616 B |        1.24 |
| GetOrSetAsync_CacheHit        | Job-NUBXJZ | 20             | Default     | 5           |  1.426 μs | 0.0008 μs | 0.0008 μs |  0.99 |    0.00 | 0.0324 |      - |     560 B |        1.13 |
| GetOrSetAsync_CacheMiss       | Job-NUBXJZ | 20             | Default     | 5           |  7.263 μs | 0.0727 μs | 0.0747 μs |  5.04 |    0.05 | 0.1068 | 0.0992 |    1792 B |        3.61 |
| GetOrSetAsync_WithTags        | Job-NUBXJZ | 20             | Default     | 5           | 12.590 μs | 0.8164 μs | 0.8735 μs |  8.74 |    0.59 | 0.1221 | 0.1068 |    2216 B |        4.47 |
| RemoveAsync                   | Job-NUBXJZ | 20             | Default     | 5           |  4.437 μs | 0.0355 μs | 0.0395 μs |  3.08 |    0.03 | 0.0610 |      - |    1136 B |        2.29 |
| RemoveByTagAsync              | Job-NUBXJZ | 20             | Default     | 5           |  9.895 μs | 0.3041 μs | 0.3254 μs |  6.87 |    0.22 | 0.1373 | 0.1221 |    2448 B |        4.94 |
| SetAsync                      | Job-NUBXJZ | 20             | Default     | 5           |  4.238 μs | 0.1383 μs | 0.1480 μs |  2.94 |    0.10 | 0.0610 | 0.0534 |    1064 B |        2.15 |
| SetWithSlidingExpirationAsync | Job-NUBXJZ | 20             | Default     | 5           |  4.296 μs | 0.2664 μs | 0.2736 μs |  2.98 |    0.18 | 0.0763 | 0.0687 |    1342 B |        2.71 |
|                               |            |                |             |             |           |           |           |       |         |        |        |           |             |
| ExistsAsync_False             | ShortRun   | 3              | 1           | 3           |  1.184 μs | 0.0495 μs | 0.0027 μs |  0.83 |    0.00 | 0.0362 |      - |     616 B |        1.24 |
| ExistsAsync_True              | ShortRun   | 3              | 1           | 3           |  1.206 μs | 0.0504 μs | 0.0028 μs |  0.85 |    0.00 | 0.0362 |      - |     616 B |        1.24 |
| GetAsync_CacheHit             | ShortRun   | 3              | 1           | 3           |  1.424 μs | 0.0652 μs | 0.0036 μs |  1.00 |    0.00 | 0.0286 |      - |     496 B |        1.00 |
| GetAsync_CacheMiss            | ShortRun   | 3              | 1           | 3           |  1.127 μs | 0.0737 μs | 0.0040 μs |  0.79 |    0.00 | 0.0362 |      - |     616 B |        1.24 |
| GetOrSetAsync_CacheHit        | ShortRun   | 3              | 1           | 3           |  1.478 μs | 0.0404 μs | 0.0022 μs |  1.04 |    0.00 | 0.0324 |      - |     560 B |        1.13 |
| GetOrSetAsync_CacheMiss       | ShortRun   | 3              | 1           | 3           |  7.045 μs | 0.6731 μs | 0.0369 μs |  4.95 |    0.02 | 0.1068 | 0.0992 |    1792 B |        3.61 |
| GetOrSetAsync_WithTags        | ShortRun   | 3              | 1           | 3           | 10.853 μs | 4.1129 μs | 0.2254 μs |  7.62 |    0.14 | 0.1221 | 0.1068 |    2216 B |        4.47 |
| RemoveAsync                   | ShortRun   | 3              | 1           | 3           |  4.433 μs | 0.3038 μs | 0.0166 μs |  3.11 |    0.01 | 0.0610 |      - |    1136 B |        2.29 |
| RemoveByTagAsync              | ShortRun   | 3              | 1           | 3           |  9.553 μs | 3.5853 μs | 0.1965 μs |  6.71 |    0.12 | 0.1831 | 0.1678 |    3649 B |        7.36 |
| SetAsync                      | ShortRun   | 3              | 1           | 3           |  4.234 μs | 0.3679 μs | 0.0202 μs |  2.97 |    0.01 | 0.0610 | 0.0572 |    1064 B |        2.15 |
| SetWithSlidingExpirationAsync | ShortRun   | 3              | 1           | 3           |  4.078 μs | 3.0014 μs | 0.1645 μs |  2.86 |    0.10 | 0.0610 | 0.0534 |    1072 B |        2.16 |
