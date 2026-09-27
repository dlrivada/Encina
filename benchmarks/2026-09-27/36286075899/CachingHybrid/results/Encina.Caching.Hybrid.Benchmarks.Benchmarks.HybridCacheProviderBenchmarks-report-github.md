```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-NUBXJZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3


```
| Method                        | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |----------- |--------------- |------------ |------------ |----------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| ExistsAsync_False             | Job-NUBXJZ | 20             | Default     | 5           |  1.155 μs | 0.0063 μs | 0.0070 μs |  0.82 |    0.00 | 0.0362 |      - |     616 B |        1.24 |
| ExistsAsync_True              | Job-NUBXJZ | 20             | Default     | 5           |  1.194 μs | 0.0022 μs | 0.0023 μs |  0.84 |    0.00 | 0.0362 |      - |     616 B |        1.24 |
| GetAsync_CacheHit             | Job-NUBXJZ | 20             | Default     | 5           |  1.412 μs | 0.0019 μs | 0.0020 μs |  1.00 |    0.00 | 0.0286 |      - |     496 B |        1.00 |
| GetAsync_CacheMiss            | Job-NUBXJZ | 20             | Default     | 5           |  1.101 μs | 0.0040 μs | 0.0043 μs |  0.78 |    0.00 | 0.0362 |      - |     616 B |        1.24 |
| GetOrSetAsync_CacheHit        | Job-NUBXJZ | 20             | Default     | 5           |  1.398 μs | 0.0031 μs | 0.0036 μs |  0.99 |    0.00 | 0.0324 |      - |     560 B |        1.13 |
| GetOrSetAsync_CacheMiss       | Job-NUBXJZ | 20             | Default     | 5           |  6.955 μs | 0.1338 μs | 0.1431 μs |  4.92 |    0.10 | 0.1068 | 0.0992 |    1792 B |        3.61 |
| GetOrSetAsync_WithTags        | Job-NUBXJZ | 20             | Default     | 5           | 12.004 μs | 0.7531 μs | 0.8058 μs |  8.50 |    0.56 | 0.1221 | 0.1068 |    2216 B |        4.47 |
| RemoveAsync                   | Job-NUBXJZ | 20             | Default     | 5           |  4.432 μs | 0.0275 μs | 0.0306 μs |  3.14 |    0.02 | 0.0610 |      - |    1136 B |        2.29 |
| RemoveByTagAsync              | Job-NUBXJZ | 20             | Default     | 5           | 10.468 μs | 0.2551 μs | 0.2730 μs |  7.41 |    0.19 | 0.1373 | 0.1221 |    2448 B |        4.94 |
| SetAsync                      | Job-NUBXJZ | 20             | Default     | 5           |  4.353 μs | 0.1611 μs | 0.1724 μs |  3.08 |    0.12 | 0.0610 | 0.0534 |    1064 B |        2.15 |
| SetWithSlidingExpirationAsync | Job-NUBXJZ | 20             | Default     | 5           |  4.375 μs | 0.1515 μs | 0.1488 μs |  3.10 |    0.10 | 0.0610 | 0.0534 |    1072 B |        2.16 |
|                               |            |                |             |             |           |           |           |       |         |        |        |           |             |
| ExistsAsync_False             | MediumRun  | 15             | 2           | 10          |  1.141 μs | 0.0051 μs | 0.0077 μs |  0.82 |    0.01 | 0.0362 |      - |     616 B |        1.24 |
| ExistsAsync_True              | MediumRun  | 15             | 2           | 10          |  1.155 μs | 0.0021 μs | 0.0029 μs |  0.83 |    0.00 | 0.0362 |      - |     616 B |        1.24 |
| GetAsync_CacheHit             | MediumRun  | 15             | 2           | 10          |  1.392 μs | 0.0020 μs | 0.0028 μs |  1.00 |    0.00 | 0.0286 |      - |     496 B |        1.00 |
| GetAsync_CacheMiss            | MediumRun  | 15             | 2           | 10          |  1.094 μs | 0.0036 μs | 0.0050 μs |  0.79 |    0.00 | 0.0362 |      - |     616 B |        1.24 |
| GetOrSetAsync_CacheHit        | MediumRun  | 15             | 2           | 10          |  1.412 μs | 0.0020 μs | 0.0028 μs |  1.01 |    0.00 | 0.0324 |      - |     560 B |        1.13 |
| GetOrSetAsync_CacheMiss       | MediumRun  | 15             | 2           | 10          |  7.034 μs | 0.1033 μs | 0.1449 μs |  5.05 |    0.10 | 0.1068 | 0.0992 |    1792 B |        3.61 |
| GetOrSetAsync_WithTags        | MediumRun  | 15             | 2           | 10          | 12.134 μs | 0.4428 μs | 0.6351 μs |  8.72 |    0.45 | 0.1221 | 0.1068 |    2216 B |        4.47 |
| RemoveAsync                   | MediumRun  | 15             | 2           | 10          |  4.294 μs | 0.0956 μs | 0.1430 μs |  3.08 |    0.10 | 0.0610 |      - |    1136 B |        2.29 |
| RemoveByTagAsync              | MediumRun  | 15             | 2           | 10          | 10.548 μs | 0.1829 μs | 0.2564 μs |  7.58 |    0.18 | 0.1373 | 0.1221 |    2448 B |        4.94 |
| SetAsync                      | MediumRun  | 15             | 2           | 10          |  4.425 μs | 0.0945 μs | 0.1294 μs |  3.18 |    0.09 | 0.0610 | 0.0534 |    1064 B |        2.15 |
| SetWithSlidingExpirationAsync | MediumRun  | 15             | 2           | 10          |  4.343 μs | 0.0853 μs | 0.1109 μs |  3.12 |    0.08 | 0.0763 | 0.0687 |    1292 B |        2.60 |
