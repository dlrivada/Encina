```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-NUBXJZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3


```
| Method                        | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error     | StdDev    | Median    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |----------- |--------------- |------------ |------------ |----------:|----------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| ExistsAsync_False             | Job-NUBXJZ | 20             | Default     | 5           |  1.189 μs | 0.0029 μs | 0.0032 μs |  1.188 μs |  0.82 |    0.00 | 0.0362 |      - |     616 B |        1.24 |
| ExistsAsync_True              | Job-NUBXJZ | 20             | Default     | 5           |  1.194 μs | 0.0020 μs | 0.0022 μs |  1.194 μs |  0.83 |    0.00 | 0.0362 |      - |     616 B |        1.24 |
| GetAsync_CacheHit             | Job-NUBXJZ | 20             | Default     | 5           |  1.441 μs | 0.0024 μs | 0.0026 μs |  1.441 μs |  1.00 |    0.00 | 0.0286 |      - |     496 B |        1.00 |
| GetAsync_CacheMiss            | Job-NUBXJZ | 20             | Default     | 5           |  1.142 μs | 0.0034 μs | 0.0038 μs |  1.142 μs |  0.79 |    0.00 | 0.0362 |      - |     616 B |        1.24 |
| GetOrSetAsync_CacheHit        | Job-NUBXJZ | 20             | Default     | 5           |  1.426 μs | 0.0018 μs | 0.0020 μs |  1.426 μs |  0.99 |    0.00 | 0.0324 |      - |     560 B |        1.13 |
| GetOrSetAsync_CacheMiss       | Job-NUBXJZ | 20             | Default     | 5           |  7.153 μs | 0.1141 μs | 0.1172 μs |  7.166 μs |  4.96 |    0.08 | 0.1068 | 0.0992 |    1792 B |        3.61 |
| GetOrSetAsync_WithTags        | Job-NUBXJZ | 20             | Default     | 5           | 12.344 μs | 0.7476 μs | 0.7999 μs | 12.007 μs |  8.56 |    0.54 | 0.1221 | 0.1068 |    2216 B |        4.47 |
| RemoveAsync                   | Job-NUBXJZ | 20             | Default     | 5           |  4.503 μs | 0.0580 μs | 0.0668 μs |  4.515 μs |  3.12 |    0.05 | 0.0610 |      - |    1136 B |        2.29 |
| RemoveByTagAsync              | Job-NUBXJZ | 20             | Default     | 5           | 10.800 μs | 0.2434 μs | 0.2500 μs | 10.755 μs |  7.49 |    0.17 | 0.1373 | 0.1221 |    2448 B |        4.94 |
| SetAsync                      | Job-NUBXJZ | 20             | Default     | 5           |  4.481 μs | 0.1263 μs | 0.1351 μs |  4.469 μs |  3.11 |    0.09 | 0.0610 | 0.0534 |    1064 B |        2.15 |
| SetWithSlidingExpirationAsync | Job-NUBXJZ | 20             | Default     | 5           |  4.484 μs | 0.2468 μs | 0.2424 μs |  4.449 μs |  3.11 |    0.16 | 0.0763 | 0.0687 |    1307 B |        2.64 |
|                               |            |                |             |             |           |           |           |           |       |         |        |        |           |             |
| ExistsAsync_False             | MediumRun  | 15             | 2           | 10          |  1.168 μs | 0.0037 μs | 0.0055 μs |  1.167 μs |  0.81 |    0.02 | 0.0362 |      - |     616 B |        1.24 |
| ExistsAsync_True              | MediumRun  | 15             | 2           | 10          |  1.208 μs | 0.0020 μs | 0.0030 μs |  1.207 μs |  0.84 |    0.02 | 0.0362 |      - |     616 B |        1.24 |
| GetAsync_CacheHit             | MediumRun  | 15             | 2           | 10          |  1.444 μs | 0.0240 μs | 0.0337 μs |  1.470 μs |  1.00 |    0.03 | 0.0286 |      - |     496 B |        1.00 |
| GetAsync_CacheMiss            | MediumRun  | 15             | 2           | 10          |  1.129 μs | 0.0075 μs | 0.0109 μs |  1.137 μs |  0.78 |    0.02 | 0.0362 |      - |     616 B |        1.24 |
| GetOrSetAsync_CacheHit        | MediumRun  | 15             | 2           | 10          |  1.457 μs | 0.0050 μs | 0.0072 μs |  1.454 μs |  1.01 |    0.02 | 0.0324 |      - |     560 B |        1.13 |
| GetOrSetAsync_CacheMiss       | MediumRun  | 15             | 2           | 10          |  7.041 μs | 0.1115 μs | 0.1563 μs |  7.019 μs |  4.88 |    0.15 | 0.1068 | 0.0992 |    1792 B |        3.61 |
| GetOrSetAsync_WithTags        | MediumRun  | 15             | 2           | 10          | 12.346 μs | 0.5620 μs | 0.8059 μs | 12.342 μs |  8.56 |    0.58 | 0.1221 | 0.1068 |    2216 B |        4.47 |
| RemoveAsync                   | MediumRun  | 15             | 2           | 10          |  4.468 μs | 0.0542 μs | 0.0811 μs |  4.480 μs |  3.10 |    0.09 | 0.0610 |      - |    1136 B |        2.29 |
| RemoveByTagAsync              | MediumRun  | 15             | 2           | 10          | 10.691 μs | 0.2817 μs | 0.3949 μs | 10.585 μs |  7.41 |    0.32 | 0.1373 | 0.1221 |    2448 B |        4.94 |
| SetAsync                      | MediumRun  | 15             | 2           | 10          |  4.592 μs | 0.1008 μs | 0.1446 μs |  4.544 μs |  3.18 |    0.12 | 0.0610 | 0.0534 |    1064 B |        2.15 |
| SetWithSlidingExpirationAsync | MediumRun  | 15             | 2           | 10          |  4.479 μs | 0.1430 μs | 0.1860 μs |  4.450 μs |  3.10 |    0.15 | 0.0763 | 0.0687 |    1287 B |        2.59 |
