```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method                                | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error     | StdDev    | Median      | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|-------------------------------------- |----------- |--------------- |------------ |------------ |------------:|----------:|----------:|------------:|------:|--------:|-------:|-------:|----------:|------------:|
| &#39;Key generation (simple query)&#39;       | Job-YFEFPZ | 10             | Default     | 3           | 12,612.3 ns | 211.83 ns | 140.11 ns | 12,590.9 ns |  1.00 |    0.01 | 0.4425 | 0.1526 |    8600 B |        1.00 |
| &#39;Key generation (complex JOIN query)&#39; | Job-YFEFPZ | 10             | Default     | 3           | 20,987.8 ns | 279.67 ns | 166.43 ns | 20,958.3 ns |  1.66 |    0.02 | 0.8545 | 0.2747 |   16360 B |        1.90 |
| &#39;Key generation with tenant&#39;          | Job-YFEFPZ | 10             | Default     | 3           | 25,869.1 ns | 166.87 ns | 110.37 ns | 25,847.5 ns |  2.05 |    0.02 | 0.9155 | 0.2441 |   16497 B |        1.92 |
| &#39;Cache hit (memory)&#39;                  | Job-YFEFPZ | 10             | Default     | 3           | 13,262.1 ns | 237.47 ns | 124.20 ns | 13,283.7 ns |  1.05 |    0.01 | 0.4578 | 0.1526 |    8744 B |        1.02 |
| &#39;Cache miss (memory)&#39;                 | Job-YFEFPZ | 10             | Default     | 3           |    839.2 ns |   1.60 ns |   0.95 ns |    839.2 ns |  0.07 |    0.00 | 0.0067 |      - |     120 B |        0.01 |
| &#39;CachedDataReader read (5 rows)&#39;      | Job-YFEFPZ | 10             | Default     | 3           |  8,030.3 ns |  41.19 ns |  24.51 ns |  8,025.2 ns |  0.64 |    0.01 | 0.1526 |      - |    2712 B |        0.32 |
| &#39;CachedDataReader read (1000 rows)&#39;   | Job-YFEFPZ | 10             | Default     | 3           | 33,806.9 ns | 287.56 ns | 171.12 ns | 33,791.4 ns |  2.68 |    0.03 | 0.4883 |      - |    8592 B |        1.00 |
|                                       |            |                |             |             |             |           |           |             |       |         |        |        |           |             |
| &#39;Key generation (simple query)&#39;       | MediumRun  | 15             | 2           | 10          | 12,695.8 ns |  73.01 ns | 102.35 ns | 12,680.9 ns |  1.00 |    0.01 | 0.4425 | 0.1526 |    7576 B |        1.00 |
| &#39;Key generation (complex JOIN query)&#39; | MediumRun  | 15             | 2           | 10          | 21,199.4 ns | 171.92 ns | 252.00 ns | 21,133.2 ns |  1.67 |    0.02 | 0.8545 | 0.2747 |   14568 B |        1.92 |
| &#39;Key generation with tenant&#39;          | MediumRun  | 15             | 2           | 10          | 27,178.4 ns | 148.78 ns | 213.37 ns | 27,160.8 ns |  2.14 |    0.02 | 0.9155 | 0.2441 |   15473 B |        2.04 |
| &#39;Cache hit (memory)&#39;                  | MediumRun  | 15             | 2           | 10          | 13,236.7 ns |  76.99 ns | 112.85 ns | 13,217.5 ns |  1.04 |    0.01 | 0.4578 | 0.1526 |    7720 B |        1.02 |
| &#39;Cache miss (memory)&#39;                 | MediumRun  | 15             | 2           | 10          |    842.2 ns |   0.85 ns |   1.22 ns |    842.0 ns |  0.07 |    0.00 | 0.0067 |      - |     120 B |        0.02 |
| &#39;CachedDataReader read (5 rows)&#39;      | MediumRun  | 15             | 2           | 10          |  7,932.7 ns |  53.19 ns |  76.28 ns |  7,942.4 ns |  0.62 |    0.01 | 0.1526 |      - |    2712 B |        0.36 |
| &#39;CachedDataReader read (1000 rows)&#39;   | MediumRun  | 15             | 2           | 10          | 34,155.9 ns | 311.71 ns | 436.97 ns | 34,422.9 ns |  2.69 |    0.04 | 0.4883 |      - |    8592 B |        1.13 |
