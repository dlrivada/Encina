```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method                                | Job        | IterationCount | LaunchCount | Mean        | Error        | StdDev      | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|-------------------------------------- |----------- |--------------- |------------ |------------:|-------------:|------------:|------:|--------:|-------:|-------:|----------:|------------:|
| &#39;Key generation (simple query)&#39;       | Job-YFEFPZ | 10             | Default     | 13,482.6 ns |    389.88 ns |   257.88 ns |  1.00 |    0.03 | 0.4425 | 0.1526 |    8600 B |        1.00 |
| &#39;Key generation (complex JOIN query)&#39; | Job-YFEFPZ | 10             | Default     | 23,176.2 ns |  1,759.62 ns | 1,163.88 ns |  1.72 |    0.09 | 0.8545 | 0.2747 |   16360 B |        1.90 |
| &#39;Key generation with tenant&#39;          | Job-YFEFPZ | 10             | Default     | 25,772.1 ns |    429.47 ns |   284.07 ns |  1.91 |    0.04 | 0.9155 | 0.2441 |   16497 B |        1.92 |
| &#39;Cache hit (memory)&#39;                  | Job-YFEFPZ | 10             | Default     | 13,655.1 ns |    291.04 ns |   173.19 ns |  1.01 |    0.02 | 0.4578 | 0.1526 |    8744 B |        1.02 |
| &#39;Cache miss (memory)&#39;                 | Job-YFEFPZ | 10             | Default     |    835.4 ns |      1.25 ns |     0.65 ns |  0.06 |    0.00 | 0.0067 |      - |     120 B |        0.01 |
| &#39;CachedDataReader read (5 rows)&#39;      | Job-YFEFPZ | 10             | Default     |  8,025.4 ns |     19.44 ns |    12.86 ns |  0.60 |    0.01 | 0.1526 |      - |    2712 B |        0.32 |
| &#39;CachedDataReader read (1000 rows)&#39;   | Job-YFEFPZ | 10             | Default     | 33,747.9 ns |    101.68 ns |    53.18 ns |  2.50 |    0.05 | 0.4883 |      - |    8592 B |        1.00 |
|                                       |            |                |             |             |              |             |       |         |        |        |           |             |
| &#39;Key generation (simple query)&#39;       | ShortRun   | 3              | 1           | 13,047.2 ns |  1,176.88 ns |    64.51 ns |  1.00 |    0.01 | 0.4425 | 0.1526 |    7576 B |        1.00 |
| &#39;Key generation (complex JOIN query)&#39; | ShortRun   | 3              | 1           | 22,111.0 ns | 11,085.38 ns |   607.63 ns |  1.69 |    0.04 | 0.8545 | 0.2747 |   14568 B |        1.92 |
| &#39;Key generation with tenant&#39;          | ShortRun   | 3              | 1           | 25,976.5 ns |  3,244.63 ns |   177.85 ns |  1.99 |    0.01 | 0.9155 | 0.2441 |   15473 B |        2.04 |
| &#39;Cache hit (memory)&#39;                  | ShortRun   | 3              | 1           | 13,677.5 ns |  4,099.90 ns |   224.73 ns |  1.05 |    0.02 | 0.4578 | 0.1526 |    7720 B |        1.02 |
| &#39;Cache miss (memory)&#39;                 | ShortRun   | 3              | 1           |    830.5 ns |     33.12 ns |     1.82 ns |  0.06 |    0.00 | 0.0067 |      - |     120 B |        0.02 |
| &#39;CachedDataReader read (5 rows)&#39;      | ShortRun   | 3              | 1           |  8,156.6 ns |    455.12 ns |    24.95 ns |  0.63 |    0.00 | 0.1526 |      - |    2712 B |        0.36 |
| &#39;CachedDataReader read (1000 rows)&#39;   | ShortRun   | 3              | 1           | 35,869.4 ns |  2,900.86 ns |   159.01 ns |  2.75 |    0.02 | 0.4883 |      - |    8592 B |        1.13 |
