```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.53GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                       | Job        | IterationCount | LaunchCount | Mean        | Error        | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------------------- |----------- |--------------- |------------ |------------:|-------------:|----------:|------:|--------:|-------:|----------:|------------:|
| GenerateKey_WithTemplate     | Job-YFEFPZ | 10             | Default     | 1,362.89 ns |    23.076 ns | 15.263 ns |  0.74 |    0.01 | 0.0801 |    1360 B |        1.36 |
| GeneratePattern              | Job-YFEFPZ | 10             | Default     |    72.73 ns |     0.933 ns |  0.488 ns |  0.04 |    0.00 | 0.0153 |     256 B |        0.26 |
| GeneratePattern_WithTemplate | Job-YFEFPZ | 10             | Default     |   172.85 ns |     4.282 ns |  2.832 ns |  0.09 |    0.00 | 0.0534 |     896 B |        0.90 |
| GenerateKey_ComplexQuery     | Job-YFEFPZ | 10             | Default     | 2,199.00 ns |    54.746 ns | 32.579 ns |  1.19 |    0.03 | 0.0954 |    1648 B |        1.65 |
| GenerateKey_SimpleQuery      | Job-YFEFPZ | 10             | Default     | 1,848.92 ns |    50.607 ns | 33.474 ns |  1.00 |    0.02 | 0.0591 |    1000 B |        1.00 |
|                              |            |                |             |             |              |           |       |         |        |           |             |
| GenerateKey_WithTemplate     | ShortRun   | 3              | 1           | 1,344.80 ns |   134.990 ns |  7.399 ns |  0.74 |    0.01 | 0.0801 |    1360 B |        1.36 |
| GeneratePattern              | ShortRun   | 3              | 1           |    74.85 ns |     3.460 ns |  0.190 ns |  0.04 |    0.00 | 0.0153 |     256 B |        0.26 |
| GeneratePattern_WithTemplate | ShortRun   | 3              | 1           |   169.70 ns |    15.099 ns |  0.828 ns |  0.09 |    0.00 | 0.0534 |     896 B |        0.90 |
| GenerateKey_ComplexQuery     | ShortRun   | 3              | 1           | 2,289.77 ns | 1,047.563 ns | 57.420 ns |  1.26 |    0.03 | 0.0954 |    1648 B |        1.65 |
| GenerateKey_SimpleQuery      | ShortRun   | 3              | 1           | 1,810.73 ns |   542.113 ns | 29.715 ns |  1.00 |    0.02 | 0.0591 |    1000 B |        1.00 |
