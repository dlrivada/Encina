```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-NUBXJZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  MediumRun  : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4


```
| Method                        | Job        | IterationCount | LaunchCount | WarmupCount | Mean       | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |----------- |--------------- |------------ |------------ |-----------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| ExistsAsync_False             | Job-NUBXJZ | 20             | Default     | 5           |   891.2 ns |   2.62 ns |   3.01 ns |  0.82 |    0.01 | 0.0067 |      - |     616 B |        1.24 |
| ExistsAsync_True              | Job-NUBXJZ | 20             | Default     | 5           |   881.5 ns |   4.67 ns |   5.37 ns |  0.81 |    0.01 | 0.0067 |      - |     616 B |        1.24 |
| GetAsync_CacheHit             | Job-NUBXJZ | 20             | Default     | 5           | 1,086.0 ns |   6.15 ns |   7.08 ns |  1.00 |    0.01 | 0.0057 |      - |     496 B |        1.00 |
| GetAsync_CacheMiss            | Job-NUBXJZ | 20             | Default     | 5           |   859.9 ns |   6.89 ns |   7.94 ns |  0.79 |    0.01 | 0.0067 |      - |     616 B |        1.24 |
| GetOrSetAsync_CacheHit        | Job-NUBXJZ | 20             | Default     | 5           | 1,056.3 ns |   2.38 ns |   2.55 ns |  0.97 |    0.01 | 0.0057 |      - |     560 B |        1.13 |
| GetOrSetAsync_CacheMiss       | Job-NUBXJZ | 20             | Default     | 5           | 4,657.2 ns | 343.59 ns | 367.64 ns |  4.29 |    0.33 | 0.0153 | 0.0076 |    1792 B |        3.61 |
| GetOrSetAsync_WithTags        | Job-NUBXJZ | 20             | Default     | 5           | 8,167.3 ns | 540.22 ns | 578.03 ns |  7.52 |    0.52 | 0.0229 | 0.0153 |    2216 B |        4.47 |
| RemoveAsync                   | Job-NUBXJZ | 20             | Default     | 5           | 3,038.7 ns | 117.48 ns | 135.29 ns |  2.80 |    0.12 | 0.0114 |      - |    1136 B |        2.29 |
| RemoveByTagAsync              | Job-NUBXJZ | 20             | Default     | 5           | 6,352.5 ns | 343.95 ns | 368.03 ns |  5.85 |    0.33 | 0.0153 |      - |    2448 B |        4.94 |
| SetAsync                      | Job-NUBXJZ | 20             | Default     | 5           | 3,190.7 ns | 186.86 ns | 183.53 ns |  2.94 |    0.16 | 0.0114 | 0.0076 |    1064 B |        2.15 |
| SetWithSlidingExpirationAsync | Job-NUBXJZ | 20             | Default     | 5           | 3,488.7 ns | 322.53 ns | 358.49 ns |  3.21 |    0.32 | 0.0114 | 0.0076 |    1098 B |        2.21 |
|                               |            |                |             |             |            |           |           |       |         |        |        |           |             |
| ExistsAsync_False             | MediumRun  | 15             | 2           | 10          |   882.0 ns |   5.20 ns |   7.12 ns |  0.82 |    0.02 | 0.0067 |      - |     616 B |        1.24 |
| ExistsAsync_True              | MediumRun  | 15             | 2           | 10          |   890.2 ns |   4.55 ns |   6.81 ns |  0.82 |    0.02 | 0.0067 |      - |     616 B |        1.24 |
| GetAsync_CacheHit             | MediumRun  | 15             | 2           | 10          | 1,081.3 ns |  13.81 ns |  20.24 ns |  1.00 |    0.03 | 0.0057 |      - |     496 B |        1.00 |
| GetAsync_CacheMiss            | MediumRun  | 15             | 2           | 10          |   824.6 ns |   9.39 ns |  13.77 ns |  0.76 |    0.02 | 0.0067 |      - |     616 B |        1.24 |
| GetOrSetAsync_CacheHit        | MediumRun  | 15             | 2           | 10          | 1,041.6 ns |   1.96 ns |   2.74 ns |  0.96 |    0.02 | 0.0057 |      - |     560 B |        1.13 |
| GetOrSetAsync_CacheMiss       | MediumRun  | 15             | 2           | 10          | 4,675.0 ns | 151.09 ns | 211.81 ns |  4.32 |    0.21 | 0.0153 | 0.0076 |    1792 B |        3.61 |
| GetOrSetAsync_WithTags        | MediumRun  | 15             | 2           | 10          | 8,709.1 ns | 460.63 ns | 660.62 ns |  8.06 |    0.62 | 0.0229 | 0.0153 |    2216 B |        4.47 |
| RemoveAsync                   | MediumRun  | 15             | 2           | 10          | 3,162.0 ns |  82.68 ns | 123.76 ns |  2.93 |    0.12 | 0.0114 |      - |    1136 B |        2.29 |
| RemoveByTagAsync              | MediumRun  | 15             | 2           | 10          | 6,217.1 ns | 219.53 ns | 314.84 ns |  5.75 |    0.31 | 0.0153 |      - |    2448 B |        4.94 |
| SetAsync                      | MediumRun  | 15             | 2           | 10          | 3,321.1 ns | 111.37 ns | 152.45 ns |  3.07 |    0.15 | 0.0114 | 0.0076 |    1064 B |        2.15 |
| SetWithSlidingExpirationAsync | MediumRun  | 15             | 2           | 10          | 3,800.6 ns | 293.45 ns | 430.13 ns |  3.52 |    0.40 | 0.0114 | 0.0076 |    1231 B |        2.48 |
