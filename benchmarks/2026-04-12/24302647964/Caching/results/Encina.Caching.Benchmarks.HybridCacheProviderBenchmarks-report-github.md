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
| ExistsAsync_True              | Job-NUBXJZ | 20             | Default     | 5           |  1.142 μs | 0.0026 μs | 0.0028 μs |  1.141 μs |  0.85 |    0.00 | 0.0362 |      - |     616 B |        1.24 |
| ExistsAsync_False             | Job-NUBXJZ | 20             | Default     | 5           |  1.109 μs | 0.0028 μs | 0.0031 μs |  1.109 μs |  0.82 |    0.00 | 0.0362 |      - |     616 B |        1.24 |
| GetOrSetAsync_CacheHit        | Job-NUBXJZ | 20             | Default     | 5           |  1.378 μs | 0.0025 μs | 0.0028 μs |  1.378 μs |  1.02 |    0.00 | 0.0324 |      - |     560 B |        1.13 |
| GetOrSetAsync_CacheMiss       | Job-NUBXJZ | 20             | Default     | 5           |  6.746 μs | 0.1096 μs | 0.1173 μs |  6.744 μs |  5.01 |    0.09 | 0.1068 | 0.0992 |    1792 B |        3.61 |
| GetOrSetAsync_WithTags        | Job-NUBXJZ | 20             | Default     | 5           | 11.305 μs | 0.8715 μs | 0.9325 μs | 10.986 μs |  8.40 |    0.67 | 0.1221 | 0.1068 |    2216 B |        4.47 |
| RemoveAsync                   | Job-NUBXJZ | 20             | Default     | 5           |  4.276 μs | 0.0234 μs | 0.0269 μs |  4.272 μs |  3.18 |    0.02 | 0.0610 |      - |    1136 B |        2.29 |
| RemoveByTagAsync              | Job-NUBXJZ | 20             | Default     | 5           | 10.194 μs | 0.3099 μs | 0.3183 μs | 10.260 μs |  7.57 |    0.23 | 0.1373 | 0.1221 |    2448 B |        4.94 |
| SetWithSlidingExpirationAsync | Job-NUBXJZ | 20             | Default     | 5           |  4.256 μs | 0.1199 μs | 0.1178 μs |  4.265 μs |  3.16 |    0.09 | 0.0610 | 0.0572 |    1072 B |        2.16 |
| GetAsync_CacheHit             | Job-NUBXJZ | 20             | Default     | 5           |  1.346 μs | 0.0025 μs | 0.0029 μs |  1.346 μs |  1.00 |    0.00 | 0.0286 |      - |     496 B |        1.00 |
| GetAsync_CacheMiss            | Job-NUBXJZ | 20             | Default     | 5           |  1.098 μs | 0.0018 μs | 0.0020 μs |  1.097 μs |  0.82 |    0.00 | 0.0362 |      - |     616 B |        1.24 |
| SetAsync                      | Job-NUBXJZ | 20             | Default     | 5           |  4.397 μs | 0.1286 μs | 0.1376 μs |  4.417 μs |  3.27 |    0.10 | 0.0610 | 0.0572 |    1064 B |        2.15 |
|                               |            |                |             |             |           |           |           |           |       |         |        |        |           |             |
| ExistsAsync_True              | MediumRun  | 15             | 2           | 10          |  1.134 μs | 0.0092 μs | 0.0129 μs |  1.142 μs |  0.82 |    0.01 | 0.0362 |      - |     616 B |        1.24 |
| ExistsAsync_False             | MediumRun  | 15             | 2           | 10          |  1.111 μs | 0.0069 μs | 0.0101 μs |  1.117 μs |  0.80 |    0.01 | 0.0362 |      - |     616 B |        1.24 |
| GetOrSetAsync_CacheHit        | MediumRun  | 15             | 2           | 10          |  1.385 μs | 0.0019 μs | 0.0026 μs |  1.384 μs |  1.00 |    0.00 | 0.0324 |      - |     560 B |        1.13 |
| GetOrSetAsync_CacheMiss       | MediumRun  | 15             | 2           | 10          |  6.777 μs | 0.0908 μs | 0.1273 μs |  6.748 μs |  4.88 |    0.09 | 0.1068 | 0.0992 |    1792 B |        3.61 |
| GetOrSetAsync_WithTags        | MediumRun  | 15             | 2           | 10          | 11.360 μs | 0.4018 μs | 0.5763 μs | 11.201 μs |  8.18 |    0.41 | 0.1221 | 0.1068 |    2216 B |        4.47 |
| RemoveAsync                   | MediumRun  | 15             | 2           | 10          |  4.291 μs | 0.0661 μs | 0.0990 μs |  4.308 μs |  3.09 |    0.07 | 0.0610 |      - |    1136 B |        2.29 |
| RemoveByTagAsync              | MediumRun  | 15             | 2           | 10          | 10.250 μs | 0.3269 μs | 0.4689 μs | 10.244 μs |  7.38 |    0.33 | 0.1373 | 0.1221 |    2448 B |        4.94 |
| SetWithSlidingExpirationAsync | MediumRun  | 15             | 2           | 10          |  4.261 μs | 0.1186 μs | 0.1543 μs |  4.278 μs |  3.07 |    0.11 | 0.0610 | 0.0572 |    1072 B |        2.16 |
| GetAsync_CacheHit             | MediumRun  | 15             | 2           | 10          |  1.389 μs | 0.0024 μs | 0.0034 μs |  1.388 μs |  1.00 |    0.00 | 0.0286 |      - |     496 B |        1.00 |
| GetAsync_CacheMiss            | MediumRun  | 15             | 2           | 10          |  1.082 μs | 0.0040 μs | 0.0060 μs |  1.080 μs |  0.78 |    0.00 | 0.0362 |      - |     616 B |        1.24 |
| SetAsync                      | MediumRun  | 15             | 2           | 10          |  4.348 μs | 0.1329 μs | 0.1906 μs |  4.346 μs |  3.13 |    0.13 | 0.0610 | 0.0572 |    1064 B |        2.15 |
