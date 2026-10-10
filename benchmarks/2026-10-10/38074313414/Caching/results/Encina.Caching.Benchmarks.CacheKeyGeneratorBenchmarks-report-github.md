```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method                       | Job        | IterationCount | LaunchCount | Mean       | Error       | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------------------- |----------- |--------------- |------------ |-----------:|------------:|----------:|------:|--------:|-------:|----------:|------------:|
| GenerateKey_WithTemplate     | Job-YFEFPZ | 10             | Default     | 2,443.9 ns |    48.28 ns |  31.93 ns |  0.68 |    0.01 | 0.0801 |    1360 B |        1.36 |
| GeneratePattern              | Job-YFEFPZ | 10             | Default     |   123.9 ns |     0.61 ns |   0.32 ns |  0.03 |    0.00 | 0.0153 |     256 B |        0.26 |
| GeneratePattern_WithTemplate | Job-YFEFPZ | 10             | Default     |   305.3 ns |     3.53 ns |   2.10 ns |  0.09 |    0.00 | 0.0534 |     896 B |        0.90 |
| GenerateKey_ComplexQuery     | Job-YFEFPZ | 10             | Default     | 4,643.6 ns |    23.17 ns |  13.79 ns |  1.30 |    0.00 | 0.0916 |    1648 B |        1.65 |
| GenerateKey_SimpleQuery      | Job-YFEFPZ | 10             | Default     | 3,584.5 ns |    10.02 ns |   6.63 ns |  1.00 |    0.00 | 0.0572 |    1000 B |        1.00 |
|                              |            |                |             |            |             |           |       |         |        |           |             |
| GenerateKey_WithTemplate     | ShortRun   | 3              | 1           | 2,408.6 ns |    21.84 ns |   1.20 ns |  0.67 |    0.00 | 0.0801 |    1360 B |        1.36 |
| GeneratePattern              | ShortRun   | 3              | 1           |   124.1 ns |     3.64 ns |   0.20 ns |  0.03 |    0.00 | 0.0153 |     256 B |        0.26 |
| GeneratePattern_WithTemplate | ShortRun   | 3              | 1           |   306.6 ns |   107.11 ns |   5.87 ns |  0.09 |    0.00 | 0.0534 |     896 B |        0.90 |
| GenerateKey_ComplexQuery     | ShortRun   | 3              | 1           | 4,597.2 ns | 1,934.28 ns | 106.02 ns |  1.28 |    0.03 | 0.0916 |    1648 B |        1.65 |
| GenerateKey_SimpleQuery      | ShortRun   | 3              | 1           | 3,591.3 ns |   405.30 ns |  22.22 ns |  1.00 |    0.01 | 0.0572 |    1000 B |        1.00 |
