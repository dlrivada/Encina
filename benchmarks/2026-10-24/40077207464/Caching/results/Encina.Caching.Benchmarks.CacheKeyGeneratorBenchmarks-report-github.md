```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method                       | Job        | IterationCount | LaunchCount | Mean       | Error     | StdDev   | Ratio | Gen0   | Allocated | Alloc Ratio |
|----------------------------- |----------- |--------------- |------------ |-----------:|----------:|---------:|------:|-------:|----------:|------------:|
| GenerateKey_WithTemplate     | Job-YFEFPZ | 10             | Default     | 2,533.7 ns |   8.97 ns |  5.34 ns |  0.68 | 0.0801 |    1360 B |        1.36 |
| GeneratePattern              | Job-YFEFPZ | 10             | Default     |   154.3 ns |   0.82 ns |  0.54 ns |  0.04 | 0.0153 |     256 B |        0.26 |
| GeneratePattern_WithTemplate | Job-YFEFPZ | 10             | Default     |   374.2 ns |   2.76 ns |  1.83 ns |  0.10 | 0.0534 |     896 B |        0.90 |
| GenerateKey_ComplexQuery     | Job-YFEFPZ | 10             | Default     | 4,650.0 ns |  11.21 ns |  5.86 ns |  1.26 | 0.0916 |    1648 B |        1.65 |
| GenerateKey_SimpleQuery      | Job-YFEFPZ | 10             | Default     | 3,704.4 ns |  12.42 ns |  7.39 ns |  1.00 | 0.0572 |    1000 B |        1.00 |
|                              |            |                |             |            |           |          |       |        |           |             |
| GenerateKey_WithTemplate     | ShortRun   | 3              | 1           | 2,485.8 ns |  45.32 ns |  2.48 ns |  0.67 | 0.0801 |    1360 B |        1.36 |
| GeneratePattern              | ShortRun   | 3              | 1           |   152.9 ns |   4.24 ns |  0.23 ns |  0.04 | 0.0153 |     256 B |        0.26 |
| GeneratePattern_WithTemplate | ShortRun   | 3              | 1           |   367.9 ns |  17.19 ns |  0.94 ns |  0.10 | 0.0534 |     896 B |        0.90 |
| GenerateKey_ComplexQuery     | ShortRun   | 3              | 1           | 4,826.2 ns | 190.80 ns | 10.46 ns |  1.30 | 0.0916 |    1648 B |        1.65 |
| GenerateKey_SimpleQuery      | ShortRun   | 3              | 1           | 3,707.0 ns | 102.84 ns |  5.64 ns |  1.00 | 0.0572 |    1000 B |        1.00 |
