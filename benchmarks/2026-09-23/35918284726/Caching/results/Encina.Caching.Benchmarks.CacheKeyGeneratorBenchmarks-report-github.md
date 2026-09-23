```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                       | Job        | IterationCount | LaunchCount | Mean        | Error        | StdDev     | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------------------- |----------- |--------------- |------------ |------------:|-------------:|-----------:|------:|--------:|-------:|----------:|------------:|
| GenerateKey_WithTemplate     | Job-YFEFPZ | 10             | Default     | 1,358.11 ns |    46.171 ns |  27.476 ns |  0.76 |    0.02 | 0.0801 |    1360 B |        1.36 |
| GeneratePattern              | Job-YFEFPZ | 10             | Default     |    65.70 ns |     1.475 ns |   0.878 ns |  0.04 |    0.00 | 0.0153 |     256 B |        0.26 |
| GeneratePattern_WithTemplate | Job-YFEFPZ | 10             | Default     |   165.56 ns |     6.498 ns |   4.298 ns |  0.09 |    0.00 | 0.0534 |     896 B |        0.90 |
| GenerateKey_ComplexQuery     | Job-YFEFPZ | 10             | Default     | 2,159.48 ns |    39.771 ns |  23.667 ns |  1.21 |    0.02 | 0.0954 |    1648 B |        1.65 |
| GenerateKey_SimpleQuery      | Job-YFEFPZ | 10             | Default     | 1,782.33 ns |    47.314 ns |  28.156 ns |  1.00 |    0.02 | 0.0572 |    1000 B |        1.00 |
|                              |            |                |             |             |              |            |       |         |        |           |             |
| GenerateKey_WithTemplate     | ShortRun   | 3              | 1           | 1,335.32 ns |   176.244 ns |   9.661 ns |  0.74 |    0.01 | 0.0801 |    1360 B |        1.36 |
| GeneratePattern              | ShortRun   | 3              | 1           |    67.46 ns |    85.633 ns |   4.694 ns |  0.04 |    0.00 | 0.0153 |     256 B |        0.26 |
| GeneratePattern_WithTemplate | ShortRun   | 3              | 1           |   164.56 ns |    82.050 ns |   4.497 ns |  0.09 |    0.00 | 0.0534 |     896 B |        0.90 |
| GenerateKey_ComplexQuery     | ShortRun   | 3              | 1           | 2,298.44 ns | 2,374.960 ns | 130.180 ns |  1.28 |    0.07 | 0.0954 |    1648 B |        1.65 |
| GenerateKey_SimpleQuery      | ShortRun   | 3              | 1           | 1,802.19 ns |   683.492 ns |  37.464 ns |  1.00 |    0.03 | 0.0591 |    1000 B |        1.00 |
