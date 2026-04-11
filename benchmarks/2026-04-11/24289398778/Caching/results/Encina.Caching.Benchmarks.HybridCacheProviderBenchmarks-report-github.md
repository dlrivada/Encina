```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-NUBXJZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method                        | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error     | StdDev    | Median    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |----------- |--------------- |------------ |------------ |----------:|----------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| ExistsAsync_True              | Job-NUBXJZ | 20             | Default     | 5           |  1.154 μs | 0.0028 μs | 0.0031 μs |  1.155 μs |  0.84 |    0.00 | 0.0362 |      - |     616 B |        1.24 |
| ExistsAsync_False             | Job-NUBXJZ | 20             | Default     | 5           |  1.125 μs | 0.0025 μs | 0.0028 μs |  1.126 μs |  0.81 |    0.00 | 0.0362 |      - |     616 B |        1.24 |
| GetOrSetAsync_CacheHit        | Job-NUBXJZ | 20             | Default     | 5           |  1.382 μs | 0.0025 μs | 0.0029 μs |  1.382 μs |  1.00 |    0.00 | 0.0324 |      - |     560 B |        1.13 |
| GetOrSetAsync_CacheMiss       | Job-NUBXJZ | 20             | Default     | 5           |  6.769 μs | 0.1395 μs | 0.1492 μs |  6.742 μs |  4.90 |    0.11 | 0.1068 | 0.0992 |    1792 B |        3.61 |
| GetOrSetAsync_WithTags        | Job-NUBXJZ | 20             | Default     | 5           | 11.563 μs | 0.7519 μs | 0.8045 μs | 11.390 μs |  8.37 |    0.57 | 0.1221 | 0.1068 |    2216 B |        4.47 |
| RemoveAsync                   | Job-NUBXJZ | 20             | Default     | 5           |  4.281 μs | 0.0937 μs | 0.1079 μs |  4.307 μs |  3.10 |    0.08 | 0.0610 |      - |    1136 B |        2.29 |
| RemoveByTagAsync              | Job-NUBXJZ | 20             | Default     | 5           | 10.347 μs | 0.2964 μs | 0.3172 μs | 10.319 μs |  7.49 |    0.22 | 0.1373 | 0.1221 |    2448 B |        4.94 |
| SetWithSlidingExpirationAsync | Job-NUBXJZ | 20             | Default     | 5           |  4.266 μs | 0.1815 μs | 0.1782 μs |  4.227 μs |  3.09 |    0.13 | 0.0763 | 0.0687 |    1334 B |        2.69 |
| GetAsync_CacheHit             | Job-NUBXJZ | 20             | Default     | 5           |  1.381 μs | 0.0022 μs | 0.0024 μs |  1.381 μs |  1.00 |    0.00 | 0.0286 |      - |     496 B |        1.00 |
| GetAsync_CacheMiss            | Job-NUBXJZ | 20             | Default     | 5           |  1.115 μs | 0.0023 μs | 0.0026 μs |  1.116 μs |  0.81 |    0.00 | 0.0362 |      - |     616 B |        1.24 |
| SetAsync                      | Job-NUBXJZ | 20             | Default     | 5           |  4.248 μs | 0.1232 μs | 0.1266 μs |  4.232 μs |  3.08 |    0.09 | 0.0610 | 0.0534 |    1064 B |        2.15 |
|                               |            |                |             |             |           |           |           |           |       |         |        |        |           |             |
| ExistsAsync_True              | MediumRun  | 15             | 2           | 10          |  1.159 μs | 0.0023 μs | 0.0032 μs |  1.159 μs |  0.82 |    0.01 | 0.0362 |      - |     616 B |        1.24 |
| ExistsAsync_False             | MediumRun  | 15             | 2           | 10          |  1.120 μs | 0.0021 μs | 0.0030 μs |  1.119 μs |  0.80 |    0.01 | 0.0362 |      - |     616 B |        1.24 |
| GetOrSetAsync_CacheHit        | MediumRun  | 15             | 2           | 10          |  1.407 μs | 0.0038 μs | 0.0056 μs |  1.408 μs |  1.00 |    0.01 | 0.0324 |      - |     560 B |        1.13 |
| GetOrSetAsync_CacheMiss       | MediumRun  | 15             | 2           | 10          |  7.004 μs | 0.0661 μs | 0.0927 μs |  6.990 μs |  4.98 |    0.09 | 0.1068 | 0.0992 |    1792 B |        3.61 |
| GetOrSetAsync_WithTags        | MediumRun  | 15             | 2           | 10          | 11.990 μs | 0.5199 μs | 0.7456 μs | 12.045 μs |  8.52 |    0.53 | 0.1221 | 0.1068 |    2216 B |        4.47 |
| RemoveAsync                   | MediumRun  | 15             | 2           | 10          |  4.340 μs | 0.0506 μs | 0.0757 μs |  4.333 μs |  3.08 |    0.06 | 0.0610 |      - |    1136 B |        2.29 |
| RemoveByTagAsync              | MediumRun  | 15             | 2           | 10          | 10.344 μs | 0.1767 μs | 0.2419 μs | 10.364 μs |  7.35 |    0.19 | 0.1373 | 0.1221 |    2448 B |        4.94 |
| SetWithSlidingExpirationAsync | MediumRun  | 15             | 2           | 10          |  4.283 μs | 0.1191 μs | 0.1590 μs |  4.257 μs |  3.04 |    0.12 | 0.0610 | 0.0534 |    1072 B |        2.16 |
| GetAsync_CacheHit             | MediumRun  | 15             | 2           | 10          |  1.407 μs | 0.0112 μs | 0.0161 μs |  1.419 μs |  1.00 |    0.02 | 0.0286 |      - |     496 B |        1.00 |
| GetAsync_CacheMiss            | MediumRun  | 15             | 2           | 10          |  1.106 μs | 0.0059 μs | 0.0086 μs |  1.104 μs |  0.79 |    0.01 | 0.0362 |      - |     616 B |        1.24 |
| SetAsync                      | MediumRun  | 15             | 2           | 10          |  4.320 μs | 0.1018 μs | 0.1461 μs |  4.323 μs |  3.07 |    0.11 | 0.0610 | 0.0534 |    1064 B |        2.15 |
