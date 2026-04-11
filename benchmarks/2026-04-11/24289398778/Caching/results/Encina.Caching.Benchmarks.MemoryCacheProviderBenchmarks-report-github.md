```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-NUBXJZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method                        | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error      | StdDev     | Median      | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |----------- |--------------- |------------ |------------ |------------:|-----------:|-----------:|------------:|------:|--------:|-------:|-------:|----------:|------------:|
| GetAsync_CacheHit             | Job-NUBXJZ | 20             | Default     | 5           |    79.91 ns |   0.219 ns |   0.243 ns |    79.93 ns |  1.00 |    0.00 | 0.0086 |      - |     144 B |        1.00 |
| GetAsync_CacheMiss            | Job-NUBXJZ | 20             | Default     | 5           |    56.30 ns |   0.047 ns |   0.052 ns |    56.29 ns |  0.70 |    0.00 |      - |      - |         - |        0.00 |
| SetAsync                      | Job-NUBXJZ | 20             | Default     | 5           | 4,458.21 ns | 385.756 ns | 412.755 ns | 4,322.42 ns | 55.79 |    5.03 | 0.0420 | 0.0381 |     712 B |        4.94 |
| ExistsAsync_True              | Job-NUBXJZ | 20             | Default     | 5           |    64.51 ns |   0.041 ns |   0.044 ns |    64.50 ns |  0.81 |    0.00 |      - |      - |         - |        0.00 |
| ExistsAsync_False             | Job-NUBXJZ | 20             | Default     | 5           |    56.13 ns |   0.062 ns |   0.066 ns |    56.13 ns |  0.70 |    0.00 |      - |      - |         - |        0.00 |
| GetOrSetAsync_CacheHit        | Job-NUBXJZ | 20             | Default     | 5           |   119.01 ns |   0.397 ns |   0.457 ns |   119.15 ns |  1.49 |    0.01 | 0.0167 |      - |     280 B |        1.94 |
| GetOrSetAsync_CacheMiss       | Job-NUBXJZ | 20             | Default     | 5           | 5,573.85 ns | 574.069 ns | 614.248 ns | 5,354.18 ns | 69.75 |    7.48 | 0.0610 | 0.0534 |    1080 B |        7.50 |
| RemoveAsync                   | Job-NUBXJZ | 20             | Default     | 5           | 2,955.78 ns |  15.318 ns |  15.730 ns | 2,952.66 ns | 36.99 |    0.22 | 0.0458 |      - |     784 B |        5.44 |
| SetWithSlidingExpirationAsync | Job-NUBXJZ | 20             | Default     | 5           | 4,703.30 ns | 596.791 ns | 663.332 ns | 4,840.41 ns | 58.86 |    8.09 | 0.0420 | 0.0381 |     720 B |        5.00 |
|                               |            |                |             |             |             |            |            |             |       |         |        |        |           |             |
| GetAsync_CacheHit             | MediumRun  | 15             | 2           | 10          |    81.78 ns |   1.676 ns |   2.403 ns |    83.22 ns |  1.00 |    0.04 | 0.0086 |      - |     144 B |        1.00 |
| GetAsync_CacheMiss            | MediumRun  | 15             | 2           | 10          |    59.21 ns |   1.608 ns |   2.254 ns |    61.07 ns |  0.72 |    0.03 |      - |      - |         - |        0.00 |
| SetAsync                      | MediumRun  | 15             | 2           | 10          | 4,289.44 ns | 361.072 ns | 506.173 ns | 4,189.61 ns | 52.50 |    6.27 | 0.0381 | 0.0305 |     712 B |        4.94 |
| ExistsAsync_True              | MediumRun  | 15             | 2           | 10          |    64.74 ns |   0.211 ns |   0.296 ns |    64.61 ns |  0.79 |    0.02 |      - |      - |         - |        0.00 |
| ExistsAsync_False             | MediumRun  | 15             | 2           | 10          |    56.42 ns |   0.286 ns |   0.428 ns |    56.37 ns |  0.69 |    0.02 |      - |      - |         - |        0.00 |
| GetOrSetAsync_CacheHit        | MediumRun  | 15             | 2           | 10          |   115.75 ns |   0.259 ns |   0.380 ns |   115.82 ns |  1.42 |    0.04 | 0.0167 |      - |     280 B |        1.94 |
| GetOrSetAsync_CacheMiss       | MediumRun  | 15             | 2           | 10          | 5,789.87 ns | 374.978 ns | 525.667 ns | 5,556.20 ns | 70.86 |    6.64 | 0.0610 | 0.0534 |    1080 B |        7.50 |
| RemoveAsync                   | MediumRun  | 15             | 2           | 10          | 2,936.92 ns |  15.712 ns |  22.534 ns | 2,934.43 ns | 35.94 |    1.07 | 0.0458 |      - |     784 B |        5.44 |
| SetWithSlidingExpirationAsync | MediumRun  | 15             | 2           | 10          | 4,724.50 ns | 355.279 ns | 531.765 ns | 4,517.89 ns | 57.82 |    6.62 | 0.0420 | 0.0381 |     720 B |        5.00 |
