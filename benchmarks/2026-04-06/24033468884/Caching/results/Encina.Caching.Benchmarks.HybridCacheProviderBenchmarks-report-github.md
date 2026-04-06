```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method                        | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |----------- |--------------- |------------ |------------ |----------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| GetAsync_CacheHit             | Job-YFEFPZ | 10             | Default     | 3           |  1.378 μs | 0.0015 μs | 0.0008 μs |  1.00 |    0.00 | 0.0286 |      - |     496 B |        1.00 |
| GetAsync_CacheMiss            | Job-YFEFPZ | 10             | Default     | 3           |  1.106 μs | 0.0030 μs | 0.0020 μs |  0.80 |    0.00 | 0.0362 |      - |     616 B |        1.24 |
| SetAsync                      | Job-YFEFPZ | 10             | Default     | 3           |  4.429 μs | 0.1826 μs | 0.1087 μs |  3.21 |    0.07 | 0.0610 | 0.0534 |    1064 B |        2.15 |
| ExistsAsync_True              | Job-YFEFPZ | 10             | Default     | 3           |  1.164 μs | 0.0036 μs | 0.0024 μs |  0.84 |    0.00 | 0.0362 |      - |     616 B |        1.24 |
| ExistsAsync_False             | Job-YFEFPZ | 10             | Default     | 3           |  1.133 μs | 0.0034 μs | 0.0020 μs |  0.82 |    0.00 | 0.0362 |      - |     616 B |        1.24 |
| GetOrSetAsync_CacheHit        | Job-YFEFPZ | 10             | Default     | 3           |  1.407 μs | 0.0024 μs | 0.0014 μs |  1.02 |    0.00 | 0.0324 |      - |     560 B |        1.13 |
| GetOrSetAsync_CacheMiss       | Job-YFEFPZ | 10             | Default     | 3           |  7.330 μs | 0.2282 μs | 0.1358 μs |  5.32 |    0.09 | 0.1068 | 0.0992 |    1792 B |        3.61 |
| GetOrSetAsync_WithTags        | Job-YFEFPZ | 10             | Default     | 3           | 11.594 μs | 1.0057 μs | 0.5985 μs |  8.41 |    0.41 | 0.1221 | 0.1068 |    2216 B |        4.47 |
| RemoveAsync                   | Job-YFEFPZ | 10             | Default     | 3           |  4.306 μs | 0.0594 μs | 0.0393 μs |  3.13 |    0.03 | 0.0610 |      - |    1136 B |        2.29 |
| RemoveByTagAsync              | Job-YFEFPZ | 10             | Default     | 3           | 10.691 μs | 0.5700 μs | 0.3392 μs |  7.76 |    0.23 | 0.1373 | 0.1221 |    2448 B |        4.94 |
| SetWithSlidingExpirationAsync | Job-YFEFPZ | 10             | Default     | 3           |  4.331 μs | 0.0551 μs | 0.0328 μs |  3.14 |    0.02 | 0.0610 | 0.0534 |    1072 B |        2.16 |
|                               |            |                |             |             |           |           |           |       |         |        |        |           |             |
| GetAsync_CacheHit             | MediumRun  | 15             | 2           | 10          |  1.422 μs | 0.0043 μs | 0.0062 μs |  1.00 |    0.01 | 0.0286 |      - |     496 B |        1.00 |
| GetAsync_CacheMiss            | MediumRun  | 15             | 2           | 10          |  1.117 μs | 0.0009 μs | 0.0013 μs |  0.79 |    0.00 | 0.0362 |      - |     616 B |        1.24 |
| SetAsync                      | MediumRun  | 15             | 2           | 10          |  4.537 μs | 0.1117 μs | 0.1602 μs |  3.19 |    0.11 | 0.0610 | 0.0534 |    1064 B |        2.15 |
| ExistsAsync_True              | MediumRun  | 15             | 2           | 10          |  1.192 μs | 0.0015 μs | 0.0023 μs |  0.84 |    0.00 | 0.0362 |      - |     616 B |        1.24 |
| ExistsAsync_False             | MediumRun  | 15             | 2           | 10          |  1.142 μs | 0.0041 μs | 0.0060 μs |  0.80 |    0.01 | 0.0362 |      - |     616 B |        1.24 |
| GetOrSetAsync_CacheHit        | MediumRun  | 15             | 2           | 10          |  1.386 μs | 0.0063 μs | 0.0093 μs |  0.97 |    0.01 | 0.0324 |      - |     560 B |        1.13 |
| GetOrSetAsync_CacheMiss       | MediumRun  | 15             | 2           | 10          |  7.099 μs | 0.1136 μs | 0.1592 μs |  4.99 |    0.11 | 0.1068 | 0.0992 |    1792 B |        3.61 |
| GetOrSetAsync_WithTags        | MediumRun  | 15             | 2           | 10          | 12.301 μs | 0.5226 μs | 0.7495 μs |  8.65 |    0.52 | 0.1221 | 0.1068 |    2216 B |        4.47 |
| RemoveAsync                   | MediumRun  | 15             | 2           | 10          |  4.293 μs | 0.0718 μs | 0.1075 μs |  3.02 |    0.08 | 0.0610 |      - |    1136 B |        2.29 |
| RemoveByTagAsync              | MediumRun  | 15             | 2           | 10          | 10.935 μs | 0.2504 μs | 0.3510 μs |  7.69 |    0.24 | 0.1373 | 0.1221 |    2448 B |        4.94 |
| SetWithSlidingExpirationAsync | MediumRun  | 15             | 2           | 10          |  4.403 μs | 0.1235 μs | 0.1606 μs |  3.10 |    0.11 | 0.0763 | 0.0687 |    1285 B |        2.59 |
