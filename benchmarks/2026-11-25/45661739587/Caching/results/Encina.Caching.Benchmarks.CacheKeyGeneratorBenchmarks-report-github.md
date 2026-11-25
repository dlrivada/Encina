```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 2.70GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                       | Job        | IterationCount | LaunchCount | Mean       | Error     | StdDev  | Ratio | Gen0   | Allocated | Alloc Ratio |
|----------------------------- |----------- |--------------- |------------ |-----------:|----------:|--------:|------:|-------:|----------:|------------:|
| GenerateKey_WithTemplate     | Job-YFEFPZ | 10             | Default     | 1,602.1 ns |   8.87 ns | 5.87 ns |  0.64 | 0.0153 |    1360 B |        1.36 |
| GeneratePattern              | Job-YFEFPZ | 10             | Default     |   119.8 ns |   0.83 ns | 0.55 ns |  0.05 | 0.0029 |     256 B |        0.26 |
| GeneratePattern_WithTemplate | Job-YFEFPZ | 10             | Default     |   281.3 ns |   3.35 ns | 2.21 ns |  0.11 | 0.0105 |     896 B |        0.90 |
| GenerateKey_ComplexQuery     | Job-YFEFPZ | 10             | Default     | 3,232.4 ns |   9.54 ns | 5.68 ns |  1.29 | 0.0191 |    1648 B |        1.65 |
| GenerateKey_SimpleQuery      | Job-YFEFPZ | 10             | Default     | 2,514.5 ns |  11.92 ns | 7.88 ns |  1.00 | 0.0114 |    1000 B |        1.00 |
|                              |            |                |             |            |           |         |       |        |           |             |
| GenerateKey_WithTemplate     | ShortRun   | 3              | 1           | 1,567.0 ns |   7.11 ns | 0.39 ns |  0.63 | 0.0153 |    1360 B |        1.36 |
| GeneratePattern              | ShortRun   | 3              | 1           |   119.8 ns |   3.50 ns | 0.19 ns |  0.05 | 0.0029 |     256 B |        0.26 |
| GeneratePattern_WithTemplate | ShortRun   | 3              | 1           |   286.7 ns |   8.31 ns | 0.46 ns |  0.11 | 0.0105 |     896 B |        0.90 |
| GenerateKey_ComplexQuery     | ShortRun   | 3              | 1           | 3,384.2 ns |  46.17 ns | 2.53 ns |  1.35 | 0.0191 |    1648 B |        1.65 |
| GenerateKey_SimpleQuery      | ShortRun   | 3              | 1           | 2,504.8 ns | 152.95 ns | 8.38 ns |  1.00 | 0.0114 |    1000 B |        1.00 |
