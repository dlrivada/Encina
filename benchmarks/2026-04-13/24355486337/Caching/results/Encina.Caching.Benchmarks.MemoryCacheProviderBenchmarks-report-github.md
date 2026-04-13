```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.63GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-NUBXJZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method                        | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error      | StdDev     | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |----------- |--------------- |------------ |------------ |------------:|-----------:|-----------:|------:|--------:|-------:|-------:|----------:|------------:|
| GetAsync_CacheHit             | Job-NUBXJZ | 20             | Default     | 5           |    81.42 ns |   0.314 ns |   0.350 ns |  1.00 |    0.01 | 0.0086 |      - |     144 B |        1.00 |
| GetAsync_CacheMiss            | Job-NUBXJZ | 20             | Default     | 5           |    53.29 ns |   0.089 ns |   0.095 ns |  0.65 |    0.00 |      - |      - |         - |        0.00 |
| SetAsync                      | Job-NUBXJZ | 20             | Default     | 5           | 3,743.02 ns | 229.118 ns | 235.288 ns | 45.97 |    2.81 | 0.0420 | 0.0381 |     712 B |        4.94 |
| ExistsAsync_True              | Job-NUBXJZ | 20             | Default     | 5           |    60.35 ns |   0.056 ns |   0.062 ns |  0.74 |    0.00 |      - |      - |         - |        0.00 |
| ExistsAsync_False             | Job-NUBXJZ | 20             | Default     | 5           |    52.46 ns |   0.032 ns |   0.037 ns |  0.64 |    0.00 |      - |      - |         - |        0.00 |
| GetOrSetAsync_CacheHit        | Job-NUBXJZ | 20             | Default     | 5           |   128.68 ns |   3.600 ns |   4.146 ns |  1.58 |    0.05 | 0.0167 |      - |     280 B |        1.94 |
| GetOrSetAsync_CacheMiss       | Job-NUBXJZ | 20             | Default     | 5           | 5,354.73 ns | 442.055 ns | 472.994 ns | 65.77 |    5.66 | 0.0610 | 0.0572 |    1080 B |        7.50 |
| RemoveAsync                   | Job-NUBXJZ | 20             | Default     | 5           | 2,769.71 ns |  15.325 ns |  17.648 ns | 34.02 |    0.25 | 0.0458 |      - |     784 B |        5.44 |
| SetWithSlidingExpirationAsync | Job-NUBXJZ | 20             | Default     | 5           | 4,177.69 ns | 519.013 ns | 576.881 ns | 51.31 |    6.91 | 0.0420 | 0.0381 |     720 B |        5.00 |
|                               |            |                |             |             |             |            |            |       |         |        |        |           |             |
| GetAsync_CacheHit             | MediumRun  | 15             | 2           | 10          |    82.58 ns |   0.577 ns |   0.864 ns |  1.00 |    0.01 | 0.0086 |      - |     144 B |        1.00 |
| GetAsync_CacheMiss            | MediumRun  | 15             | 2           | 10          |    53.22 ns |   0.020 ns |   0.028 ns |  0.64 |    0.01 |      - |      - |         - |        0.00 |
| SetAsync                      | MediumRun  | 15             | 2           | 10          | 3,842.82 ns | 161.202 ns | 231.190 ns | 46.54 |    2.79 | 0.0420 | 0.0381 |     712 B |        4.94 |
| ExistsAsync_True              | MediumRun  | 15             | 2           | 10          |    60.31 ns |   0.023 ns |   0.030 ns |  0.73 |    0.01 |      - |      - |         - |        0.00 |
| ExistsAsync_False             | MediumRun  | 15             | 2           | 10          |    52.43 ns |   0.029 ns |   0.041 ns |  0.63 |    0.01 |      - |      - |         - |        0.00 |
| GetOrSetAsync_CacheHit        | MediumRun  | 15             | 2           | 10          |   133.60 ns |   1.152 ns |   1.688 ns |  1.62 |    0.03 | 0.0167 |      - |     280 B |        1.94 |
| GetOrSetAsync_CacheMiss       | MediumRun  | 15             | 2           | 10          | 5,442.99 ns | 296.720 ns | 425.547 ns | 65.92 |    5.11 | 0.0610 | 0.0534 |    1080 B |        7.50 |
| RemoveAsync                   | MediumRun  | 15             | 2           | 10          | 2,774.60 ns |  18.196 ns |  27.236 ns | 33.60 |    0.47 | 0.0458 |      - |     784 B |        5.44 |
| SetWithSlidingExpirationAsync | MediumRun  | 15             | 2           | 10          | 4,228.92 ns | 491.245 ns | 720.060 ns | 51.21 |    8.59 | 0.0420 | 0.0381 |     720 B |        5.00 |
