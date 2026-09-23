```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                                | Job        | IterationCount | LaunchCount | Mean        | Error       | StdDev      | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|-------------------------------------- |----------- |--------------- |------------ |------------:|------------:|------------:|------:|--------:|-------:|-------:|----------:|------------:|
| &#39;Key generation (simple query)&#39;       | Job-YFEFPZ | 10             | Default     | 13,489.2 ns |   595.45 ns |   393.86 ns |  1.00 |    0.04 | 0.0763 | 0.0610 |    8600 B |        1.00 |
| &#39;Key generation (complex JOIN query)&#39; | Job-YFEFPZ | 10             | Default     | 22,138.1 ns | 1,737.68 ns | 1,149.37 ns |  1.64 |    0.09 | 0.1526 | 0.1221 |   16360 B |        1.90 |
| &#39;Key generation with tenant&#39;          | Job-YFEFPZ | 10             | Default     | 21,748.4 ns |   366.88 ns |   218.33 ns |  1.61 |    0.05 | 0.1831 | 0.1526 |   16496 B |        1.92 |
| &#39;Cache hit (memory)&#39;                  | Job-YFEFPZ | 10             | Default     | 14,850.2 ns |   568.21 ns |   375.84 ns |  1.10 |    0.04 | 0.0916 | 0.0763 |    8744 B |        1.02 |
| &#39;Cache miss (memory)&#39;                 | Job-YFEFPZ | 10             | Default     |    571.4 ns |    13.57 ns |     8.98 ns |  0.04 |    0.00 | 0.0010 |      - |     120 B |        0.01 |
| &#39;CachedDataReader read (5 rows)&#39;      | Job-YFEFPZ | 10             | Default     |  6,165.2 ns |   202.65 ns |   134.04 ns |  0.46 |    0.02 | 0.0305 |      - |    2712 B |        0.32 |
| &#39;CachedDataReader read (1000 rows)&#39;   | Job-YFEFPZ | 10             | Default     | 26,562.6 ns |   353.48 ns |   233.81 ns |  1.97 |    0.06 | 0.0916 |      - |    8592 B |        1.00 |
|                                       |            |                |             |             |             |             |       |         |        |        |           |             |
| &#39;Key generation (simple query)&#39;       | ShortRun   | 3              | 1           | 13,377.6 ns | 7,578.62 ns |   415.41 ns |  1.00 |    0.04 | 0.0763 | 0.0610 |    7576 B |        1.00 |
| &#39;Key generation (complex JOIN query)&#39; | ShortRun   | 3              | 1           | 23,218.3 ns | 3,716.08 ns |   203.69 ns |  1.74 |    0.05 | 0.1526 | 0.1221 |   14568 B |        1.92 |
| &#39;Key generation with tenant&#39;          | ShortRun   | 3              | 1           | 22,605.7 ns | 1,423.33 ns |    78.02 ns |  1.69 |    0.05 | 0.1831 | 0.1526 |   15472 B |        2.04 |
| &#39;Cache hit (memory)&#39;                  | ShortRun   | 3              | 1           | 14,783.5 ns | 2,608.74 ns |   142.99 ns |  1.11 |    0.03 | 0.0916 | 0.0763 |    7720 B |        1.02 |
| &#39;Cache miss (memory)&#39;                 | ShortRun   | 3              | 1           |    583.6 ns |    65.76 ns |     3.60 ns |  0.04 |    0.00 | 0.0010 |      - |     120 B |        0.02 |
| &#39;CachedDataReader read (5 rows)&#39;      | ShortRun   | 3              | 1           |  6,309.7 ns | 1,540.42 ns |    84.44 ns |  0.47 |    0.01 | 0.0305 |      - |    2712 B |        0.36 |
| &#39;CachedDataReader read (1000 rows)&#39;   | ShortRun   | 3              | 1           | 27,185.4 ns |   828.47 ns |    45.41 ns |  2.03 |    0.06 | 0.0916 |      - |    8592 B |        1.13 |
