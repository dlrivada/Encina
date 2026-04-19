```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.202
  [Host]     : .NET 10.0.6 (10.0.6, 10.0.626.17701), X64 RyuJIT x86-64-v3
  Job-NUBXJZ : .NET 10.0.6 (10.0.6, 10.0.626.17701), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.6 (10.0.6, 10.0.626.17701), X64 RyuJIT x86-64-v3


```
| Method                        | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |----------- |--------------- |------------ |------------ |----------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| ExistsAsync_True              | Job-NUBXJZ | 20             | Default     | 5           |  1.193 μs | 0.0026 μs | 0.0029 μs |  0.83 |    0.00 | 0.0362 |      - |     616 B |        1.24 |
| ExistsAsync_False             | Job-NUBXJZ | 20             | Default     | 5           |  1.161 μs | 0.0020 μs | 0.0023 μs |  0.81 |    0.00 | 0.0362 |      - |     616 B |        1.24 |
| GetOrSetAsync_CacheHit        | Job-NUBXJZ | 20             | Default     | 5           |  1.419 μs | 0.0030 μs | 0.0033 μs |  0.99 |    0.00 | 0.0324 |      - |     560 B |        1.13 |
| GetOrSetAsync_CacheMiss       | Job-NUBXJZ | 20             | Default     | 5           |  7.272 μs | 0.1716 μs | 0.1836 μs |  5.06 |    0.12 | 0.1068 | 0.0992 |    1792 B |        3.61 |
| GetOrSetAsync_WithTags        | Job-NUBXJZ | 20             | Default     | 5           | 12.363 μs | 0.9472 μs | 1.0135 μs |  8.61 |    0.69 | 0.1221 | 0.1068 |    2216 B |        4.47 |
| RemoveAsync                   | Job-NUBXJZ | 20             | Default     | 5           |  4.391 μs | 0.0513 μs | 0.0591 μs |  3.06 |    0.04 | 0.0610 |      - |    1136 B |        2.29 |
| RemoveByTagAsync              | Job-NUBXJZ | 20             | Default     | 5           | 11.340 μs | 0.2477 μs | 0.2544 μs |  7.90 |    0.17 | 0.1373 | 0.1221 |    2448 B |        4.94 |
| SetWithSlidingExpirationAsync | Job-NUBXJZ | 20             | Default     | 5           |  4.584 μs | 0.1811 μs | 0.1779 μs |  3.19 |    0.12 | 0.0763 | 0.0687 |    1319 B |        2.66 |
| GetAsync_CacheHit             | Job-NUBXJZ | 20             | Default     | 5           |  1.436 μs | 0.0022 μs | 0.0024 μs |  1.00 |    0.00 | 0.0286 |      - |     496 B |        1.00 |
| GetAsync_CacheMiss            | Job-NUBXJZ | 20             | Default     | 5           |  1.105 μs | 0.0012 μs | 0.0013 μs |  0.77 |    0.00 | 0.0362 |      - |     616 B |        1.24 |
| SetAsync                      | Job-NUBXJZ | 20             | Default     | 5           |  4.581 μs | 0.1429 μs | 0.1529 μs |  3.19 |    0.10 | 0.0610 | 0.0534 |    1064 B |        2.15 |
|                               |            |                |             |             |           |           |           |       |         |        |        |           |             |
| ExistsAsync_True              | MediumRun  | 15             | 2           | 10          |  1.196 μs | 0.0019 μs | 0.0028 μs |  0.84 |    0.00 | 0.0362 |      - |     616 B |        1.24 |
| ExistsAsync_False             | MediumRun  | 15             | 2           | 10          |  1.149 μs | 0.0044 μs | 0.0064 μs |  0.80 |    0.01 | 0.0362 |      - |     616 B |        1.24 |
| GetOrSetAsync_CacheHit        | MediumRun  | 15             | 2           | 10          |  1.430 μs | 0.0027 μs | 0.0041 μs |  1.00 |    0.00 | 0.0324 |      - |     560 B |        1.13 |
| GetOrSetAsync_CacheMiss       | MediumRun  | 15             | 2           | 10          |  7.200 μs | 0.0768 μs | 0.1051 μs |  5.04 |    0.07 | 0.1068 | 0.0992 |    1792 B |        3.61 |
| GetOrSetAsync_WithTags        | MediumRun  | 15             | 2           | 10          | 12.002 μs | 0.5144 μs | 0.7377 μs |  8.40 |    0.51 | 0.1221 | 0.1068 |    2216 B |        4.47 |
| RemoveAsync                   | MediumRun  | 15             | 2           | 10          |  4.410 μs | 0.0591 μs | 0.0828 μs |  3.09 |    0.06 | 0.0610 |      - |    1136 B |        2.29 |
| RemoveByTagAsync              | MediumRun  | 15             | 2           | 10          | 10.767 μs | 0.2633 μs | 0.3692 μs |  7.54 |    0.26 | 0.1373 | 0.1221 |    2448 B |        4.94 |
| SetWithSlidingExpirationAsync | MediumRun  | 15             | 2           | 10          |  4.408 μs | 0.0945 μs | 0.1229 μs |  3.09 |    0.08 | 0.0610 | 0.0534 |    1072 B |        2.16 |
| GetAsync_CacheHit             | MediumRun  | 15             | 2           | 10          |  1.428 μs | 0.0037 μs | 0.0049 μs |  1.00 |    0.00 | 0.0286 |      - |     496 B |        1.00 |
| GetAsync_CacheMiss            | MediumRun  | 15             | 2           | 10          |  1.102 μs | 0.0058 μs | 0.0085 μs |  0.77 |    0.01 | 0.0362 |      - |     616 B |        1.24 |
| SetAsync                      | MediumRun  | 15             | 2           | 10          |  4.495 μs | 0.0842 μs | 0.1208 μs |  3.15 |    0.08 | 0.0610 | 0.0534 |    1064 B |        2.15 |
