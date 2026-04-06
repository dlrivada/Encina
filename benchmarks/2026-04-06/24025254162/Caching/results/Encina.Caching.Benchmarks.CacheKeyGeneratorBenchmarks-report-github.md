```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method                       | Job        | IterationCount | LaunchCount | Mean       | Error     | StdDev   | Ratio | Gen0   | Allocated | Alloc Ratio |
|----------------------------- |----------- |--------------- |------------ |-----------:|----------:|---------:|------:|-------:|----------:|------------:|
| GenerateKey_SimpleQuery      | Job-YFEFPZ | 10             | Default     | 3,544.2 ns |  10.40 ns |  6.88 ns |  1.00 | 0.0572 |    1000 B |        1.00 |
| GenerateKey_ComplexQuery     | Job-YFEFPZ | 10             | Default     | 4,604.1 ns |  11.90 ns |  7.08 ns |  1.30 | 0.0916 |    1648 B |        1.65 |
| GenerateKey_WithTemplate     | Job-YFEFPZ | 10             | Default     | 2,403.5 ns |  10.51 ns |  6.95 ns |  0.68 | 0.0801 |    1360 B |        1.36 |
| GeneratePattern              | Job-YFEFPZ | 10             | Default     |   130.4 ns |   0.48 ns |  0.32 ns |  0.04 | 0.0153 |     256 B |        0.26 |
| GeneratePattern_WithTemplate | Job-YFEFPZ | 10             | Default     |   314.6 ns |   2.36 ns |  1.23 ns |  0.09 | 0.0534 |     896 B |        0.90 |
|                              |            |                |             |            |           |          |       |        |           |             |
| GenerateKey_SimpleQuery      | ShortRun   | 3              | 1           | 3,697.8 ns |  92.49 ns |  5.07 ns |  1.00 | 0.0572 |    1000 B |        1.00 |
| GenerateKey_ComplexQuery     | ShortRun   | 3              | 1           | 4,680.5 ns | 152.80 ns |  8.38 ns |  1.27 | 0.0916 |    1648 B |        1.65 |
| GenerateKey_WithTemplate     | ShortRun   | 3              | 1           | 2,403.8 ns | 215.07 ns | 11.79 ns |  0.65 | 0.0801 |    1360 B |        1.36 |
| GeneratePattern              | ShortRun   | 3              | 1           |   119.6 ns |  26.64 ns |  1.46 ns |  0.03 | 0.0153 |     256 B |        0.26 |
| GeneratePattern_WithTemplate | ShortRun   | 3              | 1           |   337.0 ns |  87.09 ns |  4.77 ns |  0.09 | 0.0534 |     896 B |        0.90 |
