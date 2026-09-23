```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-NUBXJZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4


```
| Method                        | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error        | StdDev     | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |----------- |--------------- |------------ |------------ |------------:|-------------:|-----------:|------:|--------:|-------:|-------:|----------:|------------:|
| ExistsAsync_False             | Job-NUBXJZ | 20             | Default     | 5           |    35.14 ns |     0.293 ns |   0.326 ns |  0.70 |    0.02 |      - |      - |         - |        0.00 |
| ExistsAsync_True              | Job-NUBXJZ | 20             | Default     | 5           |    39.64 ns |     0.359 ns |   0.399 ns |  0.79 |    0.02 |      - |      - |         - |        0.00 |
| GetAsync_CacheHit             | Job-NUBXJZ | 20             | Default     | 5           |    50.09 ns |     1.220 ns |   1.405 ns |  1.00 |    0.04 | 0.0086 |      - |     144 B |        1.00 |
| GetAsync_CacheMiss            | Job-NUBXJZ | 20             | Default     | 5           |    36.64 ns |     0.432 ns |   0.497 ns |  0.73 |    0.02 |      - |      - |         - |        0.00 |
| GetOrSetAsync_CacheHit        | Job-NUBXJZ | 20             | Default     | 5           |    69.19 ns |     0.698 ns |   0.776 ns |  1.38 |    0.04 | 0.0167 |      - |     280 B |        1.94 |
| GetOrSetAsync_CacheMiss       | Job-NUBXJZ | 20             | Default     | 5           | 3,949.69 ns |   266.910 ns | 285.590 ns | 78.92 |    5.96 | 0.0610 | 0.0534 |    1080 B |        7.50 |
| RemoveAsync                   | Job-NUBXJZ | 20             | Default     | 5           | 1,639.75 ns |     7.871 ns |   8.422 ns | 32.76 |    0.91 | 0.0458 |      - |     784 B |        5.44 |
| SetWithSlidingExpirationAsync | Job-NUBXJZ | 20             | Default     | 5           | 3,327.86 ns |   291.857 ns | 324.399 ns | 66.49 |    6.57 | 0.0496 | 0.0458 |     842 B |        5.85 |
| SetAsync                      | Job-NUBXJZ | 20             | Default     | 5           | 3,249.77 ns |   170.761 ns | 175.359 ns | 64.93 |    3.84 | 0.0420 | 0.0381 |     712 B |        4.94 |
|                               |            |                |             |             |             |              |            |       |         |        |        |           |             |
| ExistsAsync_False             | ShortRun   | 3              | 1           | 3           |    34.93 ns |     9.863 ns |   0.541 ns |  0.70 |    0.01 |      - |      - |         - |        0.00 |
| ExistsAsync_True              | ShortRun   | 3              | 1           | 3           |    40.41 ns |    13.914 ns |   0.763 ns |  0.81 |    0.01 |      - |      - |         - |        0.00 |
| GetAsync_CacheHit             | ShortRun   | 3              | 1           | 3           |    49.96 ns |     4.289 ns |   0.235 ns |  1.00 |    0.01 | 0.0086 |      - |     144 B |        1.00 |
| GetAsync_CacheMiss            | ShortRun   | 3              | 1           | 3           |    36.73 ns |    16.696 ns |   0.915 ns |  0.74 |    0.02 |      - |      - |         - |        0.00 |
| GetOrSetAsync_CacheHit        | ShortRun   | 3              | 1           | 3           |    68.87 ns |    10.613 ns |   0.582 ns |  1.38 |    0.01 | 0.0167 |      - |     280 B |        1.94 |
| GetOrSetAsync_CacheMiss       | ShortRun   | 3              | 1           | 3           | 3,735.80 ns | 1,227.873 ns |  67.304 ns | 74.77 |    1.21 | 0.0610 | 0.0572 |    1080 B |        7.50 |
| RemoveAsync                   | ShortRun   | 3              | 1           | 3           | 1,615.99 ns |   377.644 ns |  20.700 ns | 32.34 |    0.38 | 0.0458 |      - |     784 B |        5.44 |
| SetWithSlidingExpirationAsync | ShortRun   | 3              | 1           | 3           | 2,655.67 ns | 2,975.313 ns | 163.087 ns | 53.15 |    2.84 | 0.0420 | 0.0381 |     720 B |        5.00 |
| SetAsync                      | ShortRun   | 3              | 1           | 3           | 2,987.59 ns |   872.100 ns |  47.803 ns | 59.80 |    0.86 | 0.0420 | 0.0381 |     712 B |        4.94 |
