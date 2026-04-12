```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-NUBXJZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method                        | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error     | StdDev    | Median    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |----------- |--------------- |------------ |------------ |----------:|----------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| ExistsAsync_True              | Job-NUBXJZ | 20             | Default     | 5           |  1.194 μs | 0.0043 μs | 0.0044 μs |  1.193 μs |  0.86 |    0.00 | 0.0362 |      - |     616 B |        1.24 |
| ExistsAsync_False             | Job-NUBXJZ | 20             | Default     | 5           |  1.167 μs | 0.0022 μs | 0.0024 μs |  1.167 μs |  0.84 |    0.00 | 0.0362 |      - |     616 B |        1.24 |
| GetOrSetAsync_CacheHit        | Job-NUBXJZ | 20             | Default     | 5           |  1.391 μs | 0.0044 μs | 0.0051 μs |  1.392 μs |  1.00 |    0.01 | 0.0324 |      - |     560 B |        1.13 |
| GetOrSetAsync_CacheMiss       | Job-NUBXJZ | 20             | Default     | 5           |  6.893 μs | 0.1913 μs | 0.2047 μs |  6.923 μs |  4.97 |    0.15 | 0.1068 | 0.0992 |    1792 B |        3.61 |
| GetOrSetAsync_WithTags        | Job-NUBXJZ | 20             | Default     | 5           | 12.091 μs | 1.0273 μs | 1.0992 μs | 12.062 μs |  8.72 |    0.77 | 0.1221 | 0.1068 |    2216 B |        4.47 |
| RemoveAsync                   | Job-NUBXJZ | 20             | Default     | 5           |  4.328 μs | 0.0667 μs | 0.0768 μs |  4.340 μs |  3.12 |    0.06 | 0.0610 |      - |    1136 B |        2.29 |
| RemoveByTagAsync              | Job-NUBXJZ | 20             | Default     | 5           | 10.916 μs | 0.2904 μs | 0.2982 μs | 10.961 μs |  7.87 |    0.21 | 0.1373 | 0.1221 |    2448 B |        4.94 |
| SetWithSlidingExpirationAsync | Job-NUBXJZ | 20             | Default     | 5           |  4.722 μs | 0.2494 μs | 0.2449 μs |  4.654 μs |  3.41 |    0.17 | 0.0763 | 0.0687 |    1282 B |        2.58 |
| GetAsync_CacheHit             | Job-NUBXJZ | 20             | Default     | 5           |  1.386 μs | 0.0049 μs | 0.0057 μs |  1.386 μs |  1.00 |    0.01 | 0.0286 |      - |     496 B |        1.00 |
| GetAsync_CacheMiss            | Job-NUBXJZ | 20             | Default     | 5           |  1.151 μs | 0.0027 μs | 0.0029 μs |  1.151 μs |  0.83 |    0.00 | 0.0362 |      - |     616 B |        1.24 |
| SetAsync                      | Job-NUBXJZ | 20             | Default     | 5           |  4.871 μs | 0.2316 μs | 0.2478 μs |  4.798 μs |  3.51 |    0.17 | 0.0610 | 0.0534 |    1064 B |        2.15 |
|                               |            |                |             |             |           |           |           |           |       |         |        |        |           |             |
| ExistsAsync_True              | MediumRun  | 15             | 2           | 10          |  1.196 μs | 0.0081 μs | 0.0118 μs |  1.194 μs |  0.86 |    0.01 | 0.0362 |      - |     616 B |        1.24 |
| ExistsAsync_False             | MediumRun  | 15             | 2           | 10          |  1.185 μs | 0.0099 μs | 0.0138 μs |  1.177 μs |  0.85 |    0.01 | 0.0362 |      - |     616 B |        1.24 |
| GetOrSetAsync_CacheHit        | MediumRun  | 15             | 2           | 10          |  1.398 μs | 0.0025 μs | 0.0037 μs |  1.397 μs |  1.00 |    0.01 | 0.0324 |      - |     560 B |        1.13 |
| GetOrSetAsync_CacheMiss       | MediumRun  | 15             | 2           | 10          |  7.179 μs | 0.1279 μs | 0.1707 μs |  7.225 μs |  5.14 |    0.12 | 0.1068 | 0.0992 |    1792 B |        3.61 |
| GetOrSetAsync_WithTags        | MediumRun  | 15             | 2           | 10          | 12.406 μs | 0.5603 μs | 0.8036 μs | 12.323 μs |  8.87 |    0.57 | 0.1221 | 0.1068 |    2216 B |        4.47 |
| RemoveAsync                   | MediumRun  | 15             | 2           | 10          |  4.369 μs | 0.0419 μs | 0.0614 μs |  4.368 μs |  3.13 |    0.05 | 0.0610 |      - |    1136 B |        2.29 |
| RemoveByTagAsync              | MediumRun  | 15             | 2           | 10          | 10.990 μs | 0.2029 μs | 0.2778 μs | 10.989 μs |  7.86 |    0.20 | 0.1373 | 0.1221 |    2448 B |        4.94 |
| SetWithSlidingExpirationAsync | MediumRun  | 15             | 2           | 10          |  5.042 μs | 0.4891 μs | 0.7014 μs |  4.728 μs |  3.61 |    0.49 | 0.0687 | 0.0610 |    1256 B |        2.53 |
| GetAsync_CacheHit             | MediumRun  | 15             | 2           | 10          |  1.398 μs | 0.0054 μs | 0.0081 μs |  1.402 μs |  1.00 |    0.01 | 0.0286 |      - |     496 B |        1.00 |
| GetAsync_CacheMiss            | MediumRun  | 15             | 2           | 10          |  1.139 μs | 0.0018 μs | 0.0026 μs |  1.139 μs |  0.81 |    0.01 | 0.0362 |      - |     616 B |        1.24 |
| SetAsync                      | MediumRun  | 15             | 2           | 10          |  4.713 μs | 0.1387 μs | 0.1989 μs |  4.681 μs |  3.37 |    0.14 | 0.0610 | 0.0534 |    1064 B |        2.15 |
