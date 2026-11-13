```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.32GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                       | Job        | IterationCount | LaunchCount | Mean        | Error      | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|----------------------------- |----------- |--------------- |------------ |------------:|-----------:|----------:|------:|-------:|----------:|------------:|
| GenerateKey_WithTemplate     | Job-YFEFPZ | 10             | Default     | 1,286.90 ns |   9.612 ns |  5.720 ns |  0.69 | 0.0801 |    1360 B |        1.36 |
| GeneratePattern              | Job-YFEFPZ | 10             | Default     |    68.85 ns |   0.450 ns |  0.297 ns |  0.04 | 0.0153 |     256 B |        0.26 |
| GeneratePattern_WithTemplate | Job-YFEFPZ | 10             | Default     |   160.45 ns |   2.612 ns |  1.728 ns |  0.09 | 0.0534 |     896 B |        0.90 |
| GenerateKey_ComplexQuery     | Job-YFEFPZ | 10             | Default     | 2,300.99 ns |  33.296 ns | 19.814 ns |  1.23 | 0.0954 |    1648 B |        1.65 |
| GenerateKey_SimpleQuery      | Job-YFEFPZ | 10             | Default     | 1,865.83 ns |  10.151 ns |  6.714 ns |  1.00 | 0.0572 |    1000 B |        1.00 |
|                              |            |                |             |             |            |           |       |        |           |             |
| GenerateKey_WithTemplate     | ShortRun   | 3              | 1           | 1,287.60 ns | 102.177 ns |  5.601 ns |  0.71 | 0.0801 |    1360 B |        1.36 |
| GeneratePattern              | ShortRun   | 3              | 1           |    68.94 ns |   8.349 ns |  0.458 ns |  0.04 | 0.0153 |     256 B |        0.26 |
| GeneratePattern_WithTemplate | ShortRun   | 3              | 1           |   159.13 ns |  16.830 ns |  0.923 ns |  0.09 | 0.0534 |     896 B |        0.90 |
| GenerateKey_ComplexQuery     | ShortRun   | 3              | 1           | 2,339.47 ns | 124.233 ns |  6.810 ns |  1.30 | 0.0954 |    1648 B |        1.65 |
| GenerateKey_SimpleQuery      | ShortRun   | 3              | 1           | 1,806.16 ns | 188.040 ns | 10.307 ns |  1.00 | 0.0591 |    1000 B |        1.00 |
