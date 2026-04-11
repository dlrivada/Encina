```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.79GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4
  MediumRun  : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4


```
| Method                                | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|-------------------------------------- |----------- |--------------- |------------ |------------ |------------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| &#39;Key generation (simple query)&#39;       | Job-YFEFPZ | 10             | Default     | 3           | 14,824.2 ns | 496.28 ns | 328.26 ns |  1.00 |    0.03 | 0.2899 | 0.1373 |    8600 B |        1.00 |
| &#39;Key generation (complex JOIN query)&#39; | Job-YFEFPZ | 10             | Default     | 3           | 24,345.4 ns | 487.26 ns | 322.30 ns |  1.64 |    0.04 | 0.5798 | 0.2747 |   16360 B |        1.90 |
| &#39;Key generation with tenant&#39;          | Job-YFEFPZ | 10             | Default     | 3           | 27,603.8 ns | 307.73 ns | 203.54 ns |  1.86 |    0.04 | 0.6104 | 0.2136 |   16496 B |        1.92 |
| &#39;Cache hit (memory)&#39;                  | Job-YFEFPZ | 10             | Default     | 3           | 15,323.3 ns | 367.09 ns | 218.45 ns |  1.03 |    0.03 | 0.3052 | 0.1526 |    8744 B |        1.02 |
| &#39;Cache miss (memory)&#39;                 | Job-YFEFPZ | 10             | Default     | 3           |    598.0 ns |   1.30 ns |   0.78 ns |  0.04 |    0.00 | 0.0048 |      - |     120 B |        0.01 |
| &#39;CachedDataReader read (5 rows)&#39;      | Job-YFEFPZ | 10             | Default     | 3           |  7,452.7 ns |  34.67 ns |  22.93 ns |  0.50 |    0.01 | 0.1068 |      - |    2712 B |        0.32 |
| &#39;CachedDataReader read (1000 rows)&#39;   | Job-YFEFPZ | 10             | Default     | 3           | 30,352.2 ns |  30.53 ns |  20.20 ns |  2.05 |    0.04 | 0.3357 |      - |    8592 B |        1.00 |
|                                       |            |                |             |             |             |           |           |       |         |        |        |           |             |
| &#39;Key generation (simple query)&#39;       | MediumRun  | 15             | 2           | 10          | 14,535.4 ns | 264.20 ns | 395.45 ns |  1.00 |    0.04 | 0.2899 | 0.1373 |    7576 B |        1.00 |
| &#39;Key generation (complex JOIN query)&#39; | MediumRun  | 15             | 2           | 10          | 24,731.4 ns | 217.62 ns | 318.98 ns |  1.70 |    0.05 | 0.5798 | 0.2747 |   14568 B |        1.92 |
| &#39;Key generation with tenant&#39;          | MediumRun  | 15             | 2           | 10          | 27,722.0 ns | 268.05 ns | 401.21 ns |  1.91 |    0.06 | 0.6104 | 0.2136 |   15472 B |        2.04 |
| &#39;Cache hit (memory)&#39;                  | MediumRun  | 15             | 2           | 10          | 15,372.5 ns | 188.34 ns | 276.06 ns |  1.06 |    0.03 | 0.3052 | 0.1526 |    7720 B |        1.02 |
| &#39;Cache miss (memory)&#39;                 | MediumRun  | 15             | 2           | 10          |    597.2 ns |   1.37 ns |   1.96 ns |  0.04 |    0.00 | 0.0048 |      - |     120 B |        0.02 |
| &#39;CachedDataReader read (5 rows)&#39;      | MediumRun  | 15             | 2           | 10          |  7,468.4 ns |  18.69 ns |  26.20 ns |  0.51 |    0.01 | 0.1068 |      - |    2712 B |        0.36 |
| &#39;CachedDataReader read (1000 rows)&#39;   | MediumRun  | 15             | 2           | 10          | 30,368.1 ns |  56.66 ns |  83.06 ns |  2.09 |    0.06 | 0.3052 |      - |    8592 B |        1.13 |
