```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method                                | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error     | StdDev      | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|-------------------------------------- |----------- |--------------- |------------ |------------ |------------:|----------:|------------:|------:|--------:|-------:|-------:|----------:|------------:|
| &#39;Key generation (simple query)&#39;       | Job-YFEFPZ | 10             | Default     | 3           | 13,758.0 ns | 378.32 ns |   250.24 ns |  1.00 |    0.02 | 0.4425 | 0.1526 |    8600 B |        1.00 |
| &#39;Key generation (complex JOIN query)&#39; | Job-YFEFPZ | 10             | Default     | 3           | 23,623.0 ns | 334.33 ns |   198.96 ns |  1.72 |    0.03 | 0.8545 | 0.2747 |   16360 B |        1.90 |
| &#39;Key generation with tenant&#39;          | Job-YFEFPZ | 10             | Default     | 3           | 26,397.8 ns | 103.69 ns |    54.23 ns |  1.92 |    0.03 | 0.9155 | 0.2441 |   16497 B |        1.92 |
| &#39;Cache hit (memory)&#39;                  | Job-YFEFPZ | 10             | Default     | 3           | 14,749.7 ns | 354.80 ns |   234.68 ns |  1.07 |    0.02 | 0.4578 | 0.1526 |    8744 B |        1.02 |
| &#39;Cache miss (memory)&#39;                 | Job-YFEFPZ | 10             | Default     | 3           |    829.9 ns |   1.33 ns |     0.70 ns |  0.06 |    0.00 | 0.0067 |      - |     120 B |        0.01 |
| &#39;CachedDataReader read (5 rows)&#39;      | Job-YFEFPZ | 10             | Default     | 3           |  8,024.7 ns |  38.62 ns |    25.55 ns |  0.58 |    0.01 | 0.1526 |      - |    2712 B |        0.32 |
| &#39;CachedDataReader read (1000 rows)&#39;   | Job-YFEFPZ | 10             | Default     | 3           | 33,849.1 ns | 197.60 ns |   130.70 ns |  2.46 |    0.04 | 0.4883 |      - |    8592 B |        1.00 |
|                                       |            |                |             |             |             |           |             |       |         |        |        |           |             |
| &#39;Key generation (simple query)&#39;       | MediumRun  | 15             | 2           | 10          | 14,077.1 ns | 354.08 ns |   519.00 ns |  1.00 |    0.05 | 0.4425 | 0.1526 |    7576 B |        1.00 |
| &#39;Key generation (complex JOIN query)&#39; | MediumRun  | 15             | 2           | 10          | 21,949.6 ns | 205.37 ns |   307.38 ns |  1.56 |    0.06 | 0.8545 | 0.2747 |   14568 B |        1.92 |
| &#39;Key generation with tenant&#39;          | MediumRun  | 15             | 2           | 10          | 27,174.2 ns | 847.34 ns | 1,187.85 ns |  1.93 |    0.11 | 0.9155 | 0.2441 |   15473 B |        2.04 |
| &#39;Cache hit (memory)&#39;                  | MediumRun  | 15             | 2           | 10          | 14,149.4 ns | 369.18 ns |   529.47 ns |  1.01 |    0.05 | 0.4578 | 0.1526 |    7720 B |        1.02 |
| &#39;Cache miss (memory)&#39;                 | MediumRun  | 15             | 2           | 10          |    828.4 ns |   3.04 ns |     4.26 ns |  0.06 |    0.00 | 0.0067 |      - |     120 B |        0.02 |
| &#39;CachedDataReader read (5 rows)&#39;      | MediumRun  | 15             | 2           | 10          |  8,094.8 ns |  31.27 ns |    42.80 ns |  0.58 |    0.02 | 0.1526 |      - |    2712 B |        0.36 |
| &#39;CachedDataReader read (1000 rows)&#39;   | MediumRun  | 15             | 2           | 10          | 33,867.2 ns | 148.26 ns |   202.94 ns |  2.41 |    0.09 | 0.4883 |      - |    8592 B |        1.13 |
