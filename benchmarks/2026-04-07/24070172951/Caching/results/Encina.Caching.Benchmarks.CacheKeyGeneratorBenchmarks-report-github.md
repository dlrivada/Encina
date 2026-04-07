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
| GenerateKey_SimpleQuery      | Job-YFEFPZ | 10             | Default     | 3,677.4 ns |  28.79 ns | 17.14 ns |  1.00 | 0.0572 |    1000 B |        1.00 |
| GenerateKey_ComplexQuery     | Job-YFEFPZ | 10             | Default     | 4,523.5 ns |  32.17 ns | 19.15 ns |  1.23 | 0.0916 |    1648 B |        1.65 |
| GenerateKey_WithTemplate     | Job-YFEFPZ | 10             | Default     | 2,414.5 ns |  24.29 ns | 16.07 ns |  0.66 | 0.0801 |    1360 B |        1.36 |
| GeneratePattern              | Job-YFEFPZ | 10             | Default     |   130.2 ns |   4.41 ns |  2.91 ns |  0.04 | 0.0153 |     256 B |        0.26 |
| GeneratePattern_WithTemplate | Job-YFEFPZ | 10             | Default     |   316.0 ns |   5.46 ns |  3.61 ns |  0.09 | 0.0534 |     896 B |        0.90 |
|                              |            |                |             |            |           |          |       |        |           |             |
| GenerateKey_SimpleQuery      | ShortRun   | 3              | 1           | 3,693.3 ns | 230.64 ns | 12.64 ns |  1.00 | 0.0572 |    1000 B |        1.00 |
| GenerateKey_ComplexQuery     | ShortRun   | 3              | 1           | 4,499.1 ns | 176.30 ns |  9.66 ns |  1.22 | 0.0916 |    1648 B |        1.65 |
| GenerateKey_WithTemplate     | ShortRun   | 3              | 1           | 2,417.8 ns | 153.69 ns |  8.42 ns |  0.65 | 0.0801 |    1360 B |        1.36 |
| GeneratePattern              | ShortRun   | 3              | 1           |   126.8 ns |  47.71 ns |  2.62 ns |  0.03 | 0.0153 |     256 B |        0.26 |
| GeneratePattern_WithTemplate | ShortRun   | 3              | 1           |   332.7 ns | 102.27 ns |  5.61 ns |  0.09 | 0.0534 |     896 B |        0.90 |
