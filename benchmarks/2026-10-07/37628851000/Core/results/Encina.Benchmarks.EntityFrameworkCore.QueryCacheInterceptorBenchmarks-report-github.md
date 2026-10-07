```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                                | Job        | IterationCount | LaunchCount | Mean        | Error       | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|-------------------------------------- |----------- |--------------- |------------ |------------:|------------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| &#39;Key generation (simple query)&#39;       | Job-YFEFPZ | 10             | Default     | 14,786.7 ns |   396.52 ns | 262.27 ns |  1.00 |    0.02 | 0.0763 | 0.0610 |    8600 B |        1.00 |
| &#39;Key generation (complex JOIN query)&#39; | Job-YFEFPZ | 10             | Default     | 25,069.4 ns |   911.47 ns | 602.88 ns |  1.70 |    0.05 | 0.1526 | 0.1221 |   16360 B |        1.90 |
| &#39;Key generation with tenant&#39;          | Job-YFEFPZ | 10             | Default     | 24,303.3 ns |   378.63 ns | 225.32 ns |  1.64 |    0.03 | 0.1831 | 0.1526 |   16496 B |        1.92 |
| &#39;Cache hit (memory)&#39;                  | Job-YFEFPZ | 10             | Default     | 15,678.2 ns |   133.24 ns |  88.13 ns |  1.06 |    0.02 | 0.0916 | 0.0610 |    8744 B |        1.02 |
| &#39;Cache miss (memory)&#39;                 | Job-YFEFPZ | 10             | Default     |    623.7 ns |     2.48 ns |   1.64 ns |  0.04 |    0.00 | 0.0010 |      - |     120 B |        0.01 |
| &#39;CachedDataReader read (5 rows)&#39;      | Job-YFEFPZ | 10             | Default     |  6,829.9 ns |    23.89 ns |  14.22 ns |  0.46 |    0.01 | 0.0305 |      - |    2712 B |        0.32 |
| &#39;CachedDataReader read (1000 rows)&#39;   | Job-YFEFPZ | 10             | Default     | 29,285.9 ns |    46.69 ns |  27.79 ns |  1.98 |    0.03 | 0.0916 |      - |    8592 B |        1.00 |
|                                       |            |                |             |             |             |           |       |         |        |        |           |             |
| &#39;Key generation (simple query)&#39;       | ShortRun   | 3              | 1           | 14,608.2 ns | 6,766.66 ns | 370.90 ns |  1.00 |    0.03 | 0.0763 | 0.0610 |    7576 B |        1.00 |
| &#39;Key generation (complex JOIN query)&#39; | ShortRun   | 3              | 1           | 25,280.9 ns | 2,719.08 ns | 149.04 ns |  1.73 |    0.04 | 0.1526 | 0.1221 |   14568 B |        1.92 |
| &#39;Key generation with tenant&#39;          | ShortRun   | 3              | 1           | 23,771.1 ns | 3,411.81 ns | 187.01 ns |  1.63 |    0.04 | 0.1831 | 0.1526 |   15472 B |        2.04 |
| &#39;Cache hit (memory)&#39;                  | ShortRun   | 3              | 1           | 15,742.2 ns | 2,597.83 ns | 142.40 ns |  1.08 |    0.03 | 0.0916 | 0.0610 |    7720 B |        1.02 |
| &#39;Cache miss (memory)&#39;                 | ShortRun   | 3              | 1           |    640.3 ns |     0.94 ns |   0.05 ns |  0.04 |    0.00 | 0.0010 |      - |     120 B |        0.02 |
| &#39;CachedDataReader read (5 rows)&#39;      | ShortRun   | 3              | 1           |  6,774.1 ns |   199.81 ns |  10.95 ns |  0.46 |    0.01 | 0.0305 |      - |    2712 B |        0.36 |
| &#39;CachedDataReader read (1000 rows)&#39;   | ShortRun   | 3              | 1           | 29,417.6 ns | 1,796.80 ns |  98.49 ns |  2.01 |    0.05 | 0.0916 |      - |    8592 B |        1.13 |
