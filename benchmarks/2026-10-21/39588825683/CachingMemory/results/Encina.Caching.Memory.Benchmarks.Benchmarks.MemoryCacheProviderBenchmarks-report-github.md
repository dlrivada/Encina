```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.26GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  Job-NUBXJZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4


```
| Method                        | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error        | StdDev     | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |----------- |--------------- |------------ |------------ |------------:|-------------:|-----------:|------:|--------:|-------:|-------:|----------:|------------:|
| ExistsAsync_False             | Job-NUBXJZ | 20             | Default     | 5           |    36.00 ns |     0.538 ns |   0.576 ns |  0.32 |    0.01 |      - |      - |         - |        0.00 |
| ExistsAsync_True              | Job-NUBXJZ | 20             | Default     | 5           |    40.23 ns |     0.620 ns |   0.689 ns |  0.36 |    0.01 |      - |      - |         - |        0.00 |
| GetAsync_CacheHit             | Job-NUBXJZ | 20             | Default     | 5           |   111.52 ns |     0.929 ns |   0.994 ns |  1.00 |    0.01 | 0.0086 |      - |     144 B |        1.00 |
| GetAsync_CacheMiss            | Job-NUBXJZ | 20             | Default     | 5           |    85.96 ns |     0.552 ns |   0.636 ns |  0.77 |    0.01 |      - |      - |         - |        0.00 |
| GetOrSetAsync_CacheHit        | Job-NUBXJZ | 20             | Default     | 5           |   133.88 ns |     1.227 ns |   1.313 ns |  1.20 |    0.02 | 0.0167 |      - |     280 B |        1.94 |
| GetOrSetAsync_CacheMiss       | Job-NUBXJZ | 20             | Default     | 5           | 4,405.47 ns |   339.208 ns | 362.949 ns | 39.51 |    3.19 | 0.0610 | 0.0572 |    1080 B |        7.50 |
| RemoveAsync                   | Job-NUBXJZ | 20             | Default     | 5           | 1,860.25 ns |    14.395 ns |  16.000 ns | 16.68 |    0.20 | 0.0458 |      - |     784 B |        5.44 |
| SetWithSlidingExpirationAsync | Job-NUBXJZ | 20             | Default     | 5           | 3,441.38 ns |   396.239 ns | 440.418 ns | 30.86 |    3.86 | 0.0458 | 0.0420 |     788 B |        5.47 |
| SetAsync                      | Job-NUBXJZ | 20             | Default     | 5           | 3,259.66 ns |   227.542 ns | 243.468 ns | 29.23 |    2.14 | 0.0420 | 0.0381 |     712 B |        4.94 |
|                               |            |                |             |             |             |              |            |       |         |        |        |           |             |
| ExistsAsync_False             | ShortRun   | 3              | 1           | 3           |    36.30 ns |     2.799 ns |   0.153 ns |  0.33 |    0.01 |      - |      - |         - |        0.00 |
| ExistsAsync_True              | ShortRun   | 3              | 1           | 3           |    40.73 ns |     4.289 ns |   0.235 ns |  0.37 |    0.01 |      - |      - |         - |        0.00 |
| GetAsync_CacheHit             | ShortRun   | 3              | 1           | 3           |   110.38 ns |    41.931 ns |   2.298 ns |  1.00 |    0.03 | 0.0086 |      - |     144 B |        1.00 |
| GetAsync_CacheMiss            | ShortRun   | 3              | 1           | 3           |    79.27 ns |     6.055 ns |   0.332 ns |  0.72 |    0.01 |      - |      - |         - |        0.00 |
| GetOrSetAsync_CacheHit        | ShortRun   | 3              | 1           | 3           |   134.06 ns |   120.570 ns |   6.609 ns |  1.21 |    0.06 | 0.0167 |      - |     280 B |        1.94 |
| GetOrSetAsync_CacheMiss       | ShortRun   | 3              | 1           | 3           | 4,094.06 ns | 2,149.916 ns | 117.844 ns | 37.10 |    1.14 | 0.0610 | 0.0572 |    1080 B |        7.50 |
| RemoveAsync                   | ShortRun   | 3              | 1           | 3           | 1,850.00 ns |   260.586 ns |  14.284 ns | 16.76 |    0.32 | 0.0458 |      - |     784 B |        5.44 |
| SetWithSlidingExpirationAsync | ShortRun   | 3              | 1           | 3           | 3,090.46 ns | 2,705.021 ns | 148.271 ns | 28.01 |    1.27 | 0.0420 | 0.0381 |     720 B |        5.00 |
| SetAsync                      | ShortRun   | 3              | 1           | 3           | 2,812.07 ns | 4,438.712 ns | 243.301 ns | 25.48 |    1.96 | 0.0420 | 0.0381 |     712 B |        4.94 |
