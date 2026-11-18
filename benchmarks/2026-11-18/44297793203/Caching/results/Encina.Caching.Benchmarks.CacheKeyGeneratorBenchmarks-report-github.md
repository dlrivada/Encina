```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.22GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method                       | Job        | IterationCount | LaunchCount | Mean       | Error       | StdDev   | Ratio | Gen0   | Allocated | Alloc Ratio |
|----------------------------- |----------- |--------------- |------------ |-----------:|------------:|---------:|------:|-------:|----------:|------------:|
| GenerateKey_WithTemplate     | Job-YFEFPZ | 10             | Default     | 2,516.4 ns |    23.56 ns | 15.58 ns |  0.67 | 0.0801 |    1360 B |        1.36 |
| GeneratePattern              | Job-YFEFPZ | 10             | Default     |   152.2 ns |     2.88 ns |  1.91 ns |  0.04 | 0.0153 |     256 B |        0.26 |
| GeneratePattern_WithTemplate | Job-YFEFPZ | 10             | Default     |   345.2 ns |     7.18 ns |  4.75 ns |  0.09 | 0.0534 |     896 B |        0.90 |
| GenerateKey_ComplexQuery     | Job-YFEFPZ | 10             | Default     | 4,890.8 ns |    42.52 ns | 28.12 ns |  1.30 | 0.0916 |    1648 B |        1.65 |
| GenerateKey_SimpleQuery      | Job-YFEFPZ | 10             | Default     | 3,757.2 ns |    28.37 ns | 16.88 ns |  1.00 | 0.0572 |    1000 B |        1.00 |
|                              |            |                |             |            |             |          |       |        |           |             |
| GenerateKey_WithTemplate     | ShortRun   | 3              | 1           | 2,585.0 ns | 1,120.00 ns | 61.39 ns |  0.68 | 0.0801 |    1360 B |        1.36 |
| GeneratePattern              | ShortRun   | 3              | 1           |   147.5 ns |    83.56 ns |  4.58 ns |  0.04 | 0.0153 |     256 B |        0.26 |
| GeneratePattern_WithTemplate | ShortRun   | 3              | 1           |   345.8 ns |    86.04 ns |  4.72 ns |  0.09 | 0.0534 |     896 B |        0.90 |
| GenerateKey_ComplexQuery     | ShortRun   | 3              | 1           | 4,856.9 ns | 1,029.50 ns | 56.43 ns |  1.28 | 0.0916 |    1648 B |        1.65 |
| GenerateKey_SimpleQuery      | ShortRun   | 3              | 1           | 3,793.4 ns |   103.46 ns |  5.67 ns |  1.00 | 0.0572 |    1000 B |        1.00 |
