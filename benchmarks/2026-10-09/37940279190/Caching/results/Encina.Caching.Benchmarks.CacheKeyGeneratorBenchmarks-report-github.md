```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                       | Job        | IterationCount | LaunchCount | Mean        | Error      | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------------------- |----------- |--------------- |------------ |------------:|-----------:|----------:|------:|--------:|-------:|----------:|------------:|
| GenerateKey_WithTemplate     | Job-YFEFPZ | 10             | Default     | 1,273.12 ns |  25.298 ns | 16.733 ns |  0.74 |    0.02 | 0.0801 |    1360 B |        1.36 |
| GeneratePattern              | Job-YFEFPZ | 10             | Default     |    61.45 ns |   2.461 ns |  1.628 ns |  0.04 |    0.00 | 0.0153 |     256 B |        0.26 |
| GeneratePattern_WithTemplate | Job-YFEFPZ | 10             | Default     |   157.44 ns |   2.398 ns |  1.427 ns |  0.09 |    0.00 | 0.0534 |     896 B |        0.90 |
| GenerateKey_ComplexQuery     | Job-YFEFPZ | 10             | Default     | 2,139.10 ns |  45.979 ns | 30.412 ns |  1.24 |    0.03 | 0.0954 |    1648 B |        1.65 |
| GenerateKey_SimpleQuery      | Job-YFEFPZ | 10             | Default     | 1,726.75 ns |  60.778 ns | 40.201 ns |  1.00 |    0.03 | 0.0591 |    1000 B |        1.00 |
|                              |            |                |             |             |            |           |       |         |        |           |             |
| GenerateKey_WithTemplate     | ShortRun   | 3              | 1           | 1,263.39 ns | 434.809 ns | 23.833 ns |  0.73 |    0.02 | 0.0801 |    1360 B |        1.36 |
| GeneratePattern              | ShortRun   | 3              | 1           |    61.62 ns |  15.843 ns |  0.868 ns |  0.04 |    0.00 | 0.0153 |     256 B |        0.26 |
| GeneratePattern_WithTemplate | ShortRun   | 3              | 1           |   154.11 ns |  23.024 ns |  1.262 ns |  0.09 |    0.00 | 0.0534 |     896 B |        0.90 |
| GenerateKey_ComplexQuery     | ShortRun   | 3              | 1           | 2,112.11 ns | 167.741 ns |  9.194 ns |  1.22 |    0.02 | 0.0954 |    1648 B |        1.65 |
| GenerateKey_SimpleQuery      | ShortRun   | 3              | 1           | 1,736.81 ns | 675.259 ns | 37.013 ns |  1.00 |    0.03 | 0.0591 |    1000 B |        1.00 |
