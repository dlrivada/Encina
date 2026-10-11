```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3


```
| Method                                | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|-------------------------------------- |----------- |--------------- |------------ |------------ |------------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| &#39;Key generation (simple query)&#39;       | Job-YFEFPZ | 10             | Default     | 3           | 12,871.3 ns | 144.90 ns |  86.23 ns |  1.00 |    0.01 | 0.4425 | 0.1526 |    8600 B |        1.00 |
| &#39;Key generation (complex JOIN query)&#39; | Job-YFEFPZ | 10             | Default     | 3           | 21,818.1 ns | 229.47 ns | 136.56 ns |  1.70 |    0.01 | 0.8545 | 0.2747 |   16360 B |        1.90 |
| &#39;Key generation with tenant&#39;          | Job-YFEFPZ | 10             | Default     | 3           | 26,269.0 ns | 262.16 ns | 156.00 ns |  2.04 |    0.02 | 0.9155 | 0.2441 |   16497 B |        1.92 |
| &#39;Cache hit (memory)&#39;                  | Job-YFEFPZ | 10             | Default     | 3           | 13,411.6 ns | 137.87 ns |  91.19 ns |  1.04 |    0.01 | 0.4578 | 0.1526 |    8744 B |        1.02 |
| &#39;Cache miss (memory)&#39;                 | Job-YFEFPZ | 10             | Default     | 3           |    831.2 ns |   1.08 ns |   0.64 ns |  0.06 |    0.00 | 0.0067 |      - |     120 B |        0.01 |
| &#39;CachedDataReader read (5 rows)&#39;      | Job-YFEFPZ | 10             | Default     | 3           |  7,960.6 ns |  46.34 ns |  27.57 ns |  0.62 |    0.00 | 0.1526 |      - |    2712 B |        0.32 |
| &#39;CachedDataReader read (1000 rows)&#39;   | Job-YFEFPZ | 10             | Default     | 3           | 33,449.0 ns |  78.69 ns |  46.83 ns |  2.60 |    0.02 | 0.4883 |      - |    8592 B |        1.00 |
|                                       |            |                |             |             |             |           |           |       |         |        |        |           |             |
| &#39;Key generation (simple query)&#39;       | MediumRun  | 15             | 2           | 10          | 12,766.1 ns | 194.43 ns | 291.01 ns |  1.00 |    0.03 | 0.4425 | 0.1526 |    7576 B |        1.00 |
| &#39;Key generation (complex JOIN query)&#39; | MediumRun  | 15             | 2           | 10          | 21,096.5 ns | 305.52 ns | 457.29 ns |  1.65 |    0.05 | 0.8545 | 0.2747 |   14568 B |        1.92 |
| &#39;Key generation with tenant&#39;          | MediumRun  | 15             | 2           | 10          | 25,155.1 ns | 143.00 ns | 200.47 ns |  1.97 |    0.05 | 0.9155 | 0.2441 |   15473 B |        2.04 |
| &#39;Cache hit (memory)&#39;                  | MediumRun  | 15             | 2           | 10          | 12,885.9 ns |  42.85 ns |  60.07 ns |  1.01 |    0.02 | 0.4578 | 0.1526 |    7720 B |        1.02 |
| &#39;Cache miss (memory)&#39;                 | MediumRun  | 15             | 2           | 10          |    830.5 ns |   1.75 ns |   2.56 ns |  0.07 |    0.00 | 0.0067 |      - |     120 B |        0.02 |
| &#39;CachedDataReader read (5 rows)&#39;      | MediumRun  | 15             | 2           | 10          |  7,989.2 ns |  31.27 ns |  44.85 ns |  0.63 |    0.01 | 0.1526 |      - |    2712 B |        0.36 |
| &#39;CachedDataReader read (1000 rows)&#39;   | MediumRun  | 15             | 2           | 10          | 33,648.7 ns | 107.36 ns | 160.70 ns |  2.64 |    0.06 | 0.4883 |      - |    8592 B |        1.13 |
