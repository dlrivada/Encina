```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.79GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                                | Job        | IterationCount | LaunchCount | Mean        | Error        | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|-------------------------------------- |----------- |--------------- |------------ |------------:|-------------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| &#39;Key generation (simple query)&#39;       | Job-YFEFPZ | 10             | Default     | 14,468.0 ns |    423.72 ns | 280.26 ns |  1.00 |    0.03 | 0.2899 | 0.1373 |    8600 B |        1.00 |
| &#39;Key generation (complex JOIN query)&#39; | Job-YFEFPZ | 10             | Default     | 23,174.0 ns |    652.59 ns | 431.65 ns |  1.60 |    0.04 | 0.5798 | 0.2747 |   16360 B |        1.90 |
| &#39;Key generation with tenant&#39;          | Job-YFEFPZ | 10             | Default     | 28,093.8 ns |    571.81 ns | 378.22 ns |  1.94 |    0.04 | 0.6104 | 0.2136 |   16496 B |        1.92 |
| &#39;Cache hit (memory)&#39;                  | Job-YFEFPZ | 10             | Default     | 14,695.3 ns |    345.61 ns | 228.60 ns |  1.02 |    0.02 | 0.3052 | 0.1526 |    8744 B |        1.02 |
| &#39;Cache miss (memory)&#39;                 | Job-YFEFPZ | 10             | Default     |    704.8 ns |      3.03 ns |   2.01 ns |  0.05 |    0.00 | 0.0048 |      - |     120 B |        0.01 |
| &#39;CachedDataReader read (5 rows)&#39;      | Job-YFEFPZ | 10             | Default     |  7,238.2 ns |     21.95 ns |  14.52 ns |  0.50 |    0.01 | 0.1068 |      - |    2712 B |        0.32 |
| &#39;CachedDataReader read (1000 rows)&#39;   | Job-YFEFPZ | 10             | Default     | 33,087.4 ns |     99.87 ns |  59.43 ns |  2.29 |    0.04 | 0.3052 |      - |    8592 B |        1.00 |
|                                       |            |                |             |             |              |           |       |         |        |        |           |             |
| &#39;Key generation (simple query)&#39;       | ShortRun   | 3              | 1           | 13,991.3 ns |  5,097.55 ns | 279.41 ns |  1.00 |    0.02 | 0.2899 | 0.1373 |    7576 B |        1.00 |
| &#39;Key generation (complex JOIN query)&#39; | ShortRun   | 3              | 1           | 23,607.5 ns | 10,688.12 ns | 585.85 ns |  1.69 |    0.05 | 0.5798 | 0.2747 |   14568 B |        1.92 |
| &#39;Key generation with tenant&#39;          | ShortRun   | 3              | 1           | 28,361.6 ns |  8,062.54 ns | 441.94 ns |  2.03 |    0.04 | 0.6104 | 0.2136 |   15472 B |        2.04 |
| &#39;Cache hit (memory)&#39;                  | ShortRun   | 3              | 1           | 15,300.4 ns |  1,106.37 ns |  60.64 ns |  1.09 |    0.02 | 0.3052 | 0.1526 |    7720 B |        1.02 |
| &#39;Cache miss (memory)&#39;                 | ShortRun   | 3              | 1           |    707.0 ns |     75.76 ns |   4.15 ns |  0.05 |    0.00 | 0.0048 |      - |     120 B |        0.02 |
| &#39;CachedDataReader read (5 rows)&#39;      | ShortRun   | 3              | 1           |  7,487.1 ns |    249.61 ns |  13.68 ns |  0.54 |    0.01 | 0.1068 |      - |    2712 B |        0.36 |
| &#39;CachedDataReader read (1000 rows)&#39;   | ShortRun   | 3              | 1           | 30,646.7 ns |  1,918.80 ns | 105.18 ns |  2.19 |    0.04 | 0.3052 |      - |    8592 B |        1.13 |
