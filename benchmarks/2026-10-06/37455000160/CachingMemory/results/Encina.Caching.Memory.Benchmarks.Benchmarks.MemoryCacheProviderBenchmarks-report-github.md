```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.48GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-NUBXJZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4


```
| Method                        | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error        | StdDev     | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |----------- |--------------- |------------ |------------ |------------:|-------------:|-----------:|------:|--------:|-------:|-------:|----------:|------------:|
| ExistsAsync_False             | Job-NUBXJZ | 20             | Default     | 5           |    33.95 ns |     0.040 ns |   0.044 ns |  0.71 |    0.01 |      - |      - |         - |        0.00 |
| ExistsAsync_True              | Job-NUBXJZ | 20             | Default     | 5           |    38.26 ns |     0.057 ns |   0.056 ns |  0.80 |    0.01 |      - |      - |         - |        0.00 |
| GetAsync_CacheHit             | Job-NUBXJZ | 20             | Default     | 5           |    47.75 ns |     0.596 ns |   0.687 ns |  1.00 |    0.02 | 0.0086 |      - |     144 B |        1.00 |
| GetAsync_CacheMiss            | Job-NUBXJZ | 20             | Default     | 5           |    35.03 ns |     0.040 ns |   0.045 ns |  0.73 |    0.01 |      - |      - |         - |        0.00 |
| GetOrSetAsync_CacheHit        | Job-NUBXJZ | 20             | Default     | 5           |    64.98 ns |     1.080 ns |   1.244 ns |  1.36 |    0.03 | 0.0167 |      - |     280 B |        1.94 |
| GetOrSetAsync_CacheMiss       | Job-NUBXJZ | 20             | Default     | 5           | 4,090.70 ns |   302.718 ns | 323.905 ns | 85.69 |    6.71 | 0.0610 | 0.0572 |    1080 B |        7.50 |
| RemoveAsync                   | Job-NUBXJZ | 20             | Default     | 5           | 1,538.33 ns |     7.859 ns |   9.050 ns | 32.22 |    0.49 | 0.0458 |      - |     784 B |        5.44 |
| SetWithSlidingExpirationAsync | Job-NUBXJZ | 20             | Default     | 5           | 3,076.93 ns |   316.308 ns | 351.576 ns | 64.45 |    7.24 | 0.0496 | 0.0458 |     853 B |        5.92 |
| SetAsync                      | Job-NUBXJZ | 20             | Default     | 5           | 2,948.16 ns |   277.191 ns | 296.591 ns | 61.76 |    6.11 | 0.0420 | 0.0381 |     712 B |        4.94 |
|                               |            |                |             |             |             |              |            |       |         |        |        |           |             |
| ExistsAsync_False             | ShortRun   | 3              | 1           | 3           |    33.91 ns |     1.260 ns |   0.069 ns |  0.72 |    0.02 |      - |      - |         - |        0.00 |
| ExistsAsync_True              | ShortRun   | 3              | 1           | 3           |    38.18 ns |     0.613 ns |   0.034 ns |  0.81 |    0.03 |      - |      - |         - |        0.00 |
| GetAsync_CacheHit             | ShortRun   | 3              | 1           | 3           |    47.25 ns |    31.774 ns |   1.742 ns |  1.00 |    0.04 | 0.0086 |      - |     144 B |        1.00 |
| GetAsync_CacheMiss            | ShortRun   | 3              | 1           | 3           |    35.73 ns |    15.642 ns |   0.857 ns |  0.76 |    0.03 |      - |      - |         - |        0.00 |
| GetOrSetAsync_CacheHit        | ShortRun   | 3              | 1           | 3           |    64.92 ns |    14.055 ns |   0.770 ns |  1.38 |    0.05 | 0.0167 |      - |     280 B |        1.94 |
| GetOrSetAsync_CacheMiss       | ShortRun   | 3              | 1           | 3           | 3,501.29 ns | 2,906.757 ns | 159.329 ns | 74.17 |    3.73 | 0.0610 | 0.0572 |    1080 B |        7.50 |
| RemoveAsync                   | ShortRun   | 3              | 1           | 3           | 1,549.14 ns |   197.992 ns |  10.853 ns | 32.81 |    1.04 | 0.0458 |      - |     784 B |        5.44 |
| SetWithSlidingExpirationAsync | ShortRun   | 3              | 1           | 3           | 2,437.38 ns | 4,318.143 ns | 236.692 ns | 51.63 |    4.63 | 0.0420 | 0.0381 |     720 B |        5.00 |
| SetAsync                      | ShortRun   | 3              | 1           | 3           | 2,634.22 ns | 4,761.847 ns | 261.013 ns | 55.80 |    5.10 | 0.0420 | 0.0381 |     712 B |        4.94 |
