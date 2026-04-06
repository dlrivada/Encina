```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method                        | Job        | IterationCount | LaunchCount | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |----------- |--------------- |------------ |----------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| GetAsync_CacheHit             | Job-YFEFPZ | 10             | Default     |  1.391 μs | 0.0030 μs | 0.0020 μs |  1.00 |    0.00 | 0.0286 |      - |     496 B |        1.00 |
| GetAsync_CacheMiss            | Job-YFEFPZ | 10             | Default     |  1.100 μs | 0.0041 μs | 0.0025 μs |  0.79 |    0.00 | 0.0362 |      - |     616 B |        1.24 |
| SetAsync                      | Job-YFEFPZ | 10             | Default     |  4.241 μs | 0.1718 μs | 0.1022 μs |  3.05 |    0.07 | 0.0610 | 0.0534 |    1064 B |        2.15 |
| ExistsAsync_True              | Job-YFEFPZ | 10             | Default     |  1.159 μs | 0.0049 μs | 0.0029 μs |  0.83 |    0.00 | 0.0362 |      - |     616 B |        1.24 |
| ExistsAsync_False             | Job-YFEFPZ | 10             | Default     |  1.120 μs | 0.0040 μs | 0.0026 μs |  0.81 |    0.00 | 0.0362 |      - |     616 B |        1.24 |
| GetOrSetAsync_CacheHit        | Job-YFEFPZ | 10             | Default     |  1.408 μs | 0.0065 μs | 0.0039 μs |  1.01 |    0.00 | 0.0324 |      - |     560 B |        1.13 |
| GetOrSetAsync_CacheMiss       | Job-YFEFPZ | 10             | Default     |  6.946 μs | 0.2029 μs | 0.1208 μs |  4.99 |    0.08 | 0.1068 | 0.0992 |    1792 B |        3.61 |
| GetOrSetAsync_WithTags        | Job-YFEFPZ | 10             | Default     | 10.559 μs | 1.0017 μs | 0.5961 μs |  7.59 |    0.41 | 0.1221 | 0.1068 |    2216 B |        4.47 |
| RemoveAsync                   | Job-YFEFPZ | 10             | Default     |  4.292 μs | 0.1763 μs | 0.1166 μs |  3.09 |    0.08 | 0.0610 |      - |    1136 B |        2.29 |
| RemoveByTagAsync              | Job-YFEFPZ | 10             | Default     |  9.996 μs | 0.4400 μs | 0.2301 μs |  7.19 |    0.16 | 0.1373 | 0.1221 |    2448 B |        4.94 |
| SetWithSlidingExpirationAsync | Job-YFEFPZ | 10             | Default     |  4.156 μs | 0.2059 μs | 0.1225 μs |  2.99 |    0.08 | 0.0610 | 0.0534 |    1072 B |        2.16 |
|                               |            |                |             |           |           |           |       |         |        |        |           |             |
| GetAsync_CacheHit             | ShortRun   | 3              | 1           |  1.397 μs | 0.0337 μs | 0.0018 μs |  1.00 |    0.00 | 0.0286 |      - |     496 B |        1.00 |
| GetAsync_CacheMiss            | ShortRun   | 3              | 1           |  1.120 μs | 0.1308 μs | 0.0072 μs |  0.80 |    0.00 | 0.0362 |      - |     616 B |        1.24 |
| SetAsync                      | ShortRun   | 3              | 1           |  4.108 μs | 1.5530 μs | 0.0851 μs |  2.94 |    0.05 | 0.0610 | 0.0534 |    1064 B |        2.15 |
| ExistsAsync_True              | ShortRun   | 3              | 1           |  1.149 μs | 0.0338 μs | 0.0019 μs |  0.82 |    0.00 | 0.0362 |      - |     616 B |        1.24 |
| ExistsAsync_False             | ShortRun   | 3              | 1           |  1.109 μs | 0.0675 μs | 0.0037 μs |  0.79 |    0.00 | 0.0362 |      - |     616 B |        1.24 |
| GetOrSetAsync_CacheHit        | ShortRun   | 3              | 1           |  1.402 μs | 0.0042 μs | 0.0002 μs |  1.00 |    0.00 | 0.0324 |      - |     560 B |        1.13 |
| GetOrSetAsync_CacheMiss       | ShortRun   | 3              | 1           |  6.738 μs | 1.2338 μs | 0.0676 μs |  4.82 |    0.04 | 0.1068 | 0.0992 |    1792 B |        3.61 |
| GetOrSetAsync_WithTags        | ShortRun   | 3              | 1           |  9.906 μs | 4.3851 μs | 0.2404 μs |  7.09 |    0.15 | 0.1221 | 0.1068 |    2216 B |        4.47 |
| RemoveAsync                   | ShortRun   | 3              | 1           |  4.302 μs | 1.3502 μs | 0.0740 μs |  3.08 |    0.05 | 0.0610 |      - |    1136 B |        2.29 |
| RemoveByTagAsync              | ShortRun   | 3              | 1           |  9.640 μs | 3.8104 μs | 0.2089 μs |  6.90 |    0.13 | 0.1373 | 0.1221 |    2448 B |        4.94 |
| SetWithSlidingExpirationAsync | ShortRun   | 3              | 1           |  4.032 μs | 1.1366 μs | 0.0623 μs |  2.89 |    0.04 | 0.0610 | 0.0534 |    1072 B |        2.16 |
