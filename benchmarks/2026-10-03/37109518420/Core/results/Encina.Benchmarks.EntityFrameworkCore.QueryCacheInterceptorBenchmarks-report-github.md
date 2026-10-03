```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.19GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                                | Job        | IterationCount | LaunchCount | Mean        | Error        | StdDev      | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|-------------------------------------- |----------- |--------------- |------------ |------------:|-------------:|------------:|------:|--------:|-------:|-------:|----------:|------------:|
| &#39;Key generation (simple query)&#39;       | Job-YFEFPZ | 10             | Default     |  6,973.7 ns |    219.43 ns |   130.58 ns |  1.00 |    0.03 | 0.4501 | 0.1526 |    8088 B |        1.00 |
| &#39;Key generation (complex JOIN query)&#39; | Job-YFEFPZ | 10             | Default     | 14,506.0 ns |  2,164.80 ns | 1,431.88 ns |  2.08 |    0.20 | 0.8698 | 0.2899 |   16104 B |        1.99 |
| &#39;Key generation with tenant&#39;          | Job-YFEFPZ | 10             | Default     | 12,559.6 ns |    906.19 ns |   599.39 ns |  1.80 |    0.09 | 0.9155 | 0.2289 |   16496 B |        2.04 |
| &#39;Cache hit (memory)&#39;                  | Job-YFEFPZ | 10             | Default     |  9,439.3 ns |    166.60 ns |    99.14 ns |  1.35 |    0.03 | 0.4578 | 0.1526 |    8744 B |        1.08 |
| &#39;Cache miss (memory)&#39;                 | Job-YFEFPZ | 10             | Default     |    576.8 ns |     20.84 ns |    10.90 ns |  0.08 |    0.00 | 0.0067 |      - |     120 B |        0.01 |
| &#39;CachedDataReader read (5 rows)&#39;      | Job-YFEFPZ | 10             | Default     |  4,640.1 ns |     12.30 ns |     8.14 ns |  0.67 |    0.01 | 0.1602 |      - |    2712 B |        0.34 |
| &#39;CachedDataReader read (1000 rows)&#39;   | Job-YFEFPZ | 10             | Default     | 20,406.5 ns |    192.65 ns |   100.76 ns |  2.93 |    0.05 | 0.4883 |      - |    8592 B |        1.06 |
|                                       |            |                |             |             |              |             |       |         |        |        |           |             |
| &#39;Key generation (simple query)&#39;       | ShortRun   | 3              | 1           |  9,654.7 ns |    860.88 ns |    47.19 ns |  1.00 |    0.01 | 0.4425 | 0.1526 |    7576 B |        1.00 |
| &#39;Key generation (complex JOIN query)&#39; | ShortRun   | 3              | 1           | 16,892.9 ns |  7,327.21 ns |   401.63 ns |  1.75 |    0.04 | 0.8545 | 0.2747 |   14568 B |        1.92 |
| &#39;Key generation with tenant&#39;          | ShortRun   | 3              | 1           | 13,449.4 ns |    426.36 ns |    23.37 ns |  1.39 |    0.01 | 0.9155 | 0.2289 |   15472 B |        2.04 |
| &#39;Cache hit (memory)&#39;                  | ShortRun   | 3              | 1           | 10,502.8 ns |  3,179.94 ns |   174.30 ns |  1.09 |    0.02 | 0.4578 | 0.1526 |    7720 B |        1.02 |
| &#39;Cache miss (memory)&#39;                 | ShortRun   | 3              | 1           |    591.4 ns |    110.83 ns |     6.07 ns |  0.06 |    0.00 | 0.0067 |      - |     120 B |        0.02 |
| &#39;CachedDataReader read (5 rows)&#39;      | ShortRun   | 3              | 1           |  4,979.5 ns |  9,997.55 ns |   548.00 ns |  0.52 |    0.05 | 0.1602 |      - |    2712 B |        0.36 |
| &#39;CachedDataReader read (1000 rows)&#39;   | ShortRun   | 3              | 1           | 20,915.6 ns | 19,839.94 ns | 1,087.49 ns |  2.17 |    0.10 | 0.4883 |      - |    8592 B |        1.13 |
