```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-NUBXJZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method                        | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |----------- |--------------- |------------ |------------ |----------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| ExistsAsync_True              | Job-NUBXJZ | 20             | Default     | 5           |  1.165 μs | 0.0024 μs | 0.0024 μs |  0.83 |    0.00 | 0.0362 |      - |     616 B |        1.24 |
| ExistsAsync_False             | Job-NUBXJZ | 20             | Default     | 5           |  1.115 μs | 0.0013 μs | 0.0014 μs |  0.80 |    0.00 | 0.0362 |      - |     616 B |        1.24 |
| GetOrSetAsync_CacheHit        | Job-NUBXJZ | 20             | Default     | 5           |  1.429 μs | 0.0021 μs | 0.0025 μs |  1.02 |    0.00 | 0.0324 |      - |     560 B |        1.13 |
| GetOrSetAsync_CacheMiss       | Job-NUBXJZ | 20             | Default     | 5           |  7.192 μs | 0.1496 μs | 0.1601 μs |  5.14 |    0.11 | 0.1068 | 0.0992 |    1792 B |        3.61 |
| GetOrSetAsync_WithTags        | Job-NUBXJZ | 20             | Default     | 5           | 11.816 μs | 0.6929 μs | 0.7414 μs |  8.45 |    0.52 | 0.1221 | 0.1068 |    2216 B |        4.47 |
| RemoveAsync                   | Job-NUBXJZ | 20             | Default     | 5           |  4.341 μs | 0.0796 μs | 0.0917 μs |  3.10 |    0.06 | 0.0610 |      - |    1136 B |        2.29 |
| RemoveByTagAsync              | Job-NUBXJZ | 20             | Default     | 5           | 10.573 μs | 0.3465 μs | 0.3707 μs |  7.56 |    0.26 | 0.1373 | 0.1221 |    2448 B |        4.94 |
| SetWithSlidingExpirationAsync | Job-NUBXJZ | 20             | Default     | 5           |  4.434 μs | 0.1438 μs | 0.1412 μs |  3.17 |    0.10 | 0.0763 | 0.0687 |    1286 B |        2.59 |
| GetAsync_CacheHit             | Job-NUBXJZ | 20             | Default     | 5           |  1.399 μs | 0.0027 μs | 0.0031 μs |  1.00 |    0.00 | 0.0286 |      - |     496 B |        1.00 |
| GetAsync_CacheMiss            | Job-NUBXJZ | 20             | Default     | 5           |  1.102 μs | 0.0015 μs | 0.0016 μs |  0.79 |    0.00 | 0.0362 |      - |     616 B |        1.24 |
| SetAsync                      | Job-NUBXJZ | 20             | Default     | 5           |  4.475 μs | 0.1661 μs | 0.1777 μs |  3.20 |    0.12 | 0.0610 | 0.0534 |    1064 B |        2.15 |
|                               |            |                |             |             |           |           |           |       |         |        |        |           |             |
| ExistsAsync_True              | MediumRun  | 15             | 2           | 10          |  1.168 μs | 0.0012 μs | 0.0018 μs |  0.83 |    0.00 | 0.0362 |      - |     616 B |        1.24 |
| ExistsAsync_False             | MediumRun  | 15             | 2           | 10          |  1.136 μs | 0.0061 μs | 0.0088 μs |  0.80 |    0.01 | 0.0362 |      - |     616 B |        1.24 |
| GetOrSetAsync_CacheHit        | MediumRun  | 15             | 2           | 10          |  1.428 μs | 0.0059 μs | 0.0088 μs |  1.01 |    0.01 | 0.0324 |      - |     560 B |        1.13 |
| GetOrSetAsync_CacheMiss       | MediumRun  | 15             | 2           | 10          |  7.220 μs | 0.0674 μs | 0.0922 μs |  5.11 |    0.07 | 0.1068 | 0.0992 |    1792 B |        3.61 |
| GetOrSetAsync_WithTags        | MediumRun  | 15             | 2           | 10          | 12.175 μs | 0.4421 μs | 0.6340 μs |  8.61 |    0.44 | 0.1221 | 0.1068 |    2216 B |        4.47 |
| RemoveAsync                   | MediumRun  | 15             | 2           | 10          |  4.345 μs | 0.0611 μs | 0.0915 μs |  3.07 |    0.06 | 0.0610 |      - |    1136 B |        2.29 |
| RemoveByTagAsync              | MediumRun  | 15             | 2           | 10          | 11.134 μs | 0.2659 μs | 0.3813 μs |  7.88 |    0.27 | 0.1373 | 0.1221 |    2448 B |        4.94 |
| SetWithSlidingExpirationAsync | MediumRun  | 15             | 2           | 10          |  4.455 μs | 0.0851 μs | 0.1106 μs |  3.15 |    0.08 | 0.0763 | 0.0687 |    1297 B |        2.61 |
| GetAsync_CacheHit             | MediumRun  | 15             | 2           | 10          |  1.413 μs | 0.0026 μs | 0.0037 μs |  1.00 |    0.00 | 0.0286 |      - |     496 B |        1.00 |
| GetAsync_CacheMiss            | MediumRun  | 15             | 2           | 10          |  1.138 μs | 0.0011 μs | 0.0015 μs |  0.81 |    0.00 | 0.0362 |      - |     616 B |        1.24 |
| SetAsync                      | MediumRun  | 15             | 2           | 10          |  4.548 μs | 0.1118 μs | 0.1603 μs |  3.22 |    0.11 | 0.0610 | 0.0534 |    1064 B |        2.15 |
