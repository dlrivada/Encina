```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.53GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun  : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4


```
| Method                                | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error       | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|-------------------------------------- |----------- |--------------- |------------ |------------ |------------:|------------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| &#39;Key generation (simple query)&#39;       | Job-YFEFPZ | 10             | Default     | 3           |  7,350.9 ns |   248.61 ns | 147.95 ns |  1.00 |    0.03 | 0.4501 | 0.1526 |    8088 B |        1.00 |
| &#39;Key generation (complex JOIN query)&#39; | Job-YFEFPZ | 10             | Default     | 3           | 11,756.1 ns |   495.16 ns | 294.66 ns |  1.60 |    0.05 | 0.8698 | 0.2899 |   16104 B |        1.99 |
| &#39;Key generation with tenant&#39;          | Job-YFEFPZ | 10             | Default     | 3           | 11,545.9 ns | 1,210.43 ns | 800.63 ns |  1.57 |    0.11 | 0.9155 | 0.2289 |   16496 B |        2.04 |
| &#39;Cache hit (memory)&#39;                  | Job-YFEFPZ | 10             | Default     | 3           |  7,331.7 ns |   385.22 ns | 229.24 ns |  1.00 |    0.04 | 0.4578 | 0.1526 |    8744 B |        1.08 |
| &#39;Cache miss (memory)&#39;                 | Job-YFEFPZ | 10             | Default     | 3           |    651.4 ns |    14.76 ns |   9.77 ns |  0.09 |    0.00 | 0.0067 |      - |     120 B |        0.01 |
| &#39;CachedDataReader read (5 rows)&#39;      | Job-YFEFPZ | 10             | Default     | 3           |  4,336.7 ns |    71.05 ns |  47.00 ns |  0.59 |    0.01 | 0.1602 |      - |    2712 B |        0.34 |
| &#39;CachedDataReader read (1000 rows)&#39;   | Job-YFEFPZ | 10             | Default     | 3           | 18,343.9 ns |   285.96 ns | 170.17 ns |  2.50 |    0.05 | 0.4883 |      - |    8592 B |        1.06 |
|                                       |            |                |             |             |             |             |           |       |         |        |        |           |             |
| &#39;Key generation (simple query)&#39;       | MediumRun  | 15             | 2           | 10          |  7,098.6 ns |   200.94 ns | 294.54 ns |  1.00 |    0.06 | 0.4425 | 0.1526 |    7576 B |        1.00 |
| &#39;Key generation (complex JOIN query)&#39; | MediumRun  | 15             | 2           | 10          | 12,039.7 ns |   372.05 ns | 521.56 ns |  1.70 |    0.10 | 0.8698 | 0.2899 |   14568 B |        1.92 |
| &#39;Key generation with tenant&#39;          | MediumRun  | 15             | 2           | 10          | 10,970.3 ns |   418.49 ns | 626.37 ns |  1.55 |    0.11 | 0.9155 | 0.2289 |   15472 B |        2.04 |
| &#39;Cache hit (memory)&#39;                  | MediumRun  | 15             | 2           | 10          |  7,189.1 ns |   163.70 ns | 239.95 ns |  1.01 |    0.05 | 0.4578 | 0.1526 |    7720 B |        1.02 |
| &#39;Cache miss (memory)&#39;                 | MediumRun  | 15             | 2           | 10          |    619.9 ns |     3.66 ns |   5.12 ns |  0.09 |    0.00 | 0.0067 |      - |     120 B |        0.02 |
| &#39;CachedDataReader read (5 rows)&#39;      | MediumRun  | 15             | 2           | 10          |  4,157.0 ns |    48.81 ns |  71.54 ns |  0.59 |    0.03 | 0.1602 |      - |    2712 B |        0.36 |
| &#39;CachedDataReader read (1000 rows)&#39;   | MediumRun  | 15             | 2           | 10          | 18,022.4 ns |   134.21 ns | 196.73 ns |  2.54 |    0.10 | 0.4883 |      - |    8592 B |        1.13 |
