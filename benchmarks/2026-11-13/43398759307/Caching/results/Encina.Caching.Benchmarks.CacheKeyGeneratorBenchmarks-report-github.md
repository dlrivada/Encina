```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.59GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method                       | Job        | IterationCount | LaunchCount | Mean       | Error       | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------------------- |----------- |--------------- |------------ |-----------:|------------:|---------:|------:|--------:|-------:|----------:|------------:|
| GenerateKey_WithTemplate     | Job-YFEFPZ | 10             | Default     | 2,529.4 ns |     3.60 ns |  2.38 ns |  0.66 |    0.00 | 0.0801 |    1360 B |        1.36 |
| GeneratePattern              | Job-YFEFPZ | 10             | Default     |   147.9 ns |     2.94 ns |  1.95 ns |  0.04 |    0.00 | 0.0153 |     256 B |        0.26 |
| GeneratePattern_WithTemplate | Job-YFEFPZ | 10             | Default     |   364.6 ns |    12.21 ns |  8.08 ns |  0.10 |    0.00 | 0.0534 |     896 B |        0.90 |
| GenerateKey_ComplexQuery     | Job-YFEFPZ | 10             | Default     | 4,668.5 ns |    15.52 ns | 10.27 ns |  1.22 |    0.00 | 0.0916 |    1648 B |        1.65 |
| GenerateKey_SimpleQuery      | Job-YFEFPZ | 10             | Default     | 3,827.9 ns |     7.48 ns |  4.95 ns |  1.00 |    0.00 | 0.0534 |    1000 B |        1.00 |
|                              |            |                |             |            |             |          |       |         |        |           |             |
| GenerateKey_WithTemplate     | ShortRun   | 3              | 1           | 2,581.5 ns |    59.95 ns |  3.29 ns |  0.71 |    0.01 | 0.0801 |    1360 B |        1.36 |
| GeneratePattern              | ShortRun   | 3              | 1           |   151.8 ns |    40.14 ns |  2.20 ns |  0.04 |    0.00 | 0.0153 |     256 B |        0.26 |
| GeneratePattern_WithTemplate | ShortRun   | 3              | 1           |   363.4 ns |     3.09 ns |  0.17 ns |  0.10 |    0.00 | 0.0534 |     896 B |        0.90 |
| GenerateKey_ComplexQuery     | ShortRun   | 3              | 1           | 4,759.2 ns |   289.64 ns | 15.88 ns |  1.30 |    0.02 | 0.0916 |    1648 B |        1.65 |
| GenerateKey_SimpleQuery      | ShortRun   | 3              | 1           | 3,654.7 ns | 1,003.83 ns | 55.02 ns |  1.00 |    0.02 | 0.0572 |    1000 B |        1.00 |
