```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                       | Job        | IterationCount | LaunchCount | Mean        | Error        | StdDev     | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------------------- |----------- |--------------- |------------ |------------:|-------------:|-----------:|------:|--------:|-------:|----------:|------------:|
| GenerateKey_WithTemplate     | Job-YFEFPZ | 10             | Default     | 1,560.63 ns |   141.441 ns |  93.554 ns |  0.63 |    0.04 | 0.0153 |    1360 B |        1.36 |
| GeneratePattern              | Job-YFEFPZ | 10             | Default     |    91.87 ns |     1.184 ns |   0.705 ns |  0.04 |    0.00 | 0.0030 |     256 B |        0.26 |
| GeneratePattern_WithTemplate | Job-YFEFPZ | 10             | Default     |   229.16 ns |    10.196 ns |   6.067 ns |  0.09 |    0.00 | 0.0105 |     896 B |        0.90 |
| GenerateKey_ComplexQuery     | Job-YFEFPZ | 10             | Default     | 3,302.41 ns |   264.626 ns | 175.034 ns |  1.33 |    0.07 | 0.0191 |    1648 B |        1.65 |
| GenerateKey_SimpleQuery      | Job-YFEFPZ | 10             | Default     | 2,482.72 ns |    47.915 ns |  31.693 ns |  1.00 |    0.02 | 0.0114 |    1000 B |        1.00 |
|                              |            |                |             |             |              |            |       |         |        |           |             |
| GenerateKey_WithTemplate     | ShortRun   | 3              | 1           | 1,633.93 ns |   535.056 ns |  29.328 ns |  0.64 |    0.01 | 0.0153 |    1360 B |        1.36 |
| GeneratePattern              | ShortRun   | 3              | 1           |    95.19 ns |    81.311 ns |   4.457 ns |  0.04 |    0.00 | 0.0030 |     256 B |        0.26 |
| GeneratePattern_WithTemplate | ShortRun   | 3              | 1           |   232.95 ns |   161.414 ns |   8.848 ns |  0.09 |    0.00 | 0.0105 |     896 B |        0.90 |
| GenerateKey_ComplexQuery     | ShortRun   | 3              | 1           | 3,358.27 ns | 3,467.968 ns | 190.091 ns |  1.31 |    0.06 | 0.0191 |    1648 B |        1.65 |
| GenerateKey_SimpleQuery      | ShortRun   | 3              | 1           | 2,569.07 ns |   241.027 ns |  13.212 ns |  1.00 |    0.01 | 0.0114 |    1000 B |        1.00 |
