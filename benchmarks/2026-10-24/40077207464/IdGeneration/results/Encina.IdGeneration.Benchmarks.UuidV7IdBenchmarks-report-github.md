```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method             | Job        | IterationCount | LaunchCount | Mean     | Error     | StdDev   | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------- |----------- |--------------- |------------ |---------:|----------:|---------:|------:|--------:|----------:|------------:|
| Generate_GetValue  | Job-YFEFPZ | 10             | Default     | 767.5 ns |  14.46 ns |  9.56 ns |  1.00 |    0.02 |         - |          NA |
| NewGuid_Comparison | Job-YFEFPZ | 10             | Default     | 367.3 ns |   5.89 ns |  3.90 ns |  0.48 |    0.01 |         - |          NA |
| Generate           | Job-YFEFPZ | 10             | Default     | 766.2 ns |  11.78 ns |  7.79 ns |  1.00 |    0.01 |         - |          NA |
|                    |            |                |             |          |           |          |       |         |           |             |
| Generate_GetValue  | ShortRun   | 3              | 1           | 747.3 ns | 210.65 ns | 11.55 ns |  0.98 |    0.01 |         - |          NA |
| NewGuid_Comparison | ShortRun   | 3              | 1           | 372.5 ns |  36.56 ns |  2.00 ns |  0.49 |    0.00 |         - |          NA |
| Generate           | ShortRun   | 3              | 1           | 764.8 ns |  64.84 ns |  3.55 ns |  1.00 |    0.01 |         - |          NA |
