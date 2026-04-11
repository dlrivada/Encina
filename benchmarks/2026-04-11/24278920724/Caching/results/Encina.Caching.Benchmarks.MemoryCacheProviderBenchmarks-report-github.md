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
| GetAsync_CacheHit             | Job-NUBXJZ | 20             | Default     | 5           |    79.76 ns |   0.067 ns |   0.072 ns |    79.75 ns |  1.00 |    0.00 | 0.0086 |      - |     144 B |        1.00 |
| GetAsync_CacheMiss            | Job-NUBXJZ | 20             | Default     | 5           |    57.89 ns |   0.079 ns |   0.088 ns |    57.89 ns |  0.73 |    0.00 |      - |      - |         - |        0.00 |
| SetAsync                      | Job-NUBXJZ | 20             | Default     | 5           | 4,346.62 ns | 408.662 ns | 437.264 ns | 4,190.10 ns | 54.50 |    5.34 | 0.0420 | 0.0381 |     712 B |        4.94 |
| ExistsAsync_True              | Job-NUBXJZ | 20             | Default     | 5           |    64.70 ns |   0.059 ns |   0.068 ns |    64.70 ns |  0.81 |    0.00 |      - |      - |         - |        0.00 |
| ExistsAsync_False             | Job-NUBXJZ | 20             | Default     | 5           |    56.06 ns |   0.048 ns |   0.055 ns |    56.06 ns |  0.70 |    0.00 |      - |      - |         - |        0.00 |
| GetOrSetAsync_CacheHit        | Job-NUBXJZ | 20             | Default     | 5           |   116.49 ns |   0.103 ns |   0.118 ns |   116.47 ns |  1.46 |    0.00 | 0.0167 |      - |     280 B |        1.94 |
| GetOrSetAsync_CacheMiss       | Job-NUBXJZ | 20             | Default     | 5           | 5,484.14 ns | 567.809 ns | 607.550 ns | 5,285.12 ns | 68.76 |    7.41 | 0.0610 | 0.0534 |    1080 B |        7.50 |
| RemoveAsync                   | Job-NUBXJZ | 20             | Default     | 5           | 2,938.59 ns |  34.945 ns |  35.886 ns | 2,925.79 ns | 36.84 |    0.44 | 0.0458 |      - |     784 B |        5.44 |
| SetWithSlidingExpirationAsync | Job-NUBXJZ | 20             | Default     | 5           | 4,629.36 ns | 590.676 ns | 656.535 ns | 4,743.23 ns | 58.04 |    8.02 | 0.0420 | 0.0381 |     720 B |        5.00 |
|                               |            |                |             |             |             |            |            |             |       |         |        |        |           |             |
| GetAsync_CacheHit             | MediumRun  | 15             | 2           | 10          |    79.87 ns |   0.442 ns |   0.635 ns |    79.74 ns |  1.00 |    0.01 | 0.0086 |      - |     144 B |        1.00 |
| GetAsync_CacheMiss            | MediumRun  | 15             | 2           | 10          |    56.75 ns |   0.041 ns |   0.059 ns |    56.75 ns |  0.71 |    0.01 |      - |      - |         - |        0.00 |
| SetAsync                      | MediumRun  | 15             | 2           | 10          | 4,262.03 ns | 178.585 ns | 250.351 ns | 4,189.50 ns | 53.37 |    3.11 | 0.0420 | 0.0381 |     712 B |        4.94 |
| ExistsAsync_True              | MediumRun  | 15             | 2           | 10          |    64.97 ns |   0.293 ns |   0.430 ns |    64.70 ns |  0.81 |    0.01 |      - |      - |         - |        0.00 |
| ExistsAsync_False             | MediumRun  | 15             | 2           | 10          |    56.07 ns |   0.115 ns |   0.168 ns |    56.06 ns |  0.70 |    0.01 |      - |      - |         - |        0.00 |
| GetOrSetAsync_CacheHit        | MediumRun  | 15             | 2           | 10          |   114.57 ns |   0.289 ns |   0.415 ns |   114.75 ns |  1.43 |    0.01 | 0.0167 |      - |     280 B |        1.94 |
| GetOrSetAsync_CacheMiss       | MediumRun  | 15             | 2           | 10          | 5,852.40 ns | 342.820 ns | 491.663 ns | 5,757.51 ns | 73.28 |    6.08 | 0.0610 | 0.0534 |    1080 B |        7.50 |
| RemoveAsync                   | MediumRun  | 15             | 2           | 10          | 2,957.10 ns |  24.045 ns |  35.245 ns | 2,959.07 ns | 37.03 |    0.52 | 0.0458 |      - |     784 B |        5.44 |
| SetWithSlidingExpirationAsync | MediumRun  | 15             | 2           | 10          | 4,662.20 ns | 321.974 ns | 481.916 ns | 4,601.50 ns | 58.38 |    5.95 | 0.0534 | 0.0496 |     909 B |        6.31 |
