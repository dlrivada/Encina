```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 3.39GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method             | Job        | IterationCount | LaunchCount | Mean     | Error     | StdDev   | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------- |----------- |--------------- |------------ |---------:|----------:|---------:|------:|--------:|----------:|------------:|
| Generate_GetValue  | Job-YFEFPZ | 10             | Default     | 697.9 ns |  68.03 ns | 45.00 ns |  1.05 |    0.06 |         - |          NA |
| NewGuid_Comparison | Job-YFEFPZ | 10             | Default     | 335.7 ns |  23.91 ns | 15.81 ns |  0.51 |    0.02 |         - |          NA |
| Generate           | Job-YFEFPZ | 10             | Default     | 663.1 ns |   4.31 ns |  2.57 ns |  1.00 |    0.01 |         - |          NA |
|                    |            |                |             |          |           |          |       |         |           |             |
| Generate_GetValue  | ShortRun   | 3              | 1           | 668.2 ns |  28.01 ns |  1.54 ns |  1.01 |    0.00 |         - |          NA |
| NewGuid_Comparison | ShortRun   | 3              | 1           | 362.0 ns | 239.69 ns | 13.14 ns |  0.55 |    0.02 |         - |          NA |
| Generate           | ShortRun   | 3              | 1           | 660.3 ns |  36.63 ns |  2.01 ns |  1.00 |    0.00 |         - |          NA |
