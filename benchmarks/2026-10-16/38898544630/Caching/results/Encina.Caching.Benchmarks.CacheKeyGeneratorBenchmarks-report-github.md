```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                       | Job        | IterationCount | LaunchCount | Mean        | Error      | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|----------------------------- |----------- |--------------- |------------ |------------:|-----------:|----------:|------:|-------:|----------:|------------:|
| GenerateKey_WithTemplate     | Job-YFEFPZ | 10             | Default     | 1,938.09 ns |   4.252 ns |  2.813 ns |  0.73 | 0.0801 |    1360 B |        1.36 |
| GeneratePattern              | Job-YFEFPZ | 10             | Default     |    97.19 ns |   1.055 ns |  0.698 ns |  0.04 | 0.0153 |     256 B |        0.26 |
| GeneratePattern_WithTemplate | Job-YFEFPZ | 10             | Default     |   230.85 ns |   3.528 ns |  2.099 ns |  0.09 | 0.0534 |     896 B |        0.90 |
| GenerateKey_ComplexQuery     | Job-YFEFPZ | 10             | Default     | 3,252.72 ns |   9.413 ns |  5.602 ns |  1.23 | 0.0954 |    1648 B |        1.65 |
| GenerateKey_SimpleQuery      | Job-YFEFPZ | 10             | Default     | 2,648.56 ns |   6.323 ns |  3.762 ns |  1.00 | 0.0572 |    1000 B |        1.00 |
|                              |            |                |             |             |            |           |       |        |           |             |
| GenerateKey_WithTemplate     | ShortRun   | 3              | 1           | 1,898.56 ns | 312.719 ns | 17.141 ns |  0.74 | 0.0801 |    1360 B |        1.36 |
| GeneratePattern              | ShortRun   | 3              | 1           |    96.50 ns |   8.921 ns |  0.489 ns |  0.04 | 0.0153 |     256 B |        0.26 |
| GeneratePattern_WithTemplate | ShortRun   | 3              | 1           |   240.81 ns |  70.967 ns |  3.890 ns |  0.09 | 0.0534 |     896 B |        0.90 |
| GenerateKey_ComplexQuery     | ShortRun   | 3              | 1           | 3,335.40 ns | 141.657 ns |  7.765 ns |  1.30 | 0.0954 |    1648 B |        1.65 |
| GenerateKey_SimpleQuery      | ShortRun   | 3              | 1           | 2,575.05 ns |  11.196 ns |  0.614 ns |  1.00 | 0.0572 |    1000 B |        1.00 |
