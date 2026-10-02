```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                       | Job        | IterationCount | LaunchCount | Mean       | Error       | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------------------- |----------- |--------------- |------------ |-----------:|------------:|---------:|------:|--------:|-------:|----------:|------------:|
| GenerateKey_WithTemplate     | Job-YFEFPZ | 10             | Default     | 1,729.1 ns |    32.23 ns | 19.18 ns |  0.56 |    0.01 | 0.0153 |    1360 B |        1.36 |
| GeneratePattern              | Job-YFEFPZ | 10             | Default     |   113.2 ns |     1.39 ns |  0.92 ns |  0.04 |    0.00 | 0.0029 |     256 B |        0.26 |
| GeneratePattern_WithTemplate | Job-YFEFPZ | 10             | Default     |   279.9 ns |    13.85 ns |  9.16 ns |  0.09 |    0.00 | 0.0105 |     896 B |        0.90 |
| GenerateKey_ComplexQuery     | Job-YFEFPZ | 10             | Default     | 3,764.7 ns |    93.94 ns | 62.14 ns |  1.23 |    0.02 | 0.0191 |    1648 B |        1.65 |
| GenerateKey_SimpleQuery      | Job-YFEFPZ | 10             | Default     | 3,069.9 ns |    60.34 ns | 39.91 ns |  1.00 |    0.02 | 0.0114 |    1000 B |        1.00 |
|                              |            |                |             |            |             |          |       |         |        |           |             |
| GenerateKey_WithTemplate     | ShortRun   | 3              | 1           | 1,893.3 ns |   102.24 ns |  5.60 ns |  0.62 |    0.01 | 0.0153 |    1360 B |        1.36 |
| GeneratePattern              | ShortRun   | 3              | 1           |   113.3 ns |    50.36 ns |  2.76 ns |  0.04 |    0.00 | 0.0030 |     256 B |        0.26 |
| GeneratePattern_WithTemplate | ShortRun   | 3              | 1           |   302.5 ns |   127.75 ns |  7.00 ns |  0.10 |    0.00 | 0.0105 |     896 B |        0.90 |
| GenerateKey_ComplexQuery     | ShortRun   | 3              | 1           | 3,706.3 ns |   164.39 ns |  9.01 ns |  1.22 |    0.02 | 0.0191 |    1648 B |        1.65 |
| GenerateKey_SimpleQuery      | ShortRun   | 3              | 1           | 3,045.1 ns | 1,130.51 ns | 61.97 ns |  1.00 |    0.02 | 0.0114 |    1000 B |        1.00 |
