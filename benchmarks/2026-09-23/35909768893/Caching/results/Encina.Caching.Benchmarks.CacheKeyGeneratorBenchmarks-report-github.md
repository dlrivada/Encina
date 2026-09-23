```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.75GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                       | Job        | IterationCount | LaunchCount | Mean       | Error     | StdDev   | Ratio | Gen0   | Allocated | Alloc Ratio |
|----------------------------- |----------- |--------------- |------------ |-----------:|----------:|---------:|------:|-------:|----------:|------------:|
| GenerateKey_WithTemplate     | Job-YFEFPZ | 10             | Default     | 1,966.8 ns |  22.70 ns | 15.01 ns |  0.57 | 0.0153 |    1360 B |        1.36 |
| GeneratePattern              | Job-YFEFPZ | 10             | Default     |   134.1 ns |   0.80 ns |  0.48 ns |  0.04 | 0.0029 |     256 B |        0.26 |
| GeneratePattern_WithTemplate | Job-YFEFPZ | 10             | Default     |   330.9 ns |   3.07 ns |  1.82 ns |  0.10 | 0.0105 |     896 B |        0.90 |
| GenerateKey_ComplexQuery     | Job-YFEFPZ | 10             | Default     | 4,199.7 ns |  20.65 ns | 12.29 ns |  1.21 | 0.0153 |    1648 B |        1.65 |
| GenerateKey_SimpleQuery      | Job-YFEFPZ | 10             | Default     | 3,467.1 ns |  57.02 ns | 33.93 ns |  1.00 | 0.0114 |    1000 B |        1.00 |
|                              |            |                |             |            |           |          |       |        |           |             |
| GenerateKey_WithTemplate     | ShortRun   | 3              | 1           | 2,003.3 ns | 262.37 ns | 14.38 ns |  0.58 | 0.0153 |    1360 B |        1.36 |
| GeneratePattern              | ShortRun   | 3              | 1           |   131.8 ns |  19.92 ns |  1.09 ns |  0.04 | 0.0029 |     256 B |        0.26 |
| GeneratePattern_WithTemplate | ShortRun   | 3              | 1           |   329.0 ns | 132.65 ns |  7.27 ns |  0.09 | 0.0105 |     896 B |        0.90 |
| GenerateKey_ComplexQuery     | ShortRun   | 3              | 1           | 4,495.0 ns | 369.52 ns | 20.25 ns |  1.30 | 0.0153 |    1648 B |        1.65 |
| GenerateKey_SimpleQuery      | ShortRun   | 3              | 1           | 3,469.7 ns | 138.53 ns |  7.59 ns |  1.00 | 0.0114 |    1000 B |        1.00 |
