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
| ExistsAsync_False             | Job-NUBXJZ | 20             | Default     | 5           |  1.139 μs | 0.0014 μs | 0.0014 μs |  0.81 |    0.00 | 0.0362 |      - |     616 B |        1.24 |
| ExistsAsync_True              | Job-NUBXJZ | 20             | Default     | 5           |  1.186 μs | 0.0017 μs | 0.0018 μs |  0.85 |    0.00 | 0.0362 |      - |     616 B |        1.24 |
| GetAsync_CacheHit             | Job-NUBXJZ | 20             | Default     | 5           |  1.399 μs | 0.0020 μs | 0.0022 μs |  1.00 |    0.00 | 0.0286 |      - |     496 B |        1.00 |
| GetAsync_CacheMiss            | Job-NUBXJZ | 20             | Default     | 5           |  1.107 μs | 0.0014 μs | 0.0016 μs |  0.79 |    0.00 | 0.0362 |      - |     616 B |        1.24 |
| GetOrSetAsync_CacheHit        | Job-NUBXJZ | 20             | Default     | 5           |  1.406 μs | 0.0016 μs | 0.0018 μs |  1.00 |    0.00 | 0.0324 |      - |     560 B |        1.13 |
| GetOrSetAsync_CacheMiss       | Job-NUBXJZ | 20             | Default     | 5           |  7.277 μs | 0.1153 μs | 0.1184 μs |  5.20 |    0.08 | 0.1068 | 0.0992 |    1792 B |        3.61 |
| GetOrSetAsync_WithTags        | Job-NUBXJZ | 20             | Default     | 5           | 11.510 μs | 0.6806 μs | 0.7283 μs |  8.23 |    0.51 | 0.1221 | 0.1068 |    2216 B |        4.47 |
| RemoveAsync                   | Job-NUBXJZ | 20             | Default     | 5           |  4.291 μs | 0.1114 μs | 0.1239 μs |  3.07 |    0.09 | 0.0610 |      - |    1136 B |        2.29 |
| RemoveByTagAsync              | Job-NUBXJZ | 20             | Default     | 5           | 10.744 μs | 0.3388 μs | 0.3625 μs |  7.68 |    0.25 | 0.1373 | 0.1221 |    2448 B |        4.94 |
| SetAsync                      | Job-NUBXJZ | 20             | Default     | 5           |  4.503 μs | 0.1563 μs | 0.1673 μs |  3.22 |    0.12 | 0.0610 | 0.0534 |    1064 B |        2.15 |
| SetWithSlidingExpirationAsync | Job-NUBXJZ | 20             | Default     | 5           |  4.486 μs | 0.1536 μs | 0.1509 μs |  3.21 |    0.10 | 0.0763 | 0.0687 |    1321 B |        2.66 |
|                               |            |                |             |             |           |           |           |       |         |        |        |           |             |
| ExistsAsync_False             | ShortRun   | 3              | 1           | 3           |  1.142 μs | 0.0521 μs | 0.0029 μs |  0.81 |    0.00 | 0.0362 |      - |     616 B |        1.24 |
| ExistsAsync_True              | ShortRun   | 3              | 1           | 3           |  1.163 μs | 0.0423 μs | 0.0023 μs |  0.82 |    0.00 | 0.0362 |      - |     616 B |        1.24 |
| GetAsync_CacheHit             | ShortRun   | 3              | 1           | 3           |  1.416 μs | 0.0606 μs | 0.0033 μs |  1.00 |    0.00 | 0.0286 |      - |     496 B |        1.00 |
| GetAsync_CacheMiss            | ShortRun   | 3              | 1           | 3           |  1.109 μs | 0.0485 μs | 0.0027 μs |  0.78 |    0.00 | 0.0362 |      - |     616 B |        1.24 |
| GetOrSetAsync_CacheHit        | ShortRun   | 3              | 1           | 3           |  1.426 μs | 0.0222 μs | 0.0012 μs |  1.01 |    0.00 | 0.0324 |      - |     560 B |        1.13 |
| GetOrSetAsync_CacheMiss       | ShortRun   | 3              | 1           | 3           |  6.807 μs | 1.3724 μs | 0.0752 μs |  4.81 |    0.05 | 0.1068 | 0.0992 |    1792 B |        3.61 |
| GetOrSetAsync_WithTags        | ShortRun   | 3              | 1           | 3           | 10.393 μs | 4.6148 μs | 0.2530 μs |  7.34 |    0.16 | 0.1831 | 0.1678 |    3419 B |        6.89 |
| RemoveAsync                   | ShortRun   | 3              | 1           | 3           |  4.315 μs | 0.1355 μs | 0.0074 μs |  3.05 |    0.01 | 0.0610 |      - |    1136 B |        2.29 |
| RemoveByTagAsync              | ShortRun   | 3              | 1           | 3           | 10.275 μs | 3.4358 μs | 0.1883 μs |  7.25 |    0.12 | 0.1373 | 0.1221 |    2448 B |        4.94 |
| SetAsync                      | ShortRun   | 3              | 1           | 3           |  4.197 μs | 0.2829 μs | 0.0155 μs |  2.96 |    0.01 | 0.0610 | 0.0534 |    1064 B |        2.15 |
| SetWithSlidingExpirationAsync | ShortRun   | 3              | 1           | 3           |  4.249 μs | 0.9226 μs | 0.0506 μs |  3.00 |    0.03 | 0.0610 | 0.0534 |    1072 B |        2.16 |
