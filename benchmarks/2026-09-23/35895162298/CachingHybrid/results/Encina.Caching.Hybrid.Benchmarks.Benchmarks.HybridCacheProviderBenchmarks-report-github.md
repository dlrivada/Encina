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
| ExistsAsync_False             | Job-NUBXJZ | 20             | Default     | 5           |  1.154 μs | 0.0065 μs | 0.0072 μs |  0.82 |    0.01 | 0.0362 |      - |     616 B |        1.24 |
| ExistsAsync_True              | Job-NUBXJZ | 20             | Default     | 5           |  1.208 μs | 0.0027 μs | 0.0031 μs |  0.86 |    0.00 | 0.0362 |      - |     616 B |        1.24 |
| GetAsync_CacheHit             | Job-NUBXJZ | 20             | Default     | 5           |  1.402 μs | 0.0037 μs | 0.0042 μs |  1.00 |    0.00 | 0.0286 |      - |     496 B |        1.00 |
| GetAsync_CacheMiss            | Job-NUBXJZ | 20             | Default     | 5           |  1.129 μs | 0.0028 μs | 0.0031 μs |  0.81 |    0.00 | 0.0362 |      - |     616 B |        1.24 |
| GetOrSetAsync_CacheHit        | Job-NUBXJZ | 20             | Default     | 5           |  1.412 μs | 0.0023 μs | 0.0026 μs |  1.01 |    0.00 | 0.0324 |      - |     560 B |        1.13 |
| GetOrSetAsync_CacheMiss       | Job-NUBXJZ | 20             | Default     | 5           |  6.955 μs | 0.1377 μs | 0.1474 μs |  4.96 |    0.10 | 0.1068 | 0.0992 |    1792 B |        3.61 |
| GetOrSetAsync_WithTags        | Job-NUBXJZ | 20             | Default     | 5           | 11.678 μs | 0.7898 μs | 0.8451 μs |  8.33 |    0.59 | 0.1221 | 0.1068 |    2216 B |        4.47 |
| RemoveAsync                   | Job-NUBXJZ | 20             | Default     | 5           |  4.453 μs | 0.0480 μs | 0.0533 μs |  3.18 |    0.04 | 0.0610 |      - |    1136 B |        2.29 |
| RemoveByTagAsync              | Job-NUBXJZ | 20             | Default     | 5           | 10.354 μs | 0.3151 μs | 0.3371 μs |  7.39 |    0.24 | 0.1373 | 0.1221 |    2448 B |        4.94 |
| SetAsync                      | Job-NUBXJZ | 20             | Default     | 5           |  4.352 μs | 0.1680 μs | 0.1797 μs |  3.10 |    0.13 | 0.0610 | 0.0534 |    1064 B |        2.15 |
| SetWithSlidingExpirationAsync | Job-NUBXJZ | 20             | Default     | 5           |  4.382 μs | 0.1478 μs | 0.1452 μs |  3.13 |    0.10 | 0.0763 | 0.0687 |    1302 B |        2.62 |
|                               |            |                |             |             |           |           |           |       |         |        |        |           |             |
| ExistsAsync_False             | ShortRun   | 3              | 1           | 3           |  1.153 μs | 0.1533 μs | 0.0084 μs |  0.78 |    0.01 | 0.0362 |      - |     616 B |        1.24 |
| ExistsAsync_True              | ShortRun   | 3              | 1           | 3           |  1.201 μs | 0.0449 μs | 0.0025 μs |  0.82 |    0.00 | 0.0362 |      - |     616 B |        1.24 |
| GetAsync_CacheHit             | ShortRun   | 3              | 1           | 3           |  1.472 μs | 0.0433 μs | 0.0024 μs |  1.00 |    0.00 | 0.0286 |      - |     496 B |        1.00 |
| GetAsync_CacheMiss            | ShortRun   | 3              | 1           | 3           |  1.128 μs | 0.0182 μs | 0.0010 μs |  0.77 |    0.00 | 0.0362 |      - |     616 B |        1.24 |
| GetOrSetAsync_CacheHit        | ShortRun   | 3              | 1           | 3           |  1.434 μs | 0.3782 μs | 0.0207 μs |  0.97 |    0.01 | 0.0324 |      - |     560 B |        1.13 |
| GetOrSetAsync_CacheMiss       | ShortRun   | 3              | 1           | 3           |  6.778 μs | 2.8689 μs | 0.1573 μs |  4.60 |    0.09 | 0.1068 | 0.0992 |    1792 B |        3.61 |
| GetOrSetAsync_WithTags        | ShortRun   | 3              | 1           | 3           |  9.998 μs | 4.0202 μs | 0.2204 μs |  6.79 |    0.13 | 0.1221 | 0.1068 |    2216 B |        4.47 |
| RemoveAsync                   | ShortRun   | 3              | 1           | 3           |  4.462 μs | 0.2130 μs | 0.0117 μs |  3.03 |    0.01 | 0.0610 |      - |    1136 B |        2.29 |
| RemoveByTagAsync              | ShortRun   | 3              | 1           | 3           |  9.599 μs | 3.5859 μs | 0.1966 μs |  6.52 |    0.12 | 0.1678 | 0.1526 |    3035 B |        6.12 |
| SetAsync                      | ShortRun   | 3              | 1           | 3           |  4.311 μs | 1.4673 μs | 0.0804 μs |  2.93 |    0.05 | 0.0610 | 0.0534 |    1064 B |        2.15 |
| SetWithSlidingExpirationAsync | ShortRun   | 3              | 1           | 3           |  4.034 μs | 1.1694 μs | 0.0641 μs |  2.74 |    0.04 | 0.0610 | 0.0534 |    1072 B |        2.16 |
