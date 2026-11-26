```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 3.38GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                       | Job        | IterationCount | LaunchCount | Mean       | Error     | StdDev   | Ratio | Gen0   | Allocated | Alloc Ratio |
|----------------------------- |----------- |--------------- |------------ |-----------:|----------:|---------:|------:|-------:|----------:|------------:|
| GenerateKey_WithTemplate     | Job-YFEFPZ | 10             | Default     | 1,540.8 ns |  13.57 ns |  8.08 ns |  0.62 | 0.0153 |    1360 B |        1.36 |
| GeneratePattern              | Job-YFEFPZ | 10             | Default     |   109.2 ns |   1.18 ns |  0.78 ns |  0.04 | 0.0030 |     256 B |        0.26 |
| GeneratePattern_WithTemplate | Job-YFEFPZ | 10             | Default     |   257.4 ns |   9.21 ns |  6.09 ns |  0.10 | 0.0105 |     896 B |        0.90 |
| GenerateKey_ComplexQuery     | Job-YFEFPZ | 10             | Default     | 3,269.5 ns |  13.19 ns |  8.72 ns |  1.31 | 0.0191 |    1648 B |        1.65 |
| GenerateKey_SimpleQuery      | Job-YFEFPZ | 10             | Default     | 2,502.3 ns |   8.85 ns |  5.85 ns |  1.00 | 0.0114 |    1000 B |        1.00 |
|                              |            |                |             |            |           |          |       |        |           |             |
| GenerateKey_WithTemplate     | ShortRun   | 3              | 1           | 1,568.2 ns | 158.27 ns |  8.68 ns |  0.62 | 0.0153 |    1360 B |        1.36 |
| GeneratePattern              | ShortRun   | 3              | 1           |   123.0 ns |  41.68 ns |  2.28 ns |  0.05 | 0.0029 |     256 B |        0.26 |
| GeneratePattern_WithTemplate | ShortRun   | 3              | 1           |   293.3 ns |  22.41 ns |  1.23 ns |  0.12 | 0.0105 |     896 B |        0.90 |
| GenerateKey_ComplexQuery     | ShortRun   | 3              | 1           | 3,278.8 ns | 434.98 ns | 23.84 ns |  1.30 | 0.0191 |    1648 B |        1.65 |
| GenerateKey_SimpleQuery      | ShortRun   | 3              | 1           | 2,526.8 ns | 247.10 ns | 13.54 ns |  1.00 | 0.0114 |    1000 B |        1.00 |
