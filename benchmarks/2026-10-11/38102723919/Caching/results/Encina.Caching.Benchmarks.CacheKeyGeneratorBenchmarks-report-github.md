```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  MediumRun  : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4


```
| Method                       | Job        | IterationCount | LaunchCount | WarmupCount | Mean       | Error    | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------------------- |----------- |--------------- |------------ |------------ |-----------:|---------:|---------:|------:|--------:|-------:|----------:|------------:|
| GenerateKey_WithTemplate     | Job-YFEFPZ | 10             | Default     | 3           | 1,944.5 ns | 13.08 ns |  8.65 ns |  0.55 |    0.01 | 0.0153 |    1360 B |        1.36 |
| GeneratePattern              | Job-YFEFPZ | 10             | Default     | 3           |   136.4 ns |  0.32 ns |  0.19 ns |  0.04 |    0.00 | 0.0029 |     256 B |        0.26 |
| GeneratePattern_WithTemplate | Job-YFEFPZ | 10             | Default     | 3           |   345.8 ns |  1.53 ns |  0.80 ns |  0.10 |    0.00 | 0.0105 |     896 B |        0.90 |
| GenerateKey_ComplexQuery     | Job-YFEFPZ | 10             | Default     | 3           | 4,491.8 ns | 38.21 ns | 22.74 ns |  1.28 |    0.02 | 0.0153 |    1648 B |        1.65 |
| GenerateKey_SimpleQuery      | Job-YFEFPZ | 10             | Default     | 3           | 3,516.8 ns | 69.82 ns | 46.18 ns |  1.00 |    0.02 | 0.0114 |    1000 B |        1.00 |
|                              |            |                |             |             |            |          |          |       |         |        |           |             |
| GenerateKey_WithTemplate     | MediumRun  | 15             | 2           | 10          | 1,941.2 ns |  4.51 ns |  6.75 ns |  0.55 |    0.01 | 0.0153 |    1360 B |        1.36 |
| GeneratePattern              | MediumRun  | 15             | 2           | 10          |   137.0 ns |  0.50 ns |  0.73 ns |  0.04 |    0.00 | 0.0029 |     256 B |        0.26 |
| GeneratePattern_WithTemplate | MediumRun  | 15             | 2           | 10          |   345.1 ns |  1.60 ns |  2.30 ns |  0.10 |    0.00 | 0.0105 |     896 B |        0.90 |
| GenerateKey_ComplexQuery     | MediumRun  | 15             | 2           | 10          | 4,305.7 ns | 25.86 ns | 37.91 ns |  1.23 |    0.02 | 0.0153 |    1648 B |        1.65 |
| GenerateKey_SimpleQuery      | MediumRun  | 15             | 2           | 10          | 3,508.9 ns | 24.92 ns | 35.75 ns |  1.00 |    0.01 | 0.0114 |    1000 B |        1.00 |
