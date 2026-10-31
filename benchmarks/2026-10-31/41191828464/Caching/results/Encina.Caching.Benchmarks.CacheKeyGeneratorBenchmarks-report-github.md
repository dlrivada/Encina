```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 3.00GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                       | Job        | IterationCount | LaunchCount | Mean       | Error     | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------------------- |----------- |--------------- |------------ |-----------:|----------:|---------:|------:|--------:|-------:|----------:|------------:|
| GenerateKey_WithTemplate     | Job-YFEFPZ | 10             | Default     | 2,088.1 ns |  36.25 ns | 23.98 ns |  0.59 |    0.01 | 0.0153 |    1360 B |        1.36 |
| GeneratePattern              | Job-YFEFPZ | 10             | Default     |   153.9 ns |   0.65 ns |  0.43 ns |  0.04 |    0.00 | 0.0029 |     256 B |        0.26 |
| GeneratePattern_WithTemplate | Job-YFEFPZ | 10             | Default     |   376.1 ns |   4.26 ns |  2.54 ns |  0.11 |    0.00 | 0.0105 |     896 B |        0.90 |
| GenerateKey_ComplexQuery     | Job-YFEFPZ | 10             | Default     | 4,449.2 ns |  24.72 ns | 14.71 ns |  1.25 |    0.01 | 0.0153 |    1648 B |        1.65 |
| GenerateKey_SimpleQuery      | Job-YFEFPZ | 10             | Default     | 3,559.2 ns |  62.03 ns | 41.03 ns |  1.00 |    0.02 | 0.0114 |    1000 B |        1.00 |
|                              |            |                |             |            |           |          |       |         |        |           |             |
| GenerateKey_WithTemplate     | ShortRun   | 3              | 1           | 2,093.5 ns |  80.83 ns |  4.43 ns |  0.60 |    0.00 | 0.0153 |    1360 B |        1.36 |
| GeneratePattern              | ShortRun   | 3              | 1           |   155.8 ns |   5.09 ns |  0.28 ns |  0.04 |    0.00 | 0.0029 |     256 B |        0.26 |
| GeneratePattern_WithTemplate | ShortRun   | 3              | 1           |   377.1 ns |  14.31 ns |  0.78 ns |  0.11 |    0.00 | 0.0105 |     896 B |        0.90 |
| GenerateKey_ComplexQuery     | ShortRun   | 3              | 1           | 4,517.0 ns | 151.94 ns |  8.33 ns |  1.28 |    0.01 | 0.0153 |    1648 B |        1.65 |
| GenerateKey_SimpleQuery      | ShortRun   | 3              | 1           | 3,517.2 ns | 526.26 ns | 28.85 ns |  1.00 |    0.01 | 0.0114 |    1000 B |        1.00 |
