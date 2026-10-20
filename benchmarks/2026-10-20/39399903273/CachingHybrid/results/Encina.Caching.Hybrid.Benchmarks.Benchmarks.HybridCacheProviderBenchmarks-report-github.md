```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.64GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  Job-NUBXJZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3


```
| Method                        | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |----------- |--------------- |------------ |------------ |----------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| ExistsAsync_False             | Job-NUBXJZ | 20             | Default     | 5           |  1.289 μs | 0.0021 μs | 0.0023 μs |  0.80 |    0.00 | 0.0362 |      - |     616 B |        1.24 |
| ExistsAsync_True              | Job-NUBXJZ | 20             | Default     | 5           |  1.374 μs | 0.0026 μs | 0.0029 μs |  0.85 |    0.00 | 0.0362 |      - |     616 B |        1.24 |
| GetAsync_CacheHit             | Job-NUBXJZ | 20             | Default     | 5           |  1.609 μs | 0.0032 μs | 0.0034 μs |  1.00 |    0.00 | 0.0286 |      - |     496 B |        1.00 |
| GetAsync_CacheMiss            | Job-NUBXJZ | 20             | Default     | 5           |  1.252 μs | 0.0047 μs | 0.0052 μs |  0.78 |    0.00 | 0.0362 |      - |     616 B |        1.24 |
| GetOrSetAsync_CacheHit        | Job-NUBXJZ | 20             | Default     | 5           |  1.414 μs | 0.0014 μs | 0.0014 μs |  0.88 |    0.00 | 0.0324 |      - |     560 B |        1.13 |
| GetOrSetAsync_CacheMiss       | Job-NUBXJZ | 20             | Default     | 5           |  6.949 μs | 0.1712 μs | 0.1832 μs |  4.32 |    0.11 | 0.1068 | 0.0992 |    1792 B |        3.61 |
| GetOrSetAsync_WithTags        | Job-NUBXJZ | 20             | Default     | 5           | 11.714 μs | 0.7549 μs | 0.8077 μs |  7.28 |    0.49 | 0.1221 | 0.1068 |    2216 B |        4.47 |
| RemoveAsync                   | Job-NUBXJZ | 20             | Default     | 5           |  4.843 μs | 0.0925 μs | 0.1066 μs |  3.01 |    0.06 | 0.0610 |      - |    1136 B |        2.29 |
| RemoveByTagAsync              | Job-NUBXJZ | 20             | Default     | 5           | 10.522 μs | 0.2714 μs | 0.2904 μs |  6.54 |    0.18 | 0.1373 | 0.1221 |    2448 B |        4.94 |
| SetAsync                      | Job-NUBXJZ | 20             | Default     | 5           |  4.711 μs | 0.2476 μs | 0.2649 μs |  2.93 |    0.16 | 0.0610 | 0.0534 |    1064 B |        2.15 |
| SetWithSlidingExpirationAsync | Job-NUBXJZ | 20             | Default     | 5           |  4.744 μs | 0.2791 μs | 0.2866 μs |  2.95 |    0.17 | 0.0763 | 0.0687 |    1323 B |        2.67 |
|                               |            |                |             |             |           |           |           |       |         |        |        |           |             |
| ExistsAsync_False             | ShortRun   | 3              | 1           | 3           |  1.329 μs | 0.0443 μs | 0.0024 μs |  0.83 |    0.00 | 0.0362 |      - |     616 B |        1.24 |
| ExistsAsync_True              | ShortRun   | 3              | 1           | 3           |  1.347 μs | 0.0466 μs | 0.0026 μs |  0.84 |    0.00 | 0.0362 |      - |     616 B |        1.24 |
| GetAsync_CacheHit             | ShortRun   | 3              | 1           | 3           |  1.606 μs | 0.0896 μs | 0.0049 μs |  1.00 |    0.00 | 0.0286 |      - |     496 B |        1.00 |
| GetAsync_CacheMiss            | ShortRun   | 3              | 1           | 3           |  1.243 μs | 0.0464 μs | 0.0025 μs |  0.77 |    0.00 | 0.0362 |      - |     616 B |        1.24 |
| GetOrSetAsync_CacheHit        | ShortRun   | 3              | 1           | 3           |  1.389 μs | 0.0327 μs | 0.0018 μs |  0.86 |    0.00 | 0.0324 |      - |     560 B |        1.13 |
| GetOrSetAsync_CacheMiss       | ShortRun   | 3              | 1           | 3           |  6.696 μs | 1.9737 μs | 0.1082 μs |  4.17 |    0.06 | 0.1068 | 0.0992 |    1792 B |        3.61 |
| GetOrSetAsync_WithTags        | ShortRun   | 3              | 1           | 3           | 10.237 μs | 4.0049 μs | 0.2195 μs |  6.37 |    0.12 | 0.1678 | 0.1526 |    3398 B |        6.85 |
| RemoveAsync                   | ShortRun   | 3              | 1           | 3           |  4.914 μs | 0.5033 μs | 0.0276 μs |  3.06 |    0.02 | 0.0610 |      - |    1136 B |        2.29 |
| RemoveByTagAsync              | ShortRun   | 3              | 1           | 3           |  9.827 μs | 1.9432 μs | 0.1065 μs |  6.12 |    0.06 | 0.1373 | 0.1221 |    2448 B |        4.94 |
| SetAsync                      | ShortRun   | 3              | 1           | 3           |  4.341 μs | 1.2158 μs | 0.0666 μs |  2.70 |    0.04 | 0.0610 | 0.0534 |    1064 B |        2.15 |
| SetWithSlidingExpirationAsync | ShortRun   | 3              | 1           | 3           |  4.615 μs | 1.7207 μs | 0.0943 μs |  2.87 |    0.05 | 0.0610 | 0.0534 |    1072 B |        2.16 |
