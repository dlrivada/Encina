```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                       | Job        | IterationCount | LaunchCount | Mean        | Error        | StdDev     | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------------------- |----------- |--------------- |------------ |------------:|-------------:|-----------:|------:|--------:|-------:|----------:|------------:|
| GenerateKey_WithTemplate     | Job-YFEFPZ | 10             | Default     | 1,437.07 ns |    14.405 ns |   9.528 ns |  0.75 |    0.01 | 0.0801 |    1360 B |        1.36 |
| GeneratePattern              | Job-YFEFPZ | 10             | Default     |    78.27 ns |     1.419 ns |   0.845 ns |  0.04 |    0.00 | 0.0153 |     256 B |        0.26 |
| GeneratePattern_WithTemplate | Job-YFEFPZ | 10             | Default     |   186.45 ns |     4.034 ns |   2.668 ns |  0.10 |    0.00 | 0.0534 |     896 B |        0.90 |
| GenerateKey_ComplexQuery     | Job-YFEFPZ | 10             | Default     | 2,306.24 ns |    49.471 ns |  29.440 ns |  1.20 |    0.02 | 0.0954 |    1648 B |        1.65 |
| GenerateKey_SimpleQuery      | Job-YFEFPZ | 10             | Default     | 1,917.75 ns |    22.402 ns |  14.817 ns |  1.00 |    0.01 | 0.0572 |    1000 B |        1.00 |
|                              |            |                |             |             |              |            |       |         |        |           |             |
| GenerateKey_WithTemplate     | ShortRun   | 3              | 1           | 1,432.50 ns |   122.751 ns |   6.728 ns |  0.76 |    0.01 | 0.0801 |    1360 B |        1.36 |
| GeneratePattern              | ShortRun   | 3              | 1           |    82.00 ns |    20.408 ns |   1.119 ns |  0.04 |    0.00 | 0.0153 |     256 B |        0.26 |
| GeneratePattern_WithTemplate | ShortRun   | 3              | 1           |   185.97 ns |    13.935 ns |   0.764 ns |  0.10 |    0.00 | 0.0534 |     896 B |        0.90 |
| GenerateKey_ComplexQuery     | ShortRun   | 3              | 1           | 2,371.48 ns | 2,355.965 ns | 129.138 ns |  1.26 |    0.06 | 0.0954 |    1648 B |        1.65 |
| GenerateKey_SimpleQuery      | ShortRun   | 3              | 1           | 1,885.85 ns |   270.926 ns |  14.850 ns |  1.00 |    0.01 | 0.0591 |    1000 B |        1.00 |
